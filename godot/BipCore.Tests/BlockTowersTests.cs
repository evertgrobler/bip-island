using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// Block Towers (Numbers Island): every round at every level is fair (exactly one right answer),
/// stays within the level's numbers, and every clip the screen plays exists.
/// </summary>
public sealed class BlockTowersTests
{
    private const int RoundsPerLevel = 400;
    private readonly ContentLibrary _content = TestContent.Library();

    private static readonly HashSet<string> Clips =
    [
        VoiceLine.BuildTower, VoiceLine.FindTower, VoiceLine.TallestTower, VoiceLine.ShortestTower,
        VoiceLine.HowManyAltogether, VoiceLine.TakeAway, VoiceLine.HowManyLeft, VoiceLine.MakeTen, VoiceLine.Double,
        VoiceLine.HowMany, VoiceLine.OddOrEven, VoiceLine.Odd, VoiceLine.BuildNumber, VoiceLine.WhatComesNext, VoiceLine.Make,
    ];

    private List<BlockTowersGame.Round> Play(BlockTowersGame game, int level, ulong seed)
    {
        var rng = new SeededGenerator(seed);
        var learner = new Learner(Band.Foundation, 1, new HashSet<string>(), GameLevel: level);
        var rounds = new List<BlockTowersGame.Round>();
        for (var sessions = 0; rounds.Count < RoundsPerLevel; sessions++)
        {
            Assert.True(sessions < RoundsPerLevel, $"level {level + 1}: the generator can't make rounds");
            var session = GameSession.Start(game, _content.Games.Session, new HashSet<string>(), rng);
            var visit = 0;
            while (session.NextRound(game, learner, rng) is { } round)
            {
                rounds.Add(round);
                visit += 1;
            }
            Assert.Equal(session.MaxRounds, visit); // every visit is a full one, even at the smallest level
        }
        return rounds;
    }

    [Fact]
    public void EveryRoundHasExactlyOneRightAnswer()
    {
        var game = new BlockTowersGame(_content);
        var modesSeen = new HashSet<BlockTowersGame.Mode>();
        var asksSeen = new HashSet<BlockTowersGame.Ask>();
        for (var level = 0; level < game.Entry.LevelSteps.Count; level++)
        {
            var step = game.Entry.Level(level);
            var allowed = step.Modes!.Select(BlockTowersGame.ParseMode).ToHashSet();
            foreach (var round in Play(game, level, (ulong)(700 + level)))
            {
                modesSeen.Add(round.Mode);
                Assert.Contains(round.Mode, allowed);
                Assert.Single(game.CorrectChoices(round));
                Assert.Equal(round.Choices.Count, round.Choices.Distinct().Count());
                Assert.Contains(game.SkillId(round), game.Entry.Skills);
                Assert.True(round.Target >= 0, "no negative answers");
                Assert.Contains(AudioCatalogue.NumberClip(round.Target), _content.AudioIds);
                switch (round.Mode)
                {
                    case BlockTowersGame.Mode.Build:
                        Assert.InRange(round.Target, 1, step.CountTo!.Value);
                        Assert.True(round.Target <= round.MostCubes);
                        break;
                    case BlockTowersGame.Mode.Which:
                        asksSeen.Add(round.Ask);
                        Assert.Equal(3, round.Towers!.Count);
                        Assert.Equal(3, round.Towers.Distinct().Count());
                        Assert.All(round.Towers, h => Assert.InRange(h, 1, step.CountTo!.Value));
                        break;
                    case BlockTowersGame.Mode.Join:
                        Assert.Equal(round.A + round.B, round.Target);
                        Assert.InRange(round.Target, 2, step.CountTo!.Value);
                        Assert.Equal(3, round.Choices.Count);
                        break;
                    case BlockTowersGame.Mode.TakeAway:
                        Assert.InRange(round.B, 1, round.A - 1);
                        Assert.Equal(round.A - round.B, round.Target);
                        Assert.Equal(3, round.Choices.Count);
                        break;
                    case BlockTowersGame.Mode.MakeTen:
                        Assert.Equal(BlockTowersGame.Ten, round.A + round.Target);
                        Assert.InRange(round.A, 1, 9);
                        break;
                    case BlockTowersGame.Mode.Doubles:
                        Assert.Equal(2 * round.A, round.Target);
                        Assert.InRange(round.A, 1, BlockTowersGame.Ten);
                        break;
                    case BlockTowersGame.Mode.OddEven:
                        Assert.Equal(new[] { BlockTowersGame.Odd, BlockTowersGame.Even }, round.Choices);
                        Assert.InRange(round.A, 1, 20);
                        break;
                    case BlockTowersGame.Mode.TensOnes:
                        Assert.InRange(round.Target, 11, 99);
                        Assert.Equal(round.Target.ToString(), Assert.Single(game.CorrectChoices(round)));
                        break;
                    case BlockTowersGame.Mode.Pattern:
                        Assert.Equal(new[] { round.A, round.A + round.B, round.A + 2 * round.B }, round.Towers);
                        Assert.Equal(round.A + 3 * round.B, round.Target);
                        Assert.Equal(3, round.Choices.Count);
                        Assert.All(round.Choices, c => Assert.InRange(int.Parse(c), 1, 20));
                        break;
                }
            }
        }
        Assert.Equal(Enum.GetValues<BlockTowersGame.Mode>().ToHashSet(), modesSeen); // every kind of question turns up
        Assert.Equal(Enum.GetValues<BlockTowersGame.Ask>().ToHashSet(), asksSeen);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(19)]
    public void OddAndEvenFollowTheNumber(int seed)
    {
        var game = new BlockTowersGame(_content);
        var learner = new Learner(Band.Foundation, 1, new HashSet<string>(), GameLevel: 5); // odd or even
        var rng = new SeededGenerator((ulong)seed);
        var session = GameSession.Start(game, _content.Games.Session, new HashSet<string>(), rng);
        while (session.NextRound(game, learner, rng) is { } round)
            Assert.Equal(round.A % 2 == 0 ? BlockTowersGame.Even : BlockTowersGame.Odd, Assert.Single(game.CorrectChoices(round)));
    }

    [Fact]
    public void WhichFindsTheTallestAndShortestTower()
    {
        var game = new BlockTowersGame(_content);
        var tallest = new BlockTowersGame.Round(BlockTowersGame.Mode.Which, 9, ["3", "9", "5"], Towers: [3, 9, 5], Ask: BlockTowersGame.Ask.Tallest);
        Assert.Equal("9", Assert.Single(game.CorrectChoices(tallest)));
        var shortest = tallest with { Target = 3, Ask = BlockTowersGame.Ask.Shortest };
        Assert.Equal("3", Assert.Single(game.CorrectChoices(shortest)));
    }

    [Fact]
    public void AVisitDoesNotRepeatAQuestionWhileFreshOnesAreLeft()
    {
        var game = new BlockTowersGame(_content);
        var learner = new Learner(Band.Foundation, 1, new HashSet<string>(), GameLevel: 2); // join, totals to 10: 45 pairs
        var rng = new SeededGenerator(77);
        for (var i = 0; i < 30; i++)
        {
            var session = GameSession.Start(game, _content.Games.Session, new HashSet<string>(), rng);
            var asked = new List<string>();
            while (session.NextRound(game, learner, rng) is { } round) asked.Add($"{round.A}+{round.B}");
            Assert.Equal(asked.Count, asked.Distinct().Count());
        }
    }

    [Fact]
    public void EveryClipTheScreenPlaysExists()
    {
        foreach (var clip in Clips) Assert.Contains(clip, VoiceLine.All);
        Assert.Contains(AudioCatalogue.WordClip("and"), _content.AudioIds);
        Assert.Contains(AudioCatalogue.WordClip("even"), _content.AudioIds);
        for (var n = 0; n <= 99; n++) Assert.Contains(AudioCatalogue.NumberClip(n), _content.AudioIds);
    }
}
