using System.Text.Json;
using System.Text.Json.Nodes;
using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>Every content file decodes, and the C# models keep every key in it.</summary>
public sealed class ContentLoaderTests : IDisposable
{
    private readonly List<string> _copies = [];

    public void Dispose()
    {
        foreach (var copy in _copies)
        {
            try { Directory.Delete(copy, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void TheWholeLibraryLoads()
    {
        var content = TestContent.Library();
        Assert.NotEmpty(content.Objectives.Objectives);
        Assert.NotEmpty(content.Skills.Skills);
        Assert.NotEmpty(content.Games.Games);
        Assert.NotEmpty(content.Phonics.Graphemes);
        Assert.NotEmpty(content.Words.Words);
        Assert.NotEmpty(content.TrickyWords.Stage1.Words);
        Assert.NotEmpty(content.Sentences.Sentences);
        Assert.NotEmpty(content.Endings.PluralsS);
        Assert.NotEmpty(content.Homophones.Sets);
        Assert.NotEmpty(content.Contractions.Pairs);
        Assert.NotEmpty(content.Numbers.CountingObjects);
        Assert.NotEmpty(content.Sequences.Sets);
        Assert.NotEmpty(content.Patterns.Rules);
        Assert.NotEmpty(content.Levels.Levels);
        Assert.NotEmpty(content.Manifest.Audio);
    }

    [Fact]
    public void EveryFileDecodesWithoutLosingAnyKey()
    {
        CheckRoundTrip<ObjectivesFile>("curriculum/objectives.json");
        CheckRoundTrip<SkillsFile>("curriculum/skills.json");
        CheckRoundTrip<GamesFile>("curriculum/games.json");
        CheckRoundTrip<GraphemesFile>("phonics/graphemes.json");
        CheckRoundTrip<WordsFile>("words/words.json");
        CheckRoundTrip<TrickyWordsFile>("words/tricky_words.json");
        CheckRoundTrip<SentencesFile>("words/sentences.json");
        CheckRoundTrip<EndingsFile>("words/endings.json");
        CheckRoundTrip<HomophonesFile>("words/homophones.json");
        CheckRoundTrip<ContractionsFile>("words/contractions.json");
        CheckRoundTrip<NumbersFile>("numbers/numbers.json");
        CheckRoundTrip<SequencesFile>("coding/sequences.json");
        CheckRoundTrip<PatternsFile>("coding/patterns.json");
        CheckRoundTrip<LevelsFile>("coding/levels.json");
        CheckRoundTrip<AssetManifestFile>("asset_manifest.json");
    }

    /// <summary>A new JSON file in Content/ must get a model and be added to the loader.</summary>
    [Fact]
    public void TheLoaderReadsEveryContentFile()
    {
        var found = Directory.EnumerateFiles(TestContent.Directory, "*.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(TestContent.Directory, path).Replace('\\', '/'))
            .ToHashSet();
        Assert.Equal(ContentLibrary.Files.ToHashSet(), found);
    }

    [Fact]
    public void AMissingFileNamesTheFile()
    {
        var copy = CopyOfContent();
        File.Delete(Path.Combine(copy, "words/words.json"));
        var error = Assert.Throws<ContentLoadException>(() => new ContentLibrary(copy));
        Assert.Contains("words/words.json", error.Message);
    }

    [Fact]
    public void ABrokenFileNamesTheFileAndTheKey()
    {
        var copy = CopyOfContent();
        var file = Path.Combine(copy, "curriculum/games.json");
        File.WriteAllText(file, File.ReadAllText(file).Replace("\"buildPhase\"", "\"buildPhaze\""));
        var error = Assert.Throws<ContentLoadException>(() => new ContentLibrary(copy));
        Assert.Contains("curriculum/games.json", error.Message);
        Assert.Contains("buildPhase", error.Message);
    }

    /// <summary>The game reads content from inside its package through a reader function.</summary>
    [Fact]
    public void TheReaderLoadsTheSameContentAsTheFolder()
    {
        var read = new List<string>();
        var content = new ContentLibrary(file =>
        {
            read.Add(file);
            return File.ReadAllText(Path.Combine(TestContent.Directory, file));
        });
        Assert.Equal(ContentLibrary.Files.ToHashSet(), read.ToHashSet());
        Assert.Equal(TestContent.Library().Words.Words.Count, content.Words.Words.Count);
    }

    [Fact]
    public void AReaderThatFailsNamesTheFile()
    {
        var error = Assert.Throws<ContentLoadException>(() => new ContentLibrary(file =>
            file == "coding/levels.json" ? throw new FileNotFoundException(file) : File.ReadAllText(Path.Combine(TestContent.Directory, file))));
        Assert.Contains("coding/levels.json", error.Message);
    }

    [Fact]
    public void Lookups()
    {
        var content = TestContent.Library();
        Assert.Equal("sun", content.Grapheme("s")?.MnemonicWord);
        Assert.Equal(1, content.Word("sat")?.DecodableFromGroup);
        Assert.Equal(["snd_g1"], content.Skill("snd_g2")?.Prerequisites);
        Assert.Equal(Island.Letters, content.Game("sound_hunt")?.Island);
        Assert.Null(content.Word("spaza"));
        Assert.Contains("snd_s", content.AudioIds);
        Assert.Contains("pic_sun", content.PictureIds);
    }

    [Fact]
    public void ThePhaseOneGamesAreInTheRegistry()
    {
        var content = TestContent.Library();
        foreach (var id in new[] { MeetTheSoundGame.GameId, SoundHuntGame.GameId, BubblePopGame.GameId })
        {
            var entry = content.EntryForGame(id);
            Assert.Equal(Island.Letters, entry.Island);
            Assert.Equal(1, entry.BuildPhase);
            Assert.True(entry.Ages.Contains(4), id);
            foreach (var skill in entry.Skills) Assert.True(content.Skill(skill) is not null, $"{id}: {skill}");
        }
        Assert.Throws<UnknownGameException>(() => content.EntryForGame("not_a_game"));
    }

    [Fact]
    public void EveryGameAndSkillReferenceExists()
    {
        var content = TestContent.Library();
        var objectives = content.Objectives.Objectives.Select(o => o.Code).ToHashSet();
        foreach (var game in content.Games.Games)
        {
            Assert.True(game.Skills.All(id => content.Skill(id) is not null), game.Id);
            Assert.True(game.Objectives.All(objectives.Contains), game.Id);
        }
        foreach (var skill in content.Skills.Skills)
        {
            Assert.True(skill.Prerequisites.All(id => content.Skill(id) is not null), skill.Id);
        }
        foreach (var group in content.Phonics.Groups)
        {
            Assert.True(content.Skill(PhonicsCourse.SkillIdForGroup(group.Group)) is not null, $"group {group.Group}");
        }
    }

    [Fact]
    public void MasteryRulesComeFromTheContent()
    {
        // skills.json masteryRules should say 3 up, 2 back, 80% of 10 over 2 days, review 2/5/14.
        Assert.Equal(TestContent.Rules, TestContent.Library().MasteryRules);
    }

    [Fact]
    public void StartingBandByAge()
    {
        var content = TestContent.Library();
        Assert.Equal(Band.Foundation, content.StartingBand(3));
        Assert.Equal(Band.Foundation, content.StartingBand(4));
        Assert.Equal(Band.Foundation, content.StartingBand(5));
        Assert.Equal(Band.Stage1, content.StartingBand(6));
        Assert.Equal(Band.Stage2, content.StartingBand(7));
        Assert.Equal(Band.Stage3, content.StartingBand(8));
        Assert.Equal(Band.Stage3, content.StartingBand(11));
    }

    [Fact]
    public void AgeRangesAndGridPositionsDecode()
    {
        var range = BipJson.Decode<List<AgeRange>>("""["4-6"]""")[0];
        Assert.Equal(new AgeRange(4, 6), range);
        Assert.True(range.Contains(5));
        Assert.False(range.Contains(7));
        Assert.Throws<JsonException>(() => BipJson.Decode<List<AgeRange>>("""["four"]"""));
        Assert.Throws<JsonException>(() => BipJson.Decode<List<GridPosition>>("[[1, 2, 3]]"));
        Assert.True(Band.Foundation < Band.Stage1);
        Assert.True(Band.Stage3 > Band.Stage2);
    }

    // MARK: Helpers

    /// <summary>Decodes a file, encodes the model again, and checks nothing was dropped or changed.</summary>
    private static void CheckRoundTrip<T>(string file)
    {
        var text = File.ReadAllText(Path.Combine(TestContent.Directory, file));
        T model;
        try
        {
            model = BipJson.Decode<T>(text);
        }
        catch (JsonException error)
        {
            Assert.Fail($"{file} didn't decode: {error.Message}");
            return;
        }
        var original = JsonNode.Parse(text);
        var again = JsonNode.Parse(BipJson.Encode(model));
        if (!JsonNode.DeepEquals(original, again))
        {
            Assert.Fail($"{file}: the C# model loses or changes something. Add the missing fields to ContentModels.cs. "
                        + FirstDifference(original, again, ""));
        }
    }

    /// <summary>Where two JSON trees first differ, as a path like .games[3].levels.</summary>
    private static string FirstDifference(JsonNode? a, JsonNode? b, string path)
    {
        switch (a, b)
        {
            case (JsonObject left, JsonObject right):
                foreach (var key in left.Select(p => p.Key).Union(right.Select(p => p.Key)))
                {
                    if (!left.ContainsKey(key) || !right.ContainsKey(key)) return $"Key {path}.{key} is only on one side.";
                    if (!JsonNode.DeepEquals(left[key], right[key])) return FirstDifference(left[key], right[key], $"{path}.{key}");
                }
                break;
            case (JsonArray left, JsonArray right):
                if (left.Count != right.Count) return $"{path} has {left.Count} items, then {right.Count}.";
                for (var i = 0; i < left.Count; i++)
                {
                    if (!JsonNode.DeepEquals(left[i], right[i])) return FirstDifference(left[i], right[i], $"{path}[{i}]");
                }
                break;
        }
        return $"{path}: {a?.ToJsonString()} vs {b?.ToJsonString()}";
    }

    private string CopyOfContent()
    {
        var copy = Path.Combine(Path.GetTempPath(), "bip-content-" + Guid.NewGuid());
        foreach (var file in Directory.EnumerateFiles(TestContent.Directory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(copy, Path.GetRelativePath(TestContent.Directory, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        _copies.Add(copy);
        return copy;
    }
}
