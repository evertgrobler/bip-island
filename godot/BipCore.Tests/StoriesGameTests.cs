using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// Bip's Stories: every story in both languages, at every level, deals fair questions (exactly one right
/// answer, no two choices alike), the levels grow as planned, and every clip a story plays is in its
/// language's manifest.
/// </summary>
public sealed class StoriesGameTests
{
    private static readonly Lazy<ContentLibrary> CachedAf = new(() =>
        new ContentLibrary(file => File.ReadAllText(Path.Combine(TestContent.Directory, file)), HomeLanguage.Afrikaans,
                           file => File.Exists(Path.Combine(TestContent.Directory, file))));

    public static TheoryData<HomeLanguage> HomeLanguages => new() { HomeLanguage.English, HomeLanguage.Afrikaans };

    private static ContentLibrary Content(HomeLanguage language) => language == HomeLanguage.Afrikaans ? CachedAf.Value : TestContent.Library();

    private static Learner AtLevel(int level) => new(Band.Stage2, 1, new HashSet<string>(), null, GameLevel: level);

    [Theory]
    [MemberData(nameof(HomeLanguages))]
    public void TwentyStoriesFourAtEachLevel(HomeLanguage language)
    {
        var stories = Content(language).Stories.Stories;
        Assert.Equal(20, stories.Count);
        for (var level = 1; level <= StoriesGame.Levels; level++) Assert.Equal(4, stories.Count(s => s.Level == level));
        Assert.Equal(stories.Count, stories.Select(s => s.Id).Distinct().Count());
        Assert.Equal(stories.Count, stories.Select(s => s.Scenes).Distinct().Count());
    }

    [Fact]
    public void BothLanguagesTellTheirOwnStoriesOnTheSameScenes()
    {
        var en = Content(HomeLanguage.English).Stories.Stories;
        var af = Content(HomeLanguage.Afrikaans).Stories.Stories;
        Assert.Equal(en.Select(s => s.Scenes).Order(), af.Select(s => s.Scenes).Order());
        foreach (var story in af)
        {
            var english = en.Single(s => s.Scenes == story.Scenes);
            Assert.Equal(english.Level, story.Level);
            Assert.NotEqual(english.Text, story.Text);
            Assert.Contains("owner", story.Note ?? "");
        }
    }

    [Theory]
    [MemberData(nameof(HomeLanguages))]
    public void TheLevelsGrowAsPlanned(HomeLanguage language)
    {
        var content = Content(language);
        var game = new StoriesGame(content);
        Assert.Equal(StoriesGame.Levels, game.Entry.Levels!.Count);
        foreach (var story in game.Stories)
        {
            var scenes = game.ScenesOf(story).Scenes.Count;
            Assert.Equal(scenes, story.Text.Count);
            var kinds = story.Questions.Select(q => q.Kind).ToList();
            switch (story.Level)
            {
                case 1:
                    Assert.Equal(2, scenes);
                    Assert.All(story.Text, t => Assert.Single(t));
                    Assert.Single(story.Questions);
                    Assert.All(story.Questions, q => Assert.Single(q.Others));
                    break;
                case 2:
                    Assert.Equal(3, scenes);
                    Assert.Equal(2, story.Questions.Count);
                    break;
                case 3:
                    Assert.Equal(3, scenes);
                    Assert.Equal(3, kinds.Count);
                    Assert.DoesNotContain("why", kinds);
                    Assert.DoesNotContain("feel", kinds);
                    break;
                case 4:
                    Assert.Equal(4, scenes);
                    Assert.Single(kinds, k => k == "why");
                    break;
                default:
                    Assert.InRange(scenes, 5, 6);
                    Assert.Single(kinds, k => k == "feel");
                    break;
            }
            // Picture answers until a child can read the words.
            if (story.Level <= 2) Assert.All(story.Questions, q => Assert.All(q.Others.Prepend(q.Answer), c => Assert.NotNull(c.Picture)));
            Assert.All(story.Questions, q => Assert.InRange(q.Scene, 1, scenes));
        }
    }

    [Theory]
    [MemberData(nameof(HomeLanguages))]
    public void EveryStoryDealsFairQuestionsAtEveryLevel(HomeLanguage language)
    {
        var content = Content(language);
        var game = new StoriesGame(content);
        for (var level = 0; level < StoriesGame.Levels; level++)
        {
            var asked = new HashSet<string>();
            var rng = new SeededGenerator((ulong)(level + 7));
            for (var visit = 0; visit < 60; visit++)
            {
                var session = GameSession.Start(game, content.Games.Session, new HashSet<string>(), rng);
                var rounds = new List<StoriesGame.Round>();
                while (session.NextRound(game, AtLevel(level), rng) is StoriesGame.Round round) rounds.Add(round);
                Assert.InRange(rounds.Count, StoriesGame.QuestionsPerVisit, StoriesGame.QuestionsPerVisit + 1);
                Assert.True(rounds[0].NewStory);
                foreach (var round in rounds)
                {
                    asked.Add(round.Story.Id);
                    Assert.Equal(StoriesGame.StoryLevel(level), round.Story.Level);
                    Assert.Single(game.CorrectChoices(round));
                    Assert.Equal(round.Choices.Count, round.Choices.Select(c => c.Text.ToLowerInvariant()).Distinct().Count());
                    var pictures = round.Choices.Select(c => c.Picture).Where(p => p != null).ToList();
                    Assert.Equal(pictures.Count, pictures.Distinct().Count());
                    Assert.Same(round.Story.Questions[round.Number - 1], round.Question);
                }
                // Each story's questions come in order, all of them, and a new story starts only after the last.
                foreach (var story in rounds.GroupBy(r => r.Story.Id))
                {
                    Assert.Equal(Enumerable.Range(1, story.First().Story.Questions.Count), story.Select(r => r.Number));
                    Assert.Single(story, r => r.NewStory);
                }
            }
            // Every story at the level comes up.
            Assert.Equal(game.Stories.Where(s => s.Level == StoriesGame.StoryLevel(level)).Select(s => s.Id).ToHashSet(), asked);
        }
    }

    [Fact]
    public void AnEasierStoryWhenTheLevelHasNoneLeft()
    {
        var game = new StoriesGame(Content(HomeLanguage.English));
        var fresh = new GameSession(StoriesGame.GameId, game.Skins[0], 20);
        Assert.Equal(3, game.MakeRound(AtLevel(2), fresh, new SeededGenerator(3))!.Story.Level);
        // Every level-3 story already read, all its questions asked: a level-2 story instead.
        var used = game.Stories.Where(s => s.Level == 3)
            .SelectMany(s => Enumerable.Range(1, s.Questions.Count).Select(n => StoriesGame.QuestionItem(s, n)).Prepend(StoriesGame.StoryItem(s)));
        var next = game.MakeRound(AtLevel(2), SessionWith(game, used), new SeededGenerator(3));
        Assert.Equal(2, next!.Story.Level);
        Assert.True(next.NewStory);
    }

    /// <summary>A visit that has already used these items (stories read, questions asked).</summary>
    private static GameSession SessionWith(StoriesGame game, IEnumerable<string> used)
    {
        var session = new GameSession(StoriesGame.GameId, game.Skins[0], 20);
        var items = (HashSet<string>)typeof(GameSession).GetField("_usedItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(session)!;
        items.UnionWith(used);
        return session;
    }

    [Theory]
    [MemberData(nameof(HomeLanguages))]
    public void EveryClipAStoryPlaysIsInItsLanguagesManifest(HomeLanguage language)
    {
        var content = Content(language);
        foreach (var story in content.Stories.Stories)
        {
            foreach (var clip in StoriesGame.SentenceClips(story).SelectMany(c => c))
                Assert.True(content.AudioIds.Contains(clip), $"{language}: {clip}");
            for (var n = 1; n <= story.Questions.Count; n++)
                Assert.Contains(StoriesGame.QuestionClip(story, n), content.AudioIds);
            foreach (var choice in story.Questions.SelectMany(q => q.Others.Prepend(q.Answer)))
            {
                var clip = StoriesGame.AnswerClip(story, choice, content.AudioIds);
                Assert.True(content.AudioIds.Contains(clip), $"{language}: {clip} for {choice.Text}");
                Assert.True(AudioCatalogue.FollowsNamingContract(clip), clip);
            }
            Assert.All(content.StoryScenes.Sets.Single(s => s.Id == story.Scenes).Scenes.SelectMany(s => s.Pictures),
                       p => Assert.Contains(p.Picture, TestContent.Library().PictureIds));
        }
        // A word that has its own clip reuses it: "dog" is word_dog.
        var muddy = content.Stories.Stories.First(s => s.Level == 1);
        var answer = muddy.Questions[0].Answer;
        Assert.StartsWith("word_", StoriesGame.AnswerClip(muddy, answer, content.AudioIds));
    }

    [Fact]
    public void WhyAndFeelQuestionsPractiseReadingBetweenTheLines()
    {
        var content = Content(HomeLanguage.English);
        var game = new StoriesGame(content);
        var story = game.Stories.First(s => s.Level == 5);
        var feel = story.Questions.First(q => q.Kind == "feel");
        var round = new StoriesGame.Round(story, game.ScenesOf(story), feel, story.Questions.IndexOf(feel) + 1, [feel.Answer], false);
        Assert.Equal("story_inference", game.SkillId(round));
        var who = story.Questions.First(q => q.Kind is not ("why" or "feel"));
        Assert.Equal("story_literal", game.SkillId(round with { Question = who }));
    }
}
