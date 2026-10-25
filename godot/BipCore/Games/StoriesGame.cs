namespace BipCore;

/// <summary>
/// Bip's Stories (Words Island, ages 5–10): a short story, one scene at a time, read aloud by the
/// narrator; then its questions, one at a time, each with two or three picture or word answers.
/// The stories (Content/stories/stories.json, each language its own) sit on scenes both languages
/// share (Content/stories/scenes.json). Game level n deals stories of level n + 1; a level with nothing
/// fresh left deals an easier story. A round is one question; a visit reads stories until it has asked
/// <see cref="QuestionsPerVisit"/> questions, always finishing the story it is on (three little level-1 stories, two level-2 stories, or one longer one).
/// </summary>
public sealed class StoriesGame(ContentLibrary content) : IMiniGame<StoriesGame.Round, StoryAnswer>
{
    public const string GameId = "bips_stories";
    public const int QuestionsPerVisit = 3;

    /// <param name="Number">The question's number in its story, from 1.</param>
    /// <param name="NewStory">True for a story's first question: the screen reads the story before asking.</param>
    public sealed record Round(Story Story, StorySceneSet Scenes, StoryQuestion Question, int Number,
                               IReadOnlyList<StoryAnswer> Choices, bool NewStory) : IGameRound<StoryAnswer>
    {
        public IReadOnlyList<string> UsedItems => [StoryItem(Story), QuestionItem(Story, Number)];
    }

    private readonly Dictionary<string, StorySceneSet> _sets = content.StoryScenes.Sets.ToDictionary(s => s.Id);

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("story_book", "Story book")];
    /// <summary>The most questions one story asks (from the content).</summary>
    public int MostQuestions { get; } = content.Stories.Stories.Select(s => s.Questions.Count).DefaultIfEmpty(1).Max();

    /// <summary>
    /// Room for the longest visit: a new story starts only while fewer than <see cref="QuestionsPerVisit"/>
    /// questions have been asked, so at most QuestionsPerVisit - 1 have gone before the last story, which then
    /// asks up to <see cref="MostQuestions"/> (a level-up mid-visit reaches 2 + 3 today). That visit ends through
    /// <see cref="GameSession.IsFinished"/>; shorter ones end when <see cref="MakeRound"/> has nothing more to ask.
    /// Either way the story being read is always finished.
    /// </summary>
    public int? RoundsPerSession => QuestionsPerVisit - 1 + MostQuestions;

    public IReadOnlyList<Story> Stories => content.Stories.Stories;

    public static string StoryItem(Story story) => $"story:{story.Id}";
    public static string QuestionItem(Story story, int number) => $"story:{story.Id}:q{number}";

    /// <summary>Story levels: one for each of the game's levels in games.json (the validator keeps them in step).</summary>
    public int Levels => Entry.LevelSteps.Count;

    /// <summary>The story level a game level deals (game levels count from 0, stories from 1).</summary>
    public int StoryLevel(int gameLevel) => Math.Clamp(gameLevel + 1, 1, Levels);

    public StorySceneSet ScenesOf(Story story) => _sets[story.Scenes];

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        // Carry on with the story being read: its next question not asked yet.
        var open = Stories.FirstOrDefault(s => session.UsedItems.Contains(StoryItem(s))
                                               && Enumerable.Range(1, s.Questions.Count).Any(n => !session.UsedItems.Contains(QuestionItem(s, n))));
        if (open != null)
        {
            var number = Enumerable.Range(1, open.Questions.Count).First(n => !session.UsedItems.Contains(QuestionItem(open, n)));
            return Ask(open, number, newStory: false, rng);
        }
        if (session.RoundsPlayed >= QuestionsPerVisit) return null;
        // A fresh story at the child's level, else the nearest easier level with one left.
        for (var level = StoryLevel(learner.GameLevel); level >= 1; level--)
        {
            var fresh = Stories.Where(s => s.Level == level && !session.UsedItems.Contains(StoryItem(s)) && _sets.ContainsKey(s.Scenes)
                                           && s.Questions.Count > 0).ToList();
            if (rng.Pick(fresh) is Story story) return Ask(story, 1, newStory: true, rng);
        }
        return null;
    }

    private Round Ask(Story story, int number, bool newStory, IRandomSource rng)
    {
        var question = story.Questions[number - 1];
        return new Round(story, _sets[story.Scenes], question, number, rng.Shuffled(question.Others.Prepend(question.Answer)), newStory);
    }

    public bool IsCorrect(StoryAnswer choice, Round round) =>
        string.Equals(choice.Text, round.Question.Answer.Text, StringComparison.OrdinalIgnoreCase) && choice.Picture == round.Question.Answer.Picture;

    /// <summary>"Why" and "how did they feel" read between the lines; who, where and what are in the words.</summary>
    public string SkillId(Round round) => round.Question.Kind is "why" or "feel" ? "story_inference" : "story_literal";

    // Clips (the validator lists the same names in each language's manifest).

    /// <summary>Each sentence's clip, scene by scene: vo_story_&lt;id&gt;_&lt;n&gt;, numbered through the story.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> SentenceClips(Story story)
    {
        var n = 0;
        return story.Text.Select(scene => (IReadOnlyList<string>)scene.Select(_ => $"vo_story_{story.Id}_{++n}").ToList()).ToList();
    }

    public static string QuestionClip(Story story, int number) => $"vo_story_{story.Id}_q{number}";

    /// <summary>
    /// An answer's clip: the word's own clip when it is one word that has one (word_dog), else
    /// vo_story_&lt;id&gt;_a&lt;n&gt;, numbering every choice through the story (each question's answer, then its others).
    /// </summary>
    public static string AnswerClip(Story story, StoryAnswer answer, IReadOnlySet<string> audioIds)
    {
        var word = AudioCatalogue.WordClip(answer.Text.ToLowerInvariant());
        if (!answer.Text.Contains(' ') && audioIds.Contains(word)) return word;
        var n = 0;
        foreach (var question in story.Questions)
            foreach (var choice in question.Others.Prepend(question.Answer))
            {
                n++;
                if (ReferenceEquals(choice, answer)) return $"vo_story_{story.Id}_a{n}";
            }
        throw new ArgumentException($"{answer.Text} isn't a choice in {story.Id}");
    }
}
