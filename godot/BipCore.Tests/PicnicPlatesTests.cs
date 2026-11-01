using BipCore;
using Xunit;
using Mode = BipCore.PicnicPlatesGame.Mode;

namespace BipCore.Tests;

/// <summary>
/// Picnic Plates (Numbers Island): every round at every level has exactly one right card, every share
/// comes out even, the food fits on the blanket, only the level's tables are used, and every clip Bip
/// says has a recording.
/// </summary>
public sealed class PicnicPlatesTests
{
    private const int RoundsPerLevel = 1000;
    private readonly ContentLibrary _content = TestContent.Library();
    private readonly PicnicPlatesGame _game;

    public PicnicPlatesTests() => _game = new PicnicPlatesGame(_content);

    private List<PicnicPlatesGame.Round> Play(int level, ulong seed)
    {
        var rng = new SeededGenerator(seed);
        var learner = new Learner(Band.Stage2, 1, new HashSet<string>(), GameLevel: level);
        var rounds = new List<PicnicPlatesGame.Round>();
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
    public void EveryRoundIsFairAndFitsTheBlanket()
    {
        var picnic = _content.Numbers.Picnic!;
        var modesSeen = new HashSet<Mode>();
        for (var level = 0; level < _game.Entry.LevelSteps.Count; level++)
        {
            var step = _game.Entry.Level(level);
            var allowed = step.Modes!.Select(PicnicPlatesGame.ParseMode).ToHashSet();
            var tables = step.Steps!;
            foreach (var round in Play(level, (ulong)(700 + level)))
            {
                var where = $"level {level + 1} {round.Mode} {round.Groups}x{round.Each}";
                modesSeen.Add(round.Mode);
                Assert.Contains(round.Mode, allowed);
                Assert.Equal(round.Answer, Assert.Single(_game.CorrectChoices(round)));
                Assert.Equal(3, round.Choices.Count);
                Assert.Equal(3, round.Choices.Distinct().Count());
                Assert.All(round.Choices, c => Assert.True(c >= 1, $"{where}: no card below 1"));
                Assert.InRange(round.Groups, 2, picnic.MostPlates + (round.Mode == Mode.Grouping ? 1 : 0));
                Assert.InRange(round.Each, 2, picnic.MostEach);
                Assert.True(round.Total <= picnic.MostFood, $"{where}: too much food for the blanket");
                // Every share and every grouping comes out even, by construction: total = groups × each.
                Assert.Equal(0, round.Total % round.Groups);
                Assert.Equal(0, round.Total % round.Each);
                if (round.Mode == Mode.Sharing) Assert.True(tables.Contains(round.Groups) || tables.Contains(round.Each), $"{where}: not in the level's tables");
                else Assert.Contains(round.Each, tables);
                Assert.Contains(round.Food, picnic.Food);
                Assert.Contains(_game.SkillId(round), _game.Entry.Skills);
                Assert.Equal(round.Mode is Mode.Make or Mode.Sharing or Mode.Grouping, round.Deals > 0);
                foreach (var clips in new[] { _game.Instruction(round), _game.Question(round), _game.Hint(round), _game.SayWhenRight(round) })
                    Assert.All(clips, clip => Assert.True(Recorded(clip), $"{where}: {clip} has no recording"));
                // The hint's count ends on the answer.
                Assert.Equal(AudioCatalogue.NumberClip(round.Answer), _game.Hint(round)[^1]);
            }
        }
        Assert.Equal(Enum.GetValues<Mode>().ToHashSet(), modesSeen);
    }

    [Fact]
    public void TheAnswerIsWhatTheQuestionAsks()
    {
        var food = _content.Numbers.Picnic!.Food[0];
        Assert.Equal(15, new PicnicPlatesGame.Round(Mode.Groups, 3, 5, food, [15, 10, 20]).Answer);
        Assert.Equal(12, new PicnicPlatesGame.Round(Mode.Arrays, 3, 4, food, [12, 8, 16]).Answer);
        Assert.Equal(6, new PicnicPlatesGame.Round(Mode.Sharing, 2, 6, food, [6, 5, 7]).Answer);
        Assert.Equal(3, new PicnicPlatesGame.Round(Mode.Grouping, 3, 5, food, [3, 2, 4]).Answer);
        Assert.True(_game.IsCorrect(6, new PicnicPlatesGame.Round(Mode.Sharing, 2, 6, food, [6, 5, 7])));
        Assert.False(_game.IsCorrect(12, new PicnicPlatesGame.Round(Mode.Sharing, 2, 6, food, [6, 5, 12])));
    }

    [Fact]
    public void TheHintCountsInStepsOfTheTable()
    {
        var food = _content.Numbers.Picnic!.Food[0];
        var hint = _game.Hint(new PicnicPlatesGame.Round(Mode.Groups, 3, 5, food, [15, 10, 20]));
        Assert.Equal([VoiceLine.CountInSteps, "num_5", _content.MidNumberClip(5), _content.MidNumberClip(10), "num_15"], hint);
        var share = _game.Hint(new PicnicPlatesGame.Round(Mode.Sharing, 4, 3, food, [3, 2, 4]));
        Assert.Equal([VoiceLine.OneEach, _content.MidNumberClip(1), _content.MidNumberClip(2), "num_3"], share);
    }

    [Fact]
    public void SharingSaysWhatIsShared()
    {
        // "Share twelve between two plates. Click the basket."
        var food = _content.Numbers.Picnic!.Food[0];
        Assert.Equal([VoiceLine.Share, _content.MidNumberClip(12), VoiceLine.Between, _content.MidNumberClip(2), VoiceLine.Plates,
                      VoiceLine.ClickTheBasket], _game.Instruction(new PicnicPlatesGame.Round(Mode.Sharing, 2, 6, food, [6, 5, 7])));
    }

    [Fact]
    public void AfrikaansRoundsHaveTheirClips()
    {
        var af = TestContent.Library(HomeLanguage.Afrikaans);
        var game = new PicnicPlatesGame(af);
        var rng = new SeededGenerator(71);
        for (var level = 0; level < game.Entry.LevelSteps.Count; level++)
        {
            var learner = new Learner(Band.Stage2, 1, new HashSet<string>(), GameLevel: level);
            for (var visit = 0; visit < 40; visit++)
            {
                var session = GameSession.Start(game, af.Games.Session, new HashSet<string>(), rng);
                while (session.NextRound(game, learner, rng) is { } round)
                    foreach (var clips in new[] { game.Instruction(round), game.Question(round), game.Hint(round), game.SayWhenRight(round) })
                        Assert.All(clips, clip => Assert.True(af.AudioIds.Contains(clip) || VoiceLine.All.Contains(clip), $"af level {level + 1}: {clip}"));
            }
        }
    }

    [Fact]
    public void DealingTakesOneTapARoundOrAPunnet()
    {
        var food = _content.Numbers.Picnic!.Food[0];
        Assert.Equal(5, new PicnicPlatesGame.Round(Mode.Make, 3, 5, food, [15, 10, 20]).Deals);
        Assert.Equal(6, new PicnicPlatesGame.Round(Mode.Sharing, 2, 6, food, [6, 5, 7]).Deals);
        Assert.Equal(3, new PicnicPlatesGame.Round(Mode.Grouping, 3, 5, food, [3, 2, 4]).Deals);
        Assert.Equal(0, new PicnicPlatesGame.Round(Mode.Arrays, 3, 4, food, [12, 8, 16]).Deals);
    }
}
