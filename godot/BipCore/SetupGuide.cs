namespace BipCore;

/// <summary>
/// The first-time setup guide for grown-ups: the steps in order, moving between them, and when a
/// step lets you go on. Shown once (<see cref="SaveStore.SetupDone"/>), and again from Settings.
/// Plain logic, so the order and the rules are tested without Godot; the cards are drawn in
/// godot/Scripts/Parent/ParentLayer.Setup.cs.
/// </summary>
/// <param name="gatedAlready">
/// The grown-up already passed the gate to open the guide (from Settings). On a first run nobody has,
/// so the practise step can't be skipped: the passcode, children and play-time steps are parent
/// controls, and only someone who has opened the gate reaches them.
/// </param>
public sealed class SetupGuide(bool gatedAlready = false)
{
    public enum Step
    {
        /// <summary>What Bip Island is and what setting up takes.</summary>
        Welcome,
        /// <summary>The child lock, and practising the way into the grown-up area (hold Esc, answer).</summary>
        GrownUpArea,
        /// <summary>An optional parent passcode instead of the maths question.</summary>
        Passcode,
        /// <summary>The children: names, ages and pictures.</summary>
        Children,
        /// <summary>Play length, break length and the daily maximum.</summary>
        PlayTime,
        /// <summary>Three cards about what the child sees: the map, stars and stickers, Bip charging.</summary>
        Tour,
        /// <summary>The reminder, then the game is handed to the child.</summary>
        AllSet,
    }

    public static readonly IReadOnlyList<Step> Steps = Enum.GetValues<Step>();

    /// <summary>The tour's cards (see <see cref="Step.Tour"/>).</summary>
    public const int TourCards = 3;

    public Step Current { get; private set; } = Step.Welcome;
    /// <summary>Which tour card is showing while on <see cref="Step.Tour"/>.</summary>
    public int TourCard { get; private set; }
    /// <summary>The grown-up has opened the gate once during this guide (the practise step's goal).</summary>
    public bool PractisedGate { get; private set; }
    /// <summary>The guide is over (finished or skipped).</summary>
    public bool Done { get; private set; }
    /// <summary>Opened from Settings, behind the gate (see the constructor).</summary>
    public bool GatedAlready { get; } = gatedAlready;

    public int Number => (int)Current + 1;
    public static int Count => Steps.Count;

    /// <summary>
    /// Whether Next is offered. The grown-up area step waits until the gate has been opened once,
    /// because that's the one thing a parent must know (and, on a first run, what keeps the parent
    /// controls after it behind the gate). From Settings it can be skipped on purpose.
    /// </summary>
    public bool CanGoOn => Current != Step.GrownUpArea || PractisedGate;

    /// <summary>The gate opened while practising.</summary>
    public void GateOpened()
    {
        if (Current == Step.GrownUpArea) PractisedGate = true;
    }

    /// <summary>Next card or step. Returns false when nothing changed (waiting, or already done).</summary>
    public bool Next()
    {
        if (Done || !CanGoOn) return false;
        if (Current == Step.Tour && TourCard < TourCards - 1)
        {
            TourCard++;
            return true;
        }
        if (Current == Step.AllSet)
        {
            Done = true;
            return true;
        }
        Current++;
        TourCard = 0;
        return true;
    }

    /// <summary>The previous card or step (nothing before Welcome).</summary>
    public bool Back()
    {
        if (Done) return false;
        if (Current == Step.Tour && TourCard > 0)
        {
            TourCard--;
            return true;
        }
        if (Current == Step.Welcome) return false;
        Current--;
        TourCard = Current == Step.Tour ? TourCards - 1 : 0;
        return true;
    }

    /// <summary>Whether the current step offers "Skip": the passcode and the tour, and the practise step from Settings.</summary>
    public bool CanSkipStep => !Done && (Current is Step.Tour or Step.Passcode || (Current == Step.GrownUpArea && GatedAlready));

    /// <summary>Skips the practice (from Settings), the passcode or the tour, and goes to the next step.</summary>
    public bool SkipStep()
    {
        if (!CanSkipStep) return false;
        Current++;
        TourCard = 0;
        return true;
    }
}
