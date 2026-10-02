using BipCore;

namespace BipCore.Tests;

/// <summary>The real Content/ folder from the repo, loaded once for all tests.</summary>
internal static class TestContent
{
    public static readonly string RepoRoot = FindRepoRoot();
    public static readonly string Directory = Path.Combine(RepoRoot, "Content");

    private static readonly Lazy<ContentLibrary> Cached = new(() => new ContentLibrary(Directory));

    public static ContentLibrary Library() => Cached.Value;

    /// <summary>Rules for tests that don't depend on the content's numbers.</summary>
    public static readonly MasteryRules Rules = new()
    {
        CorrectInARowToMoveUp = 3, MissesInARowToDropBack = 2, MasteredWindow = 10,
        MasteredPercent = 80, MasteredDistinctDays = 2, ReviewAfterDays = [2, 5, 14],
    };

    /// <summary>Walks up from the test build folder to the folder that holds Content/.</summary>
    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Content", "curriculum", "games.json"))) return dir.FullName;
        }
        throw new InvalidOperationException("Couldn't find the repo's Content folder above " + AppContext.BaseDirectory);
    }
}
