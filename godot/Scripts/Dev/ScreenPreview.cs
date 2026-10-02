using System;
using BipCore;
using BipIsland.App;
using BipIsland.Game;
using BipIsland.Screens;
using Godot;

namespace BipIsland.Dev;

/// <summary>
/// Opens one screen directly, for screenshots (scripts/check_all.sh opens every scene in
/// Scenes/Screens with --bip-scene). With a test save folder (--bip-save-dir) it adds sample
/// children first, so "Who's playing?" and the map's child badge have something to show; it never
/// touches a real save.
/// </summary>
public partial class ScreenPreview : Node
{
    [Export] public string Screen { get; set; } = "map";
    /// <summary>The grown-ups' layer on top: "gate", "progress", "children", "settings" or "update" (the button).</summary>
    [Export] public string Parent { get; set; } = "";

    public override void _Ready() => Callable.From(Open).CallDeferred();

    private void Open()
    {
        var game = GameCoordinator.Instance;
        if (Array.IndexOf(Boot.UserArgs, "--bip-save-dir") >= 0 && game.Children.Count < 3)
        {
            game.AddChildProfile("Lily", 5, "penguin");
            game.AddChildProfile("Sam", 7, "tortoise");
        }
        BaseScreen screen = Screen switch
        {
            "profiles" => new ProfilesScreen(),
            "letters" => new LettersIslandScreen(greet: false),
            "numbers" => new GameIslandScreen(Island.Numbers, greet: false),
            "words" => new GameIslandScreen(Island.Words, greet: false),
            "coding" => new GameIslandScreen(Island.Coding, greet: false),
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
