using System;
using System.Linq;
using BipCore;
using BipIsland.App;
using BipIsland.Game;
using BipIsland.Screens;
using Godot;

namespace BipIsland.Dev;

/// <summary>
/// Opens one screen directly, for screenshots (scripts/check_all.sh opens every scene in
/// Scenes/Screens and Scenes/Games with --bip-scene). With a test save folder (--bip-save-dir) it adds sample
/// children first, so "Who's playing?" and the map's child badge have something to show; it never
/// touches a real save.
/// </summary>
public partial class ScreenPreview : Node
{
    [Export] public string Screen { get; set; } = "map";
    /// <summary>The grown-ups' layer on top: "gate", "progress", "children", "settings" or "update" (the button).</summary>
    [Export] public string Parent { get; set; } = "";

    public override void _Ready() => Callable.From(Open).CallDeferred();

    /// <summary>
    /// For previews and the walk-through only: the playing child masters the skills of groups 1 to
    /// <paramref name="groups"/> (right answers on two days) and meets their sounds, which unlocks
    /// the next group. Only ever used with a test save folder.
    /// </summary>
    public static void LearnGroups(GameCoordinator game, int groups)
    {
        if (game.Content is not { } content || game.Course is not { } course) return;
        foreach (var group in course.Groups.Where(g => g.Number <= groups + 1))
        {
            foreach (var sound in group.Sounds) game.Progress.MarkMet(sound.Id);
            if (group.Number > groups) continue;
            foreach (var day in new[] { game.Today - 1, game.Today })
                for (var i = 0; i < content.MasteryRules.MasteredWindow; i++)
                    game.Progress.RecordAnswer(true, group.SkillId, null, day, content.MasteryRules);
        }
        game.Store.Save(game.Progress, game.ChildId);
    }

    private void Open()
    {
        var game = GameCoordinator.Instance;
        if (Array.IndexOf(Boot.UserArgs, "--bip-save-dir") >= 0 && game.Children.Count < 3)
        {
            game.AddChildProfile("Lily", 5, "penguin");
            game.AddChildProfile("Sam", 7, "tortoise");
        }
        // Letters games practise sounds the child has met: the sample child has learnt groups 1-3
        // (Feed the Monster's foods start at group 4).
        if (Screen is "sound_hunt" or "bubble_pop" or "feed_the_monster") LearnGroups(game, 3);
        // A game opens the way an island opens it, with its first question straight away.
        BipIsland.Games.GameScreen.StartDelay = 0;
        if (game.StartGame(Screen)) return;
        BaseScreen screen = Screen switch
        {
            "profiles" => new ProfilesScreen(),
            "letters" => new LettersIslandScreen(greet: false),
            "numbers" => new GameIslandScreen(Island.Numbers, greet: false),
            "words" => new GameIslandScreen(Island.Words, greet: false),
            "coding" => new GameIslandScreen(Island.Coding, greet: false),
            "art" => new ArtIslandScreen(greet: false),
            "charging" => new ChargingScreen(),
            "stickers" => new StickerScreen(),
            _ => new MapScreen(greet: false),
        };
        game.Present(screen);
        ShowParent(game);
    }

    private void ShowParent(GameCoordinator game)
    {
        var layer = game.Parent;
        switch (Parent)
        {
            case "update":
                layer.ShowUpdateButtonForTest();
                return;
            case "":
                return;
        }
        layer.Flow.Open();
        if (Parent == "gate") return;
        layer.Submit(layer.Flow.Challenge.Answer.ToString());
        layer.Area?.Show(Parent switch
        {
            "children" => BipIsland.Parent.ParentArea.Tab.Children,
            "settings" => BipIsland.Parent.ParentArea.Tab.Settings,
            _ => BipIsland.Parent.ParentArea.Tab.Progress,
        });
    }
}
