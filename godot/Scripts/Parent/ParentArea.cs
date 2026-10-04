using System;
using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.App;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Game;
using Godot;

namespace BipIsland.Parent;

/// <summary>
/// The parent area behind the gate (the Swift app's ParentAreaViews): each child's progress, the
/// children on this computer, and settings (play time, passcode, updates, quit).
/// </summary>
public partial class ParentArea : VBoxContainer
{
    public enum Tab
    {
        Progress,
        Children,
        Settings,
    }

    /// <summary>One part of the parent area on its own, for the first-time setup guide.</summary>
    public enum Part
    {
        Children,
        PlayTime,
        Passcode,
    }

    private readonly ParentLayer _layer;
    private readonly Part? _only;
    private TabBar _tabs = null!;
    private VBoxContainer _body = null!;
    private Guid? _reportChild;
    private string? _settingsMessage;
    private Label? _versionNote;

    public Tab Showing { get; private set; } = Tab.Progress;

    private static GameCoordinator Coordinator => GameCoordinator.Instance;

    public ParentArea(ParentLayer layer) => _layer = layer;

    /// <summary>Just one part (the children, play time or the passcode), with no header or tabs: the setup guide's steps.</summary>
    public ParentArea(ParentLayer layer, Part only)
    {
        _layer = layer;
        _only = only;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 14);
        if (_only != null)
        {
            _body = ParentUi.Column(18);
            _body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddChild(_body);
            Refresh();
            return;
        }
        var close = ParentUi.Button("Back to the game", _layer.Close);
        AddChild(ParentUi.Row(12, ParentUi.Text("Parent area", 30, bold: true), ParentUi.Spacer(), close));

        _tabs = new TabBar { FocusMode = FocusModeEnum.All };
        foreach (var tab in Enum.GetNames<Tab>()) _tabs.AddTab(tab);
        _tabs.TabChanged += index => Show((Tab)(int)index);
        AddChild(_tabs);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _body = ParentUi.Column(18);
        _body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_body);
        AddChild(scroll);
        Show(Showing);
        close.CallDeferred(Control.MethodName.GrabFocus);
    }

    /// <summary>Switches tab (also used by the walk-through test and screenshots).</summary>
    public void Show(Tab tab)
    {
        Showing = tab;
        if (_tabs.CurrentTab != (int)tab) _tabs.CurrentTab = (int)tab;
        Refresh();
    }

    /// <summary>Opens Settings with a note at the top (an update that couldn't install).</summary>
    public void ShowSettings(string message)
    {
        _settingsMessage = message;
        Show(Tab.Settings);
    }

    public void Refresh()
    {
        foreach (var child in _body.GetChildren()) child.QueueFree();
        _versionNote = null;
        _pendingPasscode = null;
        if (_only is { } part)
        {
            switch (part)
            {
                case Part.Children: BuildChildren(); break;
                case Part.PlayTime: BuildPlayTime(); break;
                case Part.Passcode: BuildPasscode(); break;
            }
            return;
        }
        switch (Showing)
        {
            case Tab.Progress: BuildProgress(); break;
            case Tab.Children: BuildChildren(); break;
            case Tab.Settings: BuildSettings(); break;
        }
    }

    public void RefreshVersion()
    {
        if (Showing == Tab.Settings) Refresh();
    }

    // Progress

    private void BuildProgress()
    {
        var children = Coordinator.Children;
        var id = _reportChild is { } chosen && children.Any(c => c.Id == chosen) ? chosen : Coordinator.ChildId;
        if (children.Count > 1)
        {
            var picker = new OptionButton();
            foreach (var child in children)
            {
                picker.AddItem($"{Avatars.Emoji.GetValueOrDefault(child.Avatar, "")} {child.Name}");
                if (child.Id == id) picker.Selected = picker.ItemCount - 1;
            }
            picker.ItemSelected += index =>
            {
                _reportChild = children[(int)index].Id;
                Refresh();
            };
            _body.AddChild(ParentUi.Row(12, ParentUi.Text("Child", 18, bold: true), picker));
        }

        if (Coordinator.Report(id) is not { } report)
        {
            _body.AddChild(ParentUi.Text("Progress isn't available: the game content didn't load.", wrap: true));
            return;
        }

        var tiles = ParentUi.Row(12,
            Tile($"{report.Stars}", "stars"),
            Tile($"{report.StickersEarned} of {report.StickersTotal}", "stickers"),
            Tile($"{report.MinutesToday} min", "played today"),
            Tile($"{report.MinutesThisWeek} min", $"this week, {report.DaysPlayedThisWeek} of 7 days"),
            Tile(WeekAnswers(report), "right this week"));
        _body.AddChild(tiles);

        if (report.NeedsPractice.Count > 0)
        {
            var section = ParentUi.Section(_body, "Could use more practice");
            foreach (var line in report.NeedsPractice) section.AddChild(SkillRow(line));
        }
        if (report.DueForReview.Count > 0)
        {
            var section = ParentUi.Section(_body, "Due for review (Bip will suggest these)");
            foreach (var line in report.DueForReview) section.AddChild(SkillRow(line));
        }

        var sounds = ParentUi.Section(_body, "Letters and sounds");
        var legend = ParentUi.Row(14);
        foreach (var stage in Enum.GetValues<SoundStage>())
            legend.AddChild(ParentUi.Row(5, ParentUi.Pill(" ", StageColour(stage), 12, 4, 18), ParentUi.Text(StageText(stage), 15, colour: ParentUi.Secondary)));
        sounds.AddChild(legend);
        foreach (var group in report.Phonics)
        {
            var name = ParentUi.Text($"Group {group.Number}", 15, bold: true);
            name.CustomMinimumSize = new Vector2(76, 0);
            var chips = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            chips.AddThemeConstantOverride("h_separation", 5);
            chips.AddThemeConstantOverride("v_separation", 5);
            foreach (var sound in group.Sounds)
            {
                var chip = ParentUi.Pill(sound.Grapheme, StageColour(sound.Stage), 15, 7, 32);
                chip.TooltipText = StageText(sound.Stage);
                chips.AddChild(chip);
            }
            sounds.AddChild(ParentUi.Row(10, name, chips, Badge(group.Status)));
        }

        foreach (var island in report.Islands)
        {
            var section = ParentUi.Section(_body, $"{island.Island} island: {island.DoneCount} of {island.Skills.Count} skills done");
            foreach (var line in island.Skills) section.AddChild(SkillRow(line));
        }

        var recent = ParentUi.Section(_body, "Recently played");
        recent.AddChild(ParentUi.Text(report.RecentGames.Count == 0 ? "Nothing yet." : string.Join(", ", report.RecentGames), wrap: true));
    }

    private static string WeekAnswers(ProgressReport report)
    {
        if (report.AnswersThisWeek <= 0) return "–";
        var percent = (int)Math.Round(report.RightThisWeek * 100.0 / report.AnswersThisWeek);
        return $"{report.RightThisWeek} of {report.AnswersThisWeek} ({percent}%)";
    }

    private static Control Tile(string value, string label)
    {
        var tile = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 78) };
        tile.AddThemeStyleboxOverride("panel", ParentUi.Box(new Color(Palette.Sun, 0.35f), new Color(0, 0, 0, 0), 0, 12, 8, 8));
        var column = ParentUi.Column(4);
        column.Alignment = AlignmentMode.Center;
        var big = ParentUi.Text(value, 22, bold: true);
        big.HorizontalAlignment = HorizontalAlignment.Center;
        big.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        var small = ParentUi.Text(label, 15, colour: ParentUi.Secondary, wrap: true);
        small.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(big);
        column.AddChild(small);
        tile.AddChild(column);
        return tile;
    }

    private static Control SkillRow(ProgressReport.SkillLine line)
    {
        var name = ParentUi.Text(line.Name, 15);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        var row = ParentUi.Row(10, name);
        if (line.RecentPercent is { } percent)
            row.AddChild(ParentUi.Text($"{line.RecentRight} of last {line.RecentTotal} right ({percent}%)", 15, colour: ParentUi.Secondary));
        if (line.MasteredOnDay is { } day)
            row.AddChild(ParentUi.Text($"since {DayNumber.Date(day):d MMM}", 15, colour: ParentUi.Secondary));
        row.AddChild(Badge(line.Status));
        return row;
    }

    private static Control Badge(SkillStatus status)
    {
        var holder = new HBoxContainer { CustomMinimumSize = new Vector2(170, 0), Alignment = AlignmentMode.End };
        holder.AddChild(ParentUi.Pill(StatusText(status), new Color(StatusColour(status), 0.45f), 15));
        return holder;
    }

    public static string StatusText(SkillStatus status) => status switch
    {
        SkillStatus.Locked => "Not open yet",
        SkillStatus.KnownByAge => "Known for age",
        SkillStatus.Ready => "Ready to start",
        SkillStatus.Learning => "Practising",
        SkillStatus.Mastered => "Mastered",
        _ => "Review due",
    };

    private static Color StatusColour(SkillStatus status) => status switch
    {
        SkillStatus.Locked => Palette.Stone,
        SkillStatus.KnownByAge => Palette.LightTeal,
        SkillStatus.Ready => Palette.Sea,
        SkillStatus.Learning => Palette.Sun,
        SkillStatus.Mastered => Palette.Grass,
        _ => Palette.Orange,
    };

    private static Color StageColour(SoundStage stage) => stage switch
    {
        SoundStage.New => new Color(Palette.Stone, 0.6f),
        SoundStage.Met => new Color(Palette.Sun, 0.7f),
        SoundStage.Recognises => new Color(Palette.Orange, 0.7f),
        _ => Palette.Grass,
    };

    private static string StageText(SoundStage stage) => stage switch
    {
        SoundStage.New => "Not met yet",
        SoundStage.Met => "Met (Sound Hunt next)",
        SoundStage.Recognises => "Recognises it (Bubble Pop next)",
        _ => "Knows it",
    };

    // Children

    private void BuildChildren()
    {
        // The setup guide says this itself.
        if (_only == null)
            _body.AddChild(ParentUi.Text("Each child picks their animal when the game opens. Their age sets where they start; the game then adjusts to how they do.",
                16, colour: ParentUi.Secondary, wrap: true));
        // Redrawn with the list, so it goes once Player 1 has been replaced or changed.
        if (_only != null && UntouchedPlayerOne(Coordinator) != null)
            _body.AddChild(ParentUi.Text("The first child you add replaces \"Player 1\".", 16, colour: ParentUi.Secondary, wrap: true));
        var children = Coordinator.Children;
        foreach (var child in children) _body.AddChild(ChildRow(child, children.Count > 1));

        if (!ProfileRules.CanAdd(children.Count))
        {
            _body.AddChild(ParentUi.Text("Four children is the most one computer can have.", 16, colour: ParentUi.Secondary));
            return;
        }
        var section = ParentUi.Section(_body, "Add a child");
        var name = new LineEdit { PlaceholderText = "Name", CustomMinimumSize = new Vector2(240, 0), MaxLength = 40 };
        var age = AgePicker(null);
        var problem = ParentUi.Text("That child couldn't be added.", 16, colour: ParentUi.Problem);
        problem.Visible = false;
        void Add()
        {
            if (AddChild(name.Text, AgeFrom(age))) return;
            problem.Visible = true;
        }
        name.TextSubmitted += _ => Add();
        section.AddChild(ParentUi.Row(12, name, age, ParentUi.Button("Add", Add)));
        section.AddChild(problem);
    }

    /// <summary>Adds a child from the Children tab (also used by the walk-through test).</summary>
    public bool AddChild(string name, int? age)
    {
        if (ProfileRules.CleanName(name) == null) return false;
        // In the setup guide, the first child added takes over the untouched "Player 1" the game made
        // on its first run, so the family doesn't end up with a spare profile.
        if (_only == Part.Children && UntouchedPlayerOne(Coordinator) is { } placeholder)
        {
            Change(placeholder with { Name = name, Age = age });
            return true;
        }
        if (!Coordinator.AddChildProfile(name, age, null)) return false;
        _layer.MarkChanged();
        Refresh();
        return true;
    }

    /// <summary>The only child, when it's still the "Player 1" made on the first run and has never played.</summary>
    public static ChildSummary? UntouchedPlayerOne(GameCoordinator game)
    {
        if (game.Children is not [var only] || only.Name != SaveStore.FirstChildName || only.Age != null) return null;
        var progress = game.Store.Progress(only.Id);
        return progress.Stars == 0 && progress.RecentGames.Count == 0 ? only : null;
    }

    private Control ChildRow(ChildSummary child, bool canRemove)
    {
        var avatar = new OptionButton { CustomMinimumSize = new Vector2(80, 0), TooltipText = "Change the picture" };
        avatar.AddThemeFontSizeOverride("font_size", 26);
        foreach (var animal in ProfileRules.Avatars)
        {
            avatar.AddItem($"{Avatars.Emoji.GetValueOrDefault(animal, "🦁")} {char.ToUpperInvariant(animal[0])}{animal[1..]}");
            if (animal == child.Avatar) avatar.Selected = avatar.ItemCount - 1;
        }
        avatar.ItemSelected += index => Change(child with { Avatar = ProfileRules.Avatars[(int)index] });

        var name = new LineEdit { Text = child.Name, CustomMinimumSize = new Vector2(220, 0), MaxLength = 40 };
        var save = ParentUi.Button("Save name", () => { });
        save.Disabled = true;
        name.TextChanged += text => save.Disabled = text == child.Name || ProfileRules.CleanName(text) == null;
        void SaveName()
        {
            if (ProfileRules.CleanName(name.Text) != null && name.Text != child.Name) Change(child with { Name = name.Text });
        }
        save.Pressed += SaveName;
        name.TextSubmitted += _ => SaveName();

        var age = AgePicker(child.Age);
        age.ItemSelected += _ => Change(child with { Age = AgeFrom(age) });

        var row = ParentUi.Row(12, avatar, name, save, age);
        if (child.Id == Coordinator.ChildId && _only == null) row.AddChild(ParentUi.Text("playing now", 15, colour: ParentUi.Secondary));
        row.AddChild(ParentUi.Spacer());
        var remove = ParentUi.Button("Remove", () => ConfirmRemove(child), danger: true);
        remove.Disabled = !canRemove;
        row.AddChild(remove);
        return row;
    }

    private void Change(ChildSummary child)
    {
        Coordinator.UpdateChild(child);
        _layer.MarkChanged();
        Refresh();
    }

    private void ConfirmRemove(ChildSummary child)
    {
        var dialog = new ConfirmationDialog
        {
            Title = $"Remove {child.Name}?",
            DialogText = $"{child.Name}'s stars, stickers and progress are deleted from this computer. This can't be undone.",
            OkButtonText = $"Remove {child.Name} and all their progress",
            CancelButtonText = "Keep",
            Theme = ParentUi.Theme,
            ProcessMode = ProcessModeEnum.Always,
        };
        dialog.Confirmed += () =>
        {
            RemoveChild(child.Id);
            dialog.QueueFree();
        };
        dialog.Canceled += dialog.QueueFree;
        AddChild(dialog);
        dialog.PopupCentered();
    }

    /// <summary>Removes a child and their progress (also used by the walk-through test).</summary>
    public void RemoveChild(Guid id)
    {
        Coordinator.DeleteChild(id);
        _layer.MarkChanged();
        Refresh();
    }

    private static OptionButton AgePicker(int? age)
    {
        var picker = new OptionButton { CustomMinimumSize = new Vector2(130, 0) };
        picker.AddItem("Age not set");
        for (var value = ProfileRules.YoungestAge; value <= ProfileRules.OldestAge; value++)
        {
            picker.AddItem($"Age {value}");
            if (value == age) picker.Selected = picker.ItemCount - 1;
        }
        if (age == null) picker.Selected = 0;
        return picker;
    }

    private static int? AgeFrom(OptionButton picker) =>
        picker.Selected <= 0 ? null : ProfileRules.YoungestAge + picker.Selected - 1;

    // Settings

    private void BuildSettings()
    {
        if (_settingsMessage is { } message)
        {
            _body.AddChild(ParentUi.Text(message, 17, bold: true, colour: ParentUi.Problem, wrap: true));
            _settingsMessage = null;
        }

        BuildPlayTime();
        BuildPasscode();
        BuildAbout();
    }

    private void BuildPlayTime()
    {
        var play = ParentUi.Section(_body, "Play time");
        var settings = Coordinator.Store.Settings;
        play.AddChild(Stepper("Play for", "minutes", settings.PlayMinutes, 5, 120, 5,
            value => SetSettings(Coordinator.Store.Settings with { PlayMinutes = value })));
        play.AddChild(Stepper("Break for", "minutes", settings.BreakMinutes, 5, 60, 5,
            value => SetSettings(Coordinator.Store.Settings with { BreakMinutes = value })));
        var dailyOn = new CheckBox { Text = "Daily maximum", ButtonPressed = settings.DailyMaxMinutes != null, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        play.AddChild(dailyOn);
        var daily = Stepper("At most", "minutes a day", settings.DailyMaxMinutes ?? 60, 10, 240, 10,
            value => SetSettings(Coordinator.Store.Settings with { DailyMaxMinutes = value }));
        daily.Visible = dailyOn.ButtonPressed;
        play.AddChild(daily);
        dailyOn.Toggled += on =>
        {
            SetSettings(Coordinator.Store.Settings with { DailyMaxMinutes = on ? Coordinator.Store.Settings.DailyMaxMinutes ?? 60 : null });
            daily.Visible = on;
        };
        // In the setup guide there's no break to end yet.
        if (_only != null) return;
        // Each child has their own break: this ends the break of the child who was playing.
        var whose = Coordinator.Children.Count > 1 && Coordinator.CurrentChild is { } current ? $"{current.Name}'s" : "Bip's";
        var endBreak = ParentUi.Button($"End {whose} break now", () => { });
        var ended = ParentUi.Text("", 15, colour: ParentUi.Secondary);
        endBreak.Pressed += () =>
        {
            Coordinator.EndBreakEarly();
            _layer.MarkChanged();
            ended.Text = "Done: games are open again.";
        };
        play.AddChild(ParentUi.Row(12, endBreak, ended));
    }

    private void BuildAbout()
    {
        var about = ParentUi.Section(_body, "This copy of Bip Island");
        var version = ProjectSettings.GetSetting("application/config/version").AsString();
        about.AddChild(ParentUi.Text($"Version {Updater.InstalledVersion ?? version}", 17));
        if (Updater.ReadyVersion is { } ready) about.AddChild(ParentUi.Text($"Version {ready} is ready to install.", 17, bold: true));
        about.AddChild(ParentUi.Text($"Voice clips: {VoicePlayer.CountClips()} narrator recordings.", 15, colour: ParentUi.Secondary));
        about.AddChild(ParentUi.Text(KidLockText(), 15, colour: ParentUi.Secondary, wrap: true));
        _versionNote = ParentUi.Text("", 15, colour: ParentUi.Secondary, wrap: true);
        var updates = Updater.ReadyVersion == null
            ? ParentUi.Button("Check for updates now", CheckForUpdates)
            : ParentUi.Button("Install the update", InstallUpdate);
        updates.Disabled = Updater.FeedUrl == null;
        about.AddChild(ParentUi.Row(12, updates, ParentUi.Button("Quit Bip Island", () => Boot.Instance.Quit(), danger: true)));
        about.AddChild(ParentUi.Row(12, ParentUi.Button("Show the setup guide again", () => _layer.ShowSetupGuide())));
        if (Updater.FeedUrl == null) _versionNote.Text = "Automatic updates aren't switched on in this build.";
        about.AddChild(_versionNote);
    }

    private static string KidLockText() => KidLock.Status switch
    {
        "mac" or "windows" => "Child lock: on. To play in a normal window without it, hold Option (Mac) or Alt (Windows) while the game opens.",
        "parent mode" => "Child lock: off (parent mode, because Option or Alt was held as the game opened).",
        "off" => "Child lock: off on this computer.",
        var problem => $"Child lock: couldn't start ({problem}).",
    };

    private void SetSettings(PlayTimeSettings settings)
    {
        Coordinator.Store.Settings = settings;
        _layer.MarkChanged();
    }

    private static Control Stepper(string before, string after, int value, int lowest, int highest, int step, Action<int> changed)
    {
        var spin = new SpinBox { MinValue = lowest, MaxValue = highest, Step = step, Value = Math.Clamp(value, lowest, highest), Suffix = after, CustomMinimumSize = new Vector2(230, 0) };
        spin.ValueChanged += v => changed((int)v);
        var label = ParentUi.Text(before, 17);
        label.CustomMinimumSize = new Vector2(110, 0);
        return ParentUi.Row(12, label, spin);
    }

    private async void CheckForUpdates()
    {
        if (_versionNote != null) _versionNote.Text = "Checking…";
        var note = await Updater.CheckAndDownloadInBackground();
        if (!IsInstanceValid(this)) return;
        if (Updater.ReadyVersion != null && Showing == Tab.Settings) Refresh();
        if (_versionNote != null && IsInstanceValid(_versionNote)) _versionNote.Text = note;
    }

    private void InstallUpdate()
    {
        var problem = Updater.InstallAndRestart();
        ShowSettings(problem); // Only reached when installing failed.
    }

    private void BuildPasscode()
    {
        var section = ParentUi.Section(_body, "Parent passcode");
        var has = Coordinator.Store.Passcode != null;
        section.AddChild(ParentUi.Text(has
            ? "A passcode is set. Holding Esc asks for it instead of a maths question (after 3 wrong tries it asks maths)."
            : "Set a 4 to 8 digit passcode to use instead of the maths question when you hold Esc.",
            15, colour: ParentUi.Secondary, wrap: true));

        var first = new LineEdit { PlaceholderText = "New passcode", Secret = true, CustomMinimumSize = new Vector2(170, 0), MaxLength = 12 };
        var second = new LineEdit { PlaceholderText = "Type it again", Secret = true, CustomMinimumSize = new Vector2(170, 0), MaxLength = 12 };
        var problem = ParentUi.Text("", 15, colour: ParentUi.Problem);
        var editing = ParentUi.Row(12, first, second);
        editing.Visible = false;
        var idle = ParentUi.Row(12);

        string? Save()
        {
            var why = SetPasscode(first.Text, second.Text);
            if (why != null) problem.Text = why;
            return why;
        }
        // Typed but not saved when the setup guide's Next is pressed: save it then (or say why not).
        _pendingPasscode = () => editing.Visible && (first.Text != "" || second.Text != "") ? Save() : null;
        second.TextSubmitted += _ => Save();
        editing.AddChild(ParentUi.Button("Save", () => Save()));
        editing.AddChild(ParentUi.Button("Cancel", Refresh));

        idle.AddChild(ParentUi.Button(has ? "Change passcode" : "Set a passcode", () =>
        {
            idle.Visible = false;
            editing.Visible = true;
            first.GrabFocus();
        }));
        if (has) idle.AddChild(ParentUi.Button("Remove passcode", () => { Coordinator.Store.Passcode = null; Refresh(); }, danger: true));

        section.AddChild(idle);
        section.AddChild(editing);
        section.AddChild(problem);
    }

    private Func<string?>? _pendingPasscode;

    /// <summary>Saves a passcode typed but not saved yet. Returns what's wrong, or null when saved or nothing was typed.</summary>
    public string? SavePendingPasscode() => _pendingPasscode?.Invoke();

    /// <summary>Sets the passcode from the two boxes. Returns what's wrong, or null when it was saved.</summary>
    public string? SetPasscode(string first, string second)
    {
        if (ParentPasscode.Normalised(first) is not { } digits) return "Use 4 to 8 digits.";
        if (digits != ParentPasscode.Normalised(second)) return "The two don't match. Try again.";
        if (ParentPasscode.Create(digits) is not { } code) return "Use 4 to 8 digits.";
        Coordinator.Store.Passcode = code;
        Refresh();
        return null;
    }
}
