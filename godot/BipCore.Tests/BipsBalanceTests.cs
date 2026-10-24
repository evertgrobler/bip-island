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
    private readonly BipsBalanceGame _game;

    public BipsBalanceTests() => _game = new BipsBalanceGame(_content);

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
                Assert.All(_game.Hint(round), clip => Assert.True(Recorded(clip), $"{clip} has no recording"));
                // A counting hint is short: at most 11 numbers (it counts the smaller number on from the bigger).
                Assert.True(_game.Hint(round).Count <= 12, $"{s}: hint too long");
                Assert.All(_game.SayWhenRight(round), clip => Assert.True(Recorded(clip), $"{clip} has no recording"));
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
                        Assert.InRange(round.Answer, 1, BipsBalanceGame.MostMissing);
                        Assert.Contains(VoiceLine.WhatNumber, game.Prompt(round));
                        // The hint counts between the numbers shown, so it never says the answer on its own.
                        Assert.NotEqual(_game.Say(s), _game.Hint(round));
                        Assert.Contains(_game.Hint(round)[0], new[] { VoiceLine.CountOnTogether, VoiceLine.CountBackTogether });
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
        Assert.Equal(["num_7_mid", VoiceLine.Plus, VoiceLine.WhatNumber, VoiceLine.EqualsWord, "num_10"], _game.Say(sentence, Slot.Right));
        Assert.Equal(["num_9_mid", VoiceLine.TakeAway, "num_4_mid", VoiceLine.EqualsWord, "num_5"],
                     _game.Say(new Sum(9, Op.Minus, 4, 5)));
    }

    [Fact]
    public void TheHintCountsTheShortWay()
    {
        // Every number but the last leads on ("8… 9, 10").
        string[] Nums(params int[] n) => n.Select((x, i) => i < n.Length - 1 ? _content.MidNumberClip(x) : AudioCatalogue.NumberClip(x)).ToArray();
        var on = new BipsBalanceGame.Round(Mode.Past10, new Sum(8, Op.Plus, 5, 13), Slot.Result, [12, 13, 14]);
        Assert.Equal([VoiceLine.CountOnTogether, .. Nums(8, 9, 10, 11, 12, 13)], _game.Hint(on));
        // 2 + 18: on from the bigger number, not 18 steps from 2.
        var bigSecond = on with { Sentence = new Sum(2, Op.Plus, 18, 20) };
        Assert.Equal([VoiceLine.CountOnTogether, .. Nums(18, 19, 20)], _game.Hint(bigSecond));
        var back = on with { Sentence = new Sum(9, Op.Minus, 3, 6) };
        Assert.Equal([VoiceLine.CountBackTogether, .. Nums(9, 8, 7, 6)], _game.Hint(back));
        // 20 − 18: on from 18 up to 20 rather than back 18 times.
        var difference = on with { Sentence = new Sum(20, Op.Minus, 18, 2) };
        Assert.Equal([VoiceLine.CountOnTogether, .. Nums(18, 19, 20)], _game.Hint(difference));
    }

    [Fact]
    public void AMissingNumberHintCountsBetweenTheNumbersShown()
    {
        // Every number but the last leads on ("8… 9, 10").
        string[] Nums(params int[] n) => n.Select((x, i) => i < n.Length - 1 ? _content.MidNumberClip(x) : AudioCatalogue.NumberClip(x)).ToArray();
        var round = new BipsBalanceGame.Round(Mode.Missing, new Sum(7, Op.Plus, 3, 10), Slot.Right, [2, 3, 4]);
        Assert.Equal([VoiceLine.CountOnTogether, .. Nums(7, 8, 9, 10)], _game.Hint(round));
        Assert.Equal([VoiceLine.CountOnTogether, .. Nums(3, 4, 5, 6, 7, 8, 9, 10)], _game.Hint(round with { Hidden = Slot.Left }));
        var take = round with { Sentence = new Sum(8, Op.Minus, 3, 5) };
        Assert.Equal([VoiceLine.CountBackTogether, .. Nums(8, 7, 6, 5)], _game.Hint(take));
        Assert.Equal([VoiceLine.CountOnTogether, .. Nums(5, 6, 7, 8)], _game.Hint(take with { Hidden = Slot.Left }));
    }

    [Fact]
    public void AFactFamilyHintPointsBackToTheFinishedSum()
    {
        var helper = new Sum(3, Op.Plus, 5, 8);
        var round = new BipsBalanceGame.Round(Mode.FactFamily, new Sum(8, Op.Minus, 5, 3), Slot.Result, [2, 3, 4], Helper: helper);
        Assert.Equal([VoiceLine.BalanceHelps, .. _game.Say(helper)], _game.Hint(round));
    }

    [Fact]
    public void EveryKindOfSumRecordsASkillNoHarderThanItsLevel()
    {
        var game = new BipsBalanceGame(_content);
        var skills = _content.Skills.Skills.ToDictionary(s => s.Id);
        var band = Band.Foundation;
        for (var level = 0; level < game.Entry.LevelSteps.Count; level++)
        {
            band = game.Entry.Level(level).Band ?? band; // a level without a band carries on the one before
            foreach (var round in Play(game, level, (ulong)(60 + level)).Take(50))
                Assert.True(skills[game.SkillId(round)].Band <= band,
                            $"level {level + 1} ({band}): {game.SkillId(round)} is a {skills[game.SkillId(round)].Band} skill");
        }
    }

    [Fact]
    public void AStorySaysItsNumbersAndAnimals()
    {
        var game = new BipsBalanceGame(_content);
        var story = _content.Numbers.StorySums!.First(s => s.Op == "plus");
        var duck = _content.Numbers.CountingObjects.First(o => o.Id == "duck");
        var round = new BipsBalanceGame.Round(Mode.Add, new Sum(5, Op.Plus, 3, 8), Slot.Result, [7, 8, 9], Story: story, Thing: duck);
        Assert.Equal([VoiceLine.StoryThereAre, "num_5_mid", "word_ducks", "num_3_mid", VoiceLine.StoryComeAlong, VoiceLine.StoryHowManyNow],
                     game.Prompt(round));
    }
}
