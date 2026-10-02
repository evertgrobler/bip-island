namespace BipCore;

public enum GatePhase
{
    Closed,
    Question,
    Unlocked,
}

/// <summary>
/// The parent gate's rules, without any drawing (the Swift app's ParentGateModel):
/// hold Esc for 3 seconds → the parent passcode (if one is set) or an adult maths question →
/// the parent area. After <see cref="ParentPasscode.TriesBeforeMaths"/> wrong codes it asks maths
/// instead. Opened from the "Update ready" button, it installs the update once unlocked.
/// Times are seconds from any steady clock.
/// </summary>
public sealed class ParentGateFlow
{
    public const double HoldSeconds = 3;

    private readonly IRandomSource _rng;
    private readonly Func<ParentPasscode?> _passcode;
    private readonly HoldDetector _hold = new(HoldSeconds);
    private int _wrongPasscodeTries;
    private bool _installAfterUnlock;

    public GatePhase Phase { get; private set; } = GatePhase.Closed;
    public ParentChallenge Challenge { get; private set; }
    /// <summary>True while the gate asks for the passcode rather than maths.</summary>
    public bool UsingPasscode { get; private set; }
    public bool LastAnswerWasWrong { get; private set; }
    public bool HasPasscode => _passcode() != null;

    /// <summary>The gate opened (pause the game).</summary>
    public event Action? Opened;
    /// <summary>The gate closed (carry on playing).</summary>
    public event Action? Closed;
    /// <summary>Unlocked from the "Update ready" button: install the update now.</summary>
    public event Action? InstallUpdate;

    /// <param name="passcode">Reads the saved parent passcode (null when none is set).</param>
    public ParentGateFlow(IRandomSource rng, Func<ParentPasscode?> passcode)
    {
        _rng = rng;
        _passcode = passcode;
        Challenge = ParentChallenge.Random(rng);
    }

    // Holding Esc

    /// <summary>Esc is down. Key repeats don't restart the clock.</summary>
    public void EscapePressed(double time)
    {
        if (Phase == GatePhase.Closed) _hold.Press(time);
    }

    public void EscapeReleased() => _hold.Release();

    /// <summary>0…1 while Esc is held (for the little ring in the corner).</summary>
    public double HoldProgress(double time) => Phase == GatePhase.Closed ? _hold.Progress(time) : 0;

    /// <summary>Call every frame: opens the gate once Esc has been held long enough.</summary>
    public void Tick(double time)
    {
        if (Phase == GatePhase.Closed && _hold.IsComplete(time)) Open();
    }

    // Opening and answering

    public void Open()
    {
        if (Phase != GatePhase.Closed) return;
        _hold.Release();
        _installAfterUnlock = false;
        _wrongPasscodeTries = 0;
        Challenge = ParentChallenge.Random(_rng);
        LastAnswerWasWrong = false;
        UsingPasscode = HasPasscode;
        Phase = GatePhase.Question;
        Opened?.Invoke();
    }

    /// <summary>The "Update ready" button: the same passcode or maths, then the update installs.</summary>
    public void OpenForUpdate()
    {
        if (Phase != GatePhase.Closed) return;
        Open();
        _installAfterUnlock = true;
    }

    /// <summary>Checks the typed answer. Returns true when the gate unlocked.</summary>
    public bool Submit(string answer)
    {
        if (Phase != GatePhase.Question) return false;
        if (UsingPasscode && _passcode() is { } passcode)
        {
            if (passcode.Matches(answer)) return Unlock();
            LastAnswerWasWrong = true;
            _wrongPasscodeTries++;
            if (_wrongPasscodeTries >= ParentPasscode.TriesBeforeMaths) UseMathsInstead();
            return false;
        }
        if (Challenge.IsCorrect(answer)) return Unlock();
        LastAnswerWasWrong = true;
        Challenge = ParentChallenge.Random(_rng);
        return false;
    }

    /// <summary>"Forgot it?": answer a maths question instead of the passcode.</summary>
    public void UseMathsInstead()
    {
        UsingPasscode = false;
        Challenge = ParentChallenge.Random(_rng);
    }

    public void Close()
    {
        if (Phase == GatePhase.Closed) return;
        Phase = GatePhase.Closed;
        _installAfterUnlock = false;
        _hold.Release();
        Closed?.Invoke();
    }

    private bool Unlock()
    {
        Phase = GatePhase.Unlocked;
        LastAnswerWasWrong = false;
        if (_installAfterUnlock)
        {
            _installAfterUnlock = false;
            InstallUpdate?.Invoke();
        }
        return true;
    }
}
