namespace BipCore;

/// <summary>A picture card for Sound Hunt (and Sound Buttons).</summary>
/// <param name="FirstSoundId">Grapheme id of the first sound ("c" for cat).</param>
/// <param name="FirstSoundIpa">The first sound itself, so cat and kite both start with /k/.</param>
/// <param name="AvailableFromGroup">The phonics group from which the picture may be shown.</param>
public sealed record HuntPicture(string Word, string Picture, string Audio, string FirstSoundId,
                                 string FirstSoundIpa, int AvailableFromGroup);

/// <summary>
/// Sound Hunt: "Find the picture that starts with… sss". Pick the right picture out of three.
///
/// Pictures come from the decodable word bank (picturable words, only once their group is unlocked)
/// plus each unlocked sound's own picture from Meet the Sound. The word to find never repeats in a
/// session, and the other pictures never start with the same sound.
/// </summary>
public sealed class SoundHuntGame : IMiniGame<SoundHuntGame.Round, HuntPicture>
{
    public const string GameId = "sound_hunt";
    public const int ChoiceCount = 3;

    public sealed record Round(PhonicsSound Target, IReadOnlyList<HuntPicture> Choices, HuntPicture Answer) : IGameRound<HuntPicture>
    {
        public IReadOnlyList<string> UsedItems => [Answer.Word];
    }

    public string Id => GameId;
    public GameEntry Entry { get; }
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("treasure_chests", "Treasure chests"), new("market_stalls", "Market stalls"), new("picnic_basket", "Picnic basket")];
    /// <summary>Every picture the game can show, in word-bank order.</summary>
    public IReadOnlyList<HuntPicture> Pictures { get; }
    private readonly PhonicsCourse _course;

    public SoundHuntGame(ContentLibrary content, PhonicsCourse course)
    {
        Entry = content.EntryForGame(GameId);
        _course = course;
        Pictures = PicturesFrom(content);
    }

    internal static List<HuntPicture> PicturesFrom(ContentLibrary content)
    {
        var byWord = new Dictionary<string, HuntPicture>();
        var order = new List<string>();
        void Add(HuntPicture picture)
        {
            if (byWord.TryGetValue(picture.Word, out var existing))
            {
                if (picture.AvailableFromGroup < existing.AvailableFromGroup) byWord[picture.Word] = picture;
            }
            else
            {
                byWord[picture.Word] = picture;
                order.Add(picture.Word);
            }
        }
        foreach (var word in content.Words.Words.Where(w => w.Picturable))
        {
            if (word.Picture is not string picture || content.Grapheme(word.FirstSound) is not Grapheme first) continue;
            Add(new HuntPicture(word.Text, picture, word.Audio, first.Id, first.Ipa, word.DecodableFromGroup));
        }
        // A sound's own picture (s → sun) is met in Meet the Sound, so it can be shown from that sound's group.
        foreach (var grapheme in content.Phonics.Graphemes)
        {
            var mnemonic = grapheme.MnemonicWord;
            Grapheme? first;
            if (content.Word(mnemonic) is Word banked)
            {
                // "this" (for th) is in the word bank but can't be drawn.
                first = banked.Picturable ? content.Grapheme(banked.FirstSound) : null;
            }
            else
            {
                // Not in the word bank: only usable when the word plainly starts with this sound (ink, egg, queen).
                first = mnemonic.StartsWith(grapheme.Text, StringComparison.Ordinal) && !grapheme.Text.Contains('-') ? grapheme : null;
            }
            if (first is null) continue;
            var group = Math.Min(grapheme.Group, content.Word(mnemonic)?.DecodableFromGroup ?? grapheme.Group);
            Add(new HuntPicture(mnemonic, grapheme.MnemonicPicture, AudioCatalogue.WordClip(mnemonic), first.Id, first.Ipa, group));
        }
        return order.Select(w => byWord[w]).ToList();
    }

    /// <summary>Pictures the child may see at this phonics group.</summary>
    public List<HuntPicture> PicturesUpToGroup(int group) => Pictures.Where(p => p.AvailableFromGroup <= group).ToList();

    /// <summary>Whether any picture at all starts with this sound (no word starts with ng or x).</summary>
    public bool CanHunt(PhonicsSound sound, int upToGroup) => PicturesUpToGroup(upToGroup).Any(p => p.FirstSoundIpa == sound.Ipa);

    /// <summary>
    /// Alternates between the focus sound and the other sounds the child knows, so a session isn't
    /// cut short when the focus sound has only one or two pictures.
    /// </summary>
    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var available = PicturesUpToGroup(learner.UnlockedPhonicsGroup);
        List<HuntPicture> FreshAnswers(PhonicsSound sound) =>
            available.Where(p => p.FirstSoundIpa == sound.Ipa && !session.UsedItems.Contains(p.Word)).ToList();
        List<HuntPicture> Others(PhonicsSound sound) => available.Where(p => p.FirstSoundIpa != sound.Ipa).ToList();
        bool IsPlayable(PhonicsSound sound) =>
            FreshAnswers(sound).Count > 0 && Others(sound).Select(p => p.Word).Distinct().Count() >= ChoiceCount - 1;
        // More pictures at higher levels, never fewer than three.
        var wanted = Math.Max(this.Level(learner).Choices ?? ChoiceCount, ChoiceCount);

        var targets = _course.SoundsUpToGroup(learner.UnlockedPhonicsGroup)
            .Where(s => learner.KnownSoundIds.Contains(s.Id) && IsPlayable(s)).ToList();
        var focus = targets.FirstOrDefault(s => s.Id == learner.FocusSoundId);
        var rest = targets.Where(s => s.Id != learner.FocusSoundId).ToList();
        PhonicsSound target;
        if (focus is not null && (session.RoundsPlayed % 2 == 0 || rest.Count == 0)) target = focus;
        else if (rng.Pick(rest) is PhonicsSound other) target = other;
        else return null;

        if (rng.Pick(FreshAnswers(target)) is not HuntPicture answer) return null;
        var distractors = PickDistractors(Others(target), wanted - 1, session.UsedItems, rng);
        if (distractors.Count < ChoiceCount - 1) return null;
        return new Round(target, rng.Shuffled(distractors.Prepend(answer)), answer);
    }

    /// <summary>Different words, preferring ones not seen yet this session and different first sounds from each other.</summary>
    internal static List<HuntPicture> PickDistractors(IEnumerable<HuntPicture> pool, int count, IReadOnlySet<string> used, IRandomSource rng)
    {
        var shuffled = rng.Shuffled(pool);
        var ordered = shuffled.Where(p => !used.Contains(p.Word)).Concat(shuffled.Where(p => used.Contains(p.Word))).ToList();
        var picked = new List<HuntPicture>();
        foreach (var candidate in ordered)
        {
            if (picked.Count >= count) break;
            if (!picked.Any(p => p.Word == candidate.Word || p.FirstSoundIpa == candidate.FirstSoundIpa)) picked.Add(candidate);
        }
        foreach (var candidate in ordered)
        {
            if (picked.Count >= count) break;
            if (!picked.Any(p => p.Word == candidate.Word)) picked.Add(candidate);
        }
        return picked;
    }

    public bool IsCorrect(HuntPicture choice, Round round) => choice.FirstSoundIpa == round.Target.Ipa;

    public string SkillId(Round round) => PhonicsCourse.SkillIdForGroup(round.Target.Group);
}
