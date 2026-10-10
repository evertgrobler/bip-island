using BipCore;
using Xunit;

namespace BipCore.Tests;

public sealed class PhonicsTests
{
    private static PhonicsCourse Course() => new(TestContent.Library());

    private static List<string> Ids(IEnumerable<PhonicsSound> sounds) => sounds.Select(s => s.Id).ToList();

    [Fact]
    public void GroupsFollowTheLettersAndSoundsOrder()
    {
        var course = Course();
        Assert.Equal(Enumerable.Range(1, 9), course.Groups.Select(g => g.Number));
        Assert.Equal(["s", "a", "t", "p", "i", "n"], Ids(course.Groups[0].Sounds));
        Assert.Equal(["m", "d", "g", "o", "c", "k"], Ids(course.Groups[1].Sounds));
        Assert.Equal(["ck", "e", "u", "r"], Ids(course.Groups[2].Sounds));
        Assert.Equal(["h", "b", "f", "l", "ff", "ll", "ss"], Ids(course.Groups[3].Sounds));
        Assert.Equal(["j", "v", "w", "x", "y", "z", "zz", "qu"], Ids(course.Groups[4].Sounds));
        Assert.Equal(new HashSet<string> { "ch", "sh", "th", "ng" }, course.Groups[5].Sounds.Select(s => s.Grapheme).ToHashSet());
        Assert.Equal("satpin", string.Concat(course.FirstGroup.Sounds.Select(s => s.Grapheme)));
    }

    [Fact]
    public void GroupsLinkToTheirSkills()
    {
        Assert.Equal("snd_g3", PhonicsCourse.SkillIdForGroup(3));
        Assert.Equal(7, PhonicsCourse.GroupForSkill("snd_g7"));
        Assert.Null(PhonicsCourse.GroupForSkill("blend_cvc"));
    }

    [Fact]
    public void NoSoundIsTaughtTwice()
    {
        var ids = Ids(Course().AllSounds);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void StopSoundsMustBeClipped()
    {
        var course = Course();
        foreach (var id in new[] { "t", "p", "k", "c", "b", "d", "g" })
        {
            var sound = course.Sound(id);
            Assert.NotNull(sound);
            Assert.Equal(GraphemeKind.Bouncy, sound.Kind);
            Assert.True(sound.MustBeClipped, id);
        }
        foreach (var id in new[] { "s", "m", "n", "a", "sh" }) Assert.False(course.Sound(id)!.MustBeClipped, id);
    }

    [Fact]
    public void StretchySoundsAndVowels()
    {
        var course = Course();
        var stretchy = course.AllSounds.Where(s => s.Kind == GraphemeKind.Stretchy && s.Grapheme.Length == 1).Select(s => s.Id).ToHashSet();
        Assert.Equal(new HashSet<string> { "s", "m", "f", "n", "l", "r", "v", "w", "y", "z" }, stretchy);
        var vowels = course.AllSounds.Where(s => s.Kind == GraphemeKind.Vowel).Select(s => s.Id).ToHashSet();
        Assert.Equal(new HashSet<string> { "a", "e", "i", "o", "u" }, vowels);
    }

    [Fact]
    public void SoundsUpToAGroup()
    {
        var course = Course();
        Assert.Equal(["s", "a", "t", "p", "i", "n"], Ids(course.SoundsUpToGroup(1)));
        Assert.Equal(12, course.SoundsUpToGroup(2).Count);
        Assert.Equal(course.AllSounds.Count, course.SoundsUpToGroup(9).Count);
    }

    [Fact]
    public void SoundsAndPicturesUseTheFileNameContract()
    {
        var content = TestContent.Library();
        foreach (var sound in Course().AllSounds)
        {
            Assert.Equal($"snd_{sound.Id}", sound.SoundClip);
            Assert.Equal($"pic_{sound.PictureWord.Replace("-", "")}", sound.Picture);
            Assert.True(content.AudioIds.Contains(sound.SoundClip), sound.Id);
            Assert.True(content.AudioIds.Contains(sound.WordClip), sound.Id);
            Assert.True(content.PictureIds.Contains(sound.Picture), sound.Id);
        }
    }

    [Fact]
    public void SoundsThatCouldBeMixedUp()
    {
        var course = Course();
        PhonicsSound Sound(string id) => course.Sound(id) ?? throw new InvalidOperationException(id);
        Assert.True(Sound("c").IsConfusable(Sound("k")), "c and k sound the same");
        Assert.True(Sound("ck").IsConfusable(Sound("c")));
        Assert.True(Sound("ow").IsConfusable(Sound("ow_long")), "ow and ow look the same");
        Assert.True(Sound("ai").IsConfusable(Sound("ay")));
        Assert.False(Sound("s").IsConfusable(Sound("a")));
        Assert.False(Sound("s").IsConfusable(Sound("s")), "a sound isn't confusable with itself");
    }

    [Fact]
    public void LetterNamesComeOnlyAfterGroupFive()
    {
        var course = Course();
        var named = course.AllSounds.Where(s => s.LetterName is not null);
        Assert.True(named.All(s => s.Group <= 5), "letter names belong to single letters in groups 1–5");

        Assert.False(PhonicsCourse.LetterNamesUnlocked([]));
        Assert.False(PhonicsCourse.LetterNamesUnlocked(Ids(course.FirstGroup.Sounds)));
        var groupsOneToFive = Ids(course.SoundsUpToGroup(5)).ToHashSet();
        Assert.True(PhonicsCourse.LetterNamesUnlocked(groupsOneToFive));
        Assert.False(PhonicsCourse.LetterNamesUnlocked(groupsOneToFive.Where(id => id != "qu")), "q is only covered by qu");
        Assert.Equal(new HashSet<string> { "q" }, PhonicsCourse.LettersCovered(["qu", "sh"]));
    }
}
