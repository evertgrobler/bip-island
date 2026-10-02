using System.Security.Cryptography;
using System.Text;

namespace BipCore;

/// <summary>
/// An optional parent passcode for the parent gate, so grown-ups don't have to solve a maths
/// question every time. Only a salted hash is stored, never the digits themselves. Saved as
/// {"salt", "hash"} with a SHA-256 hex hash, the same as the Swift app, so a passcode carries over.
/// </summary>
public sealed record ParentPasscode
{
    public const int ShortestCode = 4;
    public const int LongestCode = 8;
    /// <summary>Wrong tries before the gate falls back to a maths question, so a child can't keep guessing.</summary>
    public const int TriesBeforeMaths = 3;

    public required string Salt { get; init; }
    public required string Hash { get; init; }

    /// <summary>A passcode from 4–8 digits, or null when the code isn't that.</summary>
    public static ParentPasscode? Create(string code, string? salt = null)
    {
        if (Normalised(code) is not string digits) return null;
        salt ??= Guid.NewGuid().ToString().ToUpperInvariant();
        return new ParentPasscode { Salt = salt, Hash = Digest(salt + digits) };
    }

    public bool Matches(string code) => Normalised(code) is string digits && Digest(Salt + digits) == Hash;

    /// <summary>Digits only (spaces ignored), and the right length. Only plain 0–9 count.</summary>
    public static string? Normalised(string code)
    {
        var digits = new string(code.Where(c => !char.IsWhiteSpace(c)).ToArray());
        return digits.Length is >= ShortestCode and <= LongestCode && digits.All(char.IsAsciiDigit) ? digits : null;
    }

    private static string Digest(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
