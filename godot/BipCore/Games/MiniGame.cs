namespace BipCore;

/// <summary>One question.</summary>
public interface IGameRound<TChoice>
{
    /// <summary>Everything the child can pick, in screen order.</summary>
    IReadOnlyList<TChoice> Choices { get; }
    /// <summary>Content items this round uses up (words), so they aren't asked again in the same session.</summary>
    IReadOnlyList<string> UsedItems { get; }
}

/// <summary>What every mini-game says about itself, whatever its rounds look like.</summary>
public interface IMiniGame
{
    /// <summary>Matches the game's id in games.json.</summary>
    string Id { get; }
    /// <summary>The game's registry entry: island, ages, skills, build phase.</summary>
    GameEntry Entry { get; }
    /// <summary>Same rules, different setting. The first skin is the one drawn when no other is ready.</summary>
    IReadOnlyList<GameSkin> Skins { get; }
    /// <summary>Rounds in one visit, if the game wants something other than games.json's roundsPerSession.</summary>
    int? RoundsPerSession => null;
}

/// <summary>
/// The plug-in template every mini-game follows (docs/GAMES.md, "Making it robust"):
/// it says which skills and ages it teaches (from Content/curriculum/games.json), builds a round at
/// random from the content lists at the child's level, checks an answer, and lists its skins.
/// A new game is one new file with one type that implements this.
/// </summary>
public interface IMiniGame<TRound, TChoice> : IMiniGame where TRound : class, IGameRound<TChoice>
{
    /// <summary>
    /// A new random round for this child, or null when there is nothing fresh left to ask.
    /// Must never reuse an item in the session's used items.
    /// </summary>
    TRound? MakeRound(Learner learner, GameSession session, IRandomSource rng);

    /// <summary>
    /// Whether a choice is right. Written from the rules, not from how the round was built, so the
    /// tests can check that every round has exactly one right answer.
    /// </summary>
    bool IsCorrect(TChoice choice, TRound round);

    /// <summary>The skill in skills.json that a round practises.</summary>
    string SkillId(TRound round);
}

public static class MiniGameExtensions
{
    /// <summary>The difficulty step this child plays at.</summary>
    public static GameLevel Level(this IMiniGame game, Learner learner) => game.Entry.Level(learner.GameLevel);

    /// <summary>The right choices in a round. A fair round has exactly one.</summary>
    public static List<TChoice> CorrectChoices<TRound, TChoice>(this IMiniGame<TRound, TChoice> game, TRound round)
        where TRound : class, IGameRound<TChoice> =>
        round.Choices.Where(choice => game.IsCorrect(choice, round)).ToList();
}

/// <summary>A look for a game: Bubble Pop can be bubbles, balloons or fireflies.</summary>
public sealed record GameSkin(string Id, string Name);

/// <summary>What the round generators need to know about the child.</summary>
/// <param name="UnlockedPhonicsGroup">The highest phonics group unlocked. Words are only offered once their group is unlocked.</param>
/// <param name="KnownSoundIds">Sounds the child has met (or that sit in a group they already know).</param>
/// <param name="FocusSoundId">The sound Bip wants to practise, if any.</param>
/// <param name="GameLevel">The child's level in the game being played (an index into the game's levels).</param>
public sealed record Learner(Band Band, int UnlockedPhonicsGroup, IReadOnlySet<string> KnownSoundIds,
                             string? FocusSoundId = null, int GameLevel = 0)
{
    public Learner Focusing(string? soundId) => this with { FocusSoundId = soundId };

    public Learner AtLevel(int level) => this with { GameLevel = level };
}

/// <summary>One visit to a game: a skin and a run of rounds, with nothing asked twice.</summary>
public sealed class GameSession
{
    public string GameId { get; }
    public GameSkin Skin { get; }
    public int MaxRounds { get; }
    public int RoundsPlayed { get; private set; }
    private readonly HashSet<string> _usedItems = [];
    public IReadOnlySet<string> UsedItems => _usedItems;

    public GameSession(string gameId, GameSkin skin, int maxRounds)
    {
        GameId = gameId;
        Skin = skin;
        MaxRounds = Math.Max(1, maxRounds);
    }

    /// <summary>Starts a session with a random skin from those the app can draw (the first skin if none match).</summary>
    public static GameSession Start(IMiniGame game, SessionSettings settings, IReadOnlySet<string> drawableSkins, IRandomSource rng)
    {
        var ready = game.Skins.Where(s => drawableSkins.Contains(s.Id)).ToList();
        var skin = rng.Pick(ready) ?? game.Skins.FirstOrDefault() ?? new GameSkin("default", "Default");
        return new GameSession(game.Id, skin, game.RoundsPerSession ?? settings.RoundsPerSession);
    }

    public bool IsFinished => RoundsPlayed >= MaxRounds;

    /// <summary>The next round, or null when the session is over (all rounds played, or nothing fresh left).</summary>
    public TRound? NextRound<TRound, TChoice>(IMiniGame<TRound, TChoice> game, Learner learner, IRandomSource rng)
        where TRound : class, IGameRound<TChoice>
    {
        if (IsFinished || game.MakeRound(learner, this, rng) is not TRound round) return null;
        RoundsPlayed += 1;
        _usedItems.UnionWith(round.UsedItems);
        return round;
    }
}

/// <summary>Three numeral answers around the right one, e.g. 4 → [3, 4, 5]. Shared by Count &amp; Tap and Quick Look.</summary>
internal static class NumeralChoices
{
    public static List<int> Three(int count, IRandomSource rng)
    {
        var near = new[] { count - 2, count - 1, count + 1, count + 2 }.Where(n => n >= 0);
        var picks = rng.Shuffled(near).Take(2);
        return rng.Shuffled(picks.Prepend(count));
    }
}
