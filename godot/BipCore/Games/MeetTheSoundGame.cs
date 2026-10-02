namespace BipCore;

/// <summary>
/// Meet the Sound: the letter bounces in, the narrator says its pure sound, then the picture word.
/// One sound per visit. The "answer" is clicking the letter to hear it again.
/// </summary>
public sealed class MeetTheSoundGame(ContentLibrary content, PhonicsCourse course) : IMiniGame<MeetTheSoundGame.Round, PhonicsSound>
{
    public const string GameId = "meet_the_sound";

    public sealed record Round(PhonicsSound Sound) : IGameRound<PhonicsSound>
    {
        public IReadOnlyList<PhonicsSound> Choices => [Sound];
        public IReadOnlyList<string> UsedItems => [Sound.PictureWord];
    }

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("paper_desk", "Paper desk"), new("chalkboard", "Chalkboard"), new("sand", "Sand")];
    public int? RoundsPerSession => 1;

    /// <summary>The focus sound if there is one; otherwise the first unlocked sound the child hasn't met.</summary>
    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var open = course.SoundsUpToGroup(learner.UnlockedPhonicsGroup)
            .Where(s => !session.UsedItems.Contains(s.PictureWord)).ToList();
        if (learner.FocusSoundId is string focus && open.FirstOrDefault(s => s.Id == focus) is PhonicsSound focused)
        {
            return new Round(focused);
        }
        if (open.FirstOrDefault(s => !learner.KnownSoundIds.Contains(s.Id)) is PhonicsSound fresh) return new Round(fresh);
        return rng.Pick(open) is PhonicsSound any ? new Round(any) : null;
    }

    public bool IsCorrect(PhonicsSound choice, Round round) => choice.Id == round.Sound.Id;

    public string SkillId(Round round) => PhonicsCourse.SkillIdForGroup(round.Sound.Group);
}
