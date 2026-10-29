using BipCore;
using Xunit;
using Mode = BipCore.NumberMachineGame.Mode;

namespace BipCore.Tests;

/// <summary>
/// Number Machine (Numbers Island): every round at every level has exactly one right card, uses only the
/// level's rules, stays within 2 to 10 going in (100 at most coming out), and every clip has a recording.
/// </summary>
public sealed class NumberMachineTests
{
    private const int RoundsPerLevel = 1000;
    private readonly ContentLibrary _content = TestContent.Library();
    private readonly NumberMachineGame _game;

    public NumberMachineTests() => _game = new NumberMachineGame(_content);

    private List<NumberMachineGame.Round> Play(int level, ulong seed)
    {
        var rng = new SeededGenerator(seed);
        var learner = new Learner(Band.Stage3, 1, new HashSet<string>(), GameLevel: level);
        var rounds = new List<NumberMachineGame.Round>();
        while (rounds.Count < RoundsPerLevel)
        {
            var session = GameSession.Start(_game, _content.Games.Session, new HashSet<string>(), rng);
            var visit = 0;
            while (session.NextRound(_game, learner, rng) is { } round)
            {
                rounds.Add(round);
                visit += 1;
            }
            Assert.Equal(session.MaxRounds, visit); // every visit is a full one
        }
        return rounds;
    }

    private bool Recorded(string clip) => _content.AudioIds.Contains(clip) || VoiceLine.All.Contains(clip);

    [Fact]
    public void EveryRoundIsFairAndUsesTheLevelsRules()
    {
        var modesSeen = new HashSet<Mode>();
        for (var level = 0; level < _game.Entry.LevelSteps.Count; level++)
        {
            var step = _game.Entry.Level(level);
            var allowed = step.Modes!.Select(NumberMachineGame.ParseMode).ToHashSet();
            foreach (var round in Play(level, (ulong)(800 + level)))
            {
                var where = $"level {level + 1} {round.Mode} {round.In}x{round.Rule}";
                modesSeen.Add(round.Mode);
                Assert.Contains(round.Mode, allowed);
                Assert.Contains(round.Rule, step.Steps!);
                Assert.InRange(round.In, NumberMachineGame.LeastIn, NumberMachineGame.MostIn);
                Assert.InRange(round.Product, 4, 100);
                Assert.Equal(round.Answer, Assert.Single(_game.CorrectChoices(round)));
                Assert.Equal(3, round.Choices.Count);
                Assert.Equal(3, round.Choices.Distinct().Count());
                Assert.All(round.Choices, c => Assert.True(c >= 1, $"{where}: no card below 1"));
                if (round.Mode == Mode.Rule)
                {
                    Assert.All(round.Choices, c => Assert.Contains(c, step.Steps!));
                    Assert.Equal(NumberMachineGame.Examples, round.Inputs!.Distinct().Count());
                }
                Assert.Contains(_game.SkillId(round), _game.Entry.Skills);
                foreach (var clips in new[] { _game.Prompt(round), _game.Hint(round), _game.SayWhenRight(round) })
                    Assert.All(clips, clip => Assert.True(Recorded(clip), $"{where}: {clip} has no recording"));
                // The hint counts in steps of the rule up to what the × machine makes.
                Assert.Equal(AudioCatalogue.NumberClip(round.Product), _game.Hint(round)[^1]);
            }
        }
        Assert.Equal(Enum.GetValues<Mode>().ToHashSet(), modesSeen);
    }

    [Fact]
    public void TheAnswerIsWhatTheMachineIsAsked()
    {
        Assert.Equal(12, new NumberMachineGame.Round(Mode.Out, 6, 2, [12, 10, 14]).Answer);
        Assert.Equal(3, new NumberMachineGame.Round(Mode.In, 3, 10, [3, 2, 4]).Answer);
        Assert.Equal(5, new NumberMachineGame.Round(Mode.Divide, 5, 4, [5, 4, 6]).Answer);
        Assert.Equal(4, new NumberMachineGame.Round(Mode.Rule, 3, 4, [2, 4, 5], [3, 5]).Answer);
        Assert.True(_game.IsCorrect(5, new NumberMachineGame.Round(Mode.Divide, 5, 4, [5, 4, 6])));
        Assert.False(_game.IsCorrect(20, new NumberMachineGame.Round(Mode.Divide, 5, 4, [5, 4, 20])));
    }

    [Fact]
    public void SkillsFollowTheTable()
    {
        Assert.Equal("count_steps", _game.SkillId(new NumberMachineGame.Round(Mode.Out, 6, 5, [30, 25, 35])));
        Assert.Equal("tables_3_4_8", _game.SkillId(new NumberMachineGame.Round(Mode.Out, 6, 8, [48, 40, 56])));
        Assert.Equal("share_group", _game.SkillId(new NumberMachineGame.Round(Mode.In, 3, 10, [3, 2, 4])));
        Assert.Equal("times_divide", _game.SkillId(new NumberMachineGame.Round(Mode.Divide, 5, 4, [5, 4, 6])));
    }
}
