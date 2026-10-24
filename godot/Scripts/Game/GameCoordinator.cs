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
    /// <summary>The calm loops on the map and the islands.</summary>
    public MusicPlayer Music { get; } = new() { Name = "Music" };
    public IRandomSource Rng { get; set; } = SystemRandomSource.Shared;

    /// <summary>The game content in the playing child's language (null when it didn't load).</summary>
    public ContentLibrary? Content { get; private set; }

    /// <summary>
    /// The language the playing child plays in, and the one place it is held: the loaded content was read
    /// in it (<see cref="ContentLibrary.Language"/> only records which language a library was read in) and
    /// the voice follows it through <see cref="SpokenLanguage"/>.
    /// </summary>
    public HomeLanguage Language { get; private set; } = HomeLanguage.English;

    /// <summary>
    /// The language the voice speaks now: the screen's (<see cref="BaseScreen.ScreenLanguage"/>), which is
    /// the playing child's except on "Who's playing?" and a brother's or sister's break.
    /// </summary>
    public HomeLanguage SpokenLanguage => IsInsideTree() && GetTree().CurrentScene is BaseScreen screen ? screen.ScreenLanguage : Language;

    /// <summary>
    /// "Who's playing?" in the language of the child at the computer; English while nobody is yet (the
    /// game has just opened), since the last child's language may not be the next one's.
    /// </summary>
    public HomeLanguage PickerLanguage => _nobodyYet ? HomeLanguage.English : Language;

    /// <summary>
    /// The playing child's voice band, worked out from their age each time (<see cref="VoiceBands.For"/>),
    /// so it can't go stale when a child is chosen, changed or removed.
    /// </summary>
    public VoiceBand ChildBand => BandOf(ChildId);

    /// <summary>A child's voice band from the age a grown-up set in the Children tab (Little when there's none).</summary>
    public VoiceBand BandOf(Guid id) => VoiceBands.For(Children.FirstOrDefault(c => c.Id == id)?.Age);

    /// <summary>
    /// The band the voice speaks in now: the screen's (<see cref="BaseScreen.ScreenBand"/>), which is the
    /// playing child's except on "Who's playing?" (Little) and a brother's or sister's break (theirs).
    /// </summary>
    public VoiceBand SpokenBand => IsInsideTree() && GetTree().CurrentScene is BaseScreen screen ? screen.ScreenBand : ChildBand;

    private readonly Dictionary<HomeLanguage, ContentLibrary?> _contentByLanguage = [];

    /// <summary>
    /// The languages a grown-up can choose for a child: English, and any whose content is in the game
    /// (Content/af/language.json), so a language still being written never reaches a child.
    /// </summary>
    public static IReadOnlyList<HomeLanguage> AvailableLanguages =>
        [.. Enum.GetValues<HomeLanguage>().Where(l => l == HomeLanguage.English
            || FileAccess.FileExists($"res://assets/content/{Languages.Folder(l)}/{Languages.MarkerFile}"))];
    /// <summary>Each island opens on its own, so one bad game file can't close the whole map.</summary>
    public bool LettersOpen { get; private set; }
    public bool NumbersOpen { get; private set; }
    public bool WordsOpen { get; private set; }
    public bool CodingOpen { get; private set; }
    public bool ArtOpen { get; private set; }
    /// <summary>
    /// The Language Island: open when the language the child would learn is in the game too (its
    /// content and clips), so an English child learns Afrikaans only once Afrikaans is switched on.
    /// </summary>
    public bool LanguageOpen { get; private set; }
    /// <summary>The content of the language the child learns on the Language Island (null when it's closed).</summary>
    public ContentLibrary? LearningContent { get; private set; }
    private bool _previewLanguage;

    /// <summary>Opens (or closes again) the Language Island before Afrikaans is switched on: screen previews and the walk-through.</summary>
    public void PreviewLanguageIsland(bool open = true)
    {
        _previewLanguage = open;
        LoadContent(Language);
    }
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
        AddChild(Music);
        Music.NarratorSpeaking = () => Voice.Speaking;
        VoicePlayer.LanguageSource = () => SpokenLanguage;
        VoicePlayer.BandSource = () => SpokenBand;
        _fadeLayer = new CanvasLayer { Layer = 100 };
        _fade = new ColorRect { Color = Palette.Paper, MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0) };
        _fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _fadeLayer.AddChild(_fade);
        AddChild(_fadeLayer);

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

    /// <summary>A language's content, read once and kept (null when it didn't load).</summary>
    private ContentLibrary? ContentFor(HomeLanguage language)
    {
        if (_contentByLanguage.TryGetValue(language, out var cached)) return cached;
        ContentLibrary? library = null;
        try
        {
            library = new ContentLibrary(file =>
            {
                var path = "res://assets/content/" + file;
                if (!FileAccess.FileExists(path)) throw new System.IO.FileNotFoundException("not in the game package", path);
                return FileAccess.GetFileAsString(path);
            }, language, file => FileAccess.FileExists("res://assets/content/" + file));
        }
        catch (ContentLoadException error)
        {
            GD.PrintErr($"Bip Island: the {Languages.Name(language)} content didn't load, so the islands stay asleep: {error.Message}");
        }
        _contentByLanguage[language] = library;
        return library;
    }

    /// <summary>
    /// The language a child plays in: their own when this build has it (<see cref="AvailableLanguages"/>),
    /// else English, so a saved language that isn't ready never leaves the game silent.
    /// </summary>
    public HomeLanguage LanguageOf(Guid id) =>
        Languages.Playable(Children.FirstOrDefault(c => c.Id == id)?.Language ?? HomeLanguage.English, AvailableLanguages);

    /// <summary>
    /// Loads the playing child's progress in their language (each language keeps its own sounds, skills
    /// and levels; stars and stickers are shared), and that language's content, games and voice when
    /// they aren't the ones loaded.
    /// </summary>
    private void LoadChildProgress()
    {
        var language = LanguageOf(ChildId);
        Progress = Store.Progress(ChildId, language);
        if (Content != null && language == Language) return;
        LoadContent(language);
    }

    /// <summary>Saves the playing child's progress, in the language it was loaded in.</summary>
    public void SaveProgress() => Store.Save(Progress, ChildId, Language);

    private void LoadContent(HomeLanguage language)
    {
        Language = language;
        Content = ContentFor(language);
        Course = null;
        Hunt = null;
        _monster = null;
        LettersOpen = NumbersOpen = WordsOpen = CodingOpen = ArtOpen = LanguageOpen = false;
        LearningContent = null;
        if (Content == null) return;
        LettersOpen = Try("Letters", () =>
        {
            Course = new PhonicsCourse(Content);
            Hunt = new SoundHuntGame(Content, Course);
            _ = new MeetTheSoundGame(Content, Course);
            _ = new BubblePopGame(Content, Course);
            _ = new LetterTraceGame(Content, Course);
            _monster = new FeedMonsterGame(Content, Course);
        });
        NumbersOpen = Try("Numbers", () => { _ = new CountTapGame(Content); _ = new QuickLookGame(Content); _ = new BlockTowersGame(Content); _ = new BipsBalanceGame(Content); });
        WordsOpen = Try("Words", () =>
        {
            var course = new PhonicsCourse(Content);
            _ = new SoundButtonsGame(Content, course);
            _ = new WordBuilderGame(Content);
            _ = new WordRocketGame(Content, course);
            _ = new StoriesGame(Content);
        });
        CodingOpen = Try("Coding", () => { _ = new MorningOrderGame(Content); _ = new BipsPathGame(Content); });
        ArtOpen = Try("Art", () => { _ = new ShapeBuilderGame(Content); _ = new PaintPotsGame(Content); _ = new MirrorMagicGame(Content); });
        var learning = Languages.Learning(language);
        if ((AvailableLanguages.Contains(learning) || _previewLanguage) && ContentFor(learning) is { } other)
        {
            LanguageOpen = Try("Language", () => { _ = new ShowMeGame(Content); _ = new WhatIsItGame(Content); _ = new BipsDayGame(Content, other); });
            if (LanguageOpen) LearningContent = other;
        }
    }

    /// <summary>
    /// Islands a child of this language can't visit: the Language Island until the language they would
    /// learn is in the game. The parent report and the sticker book leave them out.
    /// </summary>
    public IReadOnlyCollection<Island> ClosedIslandsFor(HomeLanguage language) =>
        AvailableLanguages.Contains(Languages.Learning(language)) || _previewLanguage ? [] : [Island.Language];

    /// <summary>The playing child's closed islands (see <see cref="ClosedIslandsFor"/>).</summary>
    public IReadOnlyCollection<Island> ClosedIslands => LanguageOpen ? [] : [Island.Language];

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
        Music.Level = Store.Music;
        if (Store.LoadError is { } loadError) GD.PrintErr($"Bip Island: {loadError}");
        Children = Store.Children();
        ChildId = Store.LastChildId();
        LoadChildProgress();
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
        // The first time on this computer, a grown-up is guided through setting up (over the screen, paused).
        // Until it's finished or skipped, so a first run cut short gets it again. Nobody has passed the gate
        // yet, so the parent controls in it wait until the gate has really been opened.
        if (!Store.SetupDone && Parent.Guide == null) Parent.ShowSetupGuide(firstRun: true);
    }

    /// <summary>The next map greets the child again (after the first-run setup guide).</summary>
    public void ResetWelcome() => _hasWelcomed = false;

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
        LoadChildProgress();
        LoadBreak();
        Store.SetLastChild(id);
        _hasWelcomed = false;
        ShowMap();
    }

    /// <summary>A parent adds a child (at most four). Returns false when they couldn't be added.</summary>
    public bool AddChildProfile(string name, int? age, string? avatar, HomeLanguage language = HomeLanguage.English)
    {
        var added = Store.AddChild(name, age, avatar, language) != null;
        Children = Store.Children();
        return added;
    }

    /// <summary>A parent changes a child's name, age, picture or language (the playing child's takes effect at once).</summary>
    public void UpdateChild(ChildSummary child)
    {
        // The playing child's progress so far stays with the language it was earned in.
        if (child.Id == ChildId) SaveProgress();
        Store.Update(child);
        Children = Store.Children();
        if (child.Id == ChildId && LanguageOf(ChildId) != Language) LoadChildProgress();
    }

    /// <summary>Removes a child and their progress. If they were playing, the first child takes over.</summary>
    public void DeleteChild(Guid id)
    {
        CurrentBreakPhase(); // Bank play time to the child who was playing, as Choose does.
        Store.Delete(id);
        Children = Store.Children();
        // The store always keeps one child, so the list is never empty here; the voice band follows
        // whoever is playing by itself (ChildBand).
        if (Children.Any(c => c.Id == ChildId) || Children.Count == 0) return;
        ChildId = Children[0].Id;
        LoadChildProgress();
        LoadBreak();
        Store.SetLastChild(ChildId);
    }

    /// <summary>What a parent sees for one child, or null when the content didn't load.</summary>
    public ProgressReport? Report(Guid id)
    {
        // Each child's report is in their own language's content (a brother may play in Afrikaans).
        var language = LanguageOf(id);
        if (ContentFor(language) is not { } content) return null;
        var childProgress = id == ChildId ? Progress : Store.Progress(id, language);
        var band = Children.FirstOrDefault(c => c.Id == id)?.Age is { } age ? content.StartingBand(age) : Band.Foundation;
        var closed = ClosedIslandsFor(Children.FirstOrDefault(c => c.Id == id)?.Language ?? HomeLanguage.English);
        try
        {
            return new ProgressReport(content, childProgress, band, Today, closed);
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
            SaveProgress();
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

    /// <summary>
    /// Bip's mystery box: once a day, bonus stars, and the picture sticker they make up when they reach
    /// the next 10 (shown by the map, from <see cref="TakeNewStickers"/>). Returns true when the box was full.
    /// </summary>
    public bool ClaimMystery()
    {
        if (!Progress.ClaimMysteryBox(Today)) return false;
        _visitSound = null;
        AwardStickers(finishedGame: false);
        return true;
    }

    public string RandomPraise() => Rng.Pick(VoicePlayer.PraisePool()) ?? "praise_01";
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
            Island.Language => LanguageOpen,
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
        // Sound Hunt, Bubble Pop, Feed the Monster and Word Rocket's letter level need sounds the child
        // has met. With nothing to practise yet, meeting the sound comes first instead of an empty visit.
        if (gameId is SoundHuntGame.GameId or BubblePopGame.GameId or FeedMonsterGame.GameId or WordRocketGame.GameId
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
            BipsBalanceGame.GameId when NumbersOpen => new BipsBalanceScreen(new BipsBalanceGame(Content)),
            SoundButtonsGame.GameId when WordsOpen => new SoundButtonsScreen(new SoundButtonsGame(Content, Course ?? new PhonicsCourse(Content)),
                                                                              Course ?? new PhonicsCourse(Content)),
            WordBuilderGame.GameId when WordsOpen => new WordBuilderScreen(new WordBuilderGame(Content)),
            WordRocketGame.GameId when WordsOpen => new WordRocketScreen(new WordRocketGame(Content, Course ?? new PhonicsCourse(Content))),
            StoriesGame.GameId when WordsOpen => new StoriesScreen(new StoriesGame(Content)),
            MorningOrderGame.GameId when CodingOpen => new MorningOrderScreen(new MorningOrderGame(Content)),
            BipsPathGame.GameId when CodingOpen => new BipsPathScreen(new BipsPathGame(Content)),
            ShapeBuilderGame.GameId when ArtOpen => new ShapeBuilderScreen(new ShapeBuilderGame(Content)),
            PaintPotsGame.GameId when ArtOpen => new PaintPotsScreen(new PaintPotsGame(Content)),
            MirrorMagicGame.GameId when ArtOpen => new MirrorMagicScreen(new MirrorMagicGame(Content)),
            ShowMeGame.GameId when LanguageOpen => new ShowMeScreen(new ShowMeGame(Content)),
            WhatIsItGame.GameId when LanguageOpen => new WhatIsItScreen(new WhatIsItGame(Content)),
            BipsDayGame.GameId when LanguageOpen && LearningContent != null => new BipsDayScreen(new BipsDayGame(Content, LearningContent)),
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
            WordRocketGame.GameId => new WordRocketGame(Content!, Course!) is var rocket && NewSession(rocket).NextRound(rocket, LearnerFor(gameId), Rng) != null,
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
        SaveProgress();
    }

    // Levels inside games

    /// <summary>The game being played now (null on the map and islands).</summary>
    public string? CurrentGameId { get; private set; }
    private int _visitStartStars;
    private int _visitStartLevel;
    /// <summary>
    /// Picture stickers won but not shown yet (every 10 stars, and one for finishing). They wait for the
    /// next end card or mystery box, so one won just before Home is still seen; they belong to one child.
    /// </summary>
    private readonly List<string> _newStickers = [];
    private Guid? _newStickersChild;
    private string? _visitSound;
    private bool _visitCompleted;

    /// <summary>Skins the game screens can draw; a session picks one of these when the game has it.</summary>
    public static readonly IReadOnlySet<string> DrawnSkins = new HashSet<string>
    {
        "paper_desk", "treasure_chests", "bubbles", "sparkles", "monster_blue",
        "ducks", "dice", "buttons", "tiles", "picture_cards", "island", "studio", "pegs", "blocks", "seesaw",
        "rocket", "balloon", "submarine",
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
        _visitSound = null;
        _visitCompleted = false;
        Progress.NotePlayed(gameId);
        SaveProgress();
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
    /// The game was played to its end (not cut short to practise again): a picture sticker for
    /// finishing, once per visit, matching the sound practised when there was one.
    /// </summary>
    public void CompleteVisit()
    {
        if (_visitCompleted || Content == null || CurrentGameId == null) return;
        _visitCompleted = true;
        AwardStickers(finishedGame: true);
    }

    private void AwardStickers(bool finishedGame)
    {
        if (Content == null) return;
        PendingStickers().AddRange(StickerBook.AwardStickers(Progress, Content, _visitSound, finishedGame, Rng));
        SaveProgress();
    }

    private List<string> PendingStickers()
    {
        if (_newStickersChild != ChildId)
        {
            _newStickers.Clear();
            _newStickersChild = ChildId;
        }
        return _newStickers;
    }

    /// <summary>The picture stickers won since the last were shown; they count as shown from now on.</summary>
    public List<string> TakeNewStickers()
    {
        var stickers = PendingStickers().ToList();
        _newStickers.Clear();
        return stickers;
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
        // Every 10 stars become a picture sticker, shown at the end of the visit (or the next one, after Home).
        _visitSound = soundId ?? _visitSound;
        AwardStickers(finishedGame: false);
        return change;
    }

    /// <summary>A grown-up changed the music setting: kept on this computer and heard straight away.</summary>
    public void SetMusic(MusicLevel level)
    {
        Store.Music = level;
        Music.Level = level;
    }

    /// <summary>Swaps the screen with a short fade through paper colour.</summary>
    public void Present(BaseScreen screen)
    {
        Voice.Stop();
        if (screen is not GameScreen) CurrentGameId = null;
        // While Bip charges (or the day is done) every game and island redirects here.
        if (screen is not (ChargingScreen or StickerScreen or ProfilesScreen) && !PlayAllowed())
            screen = new ChargingScreen();
        Music.PlayTrack(screen.MusicTrack);
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
