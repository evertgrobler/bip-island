using System.Numerics;
using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>The hand for moving pieces: drag with the button held, or click to pick up and click to put down.</summary>
public sealed class CarryHandTests
{
    private static readonly Vector2 Food = new(-300, 0);
    private static readonly Vector2 Mouth = new(400, 60);

    [Fact]
    public void ItUsesLetterTracesClickRule() => Assert.Equal(TracePen.TapSlop, CarryHand.TapSlop);

    [Fact]
    public void ADragWorksAsBefore()
    {
        var hand = new CarryHand();
        Assert.Equal(CarryAction.PickUp, hand.Press(Food, onPiece: true));
        Assert.Equal(CarryMove.Follow, hand.Move(Food + new Vector2(100, 0), buttonHeld: true));
        Assert.Equal(CarryMove.Follow, hand.Move(Mouth, buttonHeld: true));
        Assert.Equal(CarryAction.Drop, hand.Release(Mouth));
        Assert.Equal(CarryMode.Idle, hand.Mode);
    }

    [Fact]
    public void AClickPicksUpAndTheNextClickPutsDown()
    {
        var hand = new CarryHand();
        Assert.Equal(CarryAction.PickUp, hand.Press(Food, onPiece: true));
        Assert.Equal(CarryAction.StartCarrying, hand.Release(Food + new Vector2(5, 5)));
        Assert.Equal(CarryMode.Carrying, hand.Mode);
        // The piece follows with the button up.
        Assert.Equal(CarryMove.Follow, hand.Move(Mouth, buttonHeld: false));
        Assert.Equal(CarryAction.Drop, hand.Press(Mouth, onPiece: false));
        Assert.Equal(CarryMode.Idle, hand.Mode);
        // The release after that press belongs to nothing.
        Assert.Equal(CarryAction.None, hand.Release(Mouth));
    }

    [Fact]
    public void AClickIsNeverAnAnswerByItself()
    {
        // Exploring, or a double-click: pick up, then put straight back down where it was.
        var hand = new CarryHand();
        hand.Press(Food, onPiece: true);
        Assert.Equal(CarryAction.StartCarrying, hand.Release(Food));
        Assert.Equal(CarryAction.Drop, hand.Press(Food, onPiece: true));
        Assert.Equal(CarryAction.None, hand.Release(Food));
        Assert.Equal(CarryMode.Idle, hand.Mode);
    }

    [Fact]
    public void ADragThatComesBackToItsSpotIsNotAClick()
    {
        var hand = new CarryHand();
        hand.Press(Food, onPiece: true);
        hand.Move(Food + new Vector2(200, 0), buttonHeld: true);
        hand.Move(Food + new Vector2(3, 0), buttonHeld: true);
        Assert.False(hand.IsClick(Food));
        Assert.Equal(CarryAction.Drop, hand.Release(Food));
        Assert.Equal(CarryMode.Idle, hand.Mode);
    }

    [Fact]
    public void AWobbleWithinTheSlopIsStillAClick()
    {
        var hand = new CarryHand();
        hand.Press(Food, onPiece: true);
        hand.Move(Food + new Vector2(CarryHand.TapSlop, 0), buttonHeld: true);
        Assert.True(hand.IsClick(Food + new Vector2(CarryHand.TapSlop, 0)));
        Assert.False(hand.IsClick(Food + new Vector2(CarryHand.TapSlop + 1, 0)));
    }

    [Fact]
    public void APressOffAPieceDoesNothing()
    {
        var hand = new CarryHand();
        Assert.Equal(CarryAction.None, hand.Press(Mouth, onPiece: false));
        Assert.Equal(CarryMove.None, hand.Move(Food, buttonHeld: true));
        Assert.Equal(CarryAction.None, hand.Release(Food));
    }

    [Fact]
    public void AnUnseenReleaseAfterADragSendsThePieceBack()
    {
        var hand = new CarryHand();
        hand.Press(Food, onPiece: true);
        hand.Move(Food + new Vector2(200, 0), buttonHeld: true);
        Assert.Equal(CarryMove.LostDrop, hand.Move(Food + new Vector2(300, 0), buttonHeld: false));
        Assert.Equal(CarryMode.Idle, hand.Mode);
    }

    [Fact]
    public void AnUnseenReleaseOfAClickCarriesOn()
    {
        var hand = new CarryHand();
        hand.Press(Food, onPiece: true);
        Assert.Equal(CarryMove.Follow, hand.Move(Food + new Vector2(4, 0), buttonHeld: false));
        Assert.Equal(CarryMode.Carrying, hand.Mode);
    }

    [Fact]
    public void CancelLetsGo()
    {
        var hand = new CarryHand();
        hand.Press(Food, onPiece: true);
        hand.Release(Food);
        hand.Cancel();
        Assert.Equal(CarryMode.Idle, hand.Mode);
        Assert.Equal(CarryAction.None, hand.Press(Mouth, onPiece: false));
    }
}
