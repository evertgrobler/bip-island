namespace BipIsland.Drawing;

/// <summary>
/// SplitMix64, the same generator as the Swift app's SeededGenerator, so a hand-drawn shape
/// wobbles the same way every time it is drawn.
/// </summary>
public struct SeededRandom
{
    private ulong _state;

    public SeededRandom(ulong seed) => _state = seed;

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

    /// <summary>A number from lower to upper (inclusive), from the top 53 bits.</summary>
    public double Range(double lower, double upper) =>
        lower + (upper - lower) * ((Next() >> 11) * (1.0 / (1UL << 53)));
}
