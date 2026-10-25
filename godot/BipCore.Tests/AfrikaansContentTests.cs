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
    private static ContentLibrary Af => TestContent.Library(HomeLanguage.Afrikaans);

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
        // A level where more than one choice is right is fine when English has the same number of right
        // choices at that level by design (Word Rocket takes c or k for /k/, as Afrikaans takes v or f for
        // /f/; Mirror Magic's pegs). A round with nothing right is never excused.
        var english = Deal(TestContent.Library());
        var afrikaans = Deal(Af);
        var problems = afrikaans.Where(p => !p.StartsWith("level:") || !english.Contains(p)).ToList();
        Assert.True(problems.Count == 0, string.Join("\n", problems.Take(30)));
    }

    [Fact]
    public void EachAfrikaansSoundGroupHasItsOwnSkillAndBand()
    {
        // Which groups count as known for an older child comes from the skill's band, so it must be the
        // Afrikaans group's band, and every phonics skill must have a group to master.
        var course = new PhonicsCourse(Af);
        foreach (var group in course.Groups)
            Assert.Equal(group.Band, Af.Skill(PhonicsCourse.SkillIdForGroup(group.Number))!.Band);
        Assert.All(Af.Skills.Skills.Where(s => PhonicsCourse.GroupForSkill(s.Id) is not null),
                   s => Assert.NotNull(course.Group(PhonicsCourse.GroupForSkill(s.Id)!.Value)));

        // A stage 1 child still learns group 4 (v w j y ng), and a stage 2 child the two-letter vowels.
        var progress = new ChildProgress();
        Assert.False(progress.IsTreatedAsKnown(Af.Skill("snd_g4")!, Band.Stage1));
        Assert.False(progress.IsTreatedAsKnown(Af.Skill("snd_g7")!, Band.Stage2));
        Assert.True(progress.IsTreatedAsKnown(Af.Skill("snd_g6")!, Band.Stage2));
    }

    [Fact]
    public void EveryAfrikaansSkillCanBeUnlocked()
    {
        // With every skill mastered in turn, each one's prerequisites are all there to be known.
        foreach (var skill in Af.Skills.Skills)
            Assert.All(skill.Prerequisites, id => Assert.NotNull(Af.Skill(id)));
        Assert.Contains("snd_g7", Af.Skill("read_longer")!.Prerequisites);
    }

    [Fact]
    public void AfrikaansLetterNamesFollowTheSingleLetters()
    {
        // Afrikaans has no c, q, x or z sounds: names unlock once the 22 single letters are known (group 4).
        var course = new PhonicsCourse(Af);
        Assert.Equal(22, course.Letters.Count);
        Assert.False(course.LetterNamesUnlocked(course.SoundsUpToGroup(3).Select(s => s.Id)));
        Assert.True(course.LetterNamesUnlocked(course.SoundsUpToGroup(4).Select(s => s.Id)));
    }

    [Fact]
    public void WordsThatDontBlendStayOutOfTheSoundBySoundGames()
    {
        // hamer: the open syllable makes the a long and the e a schwa, so its sound buttons would
        // blend to another word. Sound Buttons and Word Rocket leave it out; the first-sound games keep it.
        var hamer = Af.Word("hamer")!;
        Assert.False(hamer.Blendable);
        Assert.True(Af.Word("kat")!.Blendable);
        var course = new PhonicsCourse(Af);
        var learner = new Learner(Band.Stage2, course.Groups[^1].Number, course.AllSounds.Select(s => s.Id).ToHashSet(), null);
        var rocket = new WordRocketGame(Af, course);
        Assert.DoesNotContain(hamer, rocket.Words(WordRocketGame.Mode.Longer, learner));
        var buttons = new SoundButtonsGame(Af, course);
        var rng = new SeededGenerator(3);
        for (var level = 0; level < (buttons.Entry.Levels?.Count ?? 1); level++)
            for (var visit = 0; visit < 20; visit++)
            {
                var session = GameSession.Start(buttons, Af.Games.Session, new HashSet<string>(), rng);
                while (session.NextRound(buttons, learner with { GameLevel = level }, rng) is SoundButtonsGame.Round round)
                    Assert.True(round.Word.Blendable, round.Word.Text);
            }
        Assert.All(TestContent.Library().Words.Words, w => Assert.True(w.Blendable, $"English {w.Text}"));
    }

    [Fact]
    public void ClipIdsArePlainAscii()
    {
        Assert.Equal("word_vlieer", AudioCatalogue.WordClip("vlieër"));
        Assert.Equal("word_vlieer", Af.Shapes.Shapes.First(s => s.Name == "vlieër").Audio);
        Assert.All(Af.Manifest.Audio, clip => Assert.Matches("^[a-z0-9_]+$", clip.Id));
    }

    /// <summary>Plays every game at every level for every group: "game: no rounds", "zero:game:n" for a round with nothing right, or "level:game:n:r" for a level with r right answers (r over 1).</summary>
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
                            if (right == 0) problems.Add($"zero:{game.Id}:{level}");
                            else if (right != 1) problems.Add($"level:{game.Id}:{level}:{right}");
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
