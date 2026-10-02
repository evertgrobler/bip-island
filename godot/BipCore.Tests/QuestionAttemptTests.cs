using BipCore;
using Xunit;

namespace BipCore.Tests;

public sealed class QuestionAttemptTests
{
    [Fact]
    public void RightFirstTime()
    {
        var q = new QuestionAttempt();
        Assert.Equal(AnswerOutcome.Correct(firstTry: true), q.Answer(true));
        Assert.True(q.CountsAsCorrect);
    }

    [Fact]
    public void FirstMissMeansTryAgain()
    {
        var q = new QuestionAttempt();
        Assert.Equal(AnswerOutcome.TryAgain, q.Answer(false));
        Assert.False(q.NeedsHint);
    }

    [Fact]
    public void SecondMissGivesAHint()
    {
        var q = new QuestionAttempt();
        q.Answer(false);
        Assert.Equal(AnswerOutcome.Hint, q.Answer(false));
        Assert.True(q.NeedsHint);
        Assert.Equal(AnswerOutcome.Hint, q.Answer(false)); // keeps hinting, never fails
    }

    [Fact]
    public void RightAfterAMissDoesNotCountForMastery()
    {
        var q = new QuestionAttempt();
        q.Answer(false);
        Assert.Equal(AnswerOutcome.Correct(firstTry: false), q.Answer(true));
        Assert.True(q.IsAnswered);
        Assert.False(q.CountsAsCorrect);
    }
}
