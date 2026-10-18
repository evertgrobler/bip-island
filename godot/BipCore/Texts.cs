namespace BipCore;

/// <summary>
/// The few words the game shows children on screen, in each home language. Everything a child must
/// understand is also spoken; these are for the grown-up beside them and for early readers. Afrikaans
/// follows the AWS (Afrikaanse Woordelys en Spelreëls).
/// </summary>
public static class Texts
{
    public static string WhoIsPlaying(HomeLanguage l) => Pick(l, "Who's playing?", "Wie speel?");
    public static string ComingSoon(HomeLanguage l) => Pick(l, "Coming soon!", "Kom binnekort!");
    public static string WellPlayed(HomeLanguage l) => Pick(l, "Well played!", "Mooi gespeel!");
    public static string NewStickers(HomeLanguage l, int count) =>
        count == 1 ? Pick(l, "New sticker!", "Nuwe plakker!") : Pick(l, "New stickers!", "Nuwe plakkers!");
    public static string Level(HomeLanguage l, int level) => Pick(l, $"Level {level}!", $"Vlak {level}!");
    public static string BipIsCharging(HomeLanguage l) => Pick(l, "Bip is charging", "Bip laai");
    public static string AllForToday(HomeLanguage l) => Pick(l, "That's all for today", "Dis al vir vandag");

    /// <summary>"+3 stars" / "+3 sterre".</summary>
    public static string StarsEarned(HomeLanguage l, int stars) => "+" + Stars(l, stars);

    /// <summary>"12 stars in the jar" / "12 sterre in die fles".</summary>
    public static string StarsInJar(HomeLanguage l, int stars) => Pick(l, $"{Stars(l, stars)} in the jar", $"{Stars(l, stars)} in die fles");

    /// <summary>"3 more stars for the next sticker" / "nog 3 sterre vir die volgende plakker".</summary>
    public static string StarsToNextSticker(HomeLanguage l, int stars) => Pick(l,
        $"{stars} more {(stars == 1 ? "star" : "stars")} for the next sticker",
        $"nog {stars} {(stars == 1 ? "ster" : "sterre")} vir die volgende plakker");

    public static string Stars(HomeLanguage l, int stars) => Pick(l,
        $"{stars} {(stars == 1 ? "star" : "stars")}",
        $"{stars} {(stars == 1 ? "ster" : "sterre")}");

    private static string Pick(HomeLanguage l, string english, string afrikaans) =>
        l == HomeLanguage.Afrikaans ? afrikaans : english;
}
