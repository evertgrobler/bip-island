using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// The Language Island (docs/LANGUAGES.md): Show Me, What Is It? and Bip's Day deal fair rounds in the
/// language the child is learning, for an English child and an Afrikaans child, at every level.
/// </summary>
public sealed class LanguageIslandTests
{
    private static readonly Lazy<ContentLibrary> CachedAf = new(() =>
        new ContentLibrary(file => File.ReadAllText(Path.Combine(TestContent.Directory, file)), HomeLanguage.Afrikaans,
                           file => File.Exists(Path.Combine(TestContent.Directory, file))));

    private static ContentLibrary En => TestContent.Library();
    private static ContentLibrary Af => CachedAf.Value;

    public static TheoryData<HomeLanguage> HomeLanguages => new() { HomeLanguage.English, HomeLanguage.Afrikaans };

    private static ContentLibrary Home(HomeLanguage language) => language == HomeLanguage.Afrikaans ? Af : En;

    private static Learner AtLevel(int level) => new(Band.Stage1, 1, new HashSet<string>(), null, GameLevel: level);

    [Fact]
    public void EachChildLearnsTheOtherLanguage()
    {
        Assert.Equal(HomeLanguage.Afrikaans, Languages.Learning(HomeLanguage.English));
        Assert.Equal(HomeLanguage.English, Languages.Learning(HomeLanguage.Afrikaans));
    }

    [Fact]
    public void EveryWordHasBothLanguagesAndItsOwnPicture()
    {
        var words = En.LanguageWords.Words;
        Assert.True(words.Count >= 100, $"only {words.Count} words");
        Assert.All(words, w =>
        {
            Assert.False(string.IsNullOrWhiteSpace(w.En));
            Assert.False(string.IsNullOrWhiteSpace(w.Af));
            Assert.Contains(w.Picture, En.PictureIds);
            Assert.Contains(w.Audio, En.AudioIds);
            Assert.Contains(w.Audio, Af.AudioIds);
        });
        Assert.Equal(words.Count, words.Select(w => w.Picture).Distinct().Count());
        // The Afrikaans clip says the Afrikaans word.
        var cat = words.First(w => w.Id == "cat");
        Assert.Equal("kat", Af.Manifest.Audio.First(a => a.Id == cat.Audio).Text);
        Assert.Equal("cat", En.Manifest.Audio.First(a => a.Id == cat.Audio).Text);
    }

    [Theory]
    [MemberData(nameof(HomeLanguages))]
    public void TheWordGamesDealFairRoundsAtEveryLevel(HomeLanguage home)
    {
        var content = Home(home);
        foreach (var game in new IMiniGame<LanguageWordRound, LanguageWord>[] { new ShowMeGame(content), new WhatIsItGame(content) })
        {
            for (var level = 0; level < game.Entry.Levels!.Count; level++)
            {
                var rng = new SeededGenerator((ulong)(level + 1));
                for (var visit = 0; visit < 5; visit++)
                {
                    var session = GameSession.Start(game, content.Games.Session, new HashSet<string>(), rng);
                    var rounds = 0;
                    while (session.NextRound(game, AtLevel(level), rng) is LanguageWordRound round)
                    {
                        rounds++;
                        Assert.Equal(Languages.Learning(home), round.Learning);
                        Assert.Equal(game.Entry.Levels[level].Choices, round.Choices.Count);
                        Assert.Single(game.CorrectChoices(round));
                        // Nothing in a round can be mistaken for the answer, in either language.
                        Assert.Equal(round.Choices.Count, round.Choices.Select(c => c.Af.ToLowerInvariant()).Distinct().Count());
                        Assert.Equal(round.Choices.Count, round.Choices.Select(c => c.En.ToLowerInvariant()).Distinct().Count());
                        Assert.Equal(round.Choices.Count, round.Choices.Select(c => c.Picture).Distinct().Count());
                        var open = LanguageWords.OpenAt(content, level).Select(w => w.Id).ToHashSet();
                        Assert.All(round.Choices, c => Assert.Contains(c.Id, open));
                    }
                    Assert.Equal(session.MaxRounds, rounds);
                }
            }
        }
    }

    [Fact]
    public void ThemesOpenLevelByLevel()
    {
        var first = LanguageWords.OpenAt(En, 0);
        var all = LanguageWords.OpenAt(En, 9);
        Assert.Contains(first, w => w.Theme == "colours");
        Assert.DoesNotContain(first, w => w.Theme == "animals");
        Assert.Equal(En.LanguageWords.Words.Count, all.Count);
    }

    [Theory]
    [MemberData(nameof(HomeLanguages))]
    public void BipsDayPairsEachMomentWithItsWordsInTheOtherLanguage(HomeLanguage home)
    {
        var game = new BipsDayGame(Home(home), Home(Languages.Learning(home)));
        var brush = game.Cards.Select(c => c.Card).First(c => c.Set == "morning" && c.N == 2);
        Assert.Equal(home == HomeLanguage.English ? "Brush your teeth" : "Borsel jou tande", brush.HomeText);
        Assert.Equal(home == HomeLanguage.English ? "Borsel jou tande" : "Brush your teeth", brush.LearningText);
        // Every card of every story is paired (the validator keeps the two languages' stories alike).
        Assert.Equal(En.Sequences.Sets.Sum(s => s.Cards.Count), game.Cards.Count);

        for (var level = 0; level < game.Entry.Levels!.Count; level++)
        {
            var rng = new SeededGenerator(11);
            var session = GameSession.Start(game, En.Games.Session, new HashSet<string>(), rng);
            var rounds = 0;
            while (session.NextRound(game, AtLevel(level), rng) is BipsDayGame.Round round)
            {
                rounds++;
                Assert.Equal(Languages.Learning(home), round.Learning);
                Assert.Equal(game.Entry.Levels[level].Choices, round.Choices.Count);
                Assert.Single(game.CorrectChoices(round));
                Assert.Equal(round.Choices.Count, round.Choices.Select(c => c.Set).Distinct().Count());
                // The level's stories only (games.json "storiesUpTo").
                if (game.Entry.Levels[level].StoriesUpTo is { } upTo)
                    Assert.All(round.Choices, c => Assert.True(game.Cards.First(x => x.Card == c).Band <= upTo));
            }
            Assert.Equal(session.MaxRounds, rounds);
        }
    }

    [Theory]
    [MemberData(nameof(HomeLanguages))]
    public void BipsDayNeverDealsTwoAnswersThatLookOrSoundRight(HomeLanguage home)
    {
        // Stories repeat moments: "Brush your teeth" is in the morning and at bedtime.
        var game = new BipsDayGame(Home(home), Home(Languages.Learning(home)));
        var cards = game.Cards.Select(c => c.Card).ToList();
        Assert.Contains(cards, a => cards.Any(b => a.Set != b.Set && a.LearningText == b.LearningText));
        for (var seed = 1UL; seed <= 60; seed++)
        {
            var rng = new SeededGenerator(seed);
            var level = (int)(seed % (ulong)game.Entry.Levels!.Count);
            var session = GameSession.Start(game, En.Games.Session, new HashSet<string>(), rng);
            while (session.NextRound(game, AtLevel(level), rng) is BipsDayGame.Round round)
            {
                Assert.Equal(round.Choices.Count, round.Choices.Select(c => c.LearningText.ToLowerInvariant()).Distinct().Count());
                Assert.Equal(round.Choices.Count, round.Choices.Select(c => c.HomeText.ToLowerInvariant()).Distinct().Count());
                Assert.Equal(round.Choices.Count, round.Choices.Select(c => c.Picture).Distinct().Count());
            }
        }
    }

    [Fact]
    public void LanguageSkillsAreNeverKnownByAge()
    {
        // A 7-year-old who has never heard Afrikaans hasn't learnt its first words by growing up.
        var report = new ProgressReport(En, new ChildProgress(), Band.Stage2, today: 100);
        var language = report.Islands.Single(i => i.Island == Island.Language);
        Assert.Equal(3, language.Skills.Count);
        Assert.All(language.Skills, s => Assert.NotEqual(SkillStatus.KnownByAge, s.Status));
        Assert.Equal(0, language.DoneCount);
        Assert.All(En.Skills.Skills.Where(s => s.Island == Island.Language), s => Assert.False(new ChildProgress().IsTreatedAsKnown(s, Band.Stage3)));
        // Home-language skills below the starting band still are.
        Assert.Contains(report.Islands.SelectMany(i => i.Skills), s => s.Status == SkillStatus.KnownByAge);
    }

    [Fact]
    public void AClosedLanguageIslandStaysOutOfTheReportAndTheStickerBook()
    {
        var progress = new ChildProgress();
        Island[] closed = [Island.Language];
        var open = new ProgressReport(En, progress, Band.Foundation, today: 100);
        var hidden = new ProgressReport(En, progress, Band.Foundation, today: 100, closed);
        Assert.Contains(open.Islands, i => i.Island == Island.Language);
        Assert.DoesNotContain(hidden.Islands, i => i.Island == Island.Language);
        Assert.DoesNotContain(hidden.Islands.SelectMany(i => i.Skills), s => s.Island == Island.Language);
        Assert.Equal(open.Islands.Count - 1, hidden.Islands.Count);
        Assert.Equal(open.StickersTotal - 3, hidden.StickersTotal);

        Assert.Contains("skill_lang_listen", StickerBook.AllStickers(En));
        Assert.DoesNotContain(StickerBook.AllStickers(En, closed), s => s.StartsWith("skill_lang_", StringComparison.Ordinal));
        Assert.DoesNotContain(StickerBook.Missing(progress, En, closed), s => s.StartsWith("skill_lang_", StringComparison.Ordinal));
        Assert.DoesNotContain(StickerBook.Skills(En, closed), s => s.Island == Island.Language);
    }

    [Fact]
    public void BipRecommendsTheHomeLanguageCourseOnly()
    {
        var recommender = new PlayRecommender(En);
        var progress = new ChildProgress();
        for (var day = 0; day < 20; day++)
        {
            var suggestion = recommender.Suggest(progress, Band.Foundation, day, En.MasteryRules, new SeededGenerator((ulong)day));
            Assert.NotNull(suggestion);
            Assert.NotEqual(Island.Language, suggestion!.Island);
        }
    }
}
