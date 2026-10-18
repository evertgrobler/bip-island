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
    public void ASavedLanguageThisBuildDoesntHavePlaysInEnglish()
    {
        HomeLanguage[] englishOnly = [HomeLanguage.English];
        Assert.Equal(HomeLanguage.English, Languages.Playable(HomeLanguage.Afrikaans, englishOnly));
        Assert.Equal(HomeLanguage.Afrikaans, Languages.Playable(HomeLanguage.Afrikaans, [HomeLanguage.English, HomeLanguage.Afrikaans]));
        Assert.Equal(HomeLanguage.English, Languages.Playable(HomeLanguage.English, []));
    }

    [Fact]
    public void ABrokenLanguageFileIsNamedByItsRealPath()
    {
        var root = Path.Combine(_folder, "Content");
        CopyDirectory(TestContent.Directory, root);
        Directory.CreateDirectory(Path.Combine(root, "af", "phonics"));
        File.WriteAllText(Path.Combine(root, "af", "phonics", "graphemes.json"), "{ not json");

        var error = Assert.Throws<ContentLoadException>(() => new ContentLibrary(
            file => File.ReadAllText(Path.Combine(root, file)), HomeLanguage.Afrikaans, file => File.Exists(Path.Combine(root, file))));
        Assert.Equal("af/phonics/graphemes.json", error.File);
        Assert.StartsWith("Couldn't read Content/af/phonics/graphemes.json", error.Message);
    }

    [Fact]
    public void EachLanguageKeepsItsOwnLearningAndSharesTheStars()
    {
        var rules = TestContent.Library().MasteryRules;
        var store = new SaveStore(_folder);
        var id = store.Children()[0].Id;

        // English: a sound, a skill and a game level, and the stars they earn.
        var english = store.Progress(id);
        english.RecordAnswer(true, "snd_g1", "s", 1, rules);
        english.RecordGameAnswer(true, "word_rocket", 0, 4, rules);
        english.NotePlayed("sound_hunt");
        store.Save(english, id);

        // Switched to Afrikaans: the same stars and recent games, but nothing learnt yet.
        var afrikaans = store.Progress(id, HomeLanguage.Afrikaans);
        Assert.Equal(english.Stars, afrikaans.Stars);
        Assert.Equal(["sound_hunt"], afrikaans.RecentGames);
        Assert.Empty(afrikaans.Skills);
        Assert.Empty(afrikaans.Sounds.Skills);
        Assert.Empty(afrikaans.GameLevels);

        afrikaans.RecordAnswer(true, "snd_g2", "m", 2, rules);
        afrikaans.RecordAnswer(true, "snd_g2", "m", 2, rules);
        store.Save(afrikaans, id, HomeLanguage.Afrikaans);

        // Back in English: English's learning as it was, the stars from both.
        var reopened = new SaveStore(_folder);
        var back = reopened.Progress(id);
        Assert.Equal(english.Stars + 2, back.Stars);
        Assert.Equal(new[] { "snd_g1" }, back.Skills.Keys);
        Assert.Equal(new[] { "s" }, back.Sounds.Skills.Keys);
        Assert.Equal(new[] { "word_rocket" }, back.GameLevels.Keys);
        Assert.Null(back.OtherLanguages);
        Assert.Equal(new[] { "snd_g2" }, reopened.Progress(id, HomeLanguage.Afrikaans).Skills.Keys);
        Assert.Equal(2, reopened.Progress(id, HomeLanguage.Afrikaans).Skill("snd_g2").TotalAttempts);

        // English play after that keeps the Afrikaans learning.
        back.RecordAnswer(true, "snd_g1", "s", 3, rules);
        reopened.Save(back, id);
        Assert.Equal(2, new SaveStore(_folder).Progress(id, HomeLanguage.Afrikaans).Skill("snd_g2").TotalAttempts);
    }

    [Fact]
    public void AnOlderSavesProgressIsEnglish()
    {
        var rules = TestContent.Library().MasteryRules;
        var store = new SaveStore(_folder);
        var id = store.Children()[0].Id;
        var progress = store.Progress(id);
        progress.RecordAnswer(true, "snd_g1", "s", 1, rules);
        store.Save(progress, id);
        // Progress written before languages (the shape older builds save) reads as English.
        var older = ChildProgress.Decode("""
            {"version":2,"sounds":{"skills":{"s":{"level":2,"correctStreak":1,"missStreak":0}}},
             "skills":{"snd_g1":{"recent":[{"correct":true,"day":9403}],"totalAttempts":1,"reviewsDone":0}},
             "recentGames":["sound_hunt"],"stars":7,"gameLevels":{"count_and_tap":{"level":2,"correctStreak":0,"missStreak":0}}}
            """);
        Assert.Equal(SoundStage.Recognises, older.InLanguage(HomeLanguage.English).Sounds.Stage("s"));
        Assert.Equal(2, older.InLanguage(HomeLanguage.English).GameLevel("count_and_tap", 0));
        Assert.Equal(SoundStage.New, older.InLanguage(HomeLanguage.Afrikaans).Sounds.Stage("s"));
        Assert.Equal(0, older.InLanguage(HomeLanguage.Afrikaans).GameLevel("count_and_tap", 0));
        Assert.Equal(7, older.InLanguage(HomeLanguage.Afrikaans).Stars);

        // An English-only save has the same shape as one from before languages: no "otherLanguages" at all.
        Assert.DoesNotContain("otherLanguages", File.ReadAllText(store.FilePath));

        var reopened = new SaveStore(_folder);
        Assert.Null(reopened.LoadError);
        Assert.Equal(new[] { "snd_g1" }, reopened.Progress(id).Skills.Keys);
        Assert.Equal(1, reopened.Progress(id).Stars);
        Assert.Empty(reopened.Progress(id, HomeLanguage.Afrikaans).Skills);
        Assert.Equal(1, reopened.Progress(id, HomeLanguage.Afrikaans).Stars);
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
                Texts.StarsInJar(language, 1), Texts.StarsToNextSticker(language, 2), Texts.ThisChild(language),
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
