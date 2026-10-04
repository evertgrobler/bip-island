using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// Word Rocket: every level's rounds are fair (one right spelling, one window per letter, only
/// content the child has unlocked, nothing asked twice in a visit), and keys count in either case.
/// </summary>
public sealed class WordRocketTests
{
    private const int RoundsPerLevel = 1_000;

    private readonly ContentLibrary _content = TestContent.Library();
    private readonly PhonicsCourse _course;
    private readonly WordRocketGame _game;

    public WordRocketTests()
    {
        _course = new PhonicsCourse(_content);
        _game = new WordRocketGame(_content, _course);
    }

    private Learner RandomLearner(IRandomSource rng, int level)
    {
        var group = rng.NextInt(1, _course.Groups.Count);
        var known = _course.SoundsUpToGroup(group).Where(_ => rng.NextBool()).Select(s => s.Id).ToHashSet();
        var focus = rng.NextBool() ? rng.Pick(_course.SoundsUpToGroup(group))?.Id : null;
        return new Learner(BandExtensions.All[rng.NextIndex(BandExtensions.All.Count)], group, known, focus, level);
    }

    private void PlaySessions(int level, ulong seed, Action<WordRocketGame.Round, Learner, GameSession> check)
    {
        var rng = new SeededGenerator(seed);
        var played = 0;
        var sessions = 0;
        while (played < RoundsPerLevel)
        {
            var learner = RandomLearner(rng, level);
            var session = GameSession.Start(_game, _content.Games.Session, new HashSet<string>(), rng);
            var asked = new HashSet<string>();
            while (session.NextRound(_game, learner, rng) is { } round)
            {
                Assert.True(asked.Add(round.UsedItems.Single()), $"{round.Answer} asked twice in one visit");
                check(round, learner, session);
                played += 1;
            }
            sessions += 1;
            Assert.True(sessions < RoundsPerLevel * 10, "too many empty sessions: the generator can't make rounds");
        }
    }

    private static string Typed(WordRocketGame.Round round) => string.Concat(round.Slots.Select(s => s.Letter));

    [Fact]
    public void TheGameHasFourLevelsEasiestFirst()
    {
        var levels = _game.Entry.LevelSteps;
        Assert.Equal(4, levels.Count);
        Assert.Equal(["letter", "cvc", "longer", "tricky"], levels.Select(l => Assert.Single(l.Modes!)));
        Assert.Equal(new AgeRange(4, 8), _game.Entry.Ages);
        Assert.Equal(0, _game.Entry.StartingLevel(Band.Foundation));
        Assert.Equal(3, _game.Entry.StartingLevel(Band.Stage3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryRoundIsFair(int level)
    {
        var modes = new HashSet<WordRocketGame.Mode>();
        PlaySessions(level, (ulong)(40 + level), (round, learner, _) =>
        {
            modes.Add(round.Mode);
            Assert.True(round.Mode <= _game.LevelMode(learner), "a level never asks something harder than itself");
            Assert.InRange(round.Slots.Count, 1, WordRocketGame.MaxLetters);
            Assert.Equal(round.Answer.Length, round.Slots.Count); // one window per letter
            Assert.Equal(round.Answer, Typed(round));
            Assert.All(round.Slots, slot => Assert.Matches("^[a-z]$", slot.Letter));
            Assert.All(round.Slots, slot => Assert.Contains(slot.Letter, slot.Keys));
            Assert.All(round.Slots, slot => Assert.True(_content.AudioIds.Contains(slot.HintClip), $"no clip {slot.HintClip}"));
            Assert.True(_content.AudioIds.Contains(round.Clip), $"no clip {round.Clip}");
            Assert.True(_game.IsCorrect(round.Answer, round));
            Assert.True(_game.IsCorrect(round.Answer.ToUpperInvariant(), round), "upper case counts too");
            Assert.Contains(round.Answer, round.Choices);
            switch (round.Mode)
            {
                case WordRocketGame.Mode.Letter:
                    var sound = _course.Sound(round.SoundId!)!;
                    Assert.True(sound.Group <= learner.UnlockedPhonicsGroup, "only sounds from unlocked groups");
                    Assert.Equal(sound.SoundClip, round.Clip); // the sound, never the letter's name
                    Assert.StartsWith("snd_", round.Clip);
                    // Every right key makes the sound Bip said (c and k), and no other key does.
                    var right = _game.CorrectChoices(round);
                    Assert.Contains(round.Answer, right);
                    Assert.All(right, key => Assert.Equal(sound.Ipa, _course.SoundsUpToGroup(9).First(s => s.Grapheme == key).Ipa));
                    Assert.Equal(26, round.Choices.Count);
                    break;
                case WordRocketGame.Mode.Cvc or WordRocketGame.Mode.Longer:
                    var word = round.Word!;
                    Assert.True(word.DecodableFromGroup <= learner.UnlockedPhonicsGroup);
                    Assert.Equal(word.Audio, round.Clip);
                    if (round.Mode == WordRocketGame.Mode.Cvc) Assert.True(word.SoundCount == 3 && word.Text.Length == 3);
                    else Assert.True(word.Text.Length >= 4);
                    Assert.Single(_game.CorrectChoices(round));
                    // Each window's hint is the sound its letter belongs to.
                    var hints = word.Graphemes.Select(id => _course.Sound(id)!)
                        .SelectMany(s => Enumerable.Repeat(s.SoundClip, s.Grapheme.Length));
                    Assert.Equal(hints, round.Slots.Select(s => s.HintClip));
                    // One sound said per sound in the word, on its last letter.
                    Assert.Equal(word.Graphemes.Count, round.Slots.Count(s => s.EndsSound));
                    Assert.True(round.Slots[^1].EndsSound);
                    break;
                default:
                    Assert.Contains(round.Answer, _content.TrickyWords.Stage2.Words);
                    Assert.Single(_game.CorrectChoices(round));
                    // Letter names only once they're taught (after group 5); before that the word again.
                    var names = PhonicsCourse.LetterNamesUnlocked(learner.KnownSoundIds);
                    Assert.All(round.Slots, s => Assert.StartsWith(names && s.HintClip.StartsWith("name_") ? "name_" : "word_", s.HintClip));
                    break;
            }
            Assert.False(string.IsNullOrEmpty(_game.SkillId(round)));
            Assert.NotNull(_content.Skill(_game.SkillId(round)));
        });
        Assert.Contains(_game.LevelMode(new Learner(Band.Foundation, 9, new HashSet<string>(), GameLevel: level)), modes);
    }

    [Fact]
    public void ALetterLevelOnlyAsksSingleLettersFromUnlockedGroups()
    {
        var rng = new SeededGenerator(50);
        var learner = new Learner(Band.Foundation, 1, new HashSet<string>());
        var session = GameSession.Start(_game, _content.Games.Session, new HashSet<string>(), rng);
        var letters = new List<string>();
        while (session.NextRound(_game, learner, rng) is { } round) letters.Add(round.Answer);
        // Group 1 is s a t p i n: six letters, then the visit ends rather than repeating one.
        Assert.Equal(["a", "i", "n", "p", "s", "t"], letters.Order());
    }

    [Fact]
    public void CAndKBothFillTheKSound()
    {
        var c = _course.Sound("c")!;
        var learner = new Learner(Band.Foundation, 2, new HashSet<string>(), "c");
        var session = new GameSession(WordRocketGame.GameId, _game.Skins[0], 8);
        var round = session.NextRound(_game, learner, new SeededGenerator(51))!;
        Assert.Equal(c.SoundClip, round.Clip);
        Assert.True(WordRocketGame.IsRightKey(round, 0, "c"));
        Assert.True(WordRocketGame.IsRightKey(round, 0, "K"));
        Assert.False(WordRocketGame.IsRightKey(round, 0, "g"));
    }

    [Fact]
    public void KeysCountInEitherCaseAndOnlyInTheirWindow()
    {
        var learner = new Learner(Band.Stage1, 1, new HashSet<string>(), GameLevel: 1);
        var session = new GameSession(WordRocketGame.GameId, _game.Skins[0], 8);
        var round = session.NextRound(_game, learner, new SeededGenerator(52))!;
        Assert.Equal(WordRocketGame.Mode.Cvc, round.Mode);
        for (var i = 0; i < round.Slots.Count; i++)
        {
            Assert.True(WordRocketGame.IsRightKey(round, i, round.Answer[i].ToString()));
            Assert.True(WordRocketGame.IsRightKey(round, i, round.Answer[i].ToString().ToUpperInvariant()));
        }
        Assert.False(WordRocketGame.IsRightKey(round, round.Slots.Count, "a")); // past the last window
        Assert.False(WordRocketGame.IsRightKey(round, 0, "1"));
        Assert.False(_game.IsCorrect(round.Answer[..^1], round));
    }

    [Fact]
    public void AHarderLevelUsesEasierWordsWhenItRunsOut()
    {
        // Group 1 has only four longer words (snap, spin, ants, pins): a visit carries on with three-letter words.
        var rng = new SeededGenerator(53);
        var learner = new Learner(Band.Stage2, 1, new HashSet<string>(), GameLevel: 2);
        var session = GameSession.Start(_game, _content.Games.Session, new HashSet<string>(), rng);
        var modes = new List<WordRocketGame.Mode>();
        while (session.NextRound(_game, learner, rng) is { } round) modes.Add(round.Mode);
        Assert.Equal(_content.Games.Session.RoundsPerSession, modes.Count);
        Assert.Equal(4, modes.Count(m => m == WordRocketGame.Mode.Longer));
        Assert.Contains(WordRocketGame.Mode.Cvc, modes);
    }

    [Fact]
    public void TrickyWordsSkipCapitalsAndSplitDigraphWordsStayOut()
    {
        Assert.DoesNotContain("Mr", _game.TrickyWords);
        Assert.DoesNotContain("Mrs", _game.TrickyWords);
        var everyone = new Learner(Band.Stage3, 9, new HashSet<string>());
        Assert.DoesNotContain(_game.Words(WordRocketGame.Mode.Longer, everyone), w => w.Text == "cake");
    }
}
