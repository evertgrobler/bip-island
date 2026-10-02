namespace BipCore;

/// <summary>
/// Letter Trace: follow the letter's path with the mouse while sparkles trail behind.
/// One sound per visit, like Meet the Sound. The "answer" is finishing the trace.
/// </summary>
public sealed class LetterTraceGame(ContentLibrary content, PhonicsCourse course) : IMiniGame<LetterTraceGame.Round, PhonicsSound>
{
    public const string GameId = "letter_trace";

    public sealed record Round(PhonicsSound Sound) : IGameRound<PhonicsSound>
    {
        public IReadOnlyList<PhonicsSound> Choices => [Sound];
        public IReadOnlyList<string> UsedItems => [Sound.Id];
    }

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("sparkles", "Sparkles"), new("snail_trail", "Snail trail"), new("paint", "Paint")];
    public int? RoundsPerSession => 1;

    /// <summary>
    /// The focus sound if there is one; otherwise the first unlocked sound the child hasn't met,
    /// so tracing follows meeting in teaching order.
    /// </summary>
    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var open = course.SoundsUpToGroup(learner.UnlockedPhonicsGroup)
            .Where(s => !session.UsedItems.Contains(s.Id)).ToList();
        if (learner.FocusSoundId is string focus && open.FirstOrDefault(s => s.Id == focus) is PhonicsSound focused)
        {
            return new Round(focused);
        }
        if (open.FirstOrDefault(s => !learner.KnownSoundIds.Contains(s.Id)) is PhonicsSound fresh) return new Round(fresh);
        return rng.Pick(open) is PhonicsSound any ? new Round(any) : null;
    }

    public bool IsCorrect(PhonicsSound choice, Round round) => choice.Id == round.Sound.Id;

    public string SkillId(Round round) => "letter_form";
}
