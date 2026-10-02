namespace BipCore;

/// <summary>A food card for Feed the Monster.</summary>
/// <param name="FirstSoundId">Grapheme id of the first sound ("m" for milk).</param>
/// <param name="FirstSoundIpa">The first sound itself, so corn and kite both start with /k/.</param>
/// <param name="AvailableFromGroup">The phonics group from which the food may be shown.</param>
public sealed record MonsterFood(string Word, string Picture, string Audio, string FirstSoundId,
                                 string FirstSoundIpa, int AvailableFromGroup);

/// <summary>
/// Feed the Monster: drag only the foods that start with the sound into a hungry monster.
/// One right food hides among the wrong ones. Foods come from the word bank's tagged foods,
/// so the game opens up as groups unlock.
/// </summary>
public sealed class FeedMonsterGame : IMiniGame<FeedMonsterGame.Round, MonsterFood>
{
    public const string GameId = "feed_the_monster";
    public const int ChoiceCount = 3;

    public sealed record Round(PhonicsSound Target, IReadOnlyList<MonsterFood> Choices, MonsterFood Answer) : IGameRound<MonsterFood>
    {
        public IReadOnlyList<string> UsedItems => [Answer.Word];
    }

    public string Id => GameId;
    public GameEntry Entry { get; }
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("monster_blue", "Blue"), new("monster_green", "Green"), new("monster_purple", "Purple")];
    /// <summary>Every food the monster can eat, in word-bank order.</summary>
    public IReadOnlyList<MonsterFood> Foods { get; }
    private readonly PhonicsCourse _course;

    public FeedMonsterGame(ContentLibrary content, PhonicsCourse course)
    {
        Entry = content.EntryForGame(GameId);
        _course = course;
        Foods = content.Words.Words
            .Where(w => w.Tags?.Contains("food") == true && w.Picturable && w.Picture is not null)
            .Select(w => content.Grapheme(w.FirstSound) is Grapheme first
                ? new MonsterFood(w.Text, w.Picture!, w.Audio, first.Id, first.Ipa, w.DecodableFromGroup)
                : null)
            .OfType<MonsterFood>()
            .ToList();
    }

    /// <summary>Foods the child may see at this phonics group.</summary>
    public List<MonsterFood> FoodsUpToGroup(int group) => Foods.Where(f => f.AvailableFromGroup <= group).ToList();

    /// <summary>
    /// Alternates between the focus sound and the other sounds the child knows, so a session isn't
    /// cut short when the focus sound has only one food.
    /// </summary>
    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var available = FoodsUpToGroup(learner.UnlockedPhonicsGroup);
        List<MonsterFood> FreshAnswers(PhonicsSound sound) =>
            available.Where(f => f.FirstSoundIpa == sound.Ipa && !session.UsedItems.Contains(f.Word)).ToList();
        List<MonsterFood> Others(PhonicsSound sound) => available.Where(f => f.FirstSoundIpa != sound.Ipa).ToList();
        bool IsPlayable(PhonicsSound sound) =>
            FreshAnswers(sound).Count > 0 && Others(sound).Select(f => f.Word).Distinct().Count() >= ChoiceCount - 1;

        // More foods at higher levels, never fewer than three.
        var wanted = Math.Max(this.Level(learner).Choices ?? ChoiceCount, ChoiceCount);
        var targets = _course.SoundsUpToGroup(learner.UnlockedPhonicsGroup)
            .Where(s => learner.KnownSoundIds.Contains(s.Id) && IsPlayable(s)).ToList();
        var focus = targets.FirstOrDefault(s => s.Id == learner.FocusSoundId);
        var rest = targets.Where(s => s.Id != learner.FocusSoundId).ToList();
        PhonicsSound target;
        if (focus is not null && (session.RoundsPlayed % 2 == 0 || rest.Count == 0)) target = focus;
        else if (rng.Pick(rest) is PhonicsSound other) target = other;
        else return null;

        if (rng.Pick(FreshAnswers(target)) is not MonsterFood answer) return null;
        var distractors = SoundHuntGame.PickDistractors(
            Others(target).Select(f => new HuntPicture(f.Word, f.Picture, f.Audio, f.FirstSoundId, f.FirstSoundIpa, f.AvailableFromGroup)),
            wanted - 1, session.UsedItems, rng);
        if (distractors.Count < ChoiceCount - 1) return null;
        var byWord = available.ToDictionary(f => f.Word);
        var picked = distractors.Select(d => byWord.GetValueOrDefault(d.Word)).OfType<MonsterFood>().ToList();
        if (picked.Count != distractors.Count) return null;
        return new Round(target, rng.Shuffled(picked.Prepend(answer)), answer);
    }

    public bool IsCorrect(MonsterFood choice, Round round) => choice.FirstSoundIpa == round.Target.Ipa;

    public string SkillId(Round round) => PhonicsCourse.SkillIdForGroup(round.Target.Group);
}
