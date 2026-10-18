namespace BipCore;

/// <summary>
/// Show Me (Language Island, "Wys my"): Bip says a word in the language the child is learning and the
/// child taps its picture (3, then 4, then 5 pictures). After two misses Bip says it in the child's own
/// language too.
/// </summary>
public sealed class ShowMeGame(ContentLibrary content) : IMiniGame<LanguageWordRound, LanguageWord>
{
    public const string GameId = "show_me";
    public const int ChoiceCount = 3;

    public string Id => GameId;
    public GameEntry Entry { get; } = content.EntryForGame(GameId);
    public IReadOnlyList<GameSkin> Skins { get; } = [new("picture_cards", "Picture cards")];

    public LanguageWordRound? MakeRound(Learner learner, GameSession session, IRandomSource rng) =>
        LanguageWords.MakeRound(content, learner, Math.Max(this.Level(learner).Choices ?? ChoiceCount, 2), session, rng);

    public bool IsCorrect(LanguageWord choice, LanguageWordRound round) => choice.Id == round.Target.Id;

    public string SkillId(LanguageWordRound round) => "lang_listen";
}
