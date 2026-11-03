namespace BipCore;

/// <summary>
/// Three number cards for a maths question: the answer and two near it, none below 1, in a random order.
/// With a step above 1 (a times table) the near ones are a step of the table away, or one (6, 8, 10
/// counting in 2s); otherwise one or two away. Bip's Balance, Picnic Plates and Number Machine all use it.
/// </summary>
public static class NearChoices
{
    public static List<int> Make(int answer, int step, IRandomSource rng)
    {
        var near = (step > 1 ? new[] { answer - step, answer + step, answer - 1, answer + 1 }
                             : [answer - 2, answer - 1, answer + 1, answer + 2])
            .Where(n => n >= 1 && n != answer).Distinct();
        return rng.Shuffled(rng.Shuffled(near).Take(2).Prepend(answer));
    }
}
