using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>Each child's home language: saved, read leniently, and choosing which content files load.</summary>
public sealed class LanguageTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bip-lang-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void AChildIsEnglishUnlessAGrownUpChoosesAfrikaans()
    {
        var store = new SaveStore(_folder);
        Assert.Equal(HomeLanguage.English, store.Children()[0].Language);

        var id = store.AddChild("Lena", 6, null, HomeLanguage.Afrikaans)!.Value;
        Assert.Equal(HomeLanguage.Afrikaans, new SaveStore(_folder).Children().Single(c => c.Id == id).Language);
    }

    [Fact]
    public void ALanguageChangeIsSaved()
    {
        var store = new SaveStore(_folder);
        var child = store.Children()[0];
        store.Update(child with { Language = HomeLanguage.Afrikaans });
        Assert.Equal(HomeLanguage.Afrikaans, new SaveStore(_folder).Children()[0].Language);
    }

    [Fact]
    public void AnOlderSaveOrAnUnknownLanguageReadsAsEnglish()
    {
        var store = new SaveStore(_folder);
        var id = store.Children()[0].Id;
        var path = store.FilePath;
        // A save from before languages (no "language" at all), then one from a newer game with a language this one doesn't know.
        var language = new System.Text.RegularExpressions.Regex("\"language\":\\s*\"[A-Za-z]+\",?\\s*");
        File.WriteAllText(path, language.Replace(File.ReadAllText(path), ""));
        Assert.Equal(HomeLanguage.English, new SaveStore(_folder).Children().Single(c => c.Id == id).Language);

        File.WriteAllText(path, language.Replace(File.ReadAllText(path), "").Replace("\"avatar\"", "\"language\": \"isiZulu\", \"avatar\""));
        var reopened = new SaveStore(_folder);
        Assert.Null(reopened.LastError);
        Assert.Equal(HomeLanguage.English, reopened.Children().Single(c => c.Id == id).Language);
    }

    [Fact]
    public void ALanguageReadsItsOwnFilesAndSharesTheRest()
    {
        var afrikaans = new HashSet<string> { "af/phonics/graphemes.json" };
        Assert.Equal("af/phonics/graphemes.json", Languages.ContentPath(HomeLanguage.Afrikaans, "phonics/graphemes.json", afrikaans.Contains));
        Assert.Equal("coding/levels.json", Languages.ContentPath(HomeLanguage.Afrikaans, "coding/levels.json", afrikaans.Contains));
        Assert.Equal("phonics/graphemes.json", Languages.ContentPath(HomeLanguage.English, "phonics/graphemes.json", _ => true));
    }

    [Fact]
    public void ContentLoadsInALanguageFromItsOwnFolder()
    {
        // An Afrikaans folder with only its own graphemes file: that file comes from af/, the rest is shared.
        var root = Path.Combine(_folder, "Content");
        CopyDirectory(TestContent.Directory, root);
        Directory.CreateDirectory(Path.Combine(root, "af", "phonics"));
        var graphemes = File.ReadAllText(Path.Combine(root, "phonics", "graphemes.json"));
        File.WriteAllText(Path.Combine(root, "af", "phonics", "graphemes.json"), graphemes.Replace("\"mnemonicWord\": \"sun\"", "\"mnemonicWord\": \"son\""));

        var library = new ContentLibrary(file => File.ReadAllText(Path.Combine(root, file)), HomeLanguage.Afrikaans,
                                         file => File.Exists(Path.Combine(root, file)));
        Assert.Equal(HomeLanguage.Afrikaans, library.Language);
        Assert.Equal("son", library.Grapheme("s")!.MnemonicWord);
        Assert.Equal(TestContent.Library().Levels.Levels.Count, library.Levels.Levels.Count);
        Assert.Equal("sun", TestContent.Library().Grapheme("s")!.MnemonicWord);
    }

    [Fact]
    public void EveryOnScreenTextHasBothLanguages()
    {
        foreach (var language in Enum.GetValues<HomeLanguage>())
        {
            Assert.All(new[]
            {
                Texts.WhoIsPlaying(language), Texts.ComingSoon(language), Texts.WellPlayed(language),
                Texts.NewStickers(language, 1), Texts.NewStickers(language, 2), Texts.Level(language, 2),
                Texts.BipIsCharging(language), Texts.AllForToday(language), Texts.StarsEarned(language, 3),
                Texts.StarsInJar(language, 1), Texts.StarsToNextSticker(language, 2),
            }, text => Assert.False(string.IsNullOrWhiteSpace(text)));
        }
        Assert.Equal("Wie speel?", Texts.WhoIsPlaying(HomeLanguage.Afrikaans));
        Assert.Equal("1 ster", Texts.Stars(HomeLanguage.Afrikaans, 1));
        Assert.Equal("+3 sterre", Texts.StarsEarned(HomeLanguage.Afrikaans, 3));
        Assert.Equal("2 more stars for the next sticker", Texts.StarsToNextSticker(HomeLanguage.English, 2));
    }

    private static void CopyDirectory(string from, string to)
    {
        foreach (var dir in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(from, to));
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(from, to), overwrite: true);
    }
}
