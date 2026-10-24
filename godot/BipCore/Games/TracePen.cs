using System.Numerics;

namespace BipCore;

/// <summary>How the Letter Trace pen is moving right now.</summary>
public enum TracePenMode
{
    /// <summary>Not drawing: waiting for a drag, or a click on the start dot.</summary>
    Idle,
    /// <summary>Drawing while the button is held (a mouse).</summary>
    Dragging,
    /// <summary>Drawing with the button up, after a click on the start dot (a trackpad).</summary>
    Following,
}

/// <summary>What a click landed on, as the screen's own hit test found it.</summary>
public enum TraceSpot
{
    /// <summary>Nothing tappable: the paper, or the letter's empty space.</summary>
    Paper,
    /// <summary>The start dot.</summary>
    StartDot,
    /// <summary>Something that does its own thing (the letter says its sound, home, Bip): never a miss.</summary>
    Button,
}

/// <summary>What a click (press and release without moving much) did.</summary>
public enum TraceTapResult
{
    /// <summary>Not a tap, or a tap that needs nothing (a drag, the click that stopped the pen, a button).</summary>
    None,
    /// <summary>A tap on the start dot: the pen now follows the pointer.</summary>
    Started,
    /// <summary>The click stopped a pen that was following.</summary>
    Stopped,
    /// <summary>A tap on the paper away from the start dot: soft sound, try again.</summary>
    Missed,
    /// <summary>
    /// The second tap on the paper: show the dot and say a hint, once. Later misses are plain
    /// <see cref="Missed"/> (the dot keeps wiggling) until the pen starts or a drag traces.
    /// </summary>
    Hint,
}

/// <summary>
/// Letter Trace's pen, for a mouse and a trackpad alike. Dragging with the button held draws, as it
/// always did. A click on the start dot (press and release without moving much) starts tap mode:
/// the pen then follows the pointer with the button up, until the trace is finished or the child
/// clicks again. Clicks on the bare paper are misses: soft retry, then a hint after two. No timers.
/// The count starts again whenever the pen starts or a drag traces, so a mouse user who traces in
/// strokes never gets the hint for an old stray click.
/// A press draws nothing until it has moved past <see cref="TapSlop"/>, so a click never both
/// draws and counts as a miss. Where a click landed comes from the screen's hit test
/// (<see cref="TraceSpot"/>); how a finished trace is judged (every checkpoint touched) stays with
/// the screen.
/// </summary>
public sealed class TracePen
{
    /// <summary>The start dot's radius: 120 points across, the smallest click target allowed.</summary>
    public const float StartRadius = 60;

    /// <summary>How far the pointer may move between press and release for it still to be a click.</summary>
    public const float TapSlop = 24;

    private Vector2? _pressedAt;
    private QuestionAttempt _attempt = new();

    public TracePenMode Mode { get; private set; } = TracePenMode.Idle;
    /// <summary>True once the trace is finished: the pen does nothing more.</summary>
    public bool IsFinished { get; private set; }
    /// <summary>Clicks on the paper since the pen last started or a drag last traced.</summary>
    public int Misses => _attempt.Misses;

    /// <summary>
    /// Whether the screen shows the start dot: always, except while the pen follows the pointer and
    /// once the trace is finished. The screen checks it after every press, move and release, so any
    /// way the pen leaves following (a click, a drag that takes over) brings the dot back.
    /// </summary>
    public bool ShowsStartDot => !IsFinished && Mode != TracePenMode.Following;

    /// <summary>The button went down. A pen that was following stops here.</summary>
    public TraceTapResult Press(Vector2 at)
    {
        if (IsFinished) return TraceTapResult.None;
        if (Mode == TracePenMode.Following)
        {
            Mode = TracePenMode.Idle;
            _pressedAt = null;
            return TraceTapResult.Stopped;
        }
        _pressedAt = at;
        return TraceTapResult.None;
    }

    /// <summary>The pointer moved. Returns true when this point should be drawn and traced.</summary>
    public bool Move(Vector2 at, bool buttonHeld)
    {
        if (IsFinished) return false;
        if (buttonHeld)
        {
            // Still within a click's wobble: wait, it may be a click.
            if (_pressedAt is { } pressed)
            {
                if (Vector2.Distance(at, pressed) <= TapSlop) return false;
                _pressedAt = null;
            }
            // A drag (even one whose press went unseen, which also ends following). It traces, so
            // any earlier stray clicks are forgiven.
            Mode = TracePenMode.Dragging;
            _attempt = new QuestionAttempt();
            return true;
        }
        // The button is up: a drag whose release we didn't see (outside the window, behind the
        // parent gate) stops here; a following pen keeps drawing.
        _pressedAt = null;
        if (Mode == TracePenMode.Dragging) Mode = TracePenMode.Idle;
        return Mode == TracePenMode.Following;
    }

    /// <summary>
    /// The button came up. A click on the start dot starts the pen following; a click on the paper
    /// is a miss; a click on anything else (<see cref="TraceSpot.Button"/>) is left to that thing.
    /// <paramref name="spot"/> is where the press landed.
    /// </summary>
    public TraceTapResult Release(Vector2 at, TraceSpot spot)
    {
        if (IsFinished) return TraceTapResult.None;
        if (Mode == TracePenMode.Dragging) Mode = TracePenMode.Idle;
        if (_pressedAt is not { } pressed) return TraceTapResult.None;
        _pressedAt = null;
        if (Vector2.Distance(at, pressed) > TapSlop) return TraceTapResult.None;
        switch (spot)
        {
            case TraceSpot.StartDot:
                Mode = TracePenMode.Following;
                _attempt = new QuestionAttempt();
                return TraceTapResult.Started;
            case TraceSpot.Paper:
                _attempt.Answer(false);
                return _attempt.Misses == QuestionAttempt.MissesBeforeHint ? TraceTapResult.Hint : TraceTapResult.Missed;
            default:
                return TraceTapResult.None;
        }
    }

    /// <summary>The trace is done: stop drawing for good.</summary>
    public void Finish()
    {
        IsFinished = true;
        Mode = TracePenMode.Idle;
        _pressedAt = null;
    }
}
