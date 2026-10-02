using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.App;
using BipIsland.Audio;
using BipIsland.Drawing;
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
    public PhonicsCourse? Course { get; private set; }
    public SoundHuntGame? Hunt { get; private set; }

    public SaveStore Store { get; private set; } = null!;
    /// <summary>The grown-ups' layer: the parent gate, the parent area and the "Update ready" button.</summary>
    public ParentLayer Parent { get; private set; } = null!;
    public List<ChildSummary> Children { get; private set; } = [];
    public Guid ChildId { get; private set; }
    /// <summary>The child who is playing now.</summary>
    public ChildProgress Progress { get; private set; } = new();
    /// <summary>One break for the whole computer, so switching profiles can't skip it.</summary>
    private BreakState _break = new();
    /// <summary>The most one gap between checks can add to a child's minutes (an idle game left open overnight shouldn't count).</summary>
    private const int MaxPlayCreditSeconds = 30 * 60;
    private DateTimeOffset _lastBreakCheck = DateTimeOffset.Now;
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
            _ = new FeedMonsterGame(Content, Course);
        });
        NumbersOpen = Try("Numbers", () => { _ = new CountTapGame(Content); _ = new QuickLookGame(Content); });
        WordsOpen = Try("Words", () => { _ = new SoundButtonsGame(Content, new PhonicsCourse(Content)); _ = new WordBuilderGame(Content); });
        CodingOpen = Try("Coding", () => { _ = new MorningOrderGame(Content); _ = new BipsPathGame(Content); });
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
        _break = Store.LoadBreak(Progress.Breaks) ?? new BreakState { DayStamp = Today };
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

    /// <summary>The child picks their picture: their progress loads and the map opens.</summary>
    public void Choose(Guid id)
    {
        if (Children.All(c => c.Id != id)) return;
        CurrentBreakPhase(); // Bank play time to the child who was playing.
        ChildId = id;
        Progress = Store.Progress(id);
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
        Store.Delete(id);
        Children = Store.Children();
        if (Children.Any(c => c.Id == ChildId) || Children.Count == 0) return;
        ChildId = Children[0].Id;
        Progress = Store.Progress(ChildId);
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
        var elapsed = (int)Math.Max(0, (now - _lastBreakCheck).TotalSeconds);
        _lastBreakCheck = now;
        var wasPlaying = _break.BreakEndsAt == null;
        var phase = PlayBreaks.Advance(_break, elapsed, now, Today, Store.Settings.ToBreakSettings());
        Store.SaveBreak(_break);
        if (wasPlaying)
        {
            Progress.NotePlayTime(Math.Min(elapsed, MaxPlayCreditSeconds), Today);
            Store.Save(Progress, ChildId);
        }
        return phase;
    }

    /// <summary>A parent ends the break early from settings.</summary>
    public void EndBreakEarly()
    {
        PlayBreaks.EndBreakEarly(_break);
        Store.SaveBreak(_break);
        _lastBreakCheck = DateTimeOffset.Now;
    }

    /// <summary>For the walk-through test only: starts a one-minute break now.</summary>
    public void ForceBreakForTest()
    {
        _break.BreakEndsAt = DateTimeOffset.Now.AddMinutes(1);
        Store.SaveBreak(_break);
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
    public string RandomHint() => Rng.Pick(AudioCatalogue.HintClips) ?? "hint_01";

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
            _ => false,
        };
        if (!open)
        {
            ShowMap();
            return;
        }
        Present(island == Island.Letters ? new LettersIslandScreen(greet) : new GameIslandScreen(island, greet));
    }

    /// <summary>
    /// Starts a game from an island. The game screens arrive in Phase 3 of the Godot move, so for
    /// now this returns false and the island says "coming soon".
    /// </summary>
    public bool StartGame(string gameId) => false;

    /// <summary>Swaps the screen with a short fade through paper colour.</summary>
    public void Present(BaseScreen screen)
    {
        Voice.Stop();
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
