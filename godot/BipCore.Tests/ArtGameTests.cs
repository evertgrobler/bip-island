using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// Art Island's three games: every round, at every level, is fair (exactly one right answer, or for
/// peg rounds exactly the reflected holes), and everything the screens say or show exists.
/// </summary>
public sealed class ArtGameTests
{
    private const int RoundsPerLevel = 300;
    private readonly ContentLibrary _content = TestContent.Library();

    /// <summary>Plays whole sessions at one level until <paramref name="count"/> rounds have been checked.</summary>
    private void PlayLevel<TRound>(IMiniGame<TRound, string> game, int level, ulong seed, Action<TRound> check)
        where TRound : class, IGameRound<string>
    {
        var rng = new SeededGenerator(seed);
        var learner = new Learner(Band.Foundation, 1, new HashSet<string>(), GameLevel: level);
        var played = 0;
        for (var sessions = 0; played < RoundsPerLevel; sessions++)
        {
            Assert.True(sessions < RoundsPerLevel, $"{game.Id} level {level + 1}: the generator can't make rounds");
            var session = GameSession.Start(game, _content.Games.Session, new HashSet<string>(), rng);
            while (session.NextRound(game, learner, rng) is TRound round)
            {
                check(round);
                played += 1;
            }
        }
    }

    private static IEnumerable<int> Levels(IMiniGame game) => Enumerable.Range(0, game.Entry.LevelSteps.Count);

    [Fact]
    public void ShapeBuilderRoundsAreFair()
    {
        var game = new ShapeBuilderGame(_content);
        var modesSeen = new HashSet<ShapeBuilderGame.Mode>();
        foreach (var level in Levels(game))
        {
            var allowed = game.Entry.Level(level).Shapes!.ToHashSet();
            PlayLevel(game, level, (ulong)(300 + level), round =>
            {
                modesSeen.Add(round.Mode);
                Assert.Equal(3, round.Choices.Count);
                Assert.Equal(3, round.Choices.Distinct().Count());
                Assert.Single(game.CorrectChoices(round));
                Assert.Contains(game.SkillId(round), game.Entry.Skills);
                if (round.Mode == ShapeBuilderGame.Mode.Count)
                {
                    Assert.All(round.Choices, c => Assert.True(int.Parse(c) >= 1));
                    Assert.Contains(AudioCatalogue.NumberClip(round.Target!.LookSameTurns), _content.AudioIds);
                    return;
                }
                foreach (var id in round.Choices)
                {
                    Assert.Contains(id, allowed);
                    Assert.Contains(game.Shape(id)!.Audio, _content.AudioIds);
                }
                Assert.Equal(3, round.Turns!.Count);
                if (round.Mode == ShapeBuilderGame.Mode.Fill)
                {
                    var piece = round.Picture!.Pieces[round.Missing];
                    Assert.NotEqual(true, piece.Fixed);
                    Assert.Equal(piece.Shape, round.Target!.Id);
                }
            });
        }
        Assert.Equal(Enum.GetValues<ShapeBuilderGame.Mode>().ToHashSet(), modesSeen); // every kind of question turns up
    }

    [Fact]
    public void ShapeBuilderNeverRepeatsAPictureInAVisit()
    {
        var game = new ShapeBuilderGame(_content);
        var rng = new SeededGenerator(320);
        var learner = new Learner(Band.Foundation, 1, new HashSet<string>(), GameLevel: 1); // fill only
        for (var i = 0; i < 50; i++)
        {
            var session = GameSession.Start(game, _content.Games.Session, new HashSet<string>(), rng);
            var pictures = new List<string>();
            while (session.NextRound(game, learner, rng) is { } round) pictures.Add(round.Picture!.Id);
            Assert.Equal(pictures.Count, pictures.Distinct().Count());
            Assert.True(pictures.Count >= 6, "a visit should have at least six pictures to fill");
        }
    }

    [Fact]
    public void PaintMixesAreRealPaint()
    {
        var game = new PaintPotsGame(_content);
        Assert.Equal("orange", game.Mix("red", "yellow")?.Id);
        Assert.Equal("orange", game.Mix("yellow", "red")?.Id);
        Assert.Equal("green", game.Mix("blue", "yellow")?.Id);
        Assert.Equal("purple", game.Mix("red", "blue")?.Id);
        Assert.Equal("pink", game.Mix("red", "white")?.Id);
        Assert.Equal("grey", game.Mix("black", "white")?.Id);
        Assert.Null(game.Mix("red", "red"));
        Assert.Equal("blue+yellow", PaintPotsGame.Pair("yellow", "blue"));
    }

    [Fact]
    public void PaintPotsRoundsAreFair()
    {
        var game = new PaintPotsGame(_content);
        var modesSeen = new HashSet<PaintPotsGame.Mode>();
        var skills = new HashSet<string>();
        foreach (var level in Levels(game))
        {
            var pots = game.Entry.Level(level).Pots!;
            PlayLevel(game, level, (ulong)(400 + level), round =>
            {
                modesSeen.Add(round.Mode);
                skills.Add(game.SkillId(round));
                Assert.Single(game.CorrectChoices(round));
                Assert.Equal(pots, round.Pots.Select(p => p.Id));
                foreach (var pot in round.Pots)
                {
                    Assert.Contains(pot.Audio, _content.AudioIds);
                    Assert.Contains(game.Pot(pot.Id)!.Label, _content.PictureIds);
                }
                Assert.Contains(round.Target.Audio, _content.AudioIds);
                if (round.Mode == PaintPotsGame.Mode.Make)
                {
                    Assert.Equal(pots.Count * (pots.Count - 1) / 2, round.Choices.Count); // every pair of pots
                }
                else
                {
                    Assert.Equal(3, round.Choices.Count);
                    Assert.Equal(3, round.Choices.Distinct().Count());
                    Assert.All(round.Choices, c => Assert.Contains(game.Colour(c)!.Audio, _content.AudioIds));
                }
            });
        }
        Assert.Equal(Enum.GetValues<PaintPotsGame.Mode>().ToHashSet(), modesSeen);
        Assert.Equal(new HashSet<string> { "colour_mixing", "colour_light_dark" }, skills);
    }

    [Fact]
    public void MirrorMagicRoundsAreFair()
    {
        var game = new MirrorMagicGame(_content);
        var modesSeen = new HashSet<MirrorMagicGame.Mode>();
        var board = _content.Mirror.Pegs;
        foreach (var level in Levels(game))
        {
            PlayLevel(game, level, (ulong)(500 + level), round =>
            {
                modesSeen.Add(round.Mode);
                Assert.Contains(game.SkillId(round), game.Entry.Skills);
                if (round.Mode == MirrorMagicGame.Mode.Pegs)
                {
                    var placed = round.Picture.Sum(r => r.Count(ch => ch != MirrorMagicGame.Empty));
                    Assert.InRange(placed, board.Fewest, board.Most);
                    Assert.Equal(placed, round.PegsToPlace.Count);
                    Assert.Equal(board.Size * board.Size / 2, round.Choices.Count);
                    // Pegs plus their reflections fold onto themselves.
                    var finished = round.Picture.Select(r => r.ToCharArray()).ToArray();
                    foreach (var hole in round.PegsToPlace)
                    {
                        var rc = hole.Split(',').Select(int.Parse).ToArray();
                        finished[rc[0]][rc[1]] = 'P';
                    }
                    Assert.True(MirrorMagicGame.Symmetric(finished.Select(f => new string(f)).ToList(), round.Axis));
                    return;
                }
                Assert.Equal(3, round.Choices.Count);
                Assert.Single(game.CorrectChoices(round));
                if (round.Mode is MirrorMagicGame.Mode.Same or MirrorMagicGame.Mode.Finish)
                {
                    Assert.Equal(3, round.Candidates.Select(c => string.Join("/", c)).Distinct().Count());
                }
            });
        }
        Assert.Equal(Enum.GetValues<MirrorMagicGame.Mode>().ToHashSet(), modesSeen);
    }

    [Fact]
    public void AFinishRoundOffersTheUnflippedHalfWhenItLooksDifferent()
    {
        var game = new MirrorMagicGame(_content);
        var rng = new SeededGenerator(530);
        var learner = new Learner(Band.Stage1, 1, new HashSet<string>(), GameLevel: 1);
        var seen = 0;
        for (var i = 0; i < 100; i++)
        {
            var session = new GameSession(MirrorMagicGame.GameId, game.Skins[0], 1);
            if (session.NextRound(game, learner, rng) is not { } round) continue;
            var shown = round.ShownHalf;
            if (shown.SequenceEqual(MirrorMagicGame.Reflect(shown, round.Axis))) continue;
            Assert.Contains(round.Candidates, c => c.SequenceEqual(shown)); // the classic mistake is on offer
            seen += 1;
        }
        Assert.True(seen > 50);
    }
}
