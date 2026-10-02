using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>The save file: children, progress, the break, settings and the passcode.</summary>
public sealed class SaveStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bip-save-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    private SaveStore Open() => new(_folder);

    [Fact]
    public void AFirstLaunchHasPlayerOne()
    {
        var store = Open();
        var children = store.Children();
        Assert.Single(children);
        Assert.Equal("Player 1", children[0].Name);
        Assert.Equal(ProfileRules.Avatars[0], children[0].Avatar);
        Assert.Equal(children[0].Id, store.LastChildId());
        Assert.True(File.Exists(store.FilePath));
    }

    [Fact]
    public void ProgressSurvivesReopening()
    {
        var store = Open();
        var id = store.LastChildId();
        var progress = store.Progress(id);
        progress.RecordAnswer(true, "snd_g1", "s", 5, TestContent.Rules);
        progress.ClaimMysteryBox(5);
        store.Save(progress, id);

        var reopened = Open().Progress(id);
        Assert.Equal(progress, reopened);
        Assert.Equal(progress.Stars, reopened.Stars);
        Assert.Equal(5, reopened.LastMysteryDay);
    }

    [Fact]
    public void ProgressIsCopiedNotShared()
    {
        var store = Open();
        var id = store.LastChildId();
        var progress = store.Progress(id);
        progress.ClaimMysteryBox(1);
        // Not saved, so the store still has the old progress.
        Assert.Null(store.Progress(id).LastMysteryDay);
    }

    [Fact]
    public void AtMostFourChildrenWithCleanNamesAndFreePictures()
    {
        var store = Open();
        Assert.Null(store.AddChild("   ", 5, null));
        var ids = new[] { store.AddChild("  Lily   Rose ", 5, null), store.AddChild("Sam", 99, null), store.AddChild("Ava", 2, "crab") };
        Assert.All(ids, id => Assert.NotNull(id));
        Assert.Null(store.AddChild("Fifth", 6, null));

        var children = Open().Children();
        Assert.Equal(["Player 1", "Lily Rose", "Sam", "Ava"], children.Select(c => c.Name));
        Assert.Equal(4, children.Select(c => c.Avatar).Distinct().Count());
        Assert.Equal(ProfileRules.OldestAge, children[2].Age);
        Assert.Equal(ProfileRules.YoungestAge, children[3].Age);
        Assert.Equal("crab", children[3].Avatar);
    }

    [Fact]
    public void UpdatingKeepsTheNameWhenTheNewOneIsEmpty()
    {
        var store = Open();
        var child = store.Children()[0];
        store.Update(child with { Name = "  ", Age = 6, Avatar = "not-an-animal" });
        var updated = Open().Children()[0];
        Assert.Equal("Player 1", updated.Name);
        Assert.Equal(6, updated.Age);
        Assert.Equal(ProfileRules.Avatars[0], updated.Avatar);
    }

    [Fact]
    public void TheLastChildCantBeDeletedAndDeletingForgetsTheLastChild()
    {
        var store = Open();
        var first = store.LastChildId();
        store.Delete(first);
        Assert.Single(store.Children());

        var second = store.AddChild("Sam", 6, null)!.Value;
        store.SetLastChild(second);
        store.Delete(second);
        var reopened = Open();
        Assert.Single(reopened.Children());
        Assert.Equal(first, reopened.LastChildId());
    }

    [Fact]
    public void TheBreakSettingsAndPasscodeAreKept()
    {
        var store = Open();
        var ends = new DateTimeOffset(2026, 10, 2, 9, 30, 0, TimeSpan.Zero);
        store.SaveBreak(new BreakState { PlayedSeconds = 30, BreakEndsAt = ends, DayStamp = 9, PlayedTodaySeconds = 600 });
        store.Settings = new PlayTimeSettings { PlayMinutes = 15, BreakMinutes = 10, DailyMaxMinutes = 0 };
        store.Passcode = ParentPasscode.Create("2468", salt: "salt");

        var reopened = Open();
        var saved = reopened.LoadBreak(legacy: null)!;
        Assert.Equal(ends, saved.BreakEndsAt);
        Assert.Equal(600, saved.PlayedTodaySeconds);
        Assert.Equal(15, reopened.Settings.PlayMinutes);
        Assert.Null(reopened.Settings.DailyMaxMinutes);
        Assert.True(reopened.Passcode!.Matches("2468"));
    }

    [Fact]
    public void AnOlderPerChildBreakCarriesOver()
    {
        var legacy = new BreakState { PlayedSeconds = 120, DayStamp = 3 };
        Assert.Same(legacy, Open().LoadBreak(legacy));
    }

    [Fact]
    public void AnUnreadableSaveIsKeptAsACopyAndTheGameStartsFresh()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, SaveStore.FileName), "{ this is not json");
        var store = Open();
        Assert.Single(store.Children());
        Assert.Contains("couldn't read", store.LoadError ?? "");
        Assert.Null(store.LastError);
        var backup = Assert.Single(Directory.GetFiles(_folder, "unreadable-save-*.json"));
        Assert.Equal("{ this is not json", File.ReadAllText(backup));
    }

    [Fact]
    public void NoTemporaryFileIsLeftBehind()
    {
        var store = Open();
        store.AddChild("Sam", 6, null);
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public void ALockedSaveIsNeverWrittenOver()
    {
        var first = Open();
        first.AddChild("Sam", 6, null);
        var before = File.ReadAllText(first.FilePath);

        var locked = new SaveStore(_folder, readFile: _ => throw new IOException("in use by another process"));
        Assert.True(locked.SavingPaused);
        Assert.Contains("couldn't open", locked.LoadError ?? "");
        Assert.Single(locked.Children()); // The game still plays, as Player 1.
        locked.AddChild("Lily", 5, null);
        Assert.False(locked.Save());
        Assert.Equal(before, File.ReadAllText(first.FilePath));

        Assert.Equal(2, Open().Children().Count); // Next launch reads it again.
    }

    [Fact]
    public void WhenNoCopyCanBeKeptABrokenSaveIsLeftAlone()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, SaveStore.FileName);
        File.WriteAllText(path, "{ broken");
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        // A folder where the copy would go makes keeping the copy fail.
        Directory.CreateDirectory(Path.Combine(_folder, $"unreadable-save-{now.ToUnixTimeSeconds()}.json"));

        var store = new SaveStore(_folder, () => now);
        Assert.True(store.SavingPaused);
        Assert.False(store.Save());
        Assert.Equal("{ broken", File.ReadAllText(path));
    }

    [Fact]
    public void NullsInASaveAreTreatedAsEmpty()
    {
        Directory.CreateDirectory(_folder);
        var id = Guid.NewGuid();
        File.WriteAllText(Path.Combine(_folder, SaveStore.FileName),
            $$"""{"version":1,"children":[{"id":"{{id}}","name":null,"progress":null},null],"settings":null}""");
        var store = Open();
        Assert.Null(store.LoadError);
        Assert.Equal(id, Assert.Single(store.Children()).Id);
        Assert.Equal(0, store.Progress(id).Stars);
        Assert.Equal(20, store.Settings.PlayMinutes);

        File.WriteAllText(store.FilePath, """{"version":1,"children":null}""");
        Assert.Equal("Player 1", Assert.Single(Open().Children()).Name);
    }
}
