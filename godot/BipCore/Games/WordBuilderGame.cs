namespace BipCore;

/// <summary>
/// Word Builder: drag (or tap) letter tiles into slots to build the word in the picture.
/// Tiles are graphemes, so "chip" builds from ch-i-p, and each one says its sound as it lands.
/// The bank holds the word's tiles plus a few spare ones (more at higher levels). Spares come
/// only from sounds the child has met and never sound or look like a sound in the word, so
/// there is exactly one right spelling.
/// </summary>
public sealed class WordBuilderGame : IMiniGame<WordBuilderGame.Round, IReadOnlyList<string>>
{
    public const string GameId = "word_builder";
    public const int ChoiceCount = 3;
    /// <summary>The most spare tiles a level can ask for.</summary>
    public const int MaxSpares = 4;

    /// <summary>One letter tile: the grapheme id, what the child sees and the sound it makes.</summary>
    public sealed record Tile(string Id, string Text, string SoundClip);

    /// <param name="Choices">Candidate tile rows, in screen order. Exactly one spells the word.</param>
    /// <param name="AnswerTiles">The word's tiles in spelling order.</param>
    /// <param name="Bank">Every tile to build from, shuffled: the word's tiles plus the spares.</param>
    public sealed record Round(Word Word, IReadOnlyList<IReadOnlyList<string>> Choices, IReadOnlyList<string> Answer,
                               IReadOnlyList<Tile> AnswerTiles, IReadOnlyList<Tile> Bank) : IGameRound<IReadOnlyList<string>>
    {
        public IReadOnlyList<string> UsedItems => [Word.Text];
    }

    public string Id => GameId;
    public GameEntry Entry { get; }
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("tiles", "Tiles"), new("blocks", "Blocks"), new("fridge_magnets", "Fridge magnets")];
    private readonly List<Word> _words;
    private readonly PhonicsCourse _course;

    public WordBuilderGame(ContentLibrary content)
    {
        Entry = content.EntryForGame(GameId);
        _course = new PhonicsCourse(content);
        // Split digraphs (a-e in cake) can't sit in one slot, so those words stay out.
        _words = content.Words.Words.Where(word =>
            word.Picturable && word.Picture is not null
            && word.Graphemes.All(id => _course.Sound(id) is PhonicsSound sound && !sound.Grapheme.Contains('-'))).ToList();
    }

    private static Tile TileFor(PhonicsSound sound) => new(sound.Id, sound.Grapheme, sound.SoundClip);

    /// <summary>Spare tiles: sounds the child has met that don't sound or look like any sound in the word.</summary>
    internal List<Tile> Spares(Word word, Learner learner, int count, IRandomSource rng)
    {
        var inWord = word.Graphemes.Select(_course.Sound).OfType<PhonicsSound>().ToList();
        var pool = _course.SoundsUpToGroup(learner.UnlockedPhonicsGroup).Where(sound =>
            !sound.Grapheme.Contains('-') && !inWord.Any(w => w.Id == sound.Id || w.IsConfusable(sound)));
        return rng.Shuffled(pool).Take(count).Select(TileFor).ToList();
    }

    /// <summary>
    /// Words the child can spell: decodable, with the level's number of sounds. If the level
    /// has nothing yet at the child's phonics group, the band's usual words instead.
    /// </summary>
    internal List<Word> Candidates(Learner learner)
    {
        var decodable = _words.Where(w => w.DecodableFromGroup <= learner.UnlockedPhonicsGroup).ToList();
        if (this.Level(learner).SoundCounts is List<int> counts)
        {
            var atLevel = decodable.Where(w => counts.Contains(w.SoundCount)).ToList();
            if (atLevel.Count > 0) return atLevel;
        }
        return decodable.Where(w => learner.Band == Band.Foundation ? w.SoundCount == 3 : w.SoundCount >= 3).ToList();
    }

    /// <summary>Misspellings: distinct shuffles of the tiles that don't spell the word.</summary>
    internal static List<IReadOnlyList<string>> Misspellings(IReadOnlyList<string> tiles, int count, IRandomSource rng)
    {
        var misspelt = new List<IReadOnlyList<string>>();
        for (var attempt = 0; attempt < 30 && misspelt.Count < count; attempt++)
        {
            var shuffled = rng.Shuffled(tiles);
            if (!shuffled.SequenceEqual(tiles) && !misspelt.Any(m => m.SequenceEqual(shuffled))) misspelt.Add(shuffled);
        }
        return misspelt;
    }

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        var fresh = Candidates(learner).Where(w => !session.UsedItems.Contains(w.Text)).ToList();
        var focused = learner.FocusSoundId is string focus ? fresh.Where(w => w.Graphemes.Contains(focus)).ToList() : null;
        var pool = rng.Shuffled(focused is { Count: > 0 } ? focused : fresh);
        foreach (var word in pool)
        {
            var wrong = Misspellings(word.Graphemes, ChoiceCount - 1, rng);
            if (wrong.Count != ChoiceCount - 1) continue;
            var choices = rng.Shuffled(wrong.Prepend(word.Graphemes));
            var answerTiles = word.Graphemes.Select(_course.Sound).OfType<PhonicsSound>().Select(TileFor).ToList();
            var spareCount = Math.Clamp(this.Level(learner).Spares ?? 1, 0, MaxSpares);
            var bank = rng.Shuffled(answerTiles.Concat(Spares(word, learner, spareCount, rng)));
            return new Round(word, choices, word.Graphemes, answerTiles, bank);
        }
        return null;
    }

    public bool IsCorrect(IReadOnlyList<string> choice, Round round) => choice.SequenceEqual(round.Answer);

    public string SkillId(Round round)
    {
        var singleLetters = round.Answer.All(g => g.Length == 1 && !g.Contains('-'));
        if (singleLetters && round.Word.SoundCount == 3) return "spell_cvc";
        return singleLetters ? "endings" : "spell_digraph";
    }
}
