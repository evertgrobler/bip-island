namespace BipCore;

public enum AnswerOutcomeKind { Correct, TryAgain, Hint }

/// <summary>What the game should do after a child answers.</summary>
public readonly record struct AnswerOutcome(AnswerOutcomeKind Kind, bool FirstTry)
{
    /// <summary>Right. <paramref name="firstTry"/> is what counts towards mastery.</summary>
    public static AnswerOutcome Correct(bool firstTry) => new(AnswerOutcomeKind.Correct, firstTry);
    /// <summary>Wrong: soft sound, let them try again.</summary>
    public static readonly AnswerOutcome TryAgain = new(AnswerOutcomeKind.TryAgain, false);
    /// <summary>Wrong again: give a spoken hint and show the answer gently.</summary>
    public static readonly AnswerOutcome Hint = new(AnswerOutcomeKind.Hint, false);
}

/// <summary>Retry-then-hint for a single question. No timers, no lives, no losing.</summary>
public sealed class QuestionAttempt
{
    /// <summary>Misses on one question before a spoken hint is given.</summary>
    public const int MissesBeforeHint = 2;

    public int Misses { get; private set; }
    public bool IsAnswered { get; private set; }

    public bool NeedsHint => Misses >= MissesBeforeHint;

    /// <summary>For mastery: a question counts as right only if it was right first time.</summary>
    public bool CountsAsCorrect => IsAnswered && Misses == 0;

    public AnswerOutcome Answer(bool correct)
    {
        if (correct)
        {
            IsAnswered = true;
            return AnswerOutcome.Correct(firstTry: Misses == 0);
        }
        Misses += 1;
        return NeedsHint ? AnswerOutcome.Hint : AnswerOutcome.TryAgain;
    }
}
