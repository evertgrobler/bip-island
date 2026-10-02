namespace BipCore;

/// <summary>The adult maths question behind the parent gate. Hard enough that a 4–8-year-old won't guess it.</summary>
public sealed record ParentChallenge(int Left, int Right)
{
    public int Answer => Left * Right;
    public string Question => $"{Left} × {Right}";

    public static ParentChallenge Random(IRandomSource rng) => new(rng.NextInt(12, 19), rng.NextInt(3, 9));

    public bool IsCorrect(string input)
    {
        var digits = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
        return digits.Length > 0 && digits.All(char.IsAsciiDigit) && int.TryParse(digits, out var n) && n == Answer;
    }
}

/// <summary>
/// Detects a key held down for a set time (hold Esc for 3 seconds to open the parent gate).
/// Times are in seconds from any steady clock.
/// </summary>
public sealed class HoldDetector(double duration = 3)
{
    public double Duration { get; } = duration;
    public double? PressedAt { get; private set; }

    public bool IsHeld => PressedAt is not null;

    /// <summary>Key down. Auto-repeat presses while already held don't restart the clock.</summary>
    public void Press(double time) => PressedAt ??= time;

    public void Release() => PressedAt = null;

    /// <summary>0…1 of the way to opening.</summary>
    public double Progress(double time)
    {
        if (PressedAt is not double pressedAt || Duration <= 0) return 0;
        return Math.Clamp((time - pressedAt) / Duration, 0, 1);
    }

    public bool IsComplete(double time) => Progress(time) >= 1;
}
