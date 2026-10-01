/// What the game should do after a child answers.
public enum AnswerOutcome: Equatable, Sendable {
    /// Right. `firstTry` is what counts towards mastery.
    case correct(firstTry: Bool)
    /// Wrong: soft sound, let them try again.
    case tryAgain
    /// Wrong again: give a spoken hint and show the answer gently.
    case hint
}

/// Retry-then-hint for a single question. No timers, no lives, no losing.
public struct QuestionAttempt: Equatable, Sendable {
    /// Misses on one question before a spoken hint is given.
    public static let missesBeforeHint = 2

    public private(set) var misses = 0
    public private(set) var isAnswered = false

    public init() {}

    public var needsHint: Bool { misses >= Self.missesBeforeHint }

    /// For mastery: a question counts as right only if it was right first time.
    public var countsAsCorrect: Bool { isAnswered && misses == 0 }

    public mutating func answer(correct: Bool) -> AnswerOutcome {
        if correct {
            isAnswered = true
            return .correct(firstTry: misses == 0)
        }
        misses += 1
        return needsHint ? .hint : .tryAgain
    }
}
