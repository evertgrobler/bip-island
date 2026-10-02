namespace BipCore;

/// <summary>
/// Bubble Pop: letters float up; pop the one that makes the sound you hear. No timer: bubbles drift
/// round and round, and exactly one bubble always shows the right letter.
///
/// The other letters never sound the same as the target (c and k) or look the same (ow in cow
/// and ow in snow), so there is only ever one right answer.
/// </summary>
public sealed class BubblePopGame(ContentLibrary content, PhonicsCourse course) : IMiniGame<BubblePopGame.Round, PhonicsSound>
{
    public const string GameId = "bubble_pop";
    public const int BubbleCount = 5;

    /// <param name="Choices">The starting bubbles. Exactly one is the target.</param>
    /// <param name="Others">Letters a bubble may show when it floats back in. Never confusable with the target.</param>
    /// <param name="SpeedPercent">How fast the bubbles drift (100 = normal).</param>
    public sealed record Round(PhonicsSound Target, IReadOnlyList<PhonicsSound> Choices, IReadOnlyList<PhonicsSound> Others,
                               int SpeedPercent = 100) : IGameRound<PhonicsSound>
    {
        public IReadOnlyList<string> UsedItems => [];
    }

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("bubbles", "Bubbles"), new("balloons", "Balloons at the fair"), new("fireflies", "Fireflies at night")];

    /// <summary>The focus sound, or a random sound the child knows. Other letters come from every unlocked group.</summary>
    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var unlocked = course.SoundsUpToGroup(learner.UnlockedPhonicsGroup);
        var known = unlocked.Where(s => learner.KnownSoundIds.Contains(s.Id)).ToList();
        var target = known.FirstOrDefault(s => s.Id == learner.FocusSoundId) ?? rng.Pick(known);
        if (target is null) return null;
        var others = unlocked.Where(s => s.Id != target.Id && !s.IsConfusable(target)).ToList();
        if (others.Count == 0) return null;

        // More bubbles at higher levels. Different letters where there are enough, repeating only
        // when the pool is small.
        var step = this.Level(learner);
        var count = Math.Clamp(step.Bubbles ?? BubbleCount, 3, 8);
        var distractors = new List<PhonicsSound>();
        while (distractors.Count < count - 1)
        {
            distractors.AddRange(rng.Shuffled(others).Take(count - 1 - distractors.Count));
        }
        return new Round(target, rng.Shuffled(distractors.Prepend(target)), others, step.SpeedPercent ?? 100);
    }

    /// <summary>
    /// The letter for a bubble floating back in. If no other bubble shows the target, this one must,
    /// so the right answer is always there to find; otherwise it shows one of the other letters,
    /// so there is never more than one right bubble.
    /// </summary>
    public PhonicsSound NextBubble(Round round, IReadOnlyList<PhonicsSound> onScreen, IRandomSource rng)
    {
        if (!onScreen.Any(s => s.Id == round.Target.Id)) return round.Target;
        return rng.Pick(round.Others) ?? round.Target;
    }

    public bool IsCorrect(PhonicsSound choice, Round round) => choice.Id == round.Target.Id;

    public string SkillId(Round round) => PhonicsCourse.SkillIdForGroup(round.Target.Group);
}
