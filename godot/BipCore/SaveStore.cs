using System.Text.Json;
using System.Text.Json.Serialization;

namespace BipCore;

/// <summary>One child on this computer, as the picker and the parent area show them.</summary>
public sealed record ChildSummary(Guid Id, string Name, int? Age, string Avatar);

/// <summary>Play length, break length and the optional daily maximum, set by parents.</summary>
public sealed record PlayTimeSettings
{
    public int PlayMinutes { get; init; } = 20;
    public int BreakMinutes { get; init; } = 20;
    /// <summary>Null means no daily maximum.</summary>
    public int? DailyMaxMinutes { get; init; }

    public BreakSettings ToBreakSettings() => new(PlayMinutes, BreakMinutes, DailyMaxMinutes);
}

/// <summary>Everything the game keeps on this computer, in one JSON file.</summary>
public sealed class SaveFile
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public List<SavedChild> Children { get; set; } = [];
    public Guid? LastChildId { get; set; }
    /// <summary>The play-time break, shared by the whole computer so switching profiles can't skip it.</summary>
    public BreakState? Break { get; set; }
    public PlayTimeSettings Settings { get; set; } = new();
    /// <summary>The optional parent passcode (only a salted hash is kept).</summary>
    public ParentPasscode? Passcode { get; set; }
}

public sealed class SavedChild
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int? Age { get; set; }
    public string Avatar { get; set; } = ProfileRules.Avatars[0];
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ChildProgress Progress { get; set; } = new();
}

/// <summary>
/// Loads and saves the save file (the Godot version of the Swift app's ProgressStore). Writes go to a
/// temporary file that then replaces the real one, so a crash or power cut can't leave half a save.
/// If the file can't be read, a copy is kept next to it before anything new is saved over it, so a
/// child's progress is never thrown away silently. If the folder can't be written, the game still
/// runs; progress just isn't kept.
/// </summary>
public sealed class SaveStore
{
    public const string FileName = "save.json";

    private readonly string _folder;
    private readonly Func<DateTimeOffset> _now;
    private readonly SaveFile _file;

    /// <summary>Why the last save failed (null when it worked), for the log.</summary>
    public string? LastError { get; private set; }
    /// <summary>Why the save file couldn't be read at start-up (null when it was fine or new), for the log.</summary>
    public string? LoadError { get; private set; }

    public SaveStore(string folder, Func<DateTimeOffset>? now = null)
    {
        _folder = folder;
        _now = now ?? (() => DateTimeOffset.Now);
        _file = Load();
        if (_file.Children.Count == 0)
        {
            AddChildRecord("Player 1", null, ProfileRules.Avatars[0], new ChildProgress());
            Save();
        }
    }

    public string FilePath => Path.Combine(_folder, FileName);

    // Children

    /// <summary>Every child, in the order they were added. There is always at least one ("Player 1").</summary>
    public List<ChildSummary> Children() =>
        _file.Children.OrderBy(c => c.SortOrder).ThenBy(c => c.CreatedAt).Select(Summary).ToList();

    /// <summary>The child who played last (or the first one).</summary>
    public Guid LastChildId() =>
        _file.LastChildId is { } id && _file.Children.Any(c => c.Id == id) ? id : Children()[0].Id;

    public void SetLastChild(Guid id)
    {
        _file.LastChildId = id;
        Save();
    }

    /// <summary>Adds a child (at most <see cref="ProfileRules.MaxChildren"/>). Returns the new id, or null if it couldn't.</summary>
    public Guid? AddChild(string name, int? age, string? avatar)
    {
        if (ProfileRules.CleanName(name) is not { } clean || !ProfileRules.CanAdd(_file.Children.Count)) return null;
        var picture = avatar is null
            ? ProfileRules.FreeAvatar(_file.Children.Select(c => c.Avatar))
            : ProfileRules.ValidAvatar(avatar);
        var id = AddChildRecord(clean, ClampAge(age), picture, new ChildProgress());
        Save();
        return id;
    }

    /// <summary>Changes a child's name, age or picture. An empty name is ignored.</summary>
    public void Update(ChildSummary child)
    {
        if (Find(child.Id) is not { } row) return;
        if (ProfileRules.CleanName(child.Name) is { } clean) row.Name = clean;
        row.Age = ClampAge(child.Age);
        row.Avatar = ProfileRules.ValidAvatar(child.Avatar);
        Save();
    }

    /// <summary>Removes a child and their progress. The last child can't be removed.</summary>
    public void Delete(Guid id)
    {
        if (_file.Children.Count <= 1 || Find(id) is not { } row) return;
        _file.Children.Remove(row);
        if (_file.LastChildId == id) _file.LastChildId = null;
        Save();
    }

    public ChildProgress Progress(Guid childId) =>
        Find(childId) is { } row ? ChildProgress.Decode(row.Progress.Encode()) : new ChildProgress();

    public void Save(ChildProgress progress, Guid childId)
    {
        if (Find(childId) is not { } row) return;
        row.Progress = ChildProgress.Decode(progress.Encode());
        Save();
    }

    // The break, settings and passcode (one each for the whole computer)

    /// <summary>The saved break, or one carried over from an older save that kept it per child.</summary>
    public BreakState? LoadBreak(BreakState? legacy) => _file.Break ?? legacy;

    public void SaveBreak(BreakState state)
    {
        _file.Break = state;
        Save();
    }

    public PlayTimeSettings Settings
    {
        get => _file.Settings;
        set
        {
            _file.Settings = value with
            {
                PlayMinutes = Math.Max(1, value.PlayMinutes),
                BreakMinutes = Math.Max(1, value.BreakMinutes),
                DailyMaxMinutes = value.DailyMaxMinutes is > 0 ? value.DailyMaxMinutes : null,
            };
            Save();
        }
    }

    public ParentPasscode? Passcode
    {
        get => _file.Passcode;
        set
        {
            _file.Passcode = value;
            Save();
        }
    }

    // Loading and saving

    private SaveFile Load()
    {
        if (!File.Exists(FilePath)) return new SaveFile();
        string json;
        try
        {
            json = File.ReadAllText(FilePath);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            LoadError = error.Message;
            return new SaveFile();
        }
        try
        {
            var file = BipJson.Decode<SaveFile>(json);
            file.Children.RemoveAll(c => c.Id == Guid.Empty);
            foreach (var child in file.Children) child.Avatar = ProfileRules.ValidAvatar(child.Avatar);
            return file;
        }
        catch (JsonException error)
        {
            // Keep the unreadable file so it can be recovered by hand, then start fresh.
            var backup = Path.Combine(_folder, $"unreadable-save-{_now().ToUnixTimeSeconds()}.json");
            try { File.Copy(FilePath, backup, overwrite: true); }
            catch (Exception copyError) when (copyError is IOException or UnauthorizedAccessException) { }
            LoadError = $"couldn't read {FileName} (copy kept at {backup}): {error.Message}";
            return new SaveFile();
        }
    }

    /// <summary>Writes the whole file. Returns false (and sets <see cref="LastError"/>) when it couldn't.</summary>
    public bool Save()
    {
        try
        {
            Directory.CreateDirectory(_folder);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, BipJson.Encode(_file));
            File.Move(temp, FilePath, overwrite: true);
            LastError = null;
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            LastError = error.Message;
            return false;
        }
    }

    // Helpers

    private SavedChild? Find(Guid id) => _file.Children.FirstOrDefault(c => c.Id == id);

    private Guid AddChildRecord(string name, int? age, string avatar, ChildProgress progress)
    {
        var child = new SavedChild
        {
            Id = Guid.NewGuid(),
            Name = name,
            Age = age,
            Avatar = avatar,
            SortOrder = _file.Children.Count == 0 ? 0 : _file.Children.Max(c => c.SortOrder) + 1,
            CreatedAt = _now(),
            Progress = progress,
        };
        _file.Children.Add(child);
        return child.Id;
    }

    private static int? ClampAge(int? age) =>
        age is { } a ? Math.Clamp(a, ProfileRules.YoungestAge, ProfileRules.OldestAge) : null;

    private static ChildSummary Summary(SavedChild c) => new(c.Id, c.Name, c.Age, ProfileRules.ValidAvatar(c.Avatar));
}
