namespace BipCore;

/// <summary>One phonics sound as the games use it, built from a grapheme in Content/phonics/graphemes.json.</summary>
public sealed record PhonicsSound
{
    /// <summary>Unique id ("s", "ck", "ow_long").</summary>
    public string Id { get; }
    /// <summary>The letters the child sees ("s", "ow", "a-e").</summary>
    public string Grapheme { get; }
    public int Group { get; }
    /// <summary>The sound itself. Two graphemes with the same IPA sound the same (c, k and ck).</summary>
    public string Ipa { get; }
    public GraphemeKind Kind { get; }
    /// <summary>The pure-sound clip, e.g. snd_s.</summary>
    public string SoundClip { get; }
    /// <summary>The picture word used to introduce the sound (s → sun).</summary>
    public string PictureWord { get; }
    public string Picture { get; }
    public string? LetterName { get; }

    public PhonicsSound(Grapheme grapheme)
    {
        Id = grapheme.Id;
        Grapheme = grapheme.Text;
        Group = grapheme.Group;
        Ipa = grapheme.Ipa;
        Kind = grapheme.Kind;
        SoundClip = grapheme.Audio;
        PictureWord = grapheme.MnemonicWord;
        Picture = grapheme.MnemonicPicture;
        LetterName = grapheme.LetterName;
    }

    public string WordClip => AudioCatalogue.WordClip(PictureWord);

    /// <summary>Stop sounds have to be trimmed to a clean burst so children don't learn "tuh".</summary>
    public bool MustBeClipped => Kind == GraphemeKind.Bouncy;

    /// <summary>
    /// True when a child could mix the two up in a game: the same sound (c and k) or the
    /// same letters (ow in cow and ow in snow). A sound is not confusable with itself.
    /// </summary>
    public bool IsConfusable(PhonicsSound other) => Id != other.Id && (Ipa == other.Ipa || Grapheme == other.Grapheme);
}

/// <summary>One teaching group, e.g. group 1: s a t p i n.</summary>
public sealed record PhonicsGroup(int Number, Band Band, IReadOnlyList<PhonicsSound> Sounds)
{
    /// <summary>The skill in skills.json that this group teaches.</summary>
    public string SkillId => PhonicsCourse.SkillIdForGroup(Number);
}

/// <summary>The phonics teaching order, read from the content. Sounds come first; letter names only after group 5.</summary>
public sealed class PhonicsCourse
{
    public IReadOnlyList<PhonicsGroup> Groups { get; }
    /// <summary>Every sound in teaching order.</summary>
    public IReadOnlyList<PhonicsSound> AllSounds { get; }
    private readonly Dictionary<string, PhonicsSound> _byId = [];

    public PhonicsCourse(GraphemesFile file)
    {
        Groups = file.Groups.OrderBy(g => g.Group).Select(entry =>
            new PhonicsGroup(entry.Group, entry.Band,
                             file.Graphemes.Where(g => g.Group == entry.Group).Select(g => new PhonicsSound(g)).ToList()))
            .ToList();
        AllSounds = Groups.SelectMany(g => g.Sounds).ToList();
        foreach (var sound in AllSounds) _byId.TryAdd(sound.Id, sound);
    }

    public PhonicsCourse(ContentLibrary content) : this(content.Phonics) { }

    /// <summary>Phonics group skills are called snd_g1 … snd_g9 in skills.json.</summary>
    public static string SkillIdForGroup(int number) => $"snd_g{number}";

    /// <summary>The group number a phonics skill id belongs to, or null for other skills.</summary>
    public static int? GroupForSkill(string id) =>
        id.StartsWith("snd_g", StringComparison.Ordinal) && int.TryParse(id["snd_g".Length..], out var n) ? n : null;

    public PhonicsGroup FirstGroup => Groups[0];

    public PhonicsGroup? Group(int number) => Groups.FirstOrDefault(g => g.Number == number);

    public PhonicsSound? Sound(string id) => _byId.GetValueOrDefault(id);

    /// <summary>Every sound in groups 1…<paramref name="group"/>, in teaching order.</summary>
    public List<PhonicsSound> SoundsUpToGroup(int group) =>
        Groups.Where(g => g.Number <= group).SelectMany(g => g.Sounds).ToList();

    public static readonly IReadOnlyList<string> Alphabet = "abcdefghijklmnopqrstuvwxyz".Select(c => c.ToString()).ToList();

    /// <summary>Which of the 26 letters a set of known sounds covers. "qu" covers q.</summary>
    public static HashSet<string> LettersCovered(IEnumerable<string> knownSounds)
    {
        var known = knownSounds.ToHashSet();
        var letters = known.Where(id => id.Length == 1).ToHashSet();
        if (known.Contains("qu")) letters.Add("q");
        letters.IntersectWith(Alphabet);
        return letters;
    }

    /// <summary>Letter names ("this is ess") are only taught once the sounds of all 26 letters are known.</summary>
    public static bool LetterNamesUnlocked(IEnumerable<string> knownSounds) =>
        LettersCovered(knownSounds).Count == Alphabet.Count;
}
