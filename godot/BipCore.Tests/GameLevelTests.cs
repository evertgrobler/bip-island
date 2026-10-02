using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// Levels inside each game (games.json levels): every level stays fair, each level changes what
/// it says it does, new children start at their band's level, and play moves the level.
/// </summary>
public sealed class GameLevelTests
{
    private readonly MasteryRules _rules = TestContent.Rules;
    private const int RoundsPerLevel = 300;

    private readonly ContentLibrary _content = TestContent.Library();
    private readonly PhonicsCourse _course;
    private readonly SoundHuntGame _hunt;
    private readonly BubblePopGame _pop;
    private readonly FeedMonsterGame _monster;
    private readonly CountTapGame _count;
    private readonly QuickLookGame _quick;
    private readonly SoundButtonsGame _buttons;
    private readonly WordBuilderGame _builder;
    private readonly BipsPathGame _path;

    public GameLevelTests()
    {
        _course = new PhonicsCourse(_content);
        _hunt = new SoundHuntGame(_content, _course);
        _pop = new BubblePopGame(_content, _course);
        _monster = new FeedMonsterGame(_content, _course);
        _count = new CountTapGame(_content);
        _quick = new QuickLookGame(_content);
        _buttons = new SoundButtonsGame(_content, _course);
        _builder = new WordBuilderGame(_content);
        _path = new BipsPathGame(_content);
    }

    /// <summary>A random child at a given game level.</summary>
    private Learner RandomLearner(int level, bool allGroups, IRandomSource rng)
    {
        var group = allGroups ? _course.Groups.Count : rng.NextInt(1, _course.Groups.Count);
        var open = _course.SoundsUpToGroup(group);
        var known = open.Where(_ => rng.NextBool()).Select(s => s.Id).ToHashSet();
        known.Add(rng.Pick(open)!.Id);
        rng.TryPick(BandExtensions.All, out var band);
        return new Learner(band, group, known, GameLevel: level);
    }

    /// <summary>Plays rounds at every level of a game and hands each to <paramref name="check"/>.</summary>
    private void EveryLevel<TRound, TChoice>(IMiniGame<TRound, TChoice> game, ulong seed, Action<TRound, Learner, GameLevel> check,
                                             bool allGroups = false)
        where TRound : class, IGameRound<TChoice>
    {
        var rng = new SeededGenerator(seed);
        var steps = game.Entry.LevelSteps;
        Assert.True(steps.Count > 1, $"{game.Id} should have levels");
        for (var index = 0; index < steps.Count; index++)
        {
            var played = 0;
            var tries = 0;
            while (played < RoundsPerLevel && tries < RoundsPerLevel * 20)
            {
                tries += 1;
                var child = RandomLearner(index, allGroups, rng);
                var session = new GameSession(game.Id, game.Skins[0], 8);
                if (session.NextRound(game, child, rng) is not TRound round) continue;
                Assert.True(game.CorrectChoices(round).Count == 1, $"{game.Id} level {index + 1}: not exactly one right answer");
                check(round, child, steps[index]);
                played += 1;
            }
            Assert.True(played == RoundsPerLevel, $"{game.Id} level {index + 1} couldn't make rounds");
        }
    }

    // MARK: Each level does what it says

    [Fact]
    public void CountAndTapCountsHigherAtHigherLevels()
    {
        EveryLevel(_count, 1, (round, _, step) =>
        {
            Assert.NotNull(step.CountTo);
            Assert.InRange(round.Count, 1, step.CountTo.Value);
        });
    }

    [Fact]
    public void QuickLookShowsMoreDotsForLessTime()
    {
        EveryLevel(_quick, 2, (round, _, step) =>
        {
            Assert.True(round.Count <= (step.CountTo ?? 10));
            Assert.Equal((step.FlashTenths ?? 20) / 10.0, round.FlashSeconds, 3);
        });
        var flashes = _quick.Entry.LevelSteps.Select(s => s.FlashTenths).OfType<int>().ToList();
        Assert.Equal(flashes.OrderDescending(), flashes); // the flash gets shorter level by level
    }

    [Fact]
    public void BubblePopAddsBubblesAndSpeed()
    {
        EveryLevel(_pop, 3, (round, _, step) =>
        {
            Assert.Equal(step.Bubbles, round.Choices.Count);
            Assert.Equal(step.SpeedPercent, round.SpeedPercent);
        });
    }

    [Fact]
    public void SoundHuntAndFeedTheMonsterOfferMorePictures()
    {
        EveryLevel(_hunt, 4, (round, _, step) =>
        {
            Assert.InRange(round.Choices.Count, SoundHuntGame.ChoiceCount, step.Choices ?? 3);
            Assert.Equal(round.Choices.Count, round.Choices.Select(c => c.Word).Distinct().Count());
        });
        EveryLevel(_monster, 5, (round, _, step) =>
        {
            Assert.InRange(round.Choices.Count, FeedMonsterGame.ChoiceCount, step.Choices ?? 3);
        });
    }

    [Fact]
    public void WordGamesUseLongerWordsAtHigherLevels()
    {
        // With every sound unlocked, each level always has its own words.
        EveryLevel(_buttons, 6, (round, _, step) =>
        {
            Assert.NotNull(step.SoundCounts);
            Assert.True(step.SoundCounts.Contains(round.Word.SoundCount), round.Word.Text);
        }, allGroups: true);
        EveryLevel(_builder, 7, (round, _, step) =>
        {
            Assert.NotNull(step.SoundCounts);
            Assert.True(step.SoundCounts.Contains(round.Word.SoundCount), round.Word.Text);
        }, allGroups: true);
    }

    [Fact]
    public void WordBuilderBankHasTheWordAndSafeSpares()
    {
        EveryLevel(_builder, 10, (round, learner, step) =>
        {
            var wanted = Math.Min(step.Spares ?? 1, WordBuilderGame.MaxSpares);
            Assert.Equal(round.Answer, round.AnswerTiles.Select(t => t.Id)); // the word's tiles spell the word
            // Every tile of the word is in the bank, as often as the word needs it.
            var bank = round.Bank.Select(t => t.Id).ToList();
            foreach (var id in round.Answer)
            {
                Assert.True(bank.Remove(id), $"{id} missing from the bank for {round.Word.Text}");
            }
            Assert.True(bank.Count <= wanted, "too many spare tiles");
            var wordSounds = round.Answer.Select(_course.Sound).OfType<PhonicsSound>().ToList();
            foreach (var spare in bank)
            {
                var sound = _course.Sound(spare);
                Assert.NotNull(sound);
                Assert.True(sound.Group <= learner.UnlockedPhonicsGroup, $"spare {spare} isn't taught yet");
                Assert.False(wordSounds.Any(w => w.IsConfusable(sound)), $"spare {spare} could spell {round.Word.Text}");
            }
            foreach (var tile in round.Bank)
            {
                Assert.False(tile.Text.Contains('_') || tile.Text.Contains('-'), $"tile shows {tile.Text}");
                Assert.StartsWith("snd_", tile.SoundClip);
            }
        });
    }

    [Fact]
    public void WordBuilderGetsMoreSparesAtHigherLevels()
    {
        var spares = _builder.Entry.LevelSteps.Select(s => s.Spares ?? 1).ToList();
        Assert.Equal(spares.Order(), spares); // spare tiles never go down as levels go up
        Assert.True(spares[^1] > spares[0]);
        var top = new Learner(Band.Stage1, _course.Groups.Count, _course.AllSounds.Select(s => s.Id).ToHashSet(), GameLevel: spares.Count - 1);
        var session = new GameSession(WordBuilderGame.GameId, _builder.Skins[0], 1);
        var round = session.NextRound(_builder, top, new SeededGenerator(11));
        Assert.NotNull(round);
        Assert.Equal(round.Answer.Count + spares[^1], round.Bank.Count); // with every sound open, the top level gets all its spares
    }

    [Fact]
    public void BipsPathMovesToBiggerGrids()
    {
        EveryLevel(_path, 8, (round, _, step) =>
        {
            Assert.Equal(step.GridBand, round.Level.Band); // the first puzzle of a visit comes from the level's grids
        });
    }

    [Fact]
    public void AHarderLevelFallsBackRatherThanEndingTheVisit()
    {
        // Group 1 has no four- or five-sound words: the top level still plays, with shorter words.
        var child = new Learner(Band.Foundation, 1, new HashSet<string> { "s", "a", "t" }, GameLevel: 2);
        var session = new GameSession(WordBuilderGame.GameId, _builder.Skins[0], 8);
        Assert.NotNull(session.NextRound(_builder, child, new SeededGenerator(9)));
    }

    // MARK: Starting and moving

    [Fact]
    public void NewChildrenStartAtTheirBandsLevel()
    {
        var entry = _content.Game(CountTapGame.GameId)!;
        Assert.Equal(0, entry.StartingLevel(Band.Foundation));
        Assert.Equal(2, entry.StartingLevel(Band.Stage1)); // level 3 is marked stage1
        Assert.Equal(2, entry.StartingLevel(Band.Stage3));
        var single = _content.Game(MeetTheSoundGame.GameId)!;
        Assert.Single(single.LevelSteps); // games without levels have one
        Assert.Equal(0, single.StartingLevel(Band.Stage3));
    }

    [Fact]
    public void LevelsAreClamped()
    {
        var entry = _content.Game(BubblePopGame.GameId)!;
        Assert.Equal(entry.LevelSteps[0], entry.Level(-3));
        Assert.Equal(entry.LevelSteps[^1], entry.Level(99));
    }

    [Fact]
    public void ThreeRightInARowMovesAGameUpAndTwoMissesBack()
    {
        var progress = new ChildProgress();
        Assert.Equal(0, progress.GameLevel("count_and_tap", 0));
        for (var i = 0; i < 3; i++) progress.RecordGameAnswer(true, "count_and_tap", 0, 4, _rules);
        Assert.Equal(1, progress.GameLevel("count_and_tap", 0));
        progress.RecordGameAnswer(false, "count_and_tap", 0, 4, _rules);
        Assert.Equal(MasteryChange.DroppedBack(0), progress.RecordGameAnswer(false, "count_and_tap", 0, 4, _rules));
        Assert.Equal(2, progress.GameLevel("quick_look", 2)); // an unplayed game starts where its band does
    }

    [Fact]
    public void TheTopLevelIsTheLimitAndOneLevelGamesDontMove()
    {
        var progress = new ChildProgress();
        for (var i = 0; i < 30; i++) progress.RecordGameAnswer(true, "sound_hunt", 0, 2, _rules);
        Assert.Equal(1, progress.GameLevel("sound_hunt", 0));
        Assert.Equal(MasteryChange.None, progress.RecordGameAnswer(true, "meet_the_sound", 0, 1, _rules));
        Assert.False(progress.GameLevels.ContainsKey("meet_the_sound"));
    }

    [Fact]
    public void GameLevelsSurviveSaving()
    {
        var progress = new ChildProgress();
        for (var i = 0; i < 3; i++) progress.RecordGameAnswer(true, "bubble_pop", 1, 4, _rules);
        Assert.Equal(2, ChildProgress.Decode(progress.Encode()).GameLevel("bubble_pop", 0));
    }

    [Fact]
    public void VisitsAreEightQuestions() => Assert.Equal(8, _content.Games.Session.RoundsPerSession);
}
