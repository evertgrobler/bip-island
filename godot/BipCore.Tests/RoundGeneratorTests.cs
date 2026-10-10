using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// "Every round is fair" (docs/GAMES.md): each game plays 1,000 random rounds for random children,
/// and every round must have exactly one right answer, use only content the child has unlocked,
/// and never ask for the same word twice in a session.
/// </summary>
public sealed class RoundGeneratorTests
{
    private const int RoundsPerGame = 1_000;

    private sealed class Fixture
    {
        public ContentLibrary Content { get; } = TestContent.Library();
        public PhonicsCourse Course { get; }
        public MeetTheSoundGame Meet { get; }
        public SoundHuntGame Hunt { get; }
        public BubblePopGame Pop { get; }
        public LetterTraceGame Trace { get; }
        public FeedMonsterGame Monster { get; }
        public CountTapGame Count { get; }
        public QuickLookGame Quick { get; }
        public SoundButtonsGame Buttons { get; }
        public WordBuilderGame Builder { get; }
        public MorningOrderGame Order { get; }
        public BipsPathGame Path { get; }

        public Fixture()
        {
            Course = new PhonicsCourse(Content);
            Meet = new MeetTheSoundGame(Content, Course);
            Hunt = new SoundHuntGame(Content, Course);
            Pop = new BubblePopGame(Content, Course);
            Trace = new LetterTraceGame(Content, Course);
            Monster = new FeedMonsterGame(Content, Course);
            Count = new CountTapGame(Content);
            Quick = new QuickLookGame(Content);
            Buttons = new SoundButtonsGame(Content, Course);
            Builder = new WordBuilderGame(Content);
            Order = new MorningOrderGame(Content);
            Path = new BipsPathGame(Content);
        }
    }

    private readonly Fixture _f = new();

    /// <summary>A random child: some groups unlocked, some sounds met, sometimes a focus sound.</summary>
    private Learner RandomLearner(IRandomSource rng)
    {
        var group = rng.NextInt(1, _f.Course.Groups.Count);
        var open = _f.Course.SoundsUpToGroup(group);
        var known = open.Where(_ => rng.NextBool()).Select(s => s.Id).ToHashSet();
        known.Add(rng.Pick(open)!.Id);
        var focus = rng.NextBool() ? rng.Pick(open.Where(s => known.Contains(s.Id)).ToList())?.Id : null;
        return new Learner(Band.Foundation, group, known, focus);
    }

    /// <summary>Plays whole sessions until <paramref name="count"/> rounds have been checked.</summary>
    private void PlaySessions<TRound, TChoice>(IMiniGame<TRound, TChoice> game, int count, ulong seed,
                                               Action<TRound, Learner, GameSession> check)
        where TRound : class, IGameRound<TChoice>
    {
        var rng = new SeededGenerator(seed);
        var played = 0;
        var sessions = 0;
        while (played < count)
        {
            var learner = RandomLearner(rng);
            var session = GameSession.Start(game, _f.Content.Games.Session, new HashSet<string>(), rng);
            while (session.NextRound(game, learner, rng) is TRound round)
            {
                check(round, learner, session);
                played += 1;
            }
            sessions += 1;
            Assert.True(sessions < count * 10, "too many empty sessions: the generator can't make rounds");
        }
    }

    private static int DistinctLists<T>(IEnumerable<IReadOnlyList<T>> lists) =>
        lists.Select(l => string.Join("\u0001", l)).Distinct().Count();

    // MARK: Sound Hunt

    [Fact]
    public void SoundHuntRoundsAreFair()
    {
        var answerPositions = new HashSet<int>();
        var targets = new HashSet<string>();
        PlaySessions(_f.Hunt, RoundsPerGame, 1, (round, learner, _) =>
        {
            Assert.Equal(SoundHuntGame.ChoiceCount, round.Choices.Count);
            Assert.True(round.Choices.Select(c => c.Word).Distinct().Count() == round.Choices.Count, "a picture twice in one round");

            // Exactly one right answer, by the game's own rule…
            Assert.Equal([round.Answer], _f.Hunt.CorrectChoices(round));
            // …and checked again straight from the content: only the answer starts with the target sound.
            var starting = round.Choices.Where(c => FirstSoundIpa(c.Word) == round.Target.Ipa).Select(c => c.Word);
            Assert.Equal([round.Answer.Word], starting);

            Assert.True(learner.KnownSoundIds.Contains(round.Target.Id), "asked about a sound the child hasn't met");
            Assert.True(round.Target.Group <= learner.UnlockedPhonicsGroup);
            foreach (var choice in round.Choices)
            {
                Assert.True(IsShowable(choice.Word, learner.UnlockedPhonicsGroup), $"{choice.Word} isn't decodable at group {learner.UnlockedPhonicsGroup}");
                Assert.True(_f.Content.PictureIds.Contains(choice.Picture), choice.Picture);
                Assert.True(_f.Content.AudioIds.Contains(choice.Audio), choice.Audio);
            }
            Assert.Contains(round.Target.SoundClip, _f.Content.AudioIds);
            answerPositions.Add(IndexOf(round.Choices, round.Answer));
            targets.Add(round.Target.Id);
        });
        Assert.Equal(new HashSet<int> { 0, 1, 2 }, answerPositions); // the answer should turn up in every position
        Assert.True(targets.Count > 20, "rounds should cover many different sounds");
    }

    [Fact]
    public void SoundHuntNeverRepeatsAWordInASession()
    {
        var rng = new SeededGenerator(2);
        for (var i = 0; i < 200; i++)
        {
            var learner = RandomLearner(rng);
            // A long session, to run each child out of fresh words.
            var session = new GameSession(SoundHuntGame.GameId, _f.Hunt.Skins[0], 500);
            var answers = new List<string>();
            while (session.NextRound(_f.Hunt, learner, rng) is SoundHuntGame.Round round) answers.Add(round.Answer.Word);
            Assert.True(answers.Count == answers.Distinct().Count(), "repeated: " + string.Join(", ", answers));
        }
    }

    [Fact]
    public void SoundHuntAlternatesWithTheFocusSound()
    {
        var rng = new SeededGenerator(3);
        var learner = new Learner(Band.Foundation, 2, new HashSet<string> { "p", "t", "m", "c" }, "p");
        var session = new GameSession(SoundHuntGame.GameId, _f.Hunt.Skins[0], 6);
        var first = session.NextRound(_f.Hunt, learner, rng);
        Assert.Equal("p", first?.Target.Id); // the first question is about the focus sound
        var second = session.NextRound(_f.Hunt, learner, rng);
        Assert.NotNull(second);
        Assert.NotEqual("p", second.Target.Id); // then another sound the child knows
    }

    [Fact]
    public void SoundHuntWithOnlyOneSoundMetEndsWhenItsPicturesRunOut()
    {
        var rng = new SeededGenerator(4);
        var learner = new Learner(Band.Foundation, 1, new HashSet<string> { "s" }, "s");
        var session = new GameSession(SoundHuntGame.GameId, _f.Hunt.Skins[0], 6);
        var round = session.NextRound(_f.Hunt, learner, rng);
        Assert.Equal("sun", round?.Answer.Word); // in group 1 only the sun starts with s
        Assert.Null(session.NextRound(_f.Hunt, learner, rng));
    }

    [Fact]
    public void SoundHuntOnlyOffersSoundsThatStartWords()
    {
        Assert.True(_f.Hunt.CanHunt(_f.Course.Sound("s")!, 1));
        Assert.False(_f.Hunt.CanHunt(_f.Course.Sound("ng")!, 9), "no word starts with ng");
        Assert.False(_f.Hunt.CanHunt(_f.Course.Sound("x")!, 9), "no word starts with x");
    }

    // MARK: Bubble Pop

    [Fact]
    public void BubblePopRoundsAreFair()
    {
        var rng = new SeededGenerator(5);
        PlaySessions(_f.Pop, RoundsPerGame, 6, (round, learner, _) =>
        {
            Assert.Equal(_f.Pop.Level(learner).Bubbles ?? BubblePopGame.BubbleCount, round.Choices.Count);
            Assert.Equal([round.Target], _f.Pop.CorrectChoices(round));
            Assert.Equal(1, LookAlikesOrSoundAlikes(round.Target, round.Choices));
            if (learner.FocusSoundId is string focus) Assert.Equal(focus, round.Target.Id);
            Assert.Contains(round.Target.Id, learner.KnownSoundIds);
            foreach (var letter in round.Choices.Concat(round.Others))
            {
                Assert.True(letter.Group <= learner.UnlockedPhonicsGroup, $"{letter.Id} isn't unlocked yet");
            }
            Assert.Contains(round.Target.SoundClip, _f.Content.AudioIds);

            // Bubbles float off the top and come back with new letters: still exactly one right one.
            var onScreen = round.Choices.ToList();
            for (var i = 0; i < 40; i++)
            {
                var index = rng.NextIndex(onScreen.Count);
                var others = onScreen.ToList();
                others.RemoveAt(index);
                onScreen[index] = _f.Pop.NextBubble(round, others, rng);
                Assert.Equal(1, LookAlikesOrSoundAlikes(round.Target, onScreen));
            }
        });
    }

    [Fact]
    public void BubblePopUsesDifferentLettersWhenThereAreEnough()
    {
        var rng = new SeededGenerator(7);
        // Level 2 has five bubbles; group 1 has five other letters to show.
        var learner = new Learner(Band.Foundation, 1, new HashSet<string> { "s", "a" }, "s", GameLevel: 1);
        Assert.Equal(5, _f.Pop.Level(learner).Bubbles);
        for (var i = 0; i < 100; i++)
        {
            var session = new GameSession(BubblePopGame.GameId, _f.Pop.Skins[0], 1);
            var round = session.NextRound(_f.Pop, learner, rng);
            Assert.NotNull(round);
            Assert.Equal(5, round.Choices.Select(c => c.Id).Distinct().Count()); // group 1 has five other letters to show
        }
    }

    // MARK: Meet the Sound

    [Fact]
    public void MeetTheSoundRoundsAreFair()
    {
        PlaySessions(_f.Meet, RoundsPerGame, 8, (round, learner, session) =>
        {
            Assert.Equal(1, session.MaxRounds); // one sound per visit
            Assert.Equal([round.Sound.Id], _f.Meet.CorrectChoices(round).Select(s => s.Id));
            Assert.True(round.Sound.Group <= learner.UnlockedPhonicsGroup);
            if (learner.FocusSoundId is string focus) Assert.Equal(focus, round.Sound.Id);
            Assert.Contains(round.Sound.SoundClip, _f.Content.AudioIds);
            Assert.Contains(round.Sound.WordClip, _f.Content.AudioIds);
            Assert.Contains(round.Sound.Picture, _f.Content.PictureIds);
        });
    }

    [Fact]
    public void MeetTheSoundIntroducesTheNextNewSoundInOrder()
    {
        var rng = new SeededGenerator(9);
        var learner = new Learner(Band.Foundation, 1, new HashSet<string> { "s", "a" });
        var session = new GameSession(MeetTheSoundGame.GameId, _f.Meet.Skins[0], 10);
        var met = new List<string>();
        while (session.NextRound(_f.Meet, learner, rng) is MeetTheSoundGame.Round round) met.Add(round.Sound.Id);
        Assert.Equal(["t", "p", "i", "n"], met.Take(4));
        Assert.Equal(met.Count, met.Distinct().Count()); // no sound twice in a session
        Assert.Equal(6, met.Count); // then the known ones, until all six have been used
    }

    // MARK: Letter Trace

    [Fact]
    public void LetterTraceRoundsAreFair()
    {
        PlaySessions(_f.Trace, RoundsPerGame, 20, (round, learner, _) =>
        {
            Assert.Equal([round.Sound.Id], _f.Trace.CorrectChoices(round).Select(s => s.Id));
            Assert.True(round.Sound.Group <= learner.UnlockedPhonicsGroup);
            if (learner.FocusSoundId is string focus) Assert.Equal(focus, round.Sound.Id);
            Assert.Contains(round.Sound.SoundClip, _f.Content.AudioIds);
        });
    }

    // MARK: Feed the Monster

    [Fact]
    public void FeedMonsterRoundsAreFair()
    {
        var answerPositions = new HashSet<int>();
        var targets = new HashSet<string>();
        PlaySessions(_f.Monster, RoundsPerGame, 21, (round, learner, _) =>
        {
            Assert.Equal(FeedMonsterGame.ChoiceCount, round.Choices.Count);
            Assert.True(round.Choices.Select(c => c.Word).Distinct().Count() == round.Choices.Count, "a food twice in one round");

            // Exactly one right food, by the game's own rule…
            Assert.Equal([round.Answer], _f.Monster.CorrectChoices(round));
            // …and checked again straight from the content: only the answer starts with the target sound.
            var starting = round.Choices.Where(c => FirstSoundIpa(c.Word) == round.Target.Ipa).Select(c => c.Word);
            Assert.Equal([round.Answer.Word], starting);

            Assert.True(learner.KnownSoundIds.Contains(round.Target.Id), "asked about a sound the child hasn't met");
            Assert.True(round.Target.Group <= learner.UnlockedPhonicsGroup);
            foreach (var food in round.Choices)
            {
                var banked = _f.Content.Word(food.Word);
                Assert.True(banked is not null, $"{food.Word} isn't in the word bank");
                Assert.True(banked.DecodableFromGroup <= learner.UnlockedPhonicsGroup);
                Assert.True(_f.Content.PictureIds.Contains(food.Picture), food.Picture);
                Assert.True(_f.Content.AudioIds.Contains(food.Audio), food.Audio);
            }
            Assert.Contains(round.Target.SoundClip, _f.Content.AudioIds);
            answerPositions.Add(IndexOf(round.Choices, round.Answer));
            targets.Add(round.Target.Id);
        });
        Assert.Equal(new HashSet<int> { 0, 1, 2 }, answerPositions); // the answer should turn up in every position
        Assert.True(targets.Count > 5, "rounds should cover several sounds with foods");
    }

    [Fact]
    public void FeedMonsterNeverRepeatsAFoodInASession()
    {
        var rng = new SeededGenerator(22);
        for (var i = 0; i < 200; i++)
        {
            var learner = RandomLearner(rng);
            var session = new GameSession(FeedMonsterGame.GameId, _f.Monster.Skins[0], 500);
            var answers = new List<string>();
            while (session.NextRound(_f.Monster, learner, rng) is FeedMonsterGame.Round round) answers.Add(round.Answer.Word);
            Assert.True(answers.Count == answers.Distinct().Count(), "repeated: " + string.Join(", ", answers));
        }
    }

    // MARK: Count & Tap

    [Fact]
    public void CountTapRoundsAreFair()
    {
        var answerPositions = new HashSet<int>();
        PlaySessions(_f.Count, RoundsPerGame, 23, (round, _, _) =>
        {
            Assert.Equal(3, round.Choices.Count);
            Assert.Equal(3, round.Choices.Distinct().Count()); // two identical numerals
            Assert.Equal([round.Count], _f.Count.CorrectChoices(round));
            Assert.InRange(round.Count, 1, 20);
            Assert.Contains(round.Object, _f.Content.Numbers.CountingObjects);
            Assert.True(_f.Content.PictureIds.Contains(round.Object.Picture), round.Object.Picture);
            Assert.True(_f.Content.AudioIds.Contains(round.Object.AudioPlural), round.Object.AudioPlural);
            Assert.Contains(AudioCatalogue.NumberClip(round.Count), _f.Content.AudioIds);
            answerPositions.Add(IndexOf(round.Choices, round.Count));
        });
        Assert.Equal(new HashSet<int> { 0, 1, 2 }, answerPositions); // the answer should turn up in every position
    }

    // MARK: Quick Look

    [Fact]
    public void QuickLookRoundsAreFair()
    {
        PlaySessions(_f.Quick, RoundsPerGame, 24, (round, _, _) =>
        {
            Assert.Equal(3, round.Choices.Count);
            Assert.Equal(3, round.Choices.Distinct().Count()); // two identical numerals
            Assert.Equal([round.Count], _f.Quick.CorrectChoices(round));
            // Random learners are foundation band at level 1: they see up to 5 at a glance.
            Assert.InRange(round.Count, 1, 5);
            Assert.Contains(AudioCatalogue.NumberClip(round.Count), _f.Content.AudioIds);
        });
    }

    [Fact]
    public void QuickLookReachesTenAtTheTopLevel()
    {
        var rng = new SeededGenerator(25);
        // Levels set the range (3, 5, 8, then 10); the top level sees 1 to 10.
        var learner = new Learner(Band.Stage1, 9, _f.Course.AllSounds.Select(s => s.Id).ToHashSet())
            .AtLevel(_f.Quick.Entry.LevelSteps.Count - 1);
        var seen = new HashSet<int>();
        for (var i = 0; i < 200; i++)
        {
            var session = new GameSession(QuickLookGame.GameId, _f.Quick.Skins[0], 1);
            if (session.NextRound(_f.Quick, learner, rng) is QuickLookGame.Round round) seen.Add(round.Count);
        }
        Assert.Equal(Enumerable.Range(1, 10).ToHashSet(), seen); // the top level sees 1 to 10
    }

    // MARK: Sound Buttons

    [Fact]
    public void SoundButtonsRoundsAreFair()
    {
        var answerPositions = new HashSet<int>();
        var answers = new HashSet<string>();
        PlaySessions(_f.Buttons, RoundsPerGame, 26, (round, learner, _) =>
        {
            Assert.Equal(SoundButtonsGame.ChoiceCount, round.Choices.Count);
            Assert.True(round.Choices.Select(c => c.Word).Distinct().Count() == round.Choices.Count, "a picture twice in one round");
            Assert.Equal([round.Answer.Word], _f.Buttons.CorrectChoices(round).Select(c => c.Word));
            // Random learners are foundation band: three-sound decodable words only.
            Assert.Equal(3, round.Word.SoundCount);
            Assert.True(round.Word.DecodableFromGroup <= learner.UnlockedPhonicsGroup);
            foreach (var choice in round.Choices)
            {
                Assert.True(_f.Content.PictureIds.Contains(choice.Picture), choice.Picture);
                Assert.True(_f.Content.AudioIds.Contains(choice.Audio), choice.Audio);
            }
            answerPositions.Add(IndexOf(round.Choices, round.Answer));
            answers.Add(round.Answer.Word);
        });
        Assert.Equal(new HashSet<int> { 0, 1, 2 }, answerPositions); // the answer should turn up in every position
        Assert.True(answers.Count > 20, "rounds should cover many different words");
    }

    // MARK: Word Builder

    [Fact]
    public void WordBuilderRoundsAreFair()
    {
        var answers = new HashSet<string>();
        PlaySessions(_f.Builder, RoundsPerGame, 27, (round, learner, _) =>
        {
            Assert.Equal(WordBuilderGame.ChoiceCount, round.Choices.Count);
            var right = Assert.Single(_f.Builder.CorrectChoices(round));
            Assert.Equal(round.Answer, right);
            Assert.Equal(round.Word.Graphemes, round.Answer); // the answer must spell the word
            Assert.Equal(3, round.Word.SoundCount); // foundation band builds 3-sound words
            Assert.True(round.Word.DecodableFromGroup <= learner.UnlockedPhonicsGroup);
            foreach (var choice in round.Choices)
            {
                Assert.Equal(round.Answer.Order(StringComparer.Ordinal), choice.Order(StringComparer.Ordinal)); // made of the word's tiles
            }
            Assert.True(DistinctLists(round.Choices) == round.Choices.Count, "two identical tile rows");
            answers.Add(round.Word.Text);
        });
        Assert.True(answers.Count > 20, "rounds should cover many different words");
    }

    // MARK: Morning Order

    [Fact]
    public void MorningOrderRoundsAreFair()
    {
        var seenSets = new HashSet<string>();
        PlaySessions(_f.Order, RoundsPerGame, 28, (round, learner, _) =>
        {
            Assert.Equal(MorningOrderGame.ChoiceCount, round.Choices.Count);
            var right = Assert.Single(_f.Order.CorrectChoices(round));
            Assert.Equal(round.Answer, right);
            Assert.Equal(Enumerable.Range(1, round.Set.Cards.Count), round.Answer.Select(c => c.N)); // runs first-to-last
            Assert.True(round.Set.Band <= learner.Band);
            foreach (var choice in round.Choices) Assert.Equal(round.Answer, choice.OrderBy(c => c.N)); // made of the set's cards
            Assert.True(DistinctLists(round.Choices.Select(c => (IReadOnlyList<int>)c.Select(card => card.N).ToList())) == round.Choices.Count,
                        "two identical orders");
            seenSets.Add(round.Set.Id);
        });
        Assert.True(seenSets.Count > 2, "rounds should cover several routines");
    }

    /// <summary>Each level trims stories to its card count (3, then 4, then whole stories) and stays fair.</summary>
    [Fact]
    public void MorningOrderLevelsTrimStories()
    {
        var steps = _f.Order.Entry.LevelSteps;
        Assert.True(steps.Count > 1, "Morning Order should have levels");
        var rng = new SeededGenerator(31);
        for (var index = 0; index < steps.Count; index++)
        {
            var seenLengths = new HashSet<int>();
            for (var i = 0; i < 200; i++)
            {
                var child = new Learner(Band.Stage2, 1, new HashSet<string> { "s" }, GameLevel: index);
                var session = new GameSession(MorningOrderGame.GameId, _f.Order.Skins[0], 8);
                if (session.NextRound(_f.Order, child, rng) is not MorningOrderGame.Round round) continue;
                var count = round.Set.Cards.Count;
                seenLengths.Add(count);
                Assert.True(count >= MorningOrderGame.MinimumCards);
                if (steps[index].Cards is int limit) Assert.True(count <= Math.Max(limit, MorningOrderGame.MinimumCards));
                Assert.Equal(Enumerable.Range(1, count), round.Answer.Select(c => c.N)); // a trimmed story still starts at step 1
                Assert.Single(_f.Order.CorrectChoices(round));
            }
            Assert.True(seenLengths.Count > 0, $"level {index} made no rounds");
        }
    }

    // MARK: Bip's Path

    [Fact]
    public void BipsPathRoundsAreFair()
    {
        var answerPositions = new HashSet<int>();
        var seenLevels = new HashSet<string>();
        PlaySessions(_f.Path, RoundsPerGame, 29, (round, learner, _) =>
        {
            Assert.Equal(BipsPathGame.ChoiceCount, round.Choices.Count);
            var right = Assert.Single(_f.Path.CorrectChoices(round));
            Assert.Equal(round.Answer, right);
            Assert.Equal(round.Level.OptimalProgram, round.Answer);
            Assert.True(round.Level.Band <= learner.Band);
            // Checked with the interpreter, not by trust: only the answer reaches the battery.
            foreach (var choice in round.Choices)
            {
                Assert.True(GridWalker.ReachesGoal(choice, round.Level) == choice.SequenceEqual(round.Answer),
                            $"{round.Level.Id}: {string.Join(" ", choice)}");
            }
            Assert.True(DistinctLists(round.Choices) == round.Choices.Count, "two identical programs");
            answerPositions.Add(round.Choices.ToList().FindIndex(c => c.SequenceEqual(round.Answer)));
            seenLevels.Add(round.Level.Id);
        });
        Assert.Equal(new HashSet<int> { 0, 1, 2 }, answerPositions); // the answer should turn up in every position
        Assert.True(seenLevels.Count > 5, "rounds should cover several levels");
    }

    [Fact]
    public void GridWalkerTracksWhichWayBipFaces()
    {
        foreach (var level in _f.Content.Levels.Levels.Where(l => l.Game == BipsPathGame.GameId))
        {
            var program = level.OptimalProgram;
            var facings = GridWalker.Facings(program, level);
            Assert.Equal(GridWalker.Path(program, level).Positions.Count, facings.Count);
            Assert.Equal(level.StartFacing, facings[0].Key());
            if (!program.Any(b => b.StartsWith("turn", StringComparison.Ordinal)))
            {
                Assert.True(facings.All(f => f.Key() == level.StartFacing), "arrow blocks never turn Bip");
            }
        }
        // A crash stops the facings where the path stops.
        var first = _f.Content.Levels.Levels.First(l => l.Game == BipsPathGame.GameId);
        var offGrid = Enumerable.Repeat("up", first.Grid.Rows + 1).ToList();
        Assert.Equal(GridWalker.Path(offGrid, first).Positions.Count, GridWalker.Facings(offGrid, first).Count);
    }

    [Fact]
    public void GridWalkerBasics()
    {
        var levels = _f.Content.Levels.Levels.Where(l => l.Game == BipsPathGame.GameId).ToList();
        Assert.True(levels.Count > 10);
        foreach (var level in levels)
        {
            Assert.True(level.Start != level.Goal, $"{level.Id} starts on its goal");
            Assert.True(GridWalker.ReachesGoal(level.OptimalProgram, level), $"{level.Id}: the content's own solution doesn't reach the battery");
            Assert.False(GridWalker.ReachesGoal([], level), $"{level.Id}: doing nothing shouldn't win");
        }
        var bottomRow = levels.FirstOrDefault(l => l.Start.Row == l.Grid.Rows - 1);
        Assert.True(bottomRow is not null, "no level starts on the bottom row");
        Assert.False(GridWalker.ReachesGoal(["down"], bottomRow), "walking off the grid should fail");
    }

    // MARK: The template

    [Fact]
    public void EveryGameListsAtLeastThreeSkins()
    {
        IMiniGame[] games = [_f.Meet, _f.Hunt, _f.Pop, _f.Trace, _f.Monster, _f.Count, _f.Quick, _f.Buttons, _f.Builder, _f.Order, _f.Path,
                             new WordRocketGame(_f.Content, _f.Course)];
        foreach (var game in games) Assert.True(game.Skins.Count >= 3, game.Id);
    }

    [Fact]
    public void SessionsPickOnlySkinsTheAppCanDraw()
    {
        var rng = new SeededGenerator(10);
        var drawable = new HashSet<string> { "balloons", "fireflies" };
        for (var i = 0; i < 20; i++)
        {
            var session = GameSession.Start(_f.Pop, _f.Content.Games.Session, drawable, rng);
            Assert.Contains(session.Skin.Id, drawable);
            Assert.Equal(_f.Content.Games.Session.RoundsPerSession, session.MaxRounds);
        }
        var fallback = GameSession.Start(_f.Pop, _f.Content.Games.Session, new HashSet<string>(), rng);
        Assert.Equal(_f.Pop.Skins[0], fallback.Skin);
    }

    [Fact]
    public void ASessionStopsAfterItsRounds()
    {
        var rng = new SeededGenerator(11);
        var learner = new Learner(Band.Foundation, 3, _f.Course.SoundsUpToGroup(3).Select(s => s.Id).ToHashSet());
        var session = new GameSession(BubblePopGame.GameId, _f.Pop.Skins[0], 6);
        var count = 0;
        while (session.NextRound(_f.Pop, learner, rng) is not null) count += 1;
        Assert.Equal(6, count);
        Assert.True(session.IsFinished);
    }

    [Fact]
    public void NothingToAskMeansNoRound()
    {
        var rng = new SeededGenerator(12);
        var nobody = new Learner(Band.Foundation, 1, new HashSet<string>());
        var hunt = new GameSession(SoundHuntGame.GameId, _f.Hunt.Skins[0], 6);
        var pop = new GameSession(BubblePopGame.GameId, _f.Pop.Skins[0], 6);
        Assert.Null(hunt.NextRound(_f.Hunt, nobody, rng)); // no sounds met yet: nothing to hunt
        Assert.Null(pop.NextRound(_f.Pop, nobody, rng)); // no sounds met yet: nothing to pop
    }

    // MARK: Independent checks, straight from the content

    /// <summary>
    /// The IPA of a word's first sound: from the word bank, or (for a sound's own picture not in the
    /// bank, like ink) from the grapheme it belongs to.
    /// </summary>
    private string? FirstSoundIpa(string word)
    {
        if (_f.Content.Word(word) is Word banked) return _f.Content.Grapheme(banked.FirstSound)?.Ipa;
        return _f.Content.Phonics.Graphemes.FirstOrDefault(g => g.MnemonicWord == word)?.Ipa;
    }

    /// <summary>
    /// Words may only be shown once their phonics group is unlocked, except a sound's own picture,
    /// which the child meets with that sound.
    /// </summary>
    private bool IsShowable(string word, int group)
    {
        if (_f.Content.Word(word) is Word banked && banked.DecodableFromGroup <= group) return true;
        return _f.Content.Phonics.Graphemes.Any(g => g.MnemonicWord == word && g.Group <= group);
    }

    private static int LookAlikesOrSoundAlikes(PhonicsSound target, IEnumerable<PhonicsSound> letters) =>
        letters.Count(l => l.Ipa == target.Ipa || l.Grapheme == target.Grapheme);

    private static int IndexOf<T>(IReadOnlyList<T> items, T item) => items.ToList().IndexOf(item);
}
