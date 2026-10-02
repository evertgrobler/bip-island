namespace BipCore;

/// <summary>
/// Sound Buttons: click each letter in c-a-t to hear its sound, then pick the picture
/// of the word they make together. Blending with a safety net: the answer is one of three
/// pictures, so a child who can't blend yet can still play by listening.
/// </summary>
public sealed class SoundButtonsGame : IMiniGame<SoundButtonsGame.Round, HuntPicture>
{
    public const string GameId = "sound_buttons";
    public const int ChoiceCount = 3;

    public sealed record Round(Word Word, IReadOnlyList<HuntPicture> Choices, HuntPicture Answer) : IGameRound<HuntPicture>
    {
        public IReadOnlyList<string> UsedItems => [Answer.Word];
    }

    public string Id => GameId;
    public GameEntry Entry { get; }
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("buttons", "Buttons"), new("stepping_stones", "Stepping stones"), new("piano_keys", "Piano keys")];
    private readonly PhonicsCourse _course;
    private readonly List<Word> _words;

    public SoundButtonsGame(ContentLibrary content, PhonicsCourse course)
    {
        Entry = content.EntryForGame(GameId);
        _course = course;
        _words = content.Words.Words.Where(w => w.Picturable && w.Picture is not null).ToList();
    }

    /// <summary>
    /// Words the child can sound out: decodable, with the level's number of sounds. If the level
    /// has nothing yet at the child's phonics group, the band's usual words instead.
    /// </summary>
    internal List<Word> Candidates(Learner learner)
    {
        var decodable = _words.Where(w => w.DecodableFromGroup <= learner.UnlockedPhonicsGroup).ToList();
        if (this.Level(learner).SoundCounts is List<int> counts)
        {
            var atLevel = decodable.Where(w => counts.Contains(w.SoundCount)).ToList();
            if (atLevel.Count >= ChoiceCount) return atLevel;
        }
        return decodable.Where(w => learner.Band == Band.Foundation ? w.SoundCount == 3 : w.SoundCount >= 3).ToList();
    }

    private string? FirstSoundIpa(Word word) => _course.Sound(word.FirstSound)?.Ipa;

    private HuntPicture? Picture(Word word) =>
        word.Picture is string picture && _course.Sound(word.FirstSound) is PhonicsSound first
            ? new HuntPicture(word.Text, picture, word.Audio, first.Id, first.Ipa, word.DecodableFromGroup)
            : null;

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var fresh = Candidates(learner).Where(w => !session.UsedItems.Contains(w.Text)).ToList();
        if (fresh.Count == 0) return null;
        var focused = learner.FocusSoundId is string focus ? fresh.Where(w => w.Graphemes.Contains(focus)).ToList() : null;
        var answer = rng.Pick(focused is { Count: > 0 } ? focused : fresh);
        if (answer is null || FirstSoundIpa(answer) is not string answerFirst) return null;
        var pool = fresh
            .Where(w => w.Text != answer.Text && FirstSoundIpa(w) != answerFirst)
            .Select(Picture).OfType<HuntPicture>();
        var distractors = SoundHuntGame.PickDistractors(pool, ChoiceCount - 1, session.UsedItems, rng);
        if (distractors.Count != ChoiceCount - 1 || Picture(answer) is not HuntPicture mine) return null;
        return new Round(answer, rng.Shuffled(distractors.Prepend(mine)), mine);
    }

    public bool IsCorrect(HuntPicture choice, Round round) => choice.Word == round.Answer.Word;

    public string SkillId(Round round)
    {
        if (round.Word.SoundCount <= 3) return "blend_cvc";
        return round.Word.SoundCount == 4 ? "blend_ccvc" : "read_longer";
    }
}
