using System.Text.RegularExpressions;

namespace BipCore;

/// <summary>
/// Word Rocket: Bip says a sound or a word, and the child types it on the real keyboard. Each right
/// key fills the next window of the rocket (one window per letter, so the child sees how many); the
/// last letter launches it. A level is one kind of question (games.json "modes"), easiest first:
/// <list type="bullet">
/// <item>"letter": a letter sound (never its name) from a group the child has unlocked; press that key.</item>
/// <item>"cvc": a decodable three-letter, three-sound word (sat, dog).</item>
/// <item>"longer": a decodable word with a digraph or a blend (chip, frog, rain).</item>
/// <item>"tricky": a Stage 2 tricky word (door, because).</item>
/// </list>
/// When a level has nothing fresh at the child's phonics group, the next easier kind fills in.
/// Words are typed exactly. A letter sound accepts every key that makes it: c and k both say /k/.
/// </summary>
public sealed partial class WordRocketGame : IMiniGame<WordRocketGame.Round, string>
{
    public const string GameId = "word_rocket";
    /// <summary>The most letters (windows) a word can have.</summary>
    public const int MaxLetters = 9;

    public enum Mode { Letter, Cvc, Longer, Tricky }

    /// <summary>
    /// One window of the rocket: the letter that shows once typed, every key that fills it (lower
    /// case), what Bip says after two misses on it, and whether it is the last letter of its sound
    /// (the h of "ch"), where a word being spelt says that sound.
    /// </summary>
    public sealed record Slot(string Letter, IReadOnlySet<string> Keys, string HintClip, bool EndsSound = false);

    /// <param name="Answer">What a right typing spells (lower case).</param>
    /// <param name="Clip">What Bip says: the sound (snd_) or the word (word_).</param>
    /// <param name="Choices">Spellings Bip checks against: the answer and near misses. For a letter, every key.</param>
    /// <param name="SoundId">The letter's sound, for letter questions.</param>
    /// <param name="Word">The word bank entry, for decodable words.</param>
    public sealed record Round(Mode Mode, string Answer, string Clip, IReadOnlyList<Slot> Slots, IReadOnlyList<string> Choices,
                               string? SoundId = null, Word? Word = null) : IGameRound<string>
    {
        public IReadOnlyList<string> UsedItems => [Mode == Mode.Letter ? "sound:" + SoundId : Answer];
    }

    public string Id => GameId;
    public GameEntry Entry { get; }
    public IReadOnlyList<GameSkin> Skins { get; } =
        [new("rocket", "Rocket"), new("balloon", "Hot-air balloon"), new("submarine", "Submarine")];

    private static readonly IReadOnlyList<string> Keyboard = PhonicsCourse.Alphabet;
    private readonly PhonicsCourse _course;
    private readonly List<(Word Word, List<PhonicsSound> Parts)> _words;
    private readonly List<string> _tricky;
    private readonly IReadOnlySet<string> _audio;

    public WordRocketGame(ContentLibrary content, PhonicsCourse course)
    {
        Entry = content.EntryForGame(GameId);
        _course = course;
        _audio = content.AudioIds;
        // Only words whose graphemes spell them left to right: split digraphs (a-e in cake) can't
        // light up one window per sound, so they stay out.
        _words = content.Words.Words
            .Select(word => (Word: word, Parts: word.Graphemes.Select(course.Sound).OfType<PhonicsSound>().ToList()))
            .Where(w => w.Parts.Count == w.Word.Graphemes.Count && string.Concat(w.Parts.Select(p => p.Grapheme)) == w.Word.Text
                        && Typeable(w.Word.Text))
            .ToList();
        // Lower-case words only: Mr and Mrs need a capital, and the game shows lower case.
        _tricky = content.TrickyWords.Stage2.Words.Where(Typeable).Distinct().ToList();
    }

    private static bool Typeable(string text) => text.Length <= MaxLetters && LowerCaseLetters().IsMatch(text);

    [GeneratedRegex("^[a-z]+$")]
    private static partial Regex LowerCaseLetters();

    /// <summary>The kind of question a level asks (its first mode; "letter" when it names none).</summary>
    public Mode LevelMode(Learner learner) => this.Level(learner).Modes?.FirstOrDefault() switch
    {
        "cvc" => Mode.Cvc,
        "longer" => Mode.Longer,
        "tricky" => Mode.Tricky,
        _ => Mode.Letter,
    };

    /// <summary>Single-letter sounds from the groups the child has unlocked.</summary>
    public List<PhonicsSound> Letters(Learner learner) =>
        _course.SoundsUpToGroup(learner.UnlockedPhonicsGroup).Where(s => s.Grapheme.Length == 1 && Keyboard.Contains(s.Grapheme)).ToList();

    private static bool IsCvc((Word Word, List<PhonicsSound> Parts) w) =>
        w.Word.SoundCount == 3 && w.Word.Text.Length == 3;

    /// <summary>Decodable words at the child's group for a word mode.</summary>
    public List<Word> Words(Mode mode, Learner learner) => _words
        .Where(w => w.Word.DecodableFromGroup <= learner.UnlockedPhonicsGroup)
        .Where(w => mode == Mode.Cvc ? IsCvc(w) : !IsCvc(w) && w.Word.Text.Length >= 4)
        .Select(w => w.Word).ToList();

    public IReadOnlyList<string> TrickyWords => _tricky;

    public Round? MakeRound(Learner learner, GameSession session, IRandomSource rng)
    {
        // The level's kind first; then easier kinds, so a visit doesn't end early.
        for (var mode = LevelMode(learner); mode >= Mode.Letter; mode--)
        {
            if (MakeRound(mode, learner, session, rng) is Round round) return round;
        }
        return null;
    }

    private Round? MakeRound(Mode mode, Learner learner, GameSession session, IRandomSource rng)
    {
        switch (mode)
        {
            case Mode.Letter:
            {
                var fresh = Letters(learner).Where(s => !session.UsedItems.Contains("sound:" + s.Id)).ToList();
                var focused = fresh.Where(s => s.Id == learner.FocusSoundId).ToList();
                return rng.Pick(focused.Count > 0 ? focused : fresh) is PhonicsSound sound ? LetterRound(sound, learner) : null;
            }
            case Mode.Cvc or Mode.Longer:
            {
                var fresh = Words(mode, learner).Where(w => !session.UsedItems.Contains(w.Text)).ToList();
                var focused = learner.FocusSoundId is string focus ? fresh.Where(w => w.Graphemes.Contains(focus)).ToList() : [];
                return rng.Pick(focused.Count > 0 ? focused : fresh) is Word word ? WordRound(mode, word, rng) : null;
            }
            default:
            {
                var fresh = _tricky.Where(w => !session.UsedItems.Contains(w)).ToList();
                return rng.Pick(fresh) is string word ? TrickyRound(word, learner, rng) : null;
            }
        }
    }

    private Round LetterRound(PhonicsSound sound, Learner learner)
    {
        // Every key whose letter makes the same sound counts (c and k).
        var keys = Letters(learner).Where(s => s.Ipa == sound.Ipa).Select(s => s.Grapheme).Append(sound.Grapheme).ToHashSet();
        return new Round(Mode.Letter, sound.Grapheme, sound.SoundClip, [new Slot(sound.Grapheme, keys, sound.SoundClip)],
                         Keyboard, SoundId: sound.Id);
    }

    private Round WordRound(Mode mode, Word word, IRandomSource rng)
    {
        // Each letter's hint is the sound it is part of: both windows of "ch" say /ch/.
        var slots = word.Graphemes.Select(_course.Sound).OfType<PhonicsSound>()
            .SelectMany(sound => sound.Grapheme.Select((letter, i) =>
                new Slot(letter.ToString(), new HashSet<string> { letter.ToString() }, sound.SoundClip, EndsSound: i == sound.Grapheme.Length - 1)))
            .ToList();
        return new Round(mode, word.Text, word.Audio, slots, NearMisses(word.Text, rng), Word: word);
    }

    private Round TrickyRound(string word, Learner learner, IRandomSource rng)
    {
        // Tricky words don't sound out, so a hint names the letter once letter names are taught;
        // before that Bip says the word again.
        var names = PhonicsCourse.LetterNamesUnlocked(learner.KnownSoundIds);
        var slots = word.Select(c => c.ToString()).Select(letter =>
        {
            var name = $"name_{letter}";
            return new Slot(letter, new HashSet<string> { letter }, names && _audio.Contains(name) ? name : AudioCatalogue.WordClip(word));
        }).ToList();
        return new Round(Mode.Tricky, word, AudioCatalogue.WordClip(word), slots, NearMisses(word, rng));
    }

    /// <summary>The word and a few spellings one letter off: the last letter dropped or doubled.</summary>
    private static List<string> NearMisses(string word, IRandomSource rng) =>
        rng.Shuffled(new[] { word, word[..^1], word + word[^1] }.Where(w => w.Length > 0).Distinct());

    /// <summary>Whether a key fills a window: letters only, either case.</summary>
    public static bool IsRightKey(Round round, int index, string key) =>
        index >= 0 && index < round.Slots.Count && round.Slots[index].Keys.Contains(key.ToLowerInvariant());

    public bool IsCorrect(string choice, Round round) =>
        choice.Length == round.Slots.Count && Enumerable.Range(0, choice.Length).All(i => IsRightKey(round, i, choice[i].ToString()));

    public string SkillId(Round round) => round.Mode switch
    {
        Mode.Letter => round.SoundId is string id && _course.Sound(id) is PhonicsSound sound
            ? PhonicsCourse.SkillIdForGroup(sound.Group) : PhonicsCourse.SkillIdForGroup(1),
        Mode.Cvc => "spell_cvc",
        Mode.Longer => round.Slots.Count > (round.Word?.SoundCount ?? round.Slots.Count) ? "spell_digraph" : "spell_typed",
        _ => "tricky_s2",
    };
}
