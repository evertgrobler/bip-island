using System.Text.Json.Serialization;

namespace BipCore;

public enum MasteryChangeKind { None, LevelledUp, DroppedBack }

/// <summary>What one answer did to a level: nothing, up a level, or back a level.</summary>
public readonly record struct MasteryChange(MasteryChangeKind Kind, int To)
{
    public static readonly MasteryChange None = new(MasteryChangeKind.None, 0);
    public static MasteryChange LevelledUp(int to) => new(MasteryChangeKind.LevelledUp, to);
    public static MasteryChange DroppedBack(int to) => new(MasteryChangeKind.DroppedBack, to);
}

/// <summary>
/// A level plus the current streaks: right answers in a row move up, misses in a row drop back.
/// The numbers come from <see cref="MasteryRules"/> (Content/curriculum/skills.json).
/// </summary>
public sealed record SkillMastery
{
    [JsonInclude] public int Level { get; private set; }
    [JsonInclude] public int CorrectStreak { get; private set; }
    [JsonInclude] public int MissStreak { get; private set; }

    public SkillMastery() { }

    public SkillMastery(int level, int correctStreak = 0, int missStreak = 0)
    {
        Level = level;
        CorrectStreak = correctStreak;
        MissStreak = missStreak;
    }

    /// <summary>Records one answered question. Streaks reset whenever the level changes.</summary>
    public MasteryChange Record(bool correct, MasteryRules rules, int maxLevel, int minLevel = 0)
    {
        if (correct)
        {
            CorrectStreak += 1;
            MissStreak = 0;
            if (CorrectStreak >= rules.CorrectInARowToMoveUp)
            {
                CorrectStreak = 0;
                if (Level < maxLevel)
                {
                    Level += 1;
                    return MasteryChange.LevelledUp(Level);
                }
            }
        }
        else
        {
            MissStreak += 1;
            CorrectStreak = 0;
            if (MissStreak >= rules.MissesInARowToDropBack)
            {
                MissStreak = 0;
                if (Level > minLevel)
                {
                    Level -= 1;
                    return MasteryChange.DroppedBack(Level);
                }
            }
        }
        return MasteryChange.None;
    }

    /// <summary>Moves straight to a level (e.g. after "Meet the Sound"), clearing the streaks.</summary>
    public void Raise(int newLevel)
    {
        if (newLevel <= Level) return;
        Level = newLevel;
        CorrectStreak = 0;
        MissStreak = 0;
    }
}

/// <summary>Where a child is with one sound. Each stage has its own Letters game.</summary>
public enum SoundStage
{
    /// <summary>Not met yet → Meet the Sound.</summary>
    New = 0,
    /// <summary>Met → Sound Hunt.</summary>
    Met = 1,
    /// <summary>Recognises it in pictures → Bubble Pop.</summary>
    Recognises = 2,
    /// <summary>Knows it → review only.</summary>
    Mastered = 3,
}

/// <summary>The level of every sound a child has worked on, keyed by sound id.</summary>
public sealed class MasteryTracker : IEquatable<MasteryTracker>
{
    [JsonInclude] public Dictionary<string, SkillMastery> Skills { get; private set; }

    public MasteryTracker() => Skills = [];

    public MasteryTracker(Dictionary<string, SkillMastery> skills) => Skills = skills;

    /// <summary>A copy of a sound's mastery (a fresh one if the sound hasn't been played).</summary>
    public SkillMastery Mastery(string soundId) =>
        Skills.TryGetValue(soundId, out var found) ? found with { } : new SkillMastery();

    public SoundStage Stage(string soundId) =>
        (SoundStage)Math.Clamp(Mastery(soundId).Level, 0, (int)SoundStage.Mastered);

    public SoundStage Stage(PhonicsSound sound) => Stage(sound.Id);

    /// <summary>
    /// Records one answered question about a sound. Two misses at "met" drop back to "new",
    /// so the child hears the sound introduced again.
    /// </summary>
    public MasteryChange Record(bool correct, string soundId, MasteryRules rules)
    {
        var mastery = Mastery(soundId);
        var change = mastery.Record(correct, rules, maxLevel: (int)SoundStage.Mastered);
        Skills[soundId] = mastery;
        return change;
    }

    /// <summary>Called when Meet the Sound finishes.</summary>
    public void MarkMet(string soundId)
    {
        var mastery = Mastery(soundId);
        mastery.Raise((int)SoundStage.Met);
        Skills[soundId] = mastery;
    }

    public bool Equals(MasteryTracker? other) =>
        other is not null && Skills.Count == other.Skills.Count
        && Skills.All(pair => other.Skills.TryGetValue(pair.Key, out var theirs) && pair.Value == theirs);

    public override bool Equals(object? obj) => Equals(obj as MasteryTracker);
    public override int GetHashCode() => Skills.Count;
}
