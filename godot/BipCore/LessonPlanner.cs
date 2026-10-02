namespace BipCore;

/// <summary>The three Letters games, by their ids in games.json.</summary>
public enum ActivityKind { MeetTheSound, SoundHunt, BubblePop }

public static class ActivityKindExtensions
{
    public static readonly IReadOnlyList<ActivityKind> All = [ActivityKind.MeetTheSound, ActivityKind.SoundHunt, ActivityKind.BubblePop];

    public static string GameId(this ActivityKind kind) => kind switch
    {
        ActivityKind.MeetTheSound => MeetTheSoundGame.GameId,
        ActivityKind.SoundHunt => SoundHuntGame.GameId,
        _ => BubblePopGame.GameId,
    };
}

/// <param name="Sound">The sound Bip wants to practise.</param>
public sealed record PlannedActivity(ActivityKind Kind, PhonicsSound Sound);

/// <summary>Where one child is on Letters Island, worked out from their progress and the skill map.</summary>
public sealed class LettersProgress
{
    public PhonicsCourse Course { get; }
    public ChildProgress Progress { get; }
    public Band StartingBand { get; }
    /// <summary>Group numbers whose skill (snd_g1 …) is unlocked, in order. Group 1 is always open.</summary>
    public IReadOnlyList<int> UnlockedGroups { get; }
    /// <summary>The group shown on the island: the first open group with sounds still to learn.</summary>
    public PhonicsGroup CurrentGroup { get; }
    /// <summary>Sounds the child has met, plus every sound in a group they already know.</summary>
    public IReadOnlySet<string> KnownSoundIds { get; }

    public LettersProgress(ContentLibrary content, PhonicsCourse course, ChildProgress progress, Band startingBand)
    {
        Course = course;
        Progress = progress;
        StartingBand = startingBand;

        Skill? GroupSkill(PhonicsGroup group) => content.Skill(group.SkillId);
        var open = course.Groups.Where(group =>
            GroupSkill(group) is Skill skill && progress.IsUnlocked(skill, startingBand, content.Skill)).ToList();
        var openGroups = open.Count == 0 ? [course.FirstGroup] : open;
        UnlockedGroups = openGroups.Select(g => g.Number).ToList();

        var known = new HashSet<string>();
        foreach (var group in openGroups)
        {
            var groupKnown = GroupSkill(group) is Skill skill && progress.IsKnown(skill, startingBand);
            foreach (var sound in group.Sounds)
            {
                if (groupKnown || progress.Sounds.Stage(sound) >= SoundStage.Met) known.Add(sound.Id);
            }
        }
        KnownSoundIds = known;

        var stillToLearn = openGroups.FirstOrDefault(group =>
        {
            var treatedAsKnown = GroupSkill(group) is Skill skill && progress.IsTreatedAsKnown(skill, startingBand);
            return !treatedAsKnown && group.Sounds.Any(s => progress.Sounds.Stage(s) < SoundStage.Mastered);
        });
        CurrentGroup = stillToLearn ?? openGroups[^1];
    }

    public int HighestUnlockedGroup => UnlockedGroups.Count > 0 ? UnlockedGroups[^1] : 1;

    public SoundStage Stage(PhonicsSound sound) => Progress.Sounds.Stage(sound);

    /// <summary>What the round generators need, optionally focused on one sound.</summary>
    public Learner Learner(PhonicsSound? focus = null) =>
        new(StartingBand, HighestUnlockedGroup, KnownSoundIds, focus?.Id);
}

/// <summary>
/// Bip's suggestion of what to play next on Letters Island.
///
/// Rules: a phonics group that is due for review comes first. Otherwise new sounds are met in order,
/// keeping no more than two met-but-not-yet-recognised sounds waiting; waiting sounds are practised
/// with Sound Hunt (met) and then Bubble Pop (recognises). When the group is all learnt, it's review.
/// </summary>
/// <param name="canHunt">Whether Sound Hunt has pictures for a sound (no word starts with ng or x).</param>
public sealed class LessonPlanner(LettersProgress letters, Func<PhonicsSound, bool> canHunt)
{
    /// <summary>How many met-but-not-recognised sounds can wait before Bip stops introducing new ones.</summary>
    public const int MaxSoundsWaiting = 2;

    public LettersProgress Letters { get; } = letters;

    public PlannedActivity NextActivity(int day, MasteryRules rules, IRandomSource rng)
    {
        // 1. Reviews first.
        foreach (var number in Letters.UnlockedGroups)
        {
            if (Letters.Course.Group(number) is not PhonicsGroup group
                || !Letters.Progress.IsDueForReview(group.SkillId, day, rules)) continue;
            if (ReviewActivity(group, rng) is PlannedActivity review) return review;
        }

        // 2. Meet, then hunt, then pop, through the current group.
        var sounds = Letters.CurrentGroup.Sounds;
        var notMastered = sounds.Where(s => Letters.Stage(s) < SoundStage.Mastered).ToList();
        var waiting = notMastered.Count(s => Letters.Stage(s) == SoundStage.Met);
        if (notMastered.FirstOrDefault(s => Letters.Stage(s) == SoundStage.New) is PhonicsSound fresh && waiting < MaxSoundsWaiting)
        {
            return new PlannedActivity(ActivityKind.MeetTheSound, fresh);
        }
        if (notMastered.FirstOrDefault(s => Letters.Stage(s) != SoundStage.New) is PhonicsSound practise)
        {
            return new PlannedActivity(PracticeKind(practise), practise);
        }
        if (notMastered.FirstOrDefault() is PhonicsSound first)
        {
            return new PlannedActivity(ActivityKind.MeetTheSound, first);
        }

        // 3. Everything in the group is learnt: review it.
        return ReviewActivity(Letters.CurrentGroup, rng)
            ?? new PlannedActivity(ActivityKind.MeetTheSound, rng.Pick(sounds) ?? Letters.Course.AllSounds[0]);
    }

    /// <summary>The sound Bip suggests next (the first in the current group not learnt yet), or null.</summary>
    public PhonicsSound? SuggestedSound() =>
        Letters.CurrentGroup.Sounds.FirstOrDefault(s => Letters.Stage(s) < SoundStage.Mastered);

    /// <summary>Sound Hunt for a met sound, Bubble Pop once it's recognised (or if no picture starts with it).</summary>
    public ActivityKind PracticeKind(PhonicsSound sound) =>
        Letters.Stage(sound) == SoundStage.Met && canHunt(sound) ? ActivityKind.SoundHunt : ActivityKind.BubblePop;

    private PlannedActivity? ReviewActivity(PhonicsGroup group, IRandomSource rng)
    {
        var known = group.Sounds.Where(s => Letters.KnownSoundIds.Contains(s.Id)).ToList();
        if (rng.Pick(known) is not PhonicsSound sound) return null;
        var kind = canHunt(sound) && rng.NextBool() ? ActivityKind.SoundHunt : ActivityKind.BubblePop;
        return new PlannedActivity(kind, sound);
    }
}
