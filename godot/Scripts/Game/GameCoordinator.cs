using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.App;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Games;
using BipIsland.Parent;
using BipIsland.Screens;
using Godot;

namespace BipIsland.Game;

/// <summary>
/// Owns the game state and moves between screens: the Godot version of the Swift app's
/// GameCoordinator. An autoload, so every screen reaches it as <see cref="Instance"/>.
/// </summary>
public partial class GameCoordinator : Node
{
    public static GameCoordinator Instance { get; private set; } = null!;

    public VoicePlayer Voice { get; } = new() { Name = "Voice" };
    public BipSounds Sounds { get; } = new() { Name = "Sounds" };
    public IRandomSource Rng { get; set; } = SystemRandomSource.Shared;

    /// <summary>Null only if the bundled content couldn't be read (CI checks it, so this shouldn't happen).</summary>
    public ContentLibrary? Content { get; private set; }
    /// <summary>Each island opens on its own, so one bad game file can't close the whole map.</summary>
    public bool LettersOpen { get; private set; }
    public bool NumbersOpen { get; private set; }
    public bool WordsOpen { get; private set; }
    public bool CodingOpen { get; private set; }
    public bool ArtOpen { get; private set; }
    public PhonicsCourse? Course { get; private set; }
    public SoundHuntGame? Hunt { get; private set; }

    public SaveStore Store { get; private set; } = null!;
    /// <summary>The grown-ups' layer: the parent gate, the parent area and the "Update ready" button.</summary>
    public ParentLayer Parent { get; private set; } = null!;
    public List<ChildSummary> Children { get; private set; } = [];
    public Guid ChildId { get; private set; }
    /// <summary>The child who is playing now.</summary>
    public ChildProgress Progress { get; private set; } = new();
    /// <summary>The playing child's break (each child has their own; switching away from a resting child needs a grown-up).</summary>
    private BreakState _break = new();
    /// <summary>How often the play clock is checked while the game runs (it pauses behind the parent gate).</summary>
    private const double BreakTickSeconds = 30;
    private DateTimeOffset _lastBreakCheck = DateTimeOffset.Now;
    private bool _breakClockPaused;
    private FeedMonsterGame? _monster;
    private bool _hasWelcomed;

    private CanvasLayer _fadeLayer = null!;
    private ColorRect _fade = null!;

    public override void _EnterTree() => Instance = this;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        AddChild(Voice);
        AddChild(Sounds);
        _fadeLayer = new CanvasLayer { Layer = 100 };
        _fade = new ColorRect { Color = Palette.Paper, MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0) };
        _fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _fadeLayer.AddChild(_fade);
        AddChild(_fadeLayer);

        LoadContent();
        OpenSaves(SaveFolder());
        Parent = new ParentLayer { Name = "Parent" };
        AddChild(Parent);
        // A steady tick keeps the play clock accurate during long games, so any much longer gap
        // between checks means the computer slept or the game froze (PlayBreaks.Credit).
        var tick = new Timer { WaitTime = BreakTickSeconds, Autostart = true, ProcessMode = ProcessModeEnum.Pausable };
        tick.Timeout += () => CurrentBreakPhase();
        AddChild(tick);
        if (DisplayServer.GetName() != "headless") BigCursor.Install();
    }

    /// <summary>Where saves live: the per-user folder, or a test folder from "--bip-save-dir".</summary>
    private static string SaveFolder()
    {
        var args = Boot.UserArgs;
        var i = Array.IndexOf(args, "--bip-save-dir");
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : ProjectSettings.GlobalizePath("user://");
    }

    private void LoadContent()
    {
        try
        {
            Content = new ContentLibrary(file =>
            {
                var path = "res://assets/content/" + file;
                if (!FileAccess.FileExists(path)) throw new System.IO.FileNotFoundException("not in the game package", path);
                return FileAccess.GetFileAsString(path);
            });
        }
        catch (ContentLoadException error)
        {
            GD.PrintErr($"Bip Island: game content didn't load, so the islands stay asleep: {error.Message}");
            return;
        }
        LettersOpen = Try("Letters", () =>
        {
            Course = new PhonicsCourse(Content);
            Hunt = new SoundHuntGame(Content, Course);
            _ = new MeetTheSoundGame(Content, Course);
            _ = new BubblePopGame(Content, Course);
            _ = new LetterTraceGame(Content, Course);
            _monster = new FeedMonsterGame(Content, Course);
        });
        NumbersOpen = Try("Numbers", () => { _ = new CountTapGame(Content); _ = new QuickLookGame(Content); _ = new BlockTowersGame(Content); });
        WordsOpen = Try("Words", () => { _ = new SoundButtonsGame(Content, new PhonicsCourse(Content)); _ = new WordBuilderGame(Content); });
        CodingOpen = Try("Coding", () => { _ = new MorningOrderGame(Content); _ = new BipsPathGame(Content); });
        ArtOpen = Try("Art", () => { _ = new ShapeBuilderGame(Content); _ = new PaintPotsGame(Content); _ = new MirrorMagicGame(Content); });
    }

    private static bool Try(string island, Action build)
    {
        try
        {
            build();
            return true;
        }
        catch (Exception error)
        {
            GD.PrintErr($"Bip Island: {island} Island's games didn't load and stay asleep: {error.Message}");
            return false;
        }
    }

    /// <summary>Opens (or reopens) the save file in <paramref name="folder"/>.</summary>
    public void OpenSaves(string folder)
    {
        Store = new SaveStore(folder);
        if (Store.LoadError is { } loadError) GD.PrintErr($"Bip Island: {loadError}");
        Children = Store.Children();
        ChildId = Store.LastChildId();
        Progress = Store.Progress(ChildId);
        LoadBreak();
        _nobodyYet = true;
    }

    /// <summary>Each child has their own play clock and break: load the current child's.</summary>
    private void LoadBreak()
    {
        _break = Store.LoadBreak(ChildId) ?? new BreakState { DayStamp = Today };
        _lastBreakCheck = DateTimeOffset.Now;
    }

    /// <summary>With more than one child, the game opens on "Who's playing?".</summary>
    public void Start()
    {
        if (Children.Count > 1) ShowProfiles();
        else ShowMap();
    }

    // Children

    public ChildSummary? CurrentChild => Children.FirstOrDefault(c => c.Id == ChildId);

    /// <summary>
    /// Whether picking <paramref name="id"/> needs a grown-up first: the child at the computer is
    /// resting, and picking a child who can play would skip their break (see <see cref="PlayBreaks.SwitchNeedsGrownUp"/>).
    /// A resting brother's or sister's picture never gets here: tapping it only shows them charging
    /// (<see cref="ShowResting"/>), so the child at the computer is always <see cref="ChildId"/>.
    /// </summary>
    public bool SwitchNeedsGrownUp(Guid id)
    {
        var resting = CurrentBreakPhase() != BreakPhase.Playing; // Banks play time first.
        return PlayBreaks.SwitchNeedsGrownUp(!_nobodyYet && resting, id == ChildId, IsResting(id));
    }

    /// <summary>
    /// Straight after the game opens nobody is at the computer yet (owner, 4 October 2026): a brother
    /// or sister with play time left can start without a grown-up, and the child who was resting stays
    /// on their break. Cleared once the playing child can play.
    /// </summary>
    private bool _nobodyYet = true;

    /// <summary>Whether a child is resting (charging or done for the day), for their picture in "Who's playing?".</summary>
    public bool IsResting(Guid id) =>
        PlayBreaks.IsResting(BreakOf(id), DateTimeOffset.Now, Today, Store.Settings.ToBreakSettings());

    /// <summary>A child's break: the playing child's live one, or another child's as last saved.</summary>
    private BreakState? BreakOf(Guid id) => id == ChildId ? _break : Store.LoadBreak(id);

    /// <summary>
    /// A resting brother's or sister's picture was tapped: show them charging, without them becoming
    /// the playing child (nothing is banked to them, the grown-ups' buttons stay with the child at the
    /// computer, and when their break ends "Who's playing?" opens again).
    /// </summary>
    public void ShowResting(Guid id) => Present(new ChargingScreen(id));

    /// <summary>The child picks their picture: their progress and break load, and the map opens.</summary>
    public void Choose(Guid id)
    {
        if (Children.All(c => c.Id != id)) return;
        CurrentBreakPhase(); // Bank play time to the child who was playing.
        // Just opened: picking a resting child (their own picture) doesn't make anyone at the computer yet.
        if (_nobodyYet) _nobodyYet = IsResting(id);
        ChildId = id;
        Progress = Store.Progress(id);
        LoadBreak();
        Store.SetLastChild(id);
        _hasWelcomed = false;
        ShowMap();
    }

    /// <summary>A parent adds a child (at most four). Returns false when they couldn't be added.</summary>
    public bool AddChildProfile(string name, int? age, string? avatar)
    {
        var added = Store.AddChild(name, age, avatar) != null;
        Children = Store.Children();
        return added;
    }

    /// <summary>A parent changes a child's name, age or picture.</summary>
    public void UpdateChild(ChildSummary child)
    {
        Store.Update(child);
        Children = Store.Children();
    }

    /// <summary>Removes a child and their progress. If they were playing, the first child takes over.</summary>
    public void DeleteChild(Guid id)
    {
        CurrentBreakPhase(); // Bank play time to the child who was playing, as Choose does.
        Store.Delete(id);
        Children = Store.Children();
        if (Children.Any(c => c.Id == ChildId) || Children.Count == 0) return;
        ChildId = Children[0].Id;
        Progress = Store.Progress(ChildId);
        LoadBreak();
        Store.SetLastChild(ChildId);
    }

    /// <summary>What a parent sees for one child, or null when the content didn't load.</summary>
    public ProgressReport? Report(Guid id)
    {
        if (Content == null) return null;
        var childProgress = id == ChildId ? Progress : Store.Progress(id);
        var band = Children.FirstOrDefault(c => c.Id == id)?.Age is { } age ? Content.StartingBand(age) : Band.Foundation;
        try
        {
            return new ProgressReport(Content, childProgress, band, Today);
        }
        catch (Exception error)
        {
            GD.PrintErr($"Bip Island: the progress report couldn't be made: {error.Message}");
            return null;
        }
    }

    /// <summary>Stars in a child's jar, for their card on "Who's playing?".</summary>
    public int StarCount(Guid id) => id == ChildId ? Progress.Stars : Store.Progress(id).Stars;

    // Where the child is

    public int Today => DayNumber.Of(DateTimeOffset.Now);

    public Band StartingBand =>
        Content != null && CurrentChild?.Age is { } age ? Content.StartingBand(age) : Band.Foundation;

    public LettersProgress? LettersProgress =>
        Content != null && Course != null ? new LettersProgress(Content, Course, Progress, StartingBand) : null;

    public LessonPlanner? Planner
    {
        get
        {
            if (LettersProgress is not { } state || Hunt is not { } hunt) return null;
            var group = state.HighestUnlockedGroup;
            return new LessonPlanner(state, sound => hunt.CanHunt(sound, group));
        }
    }

    public SoundStage Stage(PhonicsSound sound) => Progress.Sounds.Stage(sound);

    /// <summary>
    /// Bip's suggestion across all four islands. The child picks freely; one island glows, and Bip
    /// nudges towards another island when one dominates recent play.
    /// </summary>
    public IslandSuggestion? IslandSuggestion =>
        Content == null ? null : new PlayRecommender(Content).Suggest(Progress, StartingBand, Today, Content.MasteryRules, Rng);

    // Breaks

    /// <summary>Banks the minutes since the last check and reports where the break stands.</summary>
    public BreakPhase CurrentBreakPhase()
    {
        var now = DateTimeOffset.Now;
        var elapsed = _breakClockPaused ? 0 : PlayBreaks.Credit((now - _lastBreakCheck).TotalSeconds);
        _lastBreakCheck = now;
        // The child's minutes get exactly what the break banked: nothing on a break or after the day is done.
        var before = _break.DayStamp == Today ? _break.PlayedTodaySeconds : 0;
        var phase = PlayBreaks.Advance(_break, elapsed, now, Today, Store.Settings.ToBreakSettings());
        Store.SaveBreak(ChildId, _break);
        var banked = _break.PlayedTodaySeconds - before;
        if (banked > 0)
        {
            Progress.NotePlayTime(banked, Today);
            Store.Save(Progress, ChildId);
        }
        // The playing child can play, so they're the one at the computer now.
        if (phase == BreakPhase.Playing) _nobodyYet = false;
        return phase;
    }

    /// <summary>The parent gate opened: bank the play so far, then stop counting until it closes.</summary>
    public void PauseBreakClock()
    {
        CurrentBreakPhase();
        _breakClockPaused = true;
    }

    /// <summary>The parent gate closed: count play again from now.</summary>
    public void ResumeBreakClock()
    {
        _breakClockPaused = false;
        _lastBreakCheck = DateTimeOffset.Now;
    }

    /// <summary>A parent ends the playing child's break early from settings.</summary>
    public void EndBreakEarly()
    {
        PlayBreaks.EndBreakEarly(_break);
        Store.SaveBreak(ChildId, _break);
        _lastBreakCheck = DateTimeOffset.Now;
    }

    /// <summary>When the break ends (null while playing), for the charging screen.</summary>
    public DateTimeOffset? BreakEndsAt => _break.BreakEndsAt;

    /// <summary>A brother's or sister's break, for their charging screen (nothing is banked).</summary>
    public BreakPhase RestingPhaseOf(Guid id) =>
        PlayBreaks.PhaseWithoutBanking(BreakOf(id), DateTimeOffset.Now, Today, Store.Settings.ToBreakSettings());
    public DateTimeOffset? BreakEndsAtOf(Guid id) => BreakOf(id)?.BreakEndsAt;
    public double BreakProgressOf(Guid id) =>
        BreakOf(id) is { } state ? PlayBreaks.BreakProgress(state, DateTimeOffset.Now, Store.Settings.BreakMinutes) : 1;
    public int BreakMinutesLeftOf(Guid id) => BreakOf(id) is { } state ? PlayBreaks.MinutesLeft(state, DateTimeOffset.Now) : 0;

    /// <summary>0 (break just started) to 1 (charged), for the charging screen's battery.</summary>
    public double BreakProgress() => PlayBreaks.BreakProgress(_break, DateTimeOffset.Now, Store.Settings.BreakMinutes);

    /// <summary>Whole minutes until games open again, rounded up.</summary>
    public int BreakMinutesLeft() => PlayBreaks.MinutesLeft(_break, DateTimeOffset.Now);

    /// <summary>For previews and the walk-through only: today's play time is used up (needs a daily maximum).</summary>
    public void ForceDayDoneForTest()
    {
        _break.BreakEndsAt = null;
        _break.PlayedTodaySeconds = int.MaxValue / 2;
        Store.SaveBreak(ChildId, _break);
    }

    /// <summary>For previews and the walk-through only: a break that ends <paramref name="minutes"/> from now.</summary>
    public void ForceBreakForTest(double minutes = 1)
    {
        _break.BreakEndsAt = DateTimeOffset.Now.AddMinutes(minutes);
        Store.SaveBreak(ChildId, _break);
    }

    /// <summary>False while Bip is charging or the day is done: games stay closed.</summary>
    public bool PlayAllowed() => CurrentBreakPhase() == BreakPhase.Playing;

    // Rewards

    public bool MysteryAvailable => Progress.LastMysteryDay != Today;

    /// <summary>Bip's mystery box: once a day, bonus stars. Returns true when the box was full.</summary>
    public bool ClaimMystery()
    {
        if (!Progress.ClaimMysteryBox(Today)) return false;
        Store.Save(Progress, ChildId);
        return true;
    }

    public string RandomPraise() => Rng.Pick(AudioCatalogue.PraiseClips) ?? "praise_01";
    /// <summary>
    /// A spoken hint. Most hints lead into a sound ("It sounds like this:"); with nothing to follow,
    /// it's the one that stands alone ("Look for the one that's wiggling!").
    /// </summary>
    public string RandomHint(bool followedBySound = true) =>
        followedBySound ? Rng.Pick(AudioCatalogue.HintClips) ?? "hint_01" : AudioCatalogue.LookHint;

    // Navigation

    public void ShowProfiles() => Present(new ProfilesScreen());

    public void ShowMap()
    {
        if (!PlayAllowed())
        {
            ShowCharging();
            return;
        }
        Present(new MapScreen(greet: !_hasWelcomed));
        _hasWelcomed = true;
    }

    public void ShowCharging() => Present(new ChargingScreen());

    public void ShowStickers() => Present(new StickerScreen());

    public void ShowIsland(Island island, bool greet = true)
    {
        var open = island switch
        {
            Island.Letters => LettersOpen && LettersProgress != null,
            Island.Numbers => NumbersOpen,
            Island.Words => WordsOpen,
            Island.Coding => CodingOpen,
            Island.Art => ArtOpen,
            _ => false,
        };
        if (!open)
        {
            ShowMap();
            return;
        }
        Present(island switch
        {
            Island.Letters => new LettersIslandScreen(greet),
            Island.Art => new ArtIslandScreen(greet),
            _ => new GameIslandScreen(island, greet),
        });
    }

    /// <summary>
    /// Starts a game from an island. Returns false when it can't (its island's content didn't
    /// load), and the island says "coming soon".
    /// </summary>
    /// <param name="focus">Letters games: the sound to practise (Bip's suggestion when null).</param>
    public bool StartGame(string gameId, PhonicsSound? focus = null)
    {
        if (Content == null) return false;
        // While Bip charges no game starts (or counts as played): the charging screen shows instead.
        if (!PlayAllowed())
        {
            ShowCharging();
            return true;
        }
        var sound = focus ?? PracticeSound();
        // Sound Hunt, Bubble Pop and Feed the Monster need sounds the child has met. With nothing to
        // practise yet, meeting the sound comes first instead of an empty visit.
        if (gameId is SoundHuntGame.GameId or BubblePopGame.GameId or FeedMonsterGame.GameId
            && LettersReady && sound != null && !CanPlay(gameId, sound))
            gameId = MeetTheSoundGame.GameId;
        GameScreen? screen = gameId switch
        {
            MeetTheSoundGame.GameId when LettersReady && sound != null => MeetScreen(sound),
            SoundHuntGame.GameId when LettersReady && sound != null && Hunt != null => new SoundHuntScreen(Hunt, sound),
            BubblePopGame.GameId when LettersReady && sound != null => new BubblePopScreen(new BubblePopGame(Content, Course!), sound),
            LetterTraceGame.GameId when LettersReady && sound != null => new LetterTraceScreen(new LetterTraceGame(Content, Course!), sound),
            FeedMonsterGame.GameId when LettersReady && sound != null => new FeedMonsterScreen(new FeedMonsterGame(Content, Course!), sound),
            CountTapGame.GameId when NumbersOpen => new CountTapScreen(new CountTapGame(Content)),
            QuickLookGame.GameId when NumbersOpen => new QuickLookScreen(new QuickLookGame(Content)),
            BlockTowersGame.GameId when NumbersOpen => new BlockTowersScreen(new BlockTowersGame(Content)),
            SoundButtonsGame.GameId when WordsOpen => new SoundButtonsScreen(new SoundButtonsGame(Content, Course ?? new PhonicsCourse(Content)),
                                                                              Course ?? new PhonicsCourse(Content)),
            WordBuilderGame.GameId when WordsOpen => new WordBuilderScreen(new WordBuilderGame(Content)),
            MorningOrderGame.GameId when CodingOpen => new MorningOrderScreen(new MorningOrderGame(Content)),
            BipsPathGame.GameId when CodingOpen => new BipsPathScreen(new BipsPathGame(Content)),
            ShapeBuilderGame.GameId when ArtOpen => new ShapeBuilderScreen(new ShapeBuilderGame(Content)),
            PaintPotsGame.GameId when ArtOpen => new PaintPotsScreen(new PaintPotsGame(Content)),
            MirrorMagicGame.GameId when ArtOpen => new MirrorMagicScreen(new MirrorMagicGame(Content)),
            _ => null,
        };
        if (screen == null) return false;
        BeginVisit(gameId);
        Present(screen);
        return true;
    }

    private bool LettersReady => LettersOpen && Course != null && LettersProgress != null;

    /// <summary>Whether a Letters game can make a first question for this sound (a trial round, nothing kept).</summary>
    private bool CanPlay(string gameId, PhonicsSound sound)
    {
        var learner = LearnerFor(gameId, sound);
        return gameId switch
        {
            SoundHuntGame.GameId => Hunt is { } hunt && NewSession(hunt).NextRound(hunt, learner, Rng) != null,
            BubblePopGame.GameId => new BubblePopGame(Content!, Course!) is var pop && NewSession(pop).NextRound(pop, learner, Rng) != null,
            FeedMonsterGame.GameId => new FeedMonsterGame(Content!, Course!) is var monster && NewSession(monster).NextRound(monster, learner, Rng) != null,
            _ => true,
        };
    }

    /// <summary>
    /// Whether Feed the Monster has foods the child can read yet (they start at phonics group 4). The
    /// island hides the monster until then, rather than opening a different game; a cheap check, since
    /// it runs on every island build (StartGame still falls back if a round can't be made).
    /// </summary>
    public bool FeedMonsterReady() =>
        LettersReady && LettersProgress is { } letters && _monster is { } monster && monster.FoodsUpToGroup(letters.HighestUnlockedGroup).Count > 0;

    /// <summary>Meet the Sound is one round: the sound itself (null if it can't be met yet).</summary>
    private GameScreen? MeetScreen(PhonicsSound sound)
    {
        var game = new MeetTheSoundGame(Content!, Course!);
        var round = NewSession(game).NextRound(game, LearnerFor(MeetTheSoundGame.GameId, sound), Rng);
        return round == null ? null : new MeetSoundScreen(round.Sound);
    }

    /// <summary>The sound to practise outside the planner: Bip's suggestion, else the group's first sound.</summary>
    public PhonicsSound? PracticeSound() => Planner?.SuggestedSound() ?? LettersProgress?.CurrentGroup.Sounds.FirstOrDefault();

    /// <summary>Bip's suggestion on Letters Island: the next activity for the first sound not yet learnt.</summary>
    public PlannedActivity? NextActivity() =>
        Planner is { } planner && Content != null ? planner.NextActivity(Today, Content.MasteryRules, Rng) : null;

    /// <summary>The child heard a new sound all the way through Meet the Sound.</summary>
    public void MarkMet(PhonicsSound sound)
    {
        Progress.MarkMet(sound.Id);
        Store.Save(Progress, ChildId);
    }

    // Levels inside games

    /// <summary>The game being played now (null on the map and islands).</summary>
    public string? CurrentGameId { get; private set; }
    private int _visitStartStars;
    private int _visitStartLevel;

    /// <summary>Skins the game screens can draw; a session picks one of these when the game has it.</summary>
    public static readonly IReadOnlySet<string> DrawnSkins = new HashSet<string>
    {
        "paper_desk", "treasure_chests", "bubbles", "sparkles", "monster_blue",
        "ducks", "dice", "buttons", "tiles", "picture_cards", "island", "studio", "pegs", "blocks",
    };

    /// <summary>The child's level in a game (games.json "levels"), starting where their band does.</summary>
    public int GameLevel(string gameId) =>
        Content?.Game(gameId) is { } entry ? Progress.GameLevel(gameId, entry.StartingLevel(StartingBand)) : 0;

    public int LevelCount(string gameId) => Content?.Game(gameId)?.LevelSteps.Count ?? 1;

    /// <summary>A learner for one game, at the child's level in it.</summary>
    public Learner LearnerFor(string gameId, PhonicsSound? focus = null) =>
        (LettersProgress?.Learner(focus) ?? new Learner(StartingBand, 1, new HashSet<string>(), focus?.Id))
            .AtLevel(GameLevel(gameId));

    /// <summary>A fresh visit to a game: a session with a random skin it can draw.</summary>
    public GameSession NewSession(IMiniGame game) =>
        GameSession.Start(game, Content!.Games.Session, DrawnSkins, Rng);

    /// <summary>Notes the game as played and remembers stars and level, for the celebration at the end.</summary>
    private void BeginVisit(string gameId)
    {
        CurrentGameId = gameId;
        _visitStartStars = Progress.Stars;
        _visitStartLevel = GameLevel(gameId);
        Progress.NotePlayed(gameId);
        Store.Save(Progress, ChildId);
    }

    /// <summary>What happened this visit, for the celebration at the end.</summary>
    public sealed record VisitSummary(int StarsEarned, int LevelBefore, int LevelNow, int LevelCount)
    {
        public bool LevelledUp => LevelNow > LevelBefore;
    }

    public VisitSummary CurrentVisit()
    {
        var id = CurrentGameId ?? "";
        return new VisitSummary(Math.Max(0, Progress.Stars - _visitStartStars), _visitStartLevel,
                                CurrentGameId == null ? _visitStartLevel : GameLevel(id), LevelCount(id));
    }

    /// <summary>
    /// Records one answered question against its skill and sound, and moves the game's own level
    /// (it shows at the end of the visit). Saves straight away. Returns the sound's level change.
    /// </summary>
    public MasteryChange Record(bool correct, string skillId, string? soundId)
    {
        if (Content == null) return MasteryChange.None;
        var change = Progress.RecordAnswer(correct, skillId, soundId, Today, Content.MasteryRules);
        if (CurrentGameId is { } gameId && Content.Game(gameId) is { } entry)
            Progress.RecordGameAnswer(correct, gameId, entry.StartingLevel(StartingBand), entry.LevelSteps.Count, Content.MasteryRules);
        Store.Save(Progress, ChildId);
        return change;
    }

    /// <summary>Swaps the screen with a short fade through paper colour.</summary>
    public void Present(BaseScreen screen)
    {
        Voice.Stop();
        if (screen is not GameScreen) CurrentGameId = null;
        // While Bip charges (or the day is done) every game and island redirects here.
        if (screen is not (ChargingScreen or StickerScreen or ProfilesScreen) && !PlayAllowed())
            screen = new ChargingScreen();
        var tree = GetTree();
        if (tree.CurrentScene == null || SelfTest.IsRequested(Boot.UserArgs))
        {
            tree.ChangeSceneToNode(screen);
            return;
        }
        var tween = CreateTween();
        tween.TweenProperty(_fade, "modulate:a", 1f, 0.2);
        tween.TweenCallback(Callable.From(() => tree.ChangeSceneToNode(screen)));
        tween.TweenProperty(_fade, "modulate:a", 0f, 0.25);
    }
}
