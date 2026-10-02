namespace BipCore;

/// <summary>
/// Where the games get their randomness. The app uses <see cref="SystemRandomSource"/>; tests and the
/// hand-drawn wobble use <see cref="SeededGenerator"/> so the same seed always gives the same results.
/// </summary>
public interface IRandomSource
{
    /// <summary>64 random bits.</summary>
    ulong Next();
}

/// <summary>A small, fast, repeatable random number generator (SplitMix64), the same as the Swift app's.</summary>
public sealed class SeededGenerator(ulong seed) : IRandomSource
{
    private ulong _state = seed;

    public ulong Next()
    {
        unchecked
        {
            _state += 0x9E3779B97F4A7C15UL;
            var z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}

/// <summary>Real randomness, for the app.</summary>
public sealed class SystemRandomSource : IRandomSource
{
    public static readonly SystemRandomSource Shared = new();

    public ulong Next()
    {
        Span<byte> bytes = stackalloc byte[8];
        Random.Shared.NextBytes(bytes);
        return BitConverter.ToUInt64(bytes);
    }
}

/// <summary>The random helpers the games use: whole numbers in a range, coin flips, picks and shuffles.</summary>
public static class RandomExtensions
{
    /// <summary>A whole number from 0 up to (not including) <paramref name="count"/>, without bias.</summary>
    public static int NextIndex(this IRandomSource rng, int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Need at least one thing to pick from");
        var range = (ulong)count;
        // Reject the top sliver of values that would make some results more likely than others.
        var limit = ulong.MaxValue - ulong.MaxValue % range;
        ulong value;
        do value = rng.Next(); while (value >= limit);
        return (int)(value % range);
    }

    /// <summary>A whole number from <paramref name="lowest"/> to <paramref name="highest"/>, both included.</summary>
    public static int NextInt(this IRandomSource rng, int lowest, int highest)
    {
        if (highest < lowest) throw new ArgumentOutOfRangeException(nameof(highest), "The range is empty");
        return lowest + rng.NextIndex(highest - lowest + 1);
    }

    public static bool NextBool(this IRandomSource rng) => (rng.Next() >> 63) == 1;

    /// <summary>A random item, or null when the list is empty.</summary>
    public static T? Pick<T>(this IRandomSource rng, IReadOnlyList<T> items) where T : class =>
        items.Count == 0 ? null : items[rng.NextIndex(items.Count)];

    /// <summary>A random item, or false when the list is empty (for numbers and other plain values).</summary>
    public static bool TryPick<T>(this IRandomSource rng, IReadOnlyList<T> items, out T item)
    {
        if (items.Count == 0)
        {
            item = default!;
            return false;
        }
        item = items[rng.NextIndex(items.Count)];
        return true;
    }

    /// <summary>The items in a random order (Fisher–Yates).</summary>
    public static List<T> Shuffled<T>(this IRandomSource rng, IEnumerable<T> items)
    {
        var list = items.ToList();
        for (var i = 0; i < list.Count - 1; i++)
        {
            var j = i + rng.NextIndex(list.Count - i);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
