using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// The Afrikaans content (Content/af/, docs/LANGUAGES.md) plays: it loads over the shared files, and
/// every game deals rounds from it at every level for children at every sound group, each with exactly
/// one right answer. The English rounds are checked in detail by RoundGeneratorTests; this makes sure
/// the Afrikaans lists are rich enough for the same games.
/// </summary>
public sealed class AfrikaansContentTests
{
    private static readonly Lazy<ContentLibrary> Cached = new(() =>
        new ContentLibrary(file => File.ReadAllText(Path.Combine(TestContent.Directory, file)), HomeLanguage.Afrikaans,
                           file => File.Exists(Path.Combine(TestContent.Directory, file))));

    private static ContentLibrary Af => Cached.Value;

    [Fact]
    public void TheAfrikaansContentLoadsItsOwnWords()
    {
        Assert.Equal(HomeLanguage.Afrikaans, Af.Language);
        Assert.Equal("a", Af.Phonics.Graphemes[0].Id);
        Assert.NotNull(Af.Word("kat"));
        Assert.Null(Af.Word("cat"));
        Assert.Equal("sirkel", Af.Shapes.Shapes.First(s => s.Id == "circle").Name);
        // Shared, language-free data is the English file.
        Assert.Equal(TestContent.Library().Levels.Levels.Count, Af.Levels.Levels.Count);
    }

    [Fact]
    public void EveryGameDealsFairAfrikaansRounds()
    {
        // A level where more than one choice is right is fine when English does the same by design
        // (Word Rocket takes c or k for /k/, as Afrikaans takes v or f for /f/; Mirror Magic's pegs).
        var english = Deal(TestContent.Library());
        var afrikaans = Deal(Af);
        var problems = afrikaans.Where(p => !p.StartsWith("level:") || !english.Contains(p)).ToList();
        Assert.True(problems.Count == 0, string.Join("\n", problems.Take(30)));
    }

    /// <summary>Plays every game at every level for every group: "game: no rounds", or "level:game:n" for a level with n != 1 right answers.</summary>
    private static HashSet<string> Deal(ContentLibrary content)
    {
        var course = new PhonicsCourse(content);
        var rng = new SeededGenerator(7);
        var problems = new HashSet<string>();

        void Play<TRound, TChoice>(IMiniGame<TRound, TChoice> game) where TRound : class, IGameRound<TChoice>
        {
            var levels = Math.Max(1, game.Entry.Levels?.Count ?? 1);
            var dealt = 0;
            for (var level = 0; level < levels; level++)
            {
                foreach (var group in course.Groups)
                {
                    var open = course.SoundsUpToGroup(group.Number);
                    var learner = new Learner(Band.Stage2, group.Number, open.Select(s => s.Id).ToHashSet(), null, GameLevel: level);
                    for (var visit = 0; visit < 3; visit++)
                    {
                        var session = GameSession.Start(game, content.Games.Session, new HashSet<string>(), rng);
                        while (session.NextRound(game, learner, rng) is TRound round)
                        {
                            dealt++;
                            var right = round.Choices.Count(c => game.IsCorrect(c, round));
                            if (right != 1) problems.Add($"level:{game.Id}:{level}");
                        }
                    }
                }
            }
            if (dealt == 0) problems.Add($"{game.Id}: no rounds at all ({content.Language})");
        }

        Play(new MeetTheSoundGame(content, course));
        Play(new SoundHuntGame(content, course));
        Play(new BubblePopGame(content, course));
        Play(new LetterTraceGame(content, course));
        Play(new FeedMonsterGame(content, course));
        Play(new CountTapGame(content));
        Play(new QuickLookGame(content));
        Play(new BlockTowersGame(content));
        Play(new BipsBalanceGame(content));
        Play(new SoundButtonsGame(content, course));
        Play(new WordBuilderGame(content));
        Play(new WordRocketGame(content, course));
        Play(new MorningOrderGame(content));
        Play(new BipsPathGame(content));
        Play(new ShapeBuilderGame(content));
        Play(new PaintPotsGame(content));
        Play(new MirrorMagicGame(content));
        return problems;
    }
}
