using System.Numerics;

namespace BipCore;

/// <summary>What the hand is doing with a piece (a food, a letter tile) right now.</summary>
public enum CarryMode
{
    /// <summary>Holding nothing.</summary>
    Idle,
    /// <summary>The button went down on a piece and is still down: a click or a drag, not yet known.</summary>
    Holding,
    /// <summary>The piece follows the pointer with the button up, after a click on it (a trackpad).</summary>
    Carrying,
}

/// <summary>What a press or a release asks the screen to do.</summary>
public enum CarryAction
{
    /// <summary>Nothing for the piece.</summary>
    None,
    /// <summary>A press on a piece: it is picked up and follows the pointer.</summary>
    PickUp,
    /// <summary>The click on a piece is over: it keeps following the pointer with the button up.</summary>
    StartCarrying,
    /// <summary>Put the piece down where the pointer is (the end of a drag, or the click that ends carrying).</summary>
    Drop,
}

/// <summary>What a pointer move asks the screen to do.</summary>
public enum CarryMove
{
    /// <summary>Nothing is held.</summary>
    None,
    /// <summary>Move the piece with the pointer.</summary>
    Follow,
    /// <summary>
    /// The button was let go where the game couldn't see it (outside the window, behind the parent
    /// gate) after a drag: the piece goes back.
    /// </summary>
    LostDrop,
}

/// <summary>
/// The hand for the games that move pieces, for a mouse and a trackpad alike (the same click rule as
/// Letter Trace's <see cref="TracePen"/>). Dragging with the button held works as it always did. A click on
/// a piece (press and release that never moves more than <see cref="TapSlop"/>) picks it up: it then
/// follows the pointer with the button up until the next click, which puts it down there. So a click
/// is never an answer by itself: a child who clicks the pictures to explore, or double-clicks, only
/// picks a piece up and puts it back. A drag that wanders off and comes back to its spot is a drag,
/// not a click. Where a piece is put down, and what that means, stays with the screen.
/// </summary>
public sealed class CarryHand
{
    /// <summary>How far the pointer may move between press and release for it still to be a click.</summary>
    public const float TapSlop = TracePen.TapSlop;

    private Vector2 _pressedAt;
    private bool _movedAway;

    public CarryMode Mode { get; private set; } = CarryMode.Idle;

    /// <summary>
    /// The button went down. <paramref name="onPiece"/> says whether it landed on a piece the screen
    /// lets the child move. A carried piece is put down by any press.
    /// </summary>
    public CarryAction Press(Vector2 at, bool onPiece)
    {
        if (Mode == CarryMode.Carrying)
        {
            Mode = CarryMode.Idle;
            return CarryAction.Drop;
        }
        if (Mode == CarryMode.Holding || !onPiece) return CarryAction.None;
        Mode = CarryMode.Holding;
        _pressedAt = at;
        _movedAway = false;
        return CarryAction.PickUp;
    }

    /// <summary>The pointer moved.</summary>
    public CarryMove Move(Vector2 at, bool buttonHeld)
    {
        switch (Mode)
        {
            case CarryMode.Holding when buttonHeld:
                if (Vector2.Distance(at, _pressedAt) > TapSlop) _movedAway = true;
                return CarryMove.Follow;
            case CarryMode.Holding:
                // The release went unseen. Still within a click's wobble: it was a click, so carry on;
                // after a drag, the piece goes back.
                if (!_movedAway && Vector2.Distance(at, _pressedAt) <= TapSlop)
                {
                    Mode = CarryMode.Carrying;
                    return CarryMove.Follow;
                }
                Mode = CarryMode.Idle;
                return CarryMove.LostDrop;
            case CarryMode.Carrying:
                return CarryMove.Follow;
            default:
                return CarryMove.None;
        }
    }

    /// <summary>Whether letting go at <paramref name="at"/> now would make the press a click.</summary>
    public bool IsClick(Vector2 at) =>
        Mode == CarryMode.Holding && !_movedAway && Vector2.Distance(at, _pressedAt) <= TapSlop;

    /// <summary>The button came up: a click starts carrying, a drag puts the piece down.</summary>
    public CarryAction Release(Vector2 at)
    {
        if (Mode != CarryMode.Holding) return CarryAction.None;
        if (IsClick(at))
        {
            Mode = CarryMode.Carrying;
            return CarryAction.StartCarrying;
        }
        Mode = CarryMode.Idle;
        return CarryAction.Drop;
    }

    /// <summary>Let go of whatever is held, without a drop (a new round, a lock, a screen that treats a click itself).</summary>
    public void Cancel() => Mode = CarryMode.Idle;
}
