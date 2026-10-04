using BipCore;
using Xunit;

namespace BipCore.Tests;

/// <summary>Hold Esc → passcode or maths → parent area; the "Update ready" route installs after unlocking.</summary>
public sealed class ParentGateFlowTests
{
    private ParentPasscode? _passcode;
    private ParentGateFlow Gate() => new(new SeededGenerator(3), () => _passcode);

    [Fact]
    public void HoldingEscForThreeSecondsOpensTheGate()
    {
        var gate = Gate();
        var opened = 0;
        gate.Opened += () => opened++;
        gate.EscapePressed(10);
        gate.EscapePressed(11); // Key repeat doesn't restart the clock.
        gate.Tick(12.9);
        Assert.Equal(GatePhase.Closed, gate.Phase);
        Assert.InRange(gate.HoldProgress(11.5), 0.49, 0.51);
        gate.Tick(13);
        Assert.Equal(GatePhase.Question, gate.Phase);
        Assert.Equal(1, opened);
        Assert.Equal(0, gate.HoldProgress(13));
    }

    [Fact]
    public void LettingGoEarlyStartsOver()
    {
        var gate = Gate();
        gate.EscapePressed(0);
        gate.Tick(2.5);
        gate.EscapeReleased();
        gate.EscapePressed(3);
        gate.Tick(5.9);
        Assert.Equal(GatePhase.Closed, gate.Phase);
        gate.Tick(6);
        Assert.Equal(GatePhase.Question, gate.Phase);
    }

    [Fact]
    public void TheRightMathsAnswerUnlocksAndAWrongOneGivesANewSum()
    {
        var gate = Gate();
        gate.Open();
        Assert.False(gate.UsingPasscode);
        var first = gate.Challenge;
        Assert.False(gate.Submit((first.Answer + 1).ToString()));
        Assert.True(gate.LastAnswerWasWrong);
        Assert.Equal(GatePhase.Question, gate.Phase);
        Assert.True(gate.Submit(gate.Challenge.Answer.ToString()));
        Assert.Equal(GatePhase.Unlocked, gate.Phase);
        Assert.False(gate.LastAnswerWasWrong);
    }

    [Fact]
    public void WithAPasscodeItAsksForThatAndFallsBackToMathsAfterThreeMisses()
    {
        _passcode = ParentPasscode.Create("2468");
        var gate = Gate();
        gate.Open();
        Assert.True(gate.UsingPasscode);
        Assert.False(gate.Submit("1111"));
        Assert.False(gate.Submit("2222"));
        Assert.True(gate.UsingPasscode);
        Assert.False(gate.Submit("3333"));
        Assert.False(gate.UsingPasscode); // Now a maths question: a child can't keep guessing.
        Assert.False(gate.Submit("2468"));
        Assert.True(gate.Submit(gate.Challenge.Answer.ToString()));
    }

    [Fact]
    public void TheRightPasscodeUnlocks()
    {
        _passcode = ParentPasscode.Create("2468");
        var gate = Gate();
        gate.Open();
        Assert.True(gate.Submit(" 2468 "));
        Assert.Equal(GatePhase.Unlocked, gate.Phase);
    }

    [Fact]
    public void TheUpdateOnlyInstallsAfterUnlockingFromTheUpdateButton()
    {
        var gate = Gate();
        var installs = 0;
        gate.InstallUpdate += () => installs++;

        gate.Open(); // Opened by holding Esc: unlocking doesn't install.
        gate.Submit(gate.Challenge.Answer.ToString());
        Assert.Equal(0, installs);
        gate.Close();

        gate.OpenForUpdate();
        Assert.False(gate.Submit("0"));
        Assert.Equal(0, installs);
        gate.Submit(gate.Challenge.Answer.ToString());
        Assert.Equal(1, installs);

        gate.Close();
        gate.OpenForUpdate();
        gate.Close(); // Backing out cancels the install.
        gate.Open();
        gate.Submit(gate.Challenge.Answer.ToString());
        Assert.Equal(1, installs);
    }

    [Fact]
    public void AnotherChildOnlyPlaysAfterUnlockingFromWhosPlaying()
    {
        var gate = Gate();
        var lily = Guid.NewGuid();
        var switched = new List<Guid>();
        gate.SwitchChild += switched.Add;

        gate.OpenToSwitchChild(lily);
        Assert.Equal(GatePhase.Question, gate.Phase);
        Assert.Equal(lily, gate.SwitchingTo);
        Assert.False(gate.Submit("0"));
        Assert.Empty(switched);
        gate.Submit(gate.Challenge.Answer.ToString());
        Assert.Equal([lily], switched);
        Assert.Null(gate.SwitchingTo);

        gate.Close();
        gate.OpenToSwitchChild(lily);
        gate.Close(); // Backing out: nobody switches, and holding Esc later just opens the parent area.
        Assert.Null(gate.SwitchingTo);
        gate.Open();
        gate.Submit(gate.Challenge.Answer.ToString());
        Assert.Single(switched);
    }

    [Fact]
    public void ClosingGoesBackToTheGameAndAnswersAreIgnoredWhileClosed()
    {
        var gate = Gate();
        var closed = 0;
        gate.Closed += () => closed++;
        Assert.False(gate.Submit(gate.Challenge.Answer.ToString()));
        Assert.Equal(GatePhase.Closed, gate.Phase);
        gate.Open();
        gate.Close();
        gate.Close();
        Assert.Equal(1, closed);
        Assert.Equal(GatePhase.Closed, gate.Phase);
    }
}
