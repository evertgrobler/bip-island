using System.Collections.Generic;
using System.Linq;
using BipCore;
using BipIsland.Audio;
using BipIsland.Drawing;
using BipIsland.Screens;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Games;

/// <summary>
/// Word Rocket: Bip says a letter sound or a word, and the child types it on the real keyboard.
/// Each right key fills the next window of the craft (a rocket, a hot-air balloon or a submarine);
/// the last one launches it with stars, and Bip says the word again. A wrong key gives a soft boop,
/// its window shakes and nothing is typed; two misses on one letter bring a spoken hint and that key
/// glows on the keyboard picture. Backspace takes the last letter back. Upper and lower case both
/// count; letters always show in lower case.
///
/// Keys: everywhere else in the game any key says the question again, but here letter keys type.
/// So Space (and the replay button, or a click on the craft) says it again, Enter does nothing, and
/// Esc is never typed: the parent layer takes it first, so holding it still opens the parent gate.
/// </summary>
public partial class WordRocketScreen : GameScreen
{
    /// <summary>Where the row of windows sits: lower for the balloon, whose envelope is above it.</summary>
    private Vector2 CraftHome => _session.Skin.Id == "balloon" ? P(40, 10) : P(40, 110);

    private readonly WordRocketGame _game;
    private readonly GameSession _session;
    private WordRocketGame.Round? _round;
    private Node2D? _craft;
    private readonly List<RocketWindow> _windows = [];
    private KeyboardPicture _keyboard = null!;
    private readonly List<string> _typed = [];
    /// <summary>Misses on the letter being typed now; two bring the hint.</summary>
    private int _misses;
    /// <summary>Any miss in this question: then it doesn't count as right first time.</summary>
    private bool _missedAny;

    /// <summary>Previews only: the skin to draw, and letters to type with two misses after them, so a screenshot shows a hint.</summary>
    public static string? PreviewSkin { get; set; }
    public static int PreviewLetters { get; set; }

    public WordRocketScreen(WordRocketGame game)
    {
        _game = game;
        _session = PreviewSkin is string skin && game.Skins.FirstOrDefault(s => s.Id == skin) is { } chosen
            ? new GameSession(game.Id, chosen, Coordinator.Content!.Games.Session.RoundsPerSession)
            : Coordinator.NewSession(game);
    }

    protected override Island HomeIsland => Island.Words;
    protected override string GameId => WordRocketGame.GameId;

    // For the walk-through test.
    public string? Answer => _round?.Answer;
    public string Typed => string.Concat(_typed);
    public int Misses => _misses;
    public string? GlowingKey => _keyboard.Glowing;
    public string Skin => _session.Skin.Id;
    /// <summary>The keys that fill the next window (lower case).</summary>
    public IReadOnlySet<string> NextKeys =>
        _round is { } round && _typed.Count < round.Slots.Count ? round.Slots[_typed.Count].Keys : new HashSet<string>();

    protected override void Build()
    {
        AddGameChrome(P(640, 360), bipAt: P(-640, -330), bipScale: 0.7f);
        _keyboard = new KeyboardPicture { Position = P(60, -175), ZIndex = 5 };
        Stage.AddChild(_keyboard);
        After(StartDelay, AskQuestion);
    }

    private void AskQuestion()
    {
        if (_session.NextRound(_game, Coordinator.LearnerFor(GameId), Coordinator.Rng) is not { } next)
        {
            EndVisit(Ending.RoundDone);
            return;
        }
        _round = next;
        _typed.Clear();
        _windows.Clear();
        _misses = 0;
        _missedAny = false;
        _keyboard.StopGlow();
        ResetKeys();

        var (centres, radius) = RocketDrawing.WindowLayout(next.Slots.Count);
        var halfWidth = -centres[0].X + radius;
        var craft = Buttons.Tappable(new Node2D { ZIndex = 10 }, "craft");
        craft.AddChild(RocketDrawing.Craft(_session.Skin.Id, halfWidth));
        for (var i = 0; i < centres.Count; i++)
        {
            var window = new RocketWindow(radius, (ulong)i) { Position = centres[i] };
            craft.AddChild(window);
            _windows.Add(window);
        }
        Hit.SetArea(craft, new Rect2((float)(-halfWidth - 120), -110, (float)(2 * halfWidth + 240), 220));
        // The new craft glides in from the left.
        craft.Position = CraftHome + P(-1400, 0);
        Stage.AddChild(craft);
        _craft = craft;
        var glide = craft.CreateTween();
        glide.TweenProperty(craft, "position", CraftHome, 0.6).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        MarkNextWindow();
        InputLocked = false;
        SayPrompt();
        if (PreviewLetters > 0) After(0.7, TypeForPreview);
    }

    private void SayPrompt()
    {
        if (_round is not { } round) return;
        Voice.Play([VoiceLine.WordRocket, round.Clip]);
        Bip.Hop();
    }

    public override void ReplayPrompt()
    {
        if (!InputLocked) SayPrompt();
    }

    protected override void HandleTap(string name, Node2D node)
    {
        if (name == "replay")
        {
            Buttons.Press(node);
            SayPrompt();
        }
        else if (name == "craft" && _round is { } round)
        {
            Voice.Play([round.Clip]);
        }
    }

    // Typing

    public override void _UnhandledInput(InputEvent @event)
    {
        // Esc belongs to the parent gate (ParentLayer takes it in _Input); never type it.
        if (@event is InputEventKey { Keycode: Key.Escape }) return;
        if (@event is InputEventKey key)
        {
            GetViewport().SetInputAsHandled();
            if (key.Pressed && !key.Echo) Press(key);
            return;
        }
        base._UnhandledInput(@event);
    }

    /// <summary>The lower-case letter a key types, or null for anything that isn't a letter.</summary>
    private static string? LetterOf(InputEventKey key)
    {
        // Unicode follows the keyboard layout and Shift or Caps Lock; Keycode is the fallback.
        var c = key.Unicode != 0 ? (char)key.Unicode : key.Keycode is >= Key.A and <= Key.Z ? (char)('a' + (key.Keycode - Key.A)) : '\0';
        c = char.ToLowerInvariant(c);
        return c is >= 'a' and <= 'z' ? c.ToString() : null;
    }

    private void Press(InputEventKey key)
    {
        // Shortcuts (Cmd, Ctrl, Option/Alt) aren't typing: Option-letter on a Mac makes other characters.
        if (InputLocked || _round is not { } round || key.CtrlPressed || key.MetaPressed || key.AltPressed) return;
        switch (key.Keycode)
        {
            case Key.Space:
                SayPrompt();
                return;
            case Key.Backspace:
                TakeBack();
                return;
            case Key.Enter or Key.KpEnter:
                return;
        }
        if (LetterOf(key) is string letter) TypeLetter(letter, round);
        // Numbers and punctuation are typing too, just not a letter of the word.
        else if (key.Unicode > ' ') Miss(round);
    }

    private void TypeLetter(string letter, WordRocketGame.Round round)
    {
        _keyboard.Press(letter);
        var index = _typed.Count;
        if (!WordRocketGame.IsRightKey(round, index, letter))
        {
            Miss(round);
            return;
        }
        _typed.Add(letter);
        _windows[index].Show(letter);
        _misses = 0;
        _keyboard.StopGlow();
        Sfx.Play(BipSounds.Effect.Tick);
        if (_typed.Count == round.Slots.Count)
        {
            Launch(round);
            return;
        }
        // Spelling a word, each sound is said as its last letter lands: c-a-t, sh-o-p.
        var slot = round.Slots[index];
        if (round.Mode is WordRocketGame.Mode.Cvc or WordRocketGame.Mode.Longer && slot.EndsSound)
            Voice.Play([slot.HintClip]);
        MarkNextWindow();
    }

    private void Miss(WordRocketGame.Round round)
    {
        var index = _typed.Count;
        _misses += 1;
        _missedAny = true;
        Sfx.Play(BipSounds.Effect.Boop);
        Bip.Tilt();
        if (index < _windows.Count) Buttons.Shake(_windows[index]);
        if (_misses != QuestionAttempt.MissesBeforeHint || index >= round.Slots.Count) return;
        // Two misses on this letter: Bip helps, and the right key glows on the keyboard.
        var slot = round.Slots[index];
        _keyboard.Glow(slot.Letter);
        // Only while that letter is still the one to type: a hint starting after the word is done
        // would cut off the launch sentence, whose end moves the game on.
        After(0.3, () =>
        {
            if (!InputLocked && _round == round && _typed.Count == index) Voice.Play([Coordinator.RandomHint(), slot.HintClip]);
        });
    }

    private void TakeBack()
    {
        if (_typed.Count == 0) return;
        _typed.RemoveAt(_typed.Count - 1);
        _windows[_typed.Count].Show(null);
        _misses = 0;
        _keyboard.StopGlow();
        _keyboard.Press(KeyboardPicture.Backspace);
        Sfx.Play(BipSounds.Effect.Tick);
        MarkNextWindow();
    }

    private void MarkNextWindow()
    {
        for (var i = 0; i < _windows.Count; i++) _windows[i].IsNext = i == _typed.Count;
    }

    /// <summary>Right: the craft launches with stars, and Bip says the word again.</summary>
    private void Launch(WordRocketGame.Round round)
    {
        InputLocked = true;
        MarkNextWindow();
        Sfx.Play(BipSounds.Effect.Chime);
        Bip.Celebrate();
        var change = Record(!_missedAny, _game.SkillId(round), round.SoundId);
        if (_craft is { } craft)
        {
            Buttons.Sparkle(craft.Position, Stage);
            FlyAway(craft);
        }
        Voice.Play([round.Clip, Coordinator.RandomPraise()], completion: () => AfterAnswer(change, _session, AskQuestion));
    }

    private void FlyAway(Node2D craft)
    {
        if (craft.FindChild("Exhaust", true, false) is Node2D exhaust) exhaust.Visible = true;
        var (to, turn) = _session.Skin.Id switch
        {
            "balloon" => (P(0, 1100), 0.0),
            "submarine" => (P(1700, 120), 0.0),
            _ => (P(1500, 900), 0.45),
        };
        Sfx.Play(BipSounds.Effect.Whirr);
        var fly = craft.CreateTween();
        fly.TweenInterval(0.35);
        fly.TweenProperty(craft, "rotation", Turn(turn), 0.3);
        fly.Parallel().TweenProperty(craft, "position", craft.Position + (to - craft.Position) * 0.08f, 0.3);
        fly.TweenProperty(craft, "position", to, 1.1).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        fly.TweenCallback(Callable.From(craft.QueueFree));
        // Stars twinkle where it took off.
        for (var i = 0; i < 6; i++)
        {
            var star = Pen(Polygon(Star(Vector2.Zero, 26)), (ulong)(1400 + i), fill: Palette.Sun, lineWidth: 4, wobble: 1);
            star.Position = CraftHome + P(-500 + i * 200, (i % 2 == 0 ? 160 : -150));
            star.Scale = Vector2.One * 0.01f;
            star.ZIndex = 30;
            Stage.AddChild(star);
            var twinkle = star.CreateTween();
            twinkle.TweenInterval(0.1 * i);
            twinkle.TweenProperty(star, "scale", Vector2.One * 1.2f, 0.15);
            twinkle.TweenProperty(star, "scale", Vector2.One, 0.1);
            twinkle.TweenInterval(0.6);
            twinkle.TweenProperty(star, "modulate:a", 0f, 0.4);
            twinkle.TweenCallback(Callable.From(star.QueueFree));
        }
    }

    /// <summary>Previews only: types the first letters, then two wrong keys so the hint shows.</summary>
    private void TypeForPreview()
    {
        if (_round is not { } round) return;
        var count = System.Math.Min(PreviewLetters, round.Slots.Count - 1);
        for (var i = 0; i < count; i++) TypeLetter(round.Slots[i].Letter, round);
        var wrong = PhonicsCourse.Alphabet.First(l => !NextKeys.Contains(l));
        TypeLetter(wrong, round);
        TypeLetter(wrong, round);
    }
}
