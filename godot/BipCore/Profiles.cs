namespace BipCore;

/// <summary>
/// Rules for the children's profiles: up to four on one computer, each with a name a parent types,
/// an optional age (which sets the starting band) and an animal picture a pre-reader can spot.
/// </summary>
public static class ProfileRules
{
    public const int MaxChildren = 4;
    public const int MaxNameLength = 20;
    public const int YoungestAge = 3;
    public const int OldestAge = 10;
    /// <summary>Animal pictures to choose from, in the order new profiles get them.</summary>
    public static readonly IReadOnlyList<string> Avatars = ["lion", "penguin", "tortoise", "zebra", "giraffe", "elephant", "crab", "rhino"];

    /// <summary>A tidy name: trimmed, single spaces, at most <see cref="MaxNameLength"/> characters. Null when empty.</summary>
    public static string? CleanName(string raw)
    {
        var joined = string.Join(' ', raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (joined.Length == 0) return null;
        var info = new System.Globalization.StringInfo(joined);
        var cut = info.LengthInTextElements > MaxNameLength ? info.SubstringByTextElements(0, MaxNameLength) : joined;
        return cut.Trim();
    }

    /// <summary>The first animal nobody is using yet (or the first one if all are taken).</summary>
    public static string FreeAvatar(IEnumerable<string> used)
    {
        var taken = used.ToHashSet();
        return Avatars.FirstOrDefault(a => !taken.Contains(a)) ?? Avatars[0];
    }

    public static bool CanAdd(int currentCount) => currentCount < MaxChildren;

    /// <summary>A known animal, or the first one for anything unexpected in an old save.</summary>
    public static string ValidAvatar(string? id) => id is not null && Avatars.Contains(id) ? id : Avatars[0];
}
