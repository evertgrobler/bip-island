using System.Text.Json;
using System.Text.Json.Serialization;

namespace BipCore;

/// <summary>
/// The language a child plays in (owner decision, 18 October 2026: an Afrikaans side next to the
/// English one; plan in docs/LANGUAGES.md). Each child has one; a grown-up sets it.
/// </summary>
public enum HomeLanguage
{
    English,
    Afrikaans,
}

public static class Languages
{
    /// <summary>
    /// Where a language's own content and clips live: Content/&lt;folder&gt;/ over Content/, and
    /// Resources/Audio/&lt;folder&gt;/. English is the base, so it has none.
    /// </summary>
    public static string Folder(HomeLanguage language) => language switch
    {
        HomeLanguage.Afrikaans => "af",
        _ => "",
    };

    /// <summary>
    /// The file that says a language's content is ready to play (Content/af/language.json). Until it
    /// exists the parent area doesn't offer the language, so a half-written language never reaches a child.
    /// </summary>
    public const string MarkerFile = "language.json";

    /// <summary>
    /// The language a child learns on the Language Island: the one that isn't their home language
    /// (an English child learns Afrikaans, an Afrikaans child learns English).
    /// </summary>
    public static HomeLanguage Learning(HomeLanguage home) =>
        home == HomeLanguage.Afrikaans ? HomeLanguage.English : HomeLanguage.Afrikaans;

    /// <summary>The language's own name, as the parent area lists it.</summary>
    public static string Name(HomeLanguage language) => language switch
    {
        HomeLanguage.Afrikaans => "Afrikaans",
        _ => "English",
    };

    /// <summary>
    /// The path to read for a content file in a language: its own copy when it has one, else the
    /// English file (data with no words in it, like the coding levels, is shared).
    /// </summary>
    public static string ContentPath(HomeLanguage language, string file, Func<string, bool> exists)
    {
        var folder = Folder(language);
        if (folder.Length == 0) return file;
        var own = $"{folder}/{file}";
        return exists(own) ? own : file;
    }
}

/// <summary>
/// Reads a language like the music level: an unknown value (from a newer game) reads as English, so
/// one odd setting never costs a child their progress.
/// </summary>
internal sealed class LenientHomeLanguageConverter : JsonConverter<HomeLanguage>
{
    public override HomeLanguage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String
            && Enum.TryParse<HomeLanguage>(reader.GetString(), ignoreCase: true, out var language)
            && Enum.IsDefined(language)
            && !int.TryParse(reader.GetString(), out _))
            return language;
        reader.Skip();
        return HomeLanguage.English;
    }

    public override void Write(Utf8JsonWriter writer, HomeLanguage value, JsonSerializerOptions options) =>
        writer.WriteStringValue(JsonNamingPolicy.CamelCase.ConvertName(value.ToString()));
}
