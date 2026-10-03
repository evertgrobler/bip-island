using System.Text.Json;

namespace BipCore;

/// <summary>A content file that couldn't be read, with the file name so the problem is easy to find.</summary>
public sealed class ContentLoadException(string file, Exception inner)
    : Exception($"Couldn't read Content/{file}: {inner.Message}", inner)
{
    public string File { get; } = file;
}

/// <summary>Every content file, decoded. Load it once at start-up and pass it to the games.</summary>
public sealed class ContentLibrary
{
    /// <summary>Every file the loader reads, relative to the Content folder.</summary>
    public static readonly IReadOnlyList<string> Files =
    [
        "curriculum/objectives.json", "curriculum/skills.json", "curriculum/games.json",
        "phonics/graphemes.json",
        "words/words.json", "words/tricky_words.json", "words/sentences.json", "words/endings.json",
        "words/homophones.json", "words/contractions.json",
        "numbers/numbers.json",
        "coding/sequences.json", "coding/patterns.json", "coding/levels.json",
        "art/shapes.json", "art/paints.json", "art/mirror.json",
        "asset_manifest.json",
    ];

    public ObjectivesFile Objectives { get; }
    public SkillsFile Skills { get; }
    public GamesFile Games { get; }
    public GraphemesFile Phonics { get; }
    public WordsFile Words { get; }
    public TrickyWordsFile TrickyWords { get; }
    public SentencesFile Sentences { get; }
    public EndingsFile Endings { get; }
    public HomophonesFile Homophones { get; }
    public ContractionsFile Contractions { get; }
    public NumbersFile Numbers { get; }
    public SequencesFile Sequences { get; }
    public PatternsFile Patterns { get; }
    public LevelsFile Levels { get; }
    public ShapesFile Shapes { get; }
    public PaintsFile Paints { get; }
    public MirrorFile Mirror { get; }
    public AssetManifestFile Manifest { get; }

    // Lookups, built once.
    private readonly Dictionary<string, Grapheme> _graphemesById;
    private readonly Dictionary<string, Word> _wordsByText;
    private readonly Dictionary<string, Skill> _skillsById;
    private readonly Dictionary<string, GameEntry> _gamesById;
    public IReadOnlySet<string> AudioIds { get; }
    public IReadOnlySet<string> PictureIds { get; }

    /// <summary>Reads every file from a Content folder.</summary>
    public ContentLibrary(string directory)
        : this(file => System.IO.File.ReadAllText(Path.Combine(directory, file)))
    {
    }

    /// <summary>
    /// Reads every file through <paramref name="readFile"/> (given a path relative to the Content
    /// folder, e.g. "phonics/graphemes.json"). The game uses this to read from inside its package,
    /// where System.IO can't see the files.
    /// </summary>
    public ContentLibrary(Func<string, string> readFile)
    {
        T Load<T>(string file)
        {
            try
            {
                return BipJson.Decode<T>(readFile(file));
            }
            catch (Exception error) when (error is not ContentLoadException)
            {
                // Whatever the reader or the decoder threw, say which file it was.
                throw new ContentLoadException(file, error);
            }
        }

        Objectives = Load<ObjectivesFile>("curriculum/objectives.json");
        Skills = Load<SkillsFile>("curriculum/skills.json");
        Games = Load<GamesFile>("curriculum/games.json");
        Phonics = Load<GraphemesFile>("phonics/graphemes.json");
        Words = Load<WordsFile>("words/words.json");
        TrickyWords = Load<TrickyWordsFile>("words/tricky_words.json");
        Sentences = Load<SentencesFile>("words/sentences.json");
        Endings = Load<EndingsFile>("words/endings.json");
        Homophones = Load<HomophonesFile>("words/homophones.json");
        Contractions = Load<ContractionsFile>("words/contractions.json");
        Numbers = Load<NumbersFile>("numbers/numbers.json");
        Sequences = Load<SequencesFile>("coding/sequences.json");
        Patterns = Load<PatternsFile>("coding/patterns.json");
        Levels = Load<LevelsFile>("coding/levels.json");
        Shapes = Load<ShapesFile>("art/shapes.json");
        Paints = Load<PaintsFile>("art/paints.json");
        Mirror = Load<MirrorFile>("art/mirror.json");
        Manifest = Load<AssetManifestFile>("asset_manifest.json");

        _graphemesById = FirstById(Phonics.Graphemes, g => g.Id);
        _wordsByText = FirstById(Words.Words, w => w.Text);
        _skillsById = FirstById(Skills.Skills, s => s.Id);
        _gamesById = FirstById(Games.Games, g => g.Id);
        AudioIds = Manifest.Audio.Select(a => a.Id).ToHashSet();
        PictureIds = Manifest.Pictures.Select(p => p.Id).ToHashSet();
    }

    /// <summary>A lookup that keeps the first item when two share a key.</summary>
    private static Dictionary<string, T> FirstById<T>(IEnumerable<T> items, Func<T, string> key)
    {
        var lookup = new Dictionary<string, T>();
        foreach (var item in items) lookup.TryAdd(key(item), item);
        return lookup;
    }

    public Grapheme? Grapheme(string id) => _graphemesById.GetValueOrDefault(id);
    public Word? Word(string text) => _wordsByText.GetValueOrDefault(text);
    public Skill? Skill(string id) => _skillsById.GetValueOrDefault(id);
    public GameEntry? Game(string id) => _gamesById.GetValueOrDefault(id);
    public MasteryRules MasteryRules => Skills.MasteryRules;

    /// <summary>The registry entry for a game type, or an error if games.json doesn't list it.</summary>
    public GameEntry EntryForGame(string id) => Game(id) ?? throw new UnknownGameException(id);

    /// <summary>
    /// The band a new child of this age starts in. Younger than the table → the first band;
    /// older → the last.
    /// </summary>
    public Band StartingBand(int age)
    {
        var table = Skills.StartingBand
            .Select(pair => (Ok: int.TryParse(pair.Key, out var a), Age: a, Band: pair.Value))
            .Where(row => row.Ok)
            .OrderBy(row => row.Age)
            .ToList();
        if (table.Count == 0) return Band.Foundation;
        if (age < table[0].Age) return table[0].Band;
        return table.LastOrDefault(row => row.Age <= age, table[0]).Band;
    }
}

/// <summary>Raised when a game type's id isn't in games.json.</summary>
public sealed class UnknownGameException(string id)
    : Exception($"Game '{id}' is not in Content/curriculum/games.json")
{
    public string Id { get; } = id;
}
