using BipCore;
using Xunit;
using Mode = BipCore.BipsBalanceGame.Mode;
using Op = BipCore.BipsBalanceGame.Op;
using Sum = BipCore.BipsBalanceGame.Sentence;
using Slot = BipCore.BipsBalanceGame.Slot;

namespace BipCore.Tests;

/// <summary>
/// Bip's Balance (Numbers Island): every round at every level has exactly one card that balances
/// the see-saw, the numbers stay within the level, each kind of question asks what it says, and
/// every clip Bip says has a recording.
/// </summary>
public sealed class BipsBalanceTests
{
    private const int RoundsPerLevel = 1000;
    private readonly ContentLibrary _content = TestContent.Library();

    private List<BipsBalanceGame.Round> Play(BipsBalanceGame game, int level, ulong seed)
    {
        var rng = new SeededGenerator(seed);
        var learner = new Learner(Band.Stage1, 1, new HashSet<string>(), GameLevel: level);
        var rounds = new List<BipsBalanceGame.Round>();
        while (rounds.Count < RoundsPerLevel)
        {
            var session = GameSession.Start(game, _content.Games.Session, new HashSet<string>(), rng);
            var visit = 0;
            while (session.NextRound(game, learner, rng) is { } round)
            {
                rounds.Add(round);
                visit += 1;
            }
            Assert.Equal(session.MaxRounds, visit); // every visit is a full one
        }
        return rounds;
    }

    /// <summary>Every clip in Bip's Balance is either generated (numbers, animals) or in the voice script.</summary>
    private bool Recorded(string clip) => _content.AudioIds.Contains(clip) || VoiceLine.All.Contains(clip);

    [Fact]
    public void EveryRoundHasExactlyOneCardThatBalances()
    {
        var game = new BipsBalanceGame(_content);
        var modesSeen = new HashSet<Mode>();
        var stories = 0;
        for (var level = 0; level < game.Entry.LevelSteps.Count; level++)
        {
            var step = game.Entry.Level(level);
            var allowed = step.Modes!.Select(BipsBalanceGame.ParseMode).ToHashSet();
            foreach (var round in Play(game, level, (ulong)(900 + level)))
            {
                var s = round.Sentence;
                modesSeen.Add(round.Mode);
                Assert.Contains(round.Mode, allowed);
                Assert.True(s.Balances, $"{s} must be a true sentence");
                Assert.Single(game.CorrectChoices(round));
                Assert.Equal(round.Answer, Assert.Single(game.CorrectChoices(round)));
                Assert.Equal(3, round.Choices.Count);
                Assert.Equal(3, round.Choices.Distinct().Count());
                Assert.All(round.Choices, c => Assert.True(c >= 1, "no card below 1"));
                Assert.All(new[] { s.Left, s.Right, s.Result }, n => Assert.InRange(n, 1, step.CountTo!.Value));
                Assert.Contains(game.SkillId(round), game.Entry.Skills);
                Assert.All(game.Prompt(round), clip => Assert.True(Recorded(clip), $"{clip} has no recording"));
                Assert.All(BipsBalanceGame.Hint(round), clip => Assert.True(Recorded(clip), $"{clip} has no recording"));
                Assert.All(BipsBalanceGame.SayWhenRight(round), clip => Assert.True(Recorded(clip), $"{clip} has no recording"));
                if (round.Story != null)
                {
                    stories += 1;
                    Assert.Contains(round.Mode, new[] { Mode.Add, Mode.TakeAway });
                    Assert.InRange(s.Result, 1, BipsBalanceGame.SmallSums);
                    Assert.True(s.Left >= 2 && s.Right >= 2, "a story never says \"1 more come along\"");
                    Assert.Equal(s.Op == Op.Plus ? "plus" : "minus", round.Story.Op);
                    Assert.Contains(round.Thing!.Id, round.Story.Things);
                }
                switch (round.Mode)
                {
                    case Mode.Add:
                        Assert.Equal((Op.Plus, Slot.Result), (s.Op, round.Hidden));
                        Assert.True(round.ShowsCubes || round.Story != null);
                        break;
                    case Mode.TakeAway:
                        Assert.Equal((Op.Minus, Slot.Result), (s.Op, round.Hidden));
                        break;
                    case Mode.Past10:
                        Assert.Equal(Slot.Result, round.Hidden);
                        Assert.False(round.ShowsCubes);
                        // It really goes past 10: from a one-digit number to a teen, or back again.
                        if (s.Op == Op.Plus) Assert.True(s.Left < 10 && s.Right < 10 && s.Result > 10, s.ToString());
                        else Assert.True(s.Left > 10 && s.Right < 10 && s.Result < 10, s.ToString());
                        break;
                    case Mode.Missing:
                        Assert.NotEqual(Slot.Result, round.Hidden);
                        Assert.Contains(VoiceLine.WhatNumber, game.Prompt(round));
                        break;
                    case Mode.FactFamily:
                        var helper = round.Helper!;
                        Assert.True(helper.Balances);
                        Assert.Equal(Op.Plus, helper.Op);
                        // The question uses the same three numbers as the finished sum.
                        Assert.Equal(new[] { helper.Left, helper.Right, helper.Result }.Order(), new[] { s.Left, s.Right, s.Result }.Order());
                        Assert.NotEqual(helper, s);
                        Assert.Contains(VoiceLine.BalanceHelps, game.Prompt(round));
                        break;
                }
            }
        }
        Assert.Equal(Enum.GetValues<Mode>().ToHashSet(), modesSeen); // every kind of question turns up
        Assert.InRange(stories, 50, 800); // about 1 in 4 of the first two levels' rounds
    }

    [Fact]
    public void AVisitDoesNotRepeatAQuestionWhileFreshOnesAreLeft()
    {
        var game = new BipsBalanceGame(_content);
        for (var level = 0; level < game.Entry.LevelSteps.Count; level++)
        {
            var learner = new Learner(Band.Stage1, 1, new HashSet<string>(), GameLevel: level);
            var rng = new SeededGenerator((ulong)(40 + level));
            for (var i = 0; i < 30; i++)
            {
                var session = GameSession.Start(game, _content.Games.Session, new HashSet<string>(), rng);
                var asked = new List<string>();
                while (session.NextRound(game, learner, rng) is { } round) asked.Add(round.UsedItems[0]);
                Assert.Equal(asked.Count, asked.Distinct().Count());
            }
        }
    }

    [Theory]
    [InlineData(Slot.Left, 3)]
    [InlineData(Slot.Right, 7)]
    [InlineData(Slot.Result, 10)]
    public void OnlyTheNumberThatBalancesIsRight(Slot hidden, int answer)
    {
        var game = new BipsBalanceGame(_content);
        var round = new BipsBalanceGame.Round(Mode.Missing, new Sum(3, Op.Plus, 7, 10), hidden, [answer - 1, answer, answer + 1]);
        Assert.Equal(answer, Assert.Single(game.CorrectChoices(round)));
        // The same numbers taken away: 10 − 7 = 3.
        var take = new Sum(10, Op.Minus, 7, 3);
        var takeRound = round with { Sentence = take, Choices = [take[hidden] - 1, take[hidden], take[hidden] + 1] };
        Assert.Equal(take[hidden], Assert.Single(game.CorrectChoices(takeRound)));
    }

    [Fact]
    public void TheSentenceIsSaidWithWhatNumberForTheBox()
    {
        var sentence = new Sum(7, Op.Plus, 3, 10);
        Assert.Equal(["num_7", VoiceLine.Plus, VoiceLine.WhatNumber, VoiceLine.EqualsWord, "num_10"], BipsBalanceGame.Say(sentence, Slot.Right));
        Assert.Equal(["num_9", VoiceLine.TakeAway, "num_4", VoiceLine.EqualsWord, "num_5"],
                     BipsBalanceGame.Say(new Sum(9, Op.Minus, 4, 5)));
    }

    [Fact]
    public void TheHintCountsOnOrBackFromTheFirstNumber()
    {
        var on = new BipsBalanceGame.Round(Mode.Past10, new Sum(8, Op.Plus, 5, 13), Slot.Result, [12, 13, 14]);
        Assert.Equal([VoiceLine.CountOnTogether, "num_8", "num_9", "num_10", "num_11", "num_12", "num_13"], BipsBalanceGame.Hint(on));
        var back = on with { Sentence = new Sum(9, Op.Minus, 3, 6) };
        Assert.Equal([VoiceLine.CountBackTogether, "num_9", "num_8", "num_7", "num_6"], BipsBalanceGame.Hint(back));
        var missing = on with { Mode = Mode.Missing, Hidden = Slot.Right };
        Assert.Equal(BipsBalanceGame.Say(missing.Sentence), BipsBalanceGame.Hint(missing));
    }

    [Fact]
    public void AStorySaysItsNumbersAndAnimals()
    {
        var game = new BipsBalanceGame(_content);
        var story = _content.Numbers.StorySums!.First(s => s.Op == "plus");
        var duck = _content.Numbers.CountingObjects.First(o => o.Id == "duck");
        var round = new BipsBalanceGame.Round(Mode.Add, new Sum(5, Op.Plus, 3, 8), Slot.Result, [7, 8, 9], Story: story, Thing: duck);
        Assert.Equal([VoiceLine.StoryThereAre, "num_5", "word_ducks", "num_3", VoiceLine.StoryComeAlong, VoiceLine.StoryHowManyNow],
                     game.Prompt(round));
    }
}
