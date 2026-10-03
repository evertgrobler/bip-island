using System.Collections.Generic;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// Meet the Sound: the letter bounces in, the narrator says the pure sound three times, then the
/// picture word (s → sun). Click the letter to hear it again; the green arrow carries on.
/// </summary>
public partial class MeetSoundScreen : GameScreen
{
    private readonly PhonicsSound _sound;
    private Node2D _letter = null!;
    private PictureCard? _picture;
    private Node2D _nextButton = null!;

    public MeetSoundScreen(PhonicsSound sound) => _sound = sound;

    protected override Island HomeIsland => Island.Letters;
    protected override string GameId => MeetTheSoundGame.GameId;
    protected override IReadOnlyList<Node2D> KeyOptions => _nextButton.Visible ? [_nextButton] : [];

    /// <summary>True once the introduction has finished and the green arrow shows (for the walk-through test).</summary>
    public bool IntroDone => _nextButton.Visible;

    protected override void Build()
    {
        AddHomeButton();
        AddBip(P(-560, -400), 0.95f);

        _letter = Buttons.Tappable(Sketch.Letter(_sound.Grapheme, 380, shadow: Palette.Sun), "letter");
        _letter.Position = P(-60, 60);
        _letter.Scale = Vector2.One * 0.01f;
        _letter.ZIndex = 10;
        Stage.AddChild(_letter);

        _nextButton = Buttons.Next();
        _nextButton.Position = P(600, -320);
        _nextButton.Visible = false;
        _nextButton.ZIndex = 10;
        Stage.AddChild(_nextButton);

        // Clicks wait until the introduction has finished, so it can't be cut short.
        InputLocked = true;
        if (StartDelay == 0) _letter.Scale = Vector2.One; // Screenshots: show the letter straight away.
        After(StartDelay == 0 ? 0 : 0.4, Introduce);
    }

    private static void Bounce(Node2D node, float to, double up, double down)
    {
        var tween = node.CreateTween();
        tween.TweenProperty(node, "scale", Vector2.One * to, up);
        tween.TweenProperty(node, "scale", Vector2.One, down);
    }

    private void Introduce()
    {
        Sfx.Play(BipSounds.Effect.Whirr);
        var grow = _letter.CreateTween();
        grow.TweenProperty(_letter, "scale", Vector2.One * 1.15f, 0.25).SetEase(Tween.EaseType.Out);
        grow.TweenProperty(_letter, "scale", Vector2.One * 0.95f, 0.12);
        grow.TweenProperty(_letter, "scale", Vector2.One, 0.1);
        Bip.Celebrate();
        Voice.Play([VoiceLine.MeetNewSound, _sound.SoundClip, _sound.SoundClip, _sound.SoundClip],
                   (_, clip) => ReactToClip(clip), () => After(0.4, ShowPicture));
    }

    private void ShowPicture()
    {
        var card = Buttons.Tappable(new PictureCard(_sound.Picture, _sound.PictureWord, 800), "picture");
        card.Position = P(470, 60);
        card.Scale = Vector2.One * 0.01f;
        card.ZIndex = 10;
        Stage.AddChild(card);
        _picture = card;
        var pop = card.CreateTween();
        pop.TweenProperty(card, "scale", Vector2.One * 1.1f, 0.25);
        pop.TweenProperty(card, "scale", Vector2.One, 0.12);
        Sfx.Play(BipSounds.Effect.Chime);

        Voice.Play([_sound.WordClip, _sound.SoundClip, VoiceLine.SayItWithMe], (_, clip) => ReactToClip(clip), () =>
            // Leave a moment for the child to say it, then model it once more.
            After(1.6, () => Voice.Play([_sound.SoundClip, VoiceLine.ClickToHearAgain], (_, clip) => ReactToClip(clip), ShowNextButton)));
    }

    private void ReactToClip(string clip)
    {
        if (clip == _sound.SoundClip)
        {
            Bounce(_letter, 1.12f, 0.12, 0.18);
            Bip.Hop();
        }
        else if (clip == _sound.WordClip && _picture != null)
        {
            Bounce(_picture, 1.08f, 0.12, 0.18);
        }
    }

    private void ShowNextButton()
    {
        if (_nextButton.Visible) return;
        InputLocked = false;
        _nextButton.Visible = true;
        _nextButton.Scale = Vector2.One * 0.01f;
        var grow = _nextButton.CreateTween();
        grow.TweenProperty(_nextButton, "scale", Vector2.One, 0.25);
        grow.TweenCallback(Callable.From(() => Buttons.Pulse(_nextButton)));
    }

    protected override void HandleTap(string name, Node2D node)
    {
        switch (name)
        {
            case "letter":
                Voice.Play([_sound.SoundClip], (_, clip) => ReactToClip(clip));
                break;
            case "picture":
                Voice.Play([_sound.WordClip, _sound.SoundClip], (_, clip) => ReactToClip(clip));
                break;
            case "next":
                InputLocked = true;
                Sfx.Play(BipSounds.Effect.Chime);
                Buttons.Press(node);
                Coordinator.MarkMet(_sound);
                Bip.Celebrate();
                Voice.Play([Coordinator.RandomPraise()], completion: () => Coordinator.ShowIsland(Island.Letters, greet: false));
                break;
        }
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) Voice.Play([_sound.SoundClip], (_, clip) => ReactToClip(clip));
    }
}
