namespace BipCore;

/// <summary>
/// What Is It? (Language Island, "Wat is dit?"): a picture appears and the child picks its word in the
/// language they're learning from two or three spoken words (tap a word to hear it). Nothing is ever
/// recorded: the child listens and points, no microphone.
/// </summary>
public sealed class WhatIsItGame(ContentLibrary content) : IMiniGame<LanguageWordRound, LanguageWord>
{
    public const string GameId = "what_is_it";
    public const int ChoiceCount = 2;

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("speech_bubbles", "Speech bubbles")];

    public LanguageWordRound? MakeRound(Learner learner, GameSession session, IRandomSource rng) =>
        LanguageWords.MakeRound(content, learner, Math.Max(this.Level(learner).Choices ?? ChoiceCount, 2), session, rng);

    public bool IsCorrect(LanguageWord choice, LanguageWordRound round) => choice.Id == round.Target.Id;

    public string SkillId(LanguageWordRound round) => "lang_words";
}
