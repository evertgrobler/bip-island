using System.Text.Json.Serialization;

namespace BipCore;

// Models for every file in Content/. The JSON is the single source of truth: these types only
// describe its shape. A unit test decodes each file and encodes it again, and fails if any key would
// be lost, so a new field in the JSON must be added here too. `required` marks keys that must be
// there; a file without one fails to load with the key's name in the message.

// MARK: - Shared

/// <summary>The four child bands, youngest first.</summary>
public enum Band { Foundation, Stage1, Stage2, Stage3 }

public enum Island { Letters, Numbers, Words, Coding, Art }

public static class BandExtensions
{
    public static readonly IReadOnlyList<Band> All = [Band.Foundation, Band.Stage1, Band.Stage2, Band.Stage3];

    /// <summary>The band as the content writes it ("foundation", "stage1").</summary>
    public static string Key(this Band band) => band switch
    {
        Band.Foundation => "foundation",
        Band.Stage1 => "stage1",
        Band.Stage2 => "stage2",
        _ => "stage3",
    };
}

public static class IslandExtensions
{
    public static readonly IReadOnlyList<Island> All = [Island.Letters, Island.Numbers, Island.Words, Island.Coding, Island.Art];
}

/// <summary>An age range written as "4-6" in the content.</summary>
[JsonConverter(typeof(AgeRangeConverter))]
public readonly record struct AgeRange(int Youngest, int Oldest)
{
    public bool Contains(int age) => age >= Youngest && age <= Oldest;
}

// MARK: - curriculum/objectives.json

public sealed record ObjectivesFile
{
    public string? Note { get; init; }
    public required List<Objective> Objectives { get; init; }
}

public sealed record Objective
{
    public required string Code { get; init; }
    public required string Framework { get; init; }
    public required int Stage { get; init; }
    public required string Strand { get; init; }
    public required string Summary { get; init; }
    public required string Source { get; init; }
}

// MARK: - curriculum/skills.json

public sealed record SkillsFile
{
    public string? Note { get; init; }
    /// <summary>The rules in words, for people reading the file.</summary>
    public required Dictionary<string, string> Mastery { get; init; }
    /// <summary>The same rules as numbers, for the game.</summary>
    public required MasteryRules MasteryRules { get; init; }
    /// <summary>Age (as text, "4") to the band a new child starts in.</summary>
    public required Dictionary<string, Band> StartingBand { get; init; }
    public required List<Skill> Skills { get; init; }
}

/// <summary>How progress moves, from skills.json. See docs/CURRICULUM.md "Levels, mastery and review".</summary>
public sealed record MasteryRules
{
    /// <summary>Right answers in a row that move a level up.</summary>
    public required int CorrectInARowToMoveUp { get; init; }
    /// <summary>Misses in a row that drop a level back.</summary>
    public required int MissesInARowToDropBack { get; init; }
    /// <summary>How many recent attempts mastery looks at.</summary>
    public required int MasteredWindow { get; init; }
    /// <summary>Percentage of those that must be right.</summary>
    public required int MasteredPercent { get; init; }
    /// <summary>Different days those attempts must be spread over.</summary>
    public required int MasteredDistinctDays { get; init; }
    /// <summary>Days until each review after mastery (2, then 5, then 14).</summary>
    public required List<int> ReviewAfterDays { get; init; }

    public bool Equals(MasteryRules? other) =>
        other is not null && CorrectInARowToMoveUp == other.CorrectInARowToMoveUp
        && MissesInARowToDropBack == other.MissesInARowToDropBack && MasteredWindow == other.MasteredWindow
        && MasteredPercent == other.MasteredPercent && MasteredDistinctDays == other.MasteredDistinctDays
        && ReviewAfterDays.SequenceEqual(other.ReviewAfterDays);

    public override int GetHashCode() => HashCode.Combine(CorrectInARowToMoveUp, MissesInARowToDropBack, MasteredWindow,
                                                          MasteredPercent, MasteredDistinctDays, ReviewAfterDays.Count);
}

public sealed record Skill
{
    public required string Id { get; init; }
    public required Island Island { get; init; }
    public required Band Band { get; init; }
    public required string Name { get; init; }
    public required List<string> Objectives { get; init; }
    public required List<string> Prerequisites { get; init; }
}

// MARK: - curriculum/games.json

public sealed record GamesFile
{
    public string? Note { get; init; }
    public required SessionSettings Session { get; init; }
    public required List<GameEntry> Games { get; init; }
}

public sealed record SessionSettings
{
    /// <summary>Questions in one visit to a game, unless the game runs out of fresh content first.</summary>
    public required int RoundsPerSession { get; init; }
}

/// <summary>One mini-game in the registry.</summary>
public sealed record GameEntry
{
    public required string Id { get; init; }
    public required Island Island { get; init; }
    public required string Name { get; init; }
    public required AgeRange Ages { get; init; }
    public required List<string> Skills { get; init; }
    public required List<string> Objectives { get; init; }
    /// <summary>Which content file the rounds come from (for people; the game type knows how to read it).</summary>
    public required string Content { get; init; }
    public required int BuildPhase { get; init; }
    /// <summary>Difficulty steps, easiest first. Missing or empty means the game has one level.</summary>
    public List<GameLevel>? Levels { get; init; }

    /// <summary>The steps, never empty.</summary>
    [JsonIgnore]
    public IReadOnlyList<GameLevel> LevelSteps => Levels is { Count: > 0 } ? Levels : [GameLevel.Single];

    /// <summary>A level by index, clamped to the ones that exist.</summary>
    public GameLevel Level(int index)
    {
        var steps = LevelSteps;
        return steps[Math.Clamp(index, 0, steps.Count - 1)];
    }

    /// <summary>
    /// Where a new child starts: the first level of their band (the last level that is marked
    /// with a band no older than theirs). A level without a band carries on the band before it.
    /// </summary>
    public int StartingLevel(Band band)
    {
        var start = 0;
        var steps = LevelSteps;
        for (var index = 0; index < steps.Count; index++)
        {
            if (steps[index].Band is Band marked && marked <= band) start = index;
        }
        return start;
    }
}

/// <summary>One difficulty step of a game. Each game reads the fields it understands; the rest stay empty.</summary>
public sealed record GameLevel
{
    /// <summary>Children of this band (and older) start at this level or above.</summary>
    public Band? Band { get; init; }
    /// <summary>Counting games: the largest number asked.</summary>
    public int? CountTo { get; init; }
    /// <summary>Answers to pick from on screen.</summary>
    public int? Choices { get; init; }
    /// <summary>Quick Look: how long the dots show, in tenths of a second.</summary>
    public int? FlashTenths { get; init; }
    /// <summary>Bubble Pop: bubbles on screen and drift speed (100 = normal).</summary>
    public int? Bubbles { get; init; }
    public int? SpeedPercent { get; init; }
    /// <summary>Word games: how many sounds the words have.</summary>
    public List<int>? SoundCounts { get; init; }
    /// <summary>Bip's Path: the band of grid puzzles to use.</summary>
    public Band? GridBand { get; init; }
    /// <summary>Picture-ordering games: the most picture cards in one story.</summary>
    public int? Cards { get; init; }
    /// <summary>Word Builder: spare tiles in the bank that aren't in the word.</summary>
    public int? Spares { get; init; }
    /// <summary>Art Island: the kinds of question a level mixes (e.g. "find", "fill").</summary>
    public List<string>? Modes { get; init; }
    /// <summary>Shape Builder: the shapes this level uses (ids in art/shapes.json).</summary>
    public List<string>? Shapes { get; init; }
    /// <summary>Paint Pots: the paint pots on the table (colour ids in art/paints.json).</summary>
    public List<string>? Pots { get; init; }
    /// <summary>Mirror Magic: "vertical" (left and right match) or "horizontal" (top and bottom).</summary>
    public string? Mirror { get; init; }
    /// <summary>For people reading the file.</summary>
    public string? Note { get; init; }

    public static readonly GameLevel Single = new();
}

// MARK: - phonics/graphemes.json

public sealed record GraphemesFile
{
    public string? Note { get; init; }
    public required List<PhonicsGroupEntry> Groups { get; init; }
    public required List<Grapheme> Graphemes { get; init; }
}

public sealed record PhonicsGroupEntry
{
    public required int Group { get; init; }
    public required Band Band { get; init; }
    public required AgeRange Ages { get; init; }
    public required List<string> Objectives { get; init; }
    public string? Note { get; init; }
}

/// <summary>How a sound behaves when spoken. This drives how its voice clip has to be made.</summary>
public enum GraphemeKind
{
    /// <summary>Can be held: s m f n l r v z. Written "sssss", never "ess".</summary>
    Stretchy,
    /// <summary>Stop sounds (t p k c b d g): must be clipped, no "tuh".</summary>
    Bouncy,
    /// <summary>Short vowels a e i o u.</summary>
    Vowel,
    /// <summary>More than one letter, one sound.</summary>
    Digraph,
}

public sealed record Grapheme
{
    /// <summary>Unique id ("s", "ow_long", "a_e").</summary>
    public required string Id { get; init; }
    /// <summary>The letters as the child sees them ("s", "ow", "a-e").</summary>
    [JsonPropertyName("grapheme")]
    public required string Text { get; init; }
    public required int Group { get; init; }
    public required string Ipa { get; init; }
    public required GraphemeKind Kind { get; init; }
    public required string Audio { get; init; }
    public required string MnemonicWord { get; init; }
    public required string MnemonicPicture { get; init; }
    public string? LetterName { get; init; }
    public string? HandwritingFamily { get; init; }
    public required string NarratorHint { get; init; }
    /// <summary>Another spelling of this grapheme's sound (ay is an alternative of ai).</summary>
    public string? AlternativeOf { get; init; }
}

// MARK: - words/words.json

public sealed record WordsFile
{
    public string? Note { get; init; }
    public required List<Word> Words { get; init; }
}

public sealed record Word
{
    [JsonPropertyName("word")]
    public required string Text { get; init; }
    /// <summary>Grapheme ids in reading order.</summary>
    public required List<string> Graphemes { get; init; }
    public required int SoundCount { get; init; }
    /// <summary>Only offer the word once this phonics group is unlocked.</summary>
    public required int DecodableFromGroup { get; init; }
    public required Band Band { get; init; }
    /// <summary>Grapheme id of the first sound.</summary>
    public required string FirstSound { get; init; }
    public required string Rime { get; init; }
    public required string Audio { get; init; }
    public required bool Picturable { get; init; }
    public required List<string> Objectives { get; init; }
    public string? Picture { get; init; }
    public List<string>? Tags { get; init; }
}

// MARK: - words/tricky_words.json

public sealed record TrickyWordsFile
{
    public string? Note { get; init; }
    public required TrickyWordList Stage1 { get; init; }
    public required TrickyWordList Stage2 { get; init; }
}

public sealed record TrickyWordList
{
    public required List<string> Objectives { get; init; }
    public required List<string> Words { get; init; }
}

// MARK: - words/sentences.json

public sealed record SentencesFile
{
    public string? Note { get; init; }
    public required List<Sentence> Sentences { get; init; }
}

public sealed record Sentence
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required int DecodableFromGroup { get; init; }
    public required string Audio { get; init; }
    public required string PictureRight { get; init; }
    public required string PictureRightBrief { get; init; }
    public required string PictureWrong { get; init; }
    public required string PictureWrongBrief { get; init; }
    public required List<string> Objectives { get; init; }
}

// MARK: - words/endings.json

public sealed record EndingsFile
{
    public string? Note { get; init; }
    [JsonPropertyName("plurals_s")]
    public required List<PluralEnding> PluralsS { get; init; }
    [JsonPropertyName("plurals_es")]
    public required List<PluralEnding> PluralsEs { get; init; }
    public required List<VerbEndings> Verbs { get; init; }
}

public sealed record PluralEnding
{
    public required string Word { get; init; }
    public required string Plural { get; init; }
}

public sealed record VerbEndings
{
    public required string Word { get; init; }
    public required string S { get; init; }
    /// <summary>Missing when the past tense isn't a plain -ed (sing → sang).</summary>
    public string? Ed { get; init; }
    public required string Ing { get; init; }
    public string? Note { get; init; }
}

// MARK: - words/homophones.json

public sealed record HomophonesFile
{
    public string? Note { get; init; }
    public required List<HomophoneSet> Sets { get; init; }
}

public sealed record HomophoneSet
{
    public required List<string> Words { get; init; }
    public required List<HomophoneSentence> Sentences { get; init; }
}

public sealed record HomophoneSentence
{
    /// <summary>Has exactly one "___" gap.</summary>
    public required string Text { get; init; }
    public required string Answer { get; init; }
}

// MARK: - words/contractions.json

public sealed record ContractionsFile
{
    public string? Note { get; init; }
    public required List<ContractionPair> Pairs { get; init; }
}

public sealed record ContractionPair
{
    public required string Long { get; init; }
    public required string Short { get; init; }
}

// MARK: - numbers/numbers.json

public sealed record NumbersFile
{
    public string? Note { get; init; }
    public required Dictionary<string, NumberBand> Bands { get; init; }
    public required List<CountingObject> CountingObjects { get; init; }
    public required Currency Currency { get; init; }
}

/// <summary>What each band works on in the Numbers island. Fields only appear where they apply.</summary>
public sealed record NumberBand
{
    public required AgeRange Ages { get; init; }
    public required int CountTo { get; init; }
    public int? SubitiseTo { get; init; }
    public bool? SubitiseUnfamiliarPatterns { get; init; }
    public int? BondsTo { get; init; }
    public int? ComplementsOf { get; init; }
    public int? AddSubWithin { get; init; }
    public bool? AddSubTwoDigitNoRegroup { get; init; }
    public int? DoublesTo { get; init; }
    public List<int>? StepCounts { get; init; }
    public List<int>? TimesTables { get; init; }
    public int? OrdinalsTo { get; init; }
    public List<string>? SequenceRules { get; init; }
    public bool? GrowingPatterns { get; init; }
    public bool? UnknownsAsObjects { get; init; }
    public bool? MoneyWithChange { get; init; }
    public required List<string> Objectives { get; init; }
}

public sealed record CountingObject
{
    public required string Id { get; init; }
    public required string Picture { get; init; }
    public required string AudioPlural { get; init; }
}

public sealed record Currency
{
    public required string Name { get; init; }
    public required string Symbol { get; init; }
    public required List<Money> Coins { get; init; }
    public required List<Money> Notes { get; init; }
    public required Dictionary<string, MoneyBand> ByBand { get; init; }
    public required List<ShopItem> ShopItems { get; init; }
}

public sealed record Money
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required int Cents { get; init; }
}

public sealed record MoneyBand
{
    /// <summary>Coin and note ids.</summary>
    public required List<string> Use { get; init; }
    public required string MaxTotal { get; init; }
    public bool? CompareCombinations { get; init; }
    public bool? GiveChange { get; init; }
    public required List<string> Objectives { get; init; }
}

public sealed record ShopItem
{
    public required string Id { get; init; }
    public required string Picture { get; init; }
}

// MARK: - coding/sequences.json

public sealed record SequencesFile
{
    public string? Note { get; init; }
    public required List<SequenceSet> Sets { get; init; }
}

public sealed record SequenceSet
{
    public required string Id { get; init; }
    public required Band Band { get; init; }
    public required List<SequenceCard> Cards { get; init; }
}

public sealed record SequenceCard
{
    /// <summary>Position in the right order, from 1.</summary>
    public required int N { get; init; }
    public required string Text { get; init; }
    public required string Picture { get; init; }
    public required string Audio { get; init; }
}

// MARK: - coding/patterns.json

public sealed record PatternsFile
{
    public string? Note { get; init; }
    public required Dictionary<string, List<string>> ItemSets { get; init; }
    public required List<PatternRule> Rules { get; init; }
}

public sealed record PatternRule
{
    public required string Id { get; init; }
    public required Band Band { get; init; }
    /// <summary>The repeating unit ("AB", "AAB"), or "any".</summary>
    public string? Unit { get; init; }
    public required int Show { get; init; }
    public required int Ask { get; init; }
    public string? Mode { get; init; }
    public int? Start { get; init; }
    public int? Step { get; init; }
    public string? Note { get; init; }
}

// MARK: - coding/levels.json

public sealed record LevelsFile
{
    public string? Note { get; init; }
    public required List<GridLevel> Levels { get; init; }
    public required List<PuddleLevel> PuddleRules { get; init; }
}

/// <summary>A grid cell. Rows count from the top. Written [row, column] in the content.</summary>
[JsonConverter(typeof(GridPositionConverter))]
public readonly record struct GridPosition(int Row, int Column);

public sealed record GridSize
{
    public required int Rows { get; init; }
    public required int Cols { get; init; }
}

/// <summary>One block repeated, written ["up", 3] in the content.</summary>
[JsonConverter(typeof(RepeatStepConverter))]
public readonly record struct RepeatStep(string Block, int Times);

/// <summary>A grid puzzle for Bip's Path, Fix-It and Repeat Robot.</summary>
public sealed record GridLevel
{
    public required string Id { get; init; }
    public required string Game { get; init; }
    public required string Tier { get; init; }
    public required Band Band { get; init; }
    public required GridSize Grid { get; init; }
    public required GridPosition Start { get; init; }
    public required string StartFacing { get; init; }
    public required GridPosition Goal { get; init; }
    public required List<GridPosition> Rocks { get; init; }
    public required List<string> Blocks { get; init; }
    public required List<string> OptimalProgram { get; init; }
    public required int OptimalLength { get; init; }
    public required List<string> Objectives { get; init; }
    // Fix-It levels only.
    public List<string>? BuggyProgram { get; init; }
    public int? BugIndex { get; init; }
    public string? Fix { get; init; }
    // Repeat Robot levels only.
    public List<RepeatStep>? OptimalWithRepeat { get; init; }
    public int? BlocksWithRepeat { get; init; }
}

/// <summary>A Puddle Rules corridor: one program must work on every map.</summary>
public sealed record PuddleLevel
{
    public required string Id { get; init; }
    public required string Game { get; init; }
    public required Band Band { get; init; }
    public required int CorridorLength { get; init; }
    public required List<PuddleMap> Maps { get; init; }
    public required List<string> Blocks { get; init; }
    public required PuddleSolution Solution { get; init; }
    public required List<string> Objectives { get; init; }
    public string? Note { get; init; }
}

public sealed record PuddleMap
{
    public required List<int> PuddlesAt { get; init; }
}

public sealed record PuddleSolution
{
    public required int Repeat { get; init; }
    public required List<PuddleRule> Body { get; init; }
}

/// <summary>"If puddle ahead, jump, otherwise go forward."</summary>
public sealed record PuddleRule
{
    [JsonPropertyName("if")]
    public required string Condition { get; init; }
    public required List<string> Then { get; init; }
    [JsonPropertyName("else")]
    public required List<string> Otherwise { get; init; }
}

// MARK: - asset_manifest.json (generated)

public sealed record AssetManifestFile
{
    public string? Note { get; init; }
    public required List<AudioAsset> Audio { get; init; }
    public required List<PictureAsset> Pictures { get; init; }
}

public sealed record AudioAsset
{
    public required string Id { get; init; }
    /// <summary>Exactly what the narrator says.</summary>
    public required string Text { get; init; }
    public required string Notes { get; init; }
}

public sealed record PictureAsset
{
    public required string Id { get; init; }
    public required string Brief { get; init; }
}

// MARK: - art/shapes.json

public sealed record ShapesFile
{
    public string? Note { get; init; }
    public required List<ArtShape> Shapes { get; init; }
    public required List<ShapePicture> Pictures { get; init; }
}

/// <summary>A flat shape. Its outline sits in a box from -1 to 1 (y up), stretched to <see cref="Card"/>.</summary>
public sealed record ArtShape
{
    public required string Id { get; init; }
    /// <summary>What Bip calls it ("triangle"); wonky shapes share their family's name.</summary>
    public required string Name { get; init; }
    public required Band Band { get; init; }
    public required ShapeOutline Outline { get; init; }
    /// <summary>Width and height (as a ratio) the shape shows at on its own.</summary>
    public required List<double> Card { get; init; }
    public required int StraightSides { get; init; }
    public required bool Curved { get; init; }
    /// <summary>All sides and corners equal. Only polygons say.</summary>
    public bool? Regular { get; init; }
    /// <summary>How many times it looks the same in one full turn (0: every way round, like a circle).</summary>
    public required int LookSameTurns { get; init; }

    /// <summary>The word clip that says the shape's name.</summary>
    [JsonIgnore]
    public string Audio => AudioCatalogue.WordClip(Name.Replace(' ', '_'));

    /// <summary>Looks the same after a quarter turn (a square, a circle).</summary>
    [JsonIgnore]
    public bool SameAfterQuarterTurn => LookSameTurns % 4 == 0;
}

public sealed record ShapeOutline
{
    /// <summary>"polygon", "ellipse" or "semicircle" (flat side down).</summary>
    public required string Kind { get; init; }
    /// <summary>Polygon corners, [x, y] from -1 to 1.</summary>
    public List<List<double>>? Points { get; init; }
}

/// <summary>A picture built from shapes, in a 600 x 420 frame centred on 0,0.</summary>
public sealed record ShapePicture
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required List<ShapePiece> Pieces { get; init; }
}

public sealed record ShapePiece
{
    public required string Shape { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int W { get; init; }
    public required int H { get; init; }
    /// <summary>A palette colour name ("sun", "red", "teal").</summary>
    public required string Colour { get; init; }
    /// <summary>Degrees, anticlockwise.</summary>
    public int? Rotation { get; init; }
    /// <summary>Too small to tap, so never the missing piece.</summary>
    public bool? Fixed { get; init; }
}

// MARK: - art/paints.json

public sealed record PaintsFile
{
    public string? Note { get; init; }
    public required List<PaintColour> Colours { get; init; }
    public required List<PaintPot> Pots { get; init; }
    public required List<PaintMix> Mixes { get; init; }
}

public sealed record PaintColour
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    /// <summary>#RRGGBB.</summary>
    public required string Hex { get; init; }

    [JsonIgnore]
    public string Audio => AudioCatalogue.WordClip(Id);
}

/// <summary>A paint pot: its colour and a label picture (an apple on the red pot).</summary>
public sealed record PaintPot
{
    public required string Colour { get; init; }
    public required string Label { get; init; }
}

/// <summary>Paint <see cref="A"/> and paint <see cref="B"/> make <see cref="Makes"/>.</summary>
public sealed record PaintMix
{
    public required string A { get; init; }
    public required string B { get; init; }
    public required string Makes { get; init; }
}

// MARK: - art/mirror.json

public sealed record MirrorFile
{
    public string? Note { get; init; }
    /// <summary>Paint letter → colour id in art/paints.json.</summary>
    public required Dictionary<string, string> Paints { get; init; }
    public required List<MirrorPicture> Pictures { get; init; }
    public required PegBoard Pegs { get; init; }
}

/// <summary>A square peg picture, top row first; "." is an empty hole.</summary>
public sealed record MirrorPicture
{
    public required string Id { get; init; }
    /// <summary>"vertical" or "horizontal": the one line it folds onto itself along.</summary>
    public required string Mirror { get; init; }
    public required List<string> Rows { get; init; }
}

/// <summary>The peg board for "finish the pattern" rounds.</summary>
public sealed record PegBoard
{
    public required int Size { get; init; }
    public required int Fewest { get; init; }
    public required int Most { get; init; }
    public required string Colour { get; init; }
}
