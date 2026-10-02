using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>
/// Keeps the voice script in step with the game: audio/script.csv holds instructions, praise and hints;
/// Content/asset_manifest.json holds every sound and word clip the content needs.
/// </summary>
public sealed class AudioScriptTests
{
    private sealed record Row(string File, string Text, string Notes);

    private static List<Row> LoadScript()
    {
        var csv = File.ReadAllText(Path.Combine(TestContent.RepoRoot, "audio", "script.csv"));
        var records = ParseCsv(csv);
        Assert.NotEmpty(records);
        Assert.Equal(["file", "text", "notes"], records[0].Take(3));
        return records.Skip(1).Where(r => !r.All(string.IsNullOrEmpty))
            .Select(r => new Row(r[0], r.Count > 1 ? r[1] : "", r.Count > 2 ? r[2] : "")).ToList();
    }

    /// <summary>The clip name without its file extension (.m4a today, .ogg in the Godot build).</summary>
    private static string ClipName(string file) => Path.GetFileNameWithoutExtension(file);

    [Fact]
    public void ScriptListsEveryInstructionPraiseAndHint()
    {
        var files = LoadScript().Select(r => ClipName(r.File)).ToList();
        Assert.True(files.Count == files.Distinct().Count(), "duplicate rows in audio/script.csv");
        var missing = AudioCatalogue.ScriptedClips.Except(files).Order().ToList();
        Assert.True(missing.Count == 0, "add to audio/script.csv: " + string.Join(", ", missing));
    }

    /// <summary>
    /// Every other row must be a clip the content needs (e.g. hand-written notes for snd_s),
    /// so the script never lists clips nothing plays.
    /// </summary>
    [Fact]
    public void EveryOtherScriptRowIsInTheManifest()
    {
        var content = TestContent.Library();
        var scripted = AudioCatalogue.ScriptedClips.ToHashSet();
        foreach (var row in LoadScript())
        {
            var name = ClipName(row.File);
            Assert.True(scripted.Contains(name) || content.AudioIds.Contains(name), $"{row.File} isn't used by the game or the content");
        }
    }

    /// <summary>The Letters games only play clips the manifest lists, so the narrator script covers them.</summary>
    [Fact]
    public void EveryClipTheLettersGamesCanPlayIsInTheManifest()
    {
        var content = TestContent.Library();
        var course = new PhonicsCourse(content);
        var hunt = new SoundHuntGame(content, course);
        foreach (var sound in course.AllSounds)
        {
            Assert.True(content.AudioIds.Contains(sound.SoundClip), sound.SoundClip);
            Assert.True(content.AudioIds.Contains(sound.WordClip), sound.WordClip);
        }
        foreach (var picture in hunt.Pictures)
        {
            Assert.True(content.AudioIds.Contains(picture.Audio), picture.Audio);
            Assert.True(content.PictureIds.Contains(picture.Picture), picture.Picture);
        }
    }

    [Fact]
    public void EveryManifestClipFollowsTheNamingContract()
    {
        foreach (var clip in TestContent.Library().Manifest.Audio)
        {
            Assert.True(AudioCatalogue.FollowsNamingContract(clip.Id), clip.Id);
            Assert.False(string.IsNullOrWhiteSpace(clip.Text), $"{clip.Id} has no text");
        }
    }

    [Fact]
    public void EveryRowFollowsTheNamingContract()
    {
        foreach (var row in LoadScript())
        {
            Assert.True(row.File.EndsWith(".m4a", StringComparison.Ordinal), row.File);
            Assert.True(AudioCatalogue.FollowsNamingContract(ClipName(row.File)), row.File);
            Assert.False(string.IsNullOrWhiteSpace(row.Text), $"{row.File} has no text");
        }
    }

    [Fact]
    public void EverySoundClipHasPronunciationNotes()
    {
        foreach (var row in LoadScript().Where(r => r.File.StartsWith("snd_", StringComparison.Ordinal)))
        {
            Assert.False(string.IsNullOrEmpty(row.Notes), $"{row.File} needs pronunciation notes");
        }
    }

    [Fact]
    public void NamingContract()
    {
        Assert.True(AudioCatalogue.FollowsNamingContract("snd_sh"));
        Assert.True(AudioCatalogue.FollowsNamingContract("vo_find_the_sound"));
        Assert.True(AudioCatalogue.FollowsNamingContract("praise_07"));
        Assert.True(AudioCatalogue.FollowsNamingContract("name_s"));
        Assert.True(AudioCatalogue.FollowsNamingContract("money_r5"));
        Assert.False(AudioCatalogue.FollowsNamingContract("praise_7"));
        Assert.False(AudioCatalogue.FollowsNamingContract("Snd_S"));
        Assert.False(AudioCatalogue.FollowsNamingContract("sound_s"));
    }

    /// <summary>Minimal CSV reader: commas, double-quoted fields, "" escapes.</summary>
    internal static List<List<string>> ParseCsv(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new System.Text.StringBuilder();
        var inQuotes = false;
        var chars = text.Replace("\r\n", "\n");
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < chars.Length && chars[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { record.Add(field.ToString()); field.Clear(); }
            else if (c == '\n') { record.Add(field.ToString()); records.Add(record); record = []; field.Clear(); }
            else field.Append(c);
        }
        if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString()); records.Add(record); }
        return records;
    }
}
