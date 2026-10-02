using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BipIsland.App;
using BipIsland.Game;
using BipIsland.Games;
using BipIsland.Screens;
using Godot;

namespace BipIsland.Dev;

/// <summary>
/// "--bip-walk &lt;file&gt;": clicks and keys its way round the game like a child would, and checks each
/// screen opens and the buttons do what they should. Writes a JSON list of steps and quits with 1 if
/// any step failed. Runs headless (no drawing needed): clicks go through the same hit-testing as real
/// clicks. Use with --bip-save-dir so it never touches a real save.
/// </summary>
public static class WalkTest
{
    private sealed record Step(string Name, bool Ok, string Detail);

    public static async Task<bool> Run(Node host, string reportPath)
    {
        SceneTree tree = host.GetTree();
        var game = GameCoordinator.Instance;
        List<Step> steps = new List<Step>();

        async Task Wait(double seconds) => await host.ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        BaseScreen? Screen() => tree.CurrentScene as BaseScreen;
        void Check(string name, bool ok, string detail = "") => steps.Add(new Step(name, ok, detail));

        async Task<bool> Tap(string name)
        {
            if (Screen()?.FindTappable(name) is not { } node || Hit.GlobalArea(node) is not { } area)
            {
                Check($"tap {name}", false, $"no '{name}' on {Screen()?.GetType().Name ?? "no screen"}");
                return false;
            }
            Screen()!.Tap(area.GetCenter());
            await Wait(0.8);
            return true;
        }

        async Task Expect<T>(string step) where T : BaseScreen
        {
            await Wait(0.2);
            Check(step, Screen() is T, $"showing {Screen()?.GetType().Name ?? "nothing"}");
        }

        game.EndBreakEarly(); // A fresh test save starts outside any break.
        game.Start();
        await Expect<MapScreen>("one child: the game opens on the map");

        if (await Tap("island:letters")) await Expect<LettersIslandScreen>("letters island opens");
        if (await Tap("trace"))
        {
            Check("a game not ported yet says coming soon and stays on the island", Screen() is LettersIslandScreen);
            await Wait(2.2);
            Check("the island unlocks again after coming soon", Screen() is { InputLocked: false });
        }
        if (await Tap("stone:s")) await Expect<LettersIslandScreen>("tapping a sound stone stays put until Meet the Sound is ported");
        await Wait(2.2);
        if (await Tap("home")) await Expect<MapScreen>("home goes back to the map");

        foreach (var island in new[] { "numbers", "words", "coding" })
        {
            if (await Tap("island:" + island)) await Expect<GameIslandScreen>($"{island} island opens");
            if (await Tap("bip")) Check($"tapping Bip on {island} island doesn't leave it", Screen() is GameIslandScreen);
            await Wait(2.2);
            if (await Tap("home")) await Expect<MapScreen>($"home from {island} island");
        }

        await PlayNumbers();

        if (await Tap("stickers")) await Expect<StickerScreen>("the sticker book opens");
        if (await Tap("page")) Check("the sticker page turns", Screen() is StickerScreen);
        if (await Tap("home")) await Expect<MapScreen>("home from the sticker book");

        var starsBefore = game.Progress.Stars;
        var mysteryShown = Screen()?.FindTappable("mystery") != null;
        Check("the mystery box is there once a day", mysteryShown == game.MysteryAvailable);
        if (mysteryShown && await Tap("mystery"))
        {
            await Wait(1.5);
            Check("the mystery box gives 5 stars, once", game.Progress.Stars == starsBefore + BipCore.ChildProgress.MysteryBonusStars && !game.MysteryAvailable,
                $"stars {starsBefore} → {game.Progress.Stars}");
            Check("the stars are saved", game.Store.Progress(game.ChildId).Stars == game.Progress.Stars);
        }

        // Keyboard: arrows move the glow over the islands, Enter opens one.
        if (Screen() is MapScreen map)
        {
            map.HandleKey(Key.Right);
            map.HandleKey(Key.Enter);
            await Wait(0.8);
            await Expect<BaseScreen>("arrow keys and Enter open an island");
            Check("the island Enter chose is open", Screen() is LettersIslandScreen or GameIslandScreen, $"showing {Screen()?.GetType().Name}");
            if (await Tap("home")) await Expect<MapScreen>("home after keyboard play");
        }

        // Two or more children: "Who's playing?" first, and choosing one opens the map as them.
        game.AddChildProfile("Lily", 5, "penguin");
        game.Start();
        await Expect<ProfilesScreen>("two children: the game opens on Who's playing?");
        var lily = game.Children.First(c => c.Name == "Lily");
        if (await Tap("child:" + lily.Id))
        {
            await Expect<MapScreen>("choosing a child opens the map");
            Check("the chosen child is playing", game.ChildId == lily.Id);
            Check("the map shows the child's badge", Screen()?.FindTappable("profiles") != null);
        }
        if (await Tap("profiles")) await Expect<ProfilesScreen>("the badge goes back to Who's playing?");

        // The break: games close while Bip charges.
        game.Store.Settings = game.Store.Settings with { PlayMinutes = 1, BreakMinutes = 1 };
        game.ForceBreakForTest();
        game.ShowMap();
        await Expect<ChargingScreen>("during a break the map shows Bip charging");
        game.EndBreakEarly();
        game.ShowMap();
        await Expect<MapScreen>("after the break the map opens again");

        // Phase 3 games: one round of each, through the real clicks.

        async Task<bool> WaitFor(Func<bool> ready, double seconds)
        {
            for (var waited = 0.0; waited < seconds; waited += 0.1)
            {
                if (ready()) return true;
                await Wait(0.1);
            }
            return ready();
        }

        async Task<bool> TapNumeral(bool right)
        {
            if (Screen() is not NumeralGameScreen screen || screen.RightIndex < 0) return false;
            var index = right ? screen.RightIndex : (screen.RightIndex + 1) % 3;
            return await Tap($"num:{index}");
        }

        async Task PlayNumbers()
        {
            if (await Tap("island:numbers")) await Expect<GameIslandScreen>("numbers island opens for a game");
            var starsBefore = game.Progress.Stars;
            if (await Tap("count")) await Expect<CountTapScreen>("Count & Tap opens from the island");
            Check("Count & Tap shows its level badge", Screen()?.FindChild("LevelBadge", true, false) != null);
            Check("Count & Tap shows animals to count", await WaitFor(() => Screen()?.FindTappable("beast:0") != null, 3));
            for (var i = 0; Screen()?.FindTappable($"beast:{i}") is { } beast && Hit.GlobalArea(beast) is { } area; i++)
            {
                Screen()!.Tap(area.GetCenter());
                await Wait(0.05);
            }
            Check("counting every animal brings up three numerals",
                  await WaitFor(() => Screen() is NumeralGameScreen { RightIndex: >= 0 }, 3));
            if (await TapNumeral(right: true))
            {
                Check("the right numeral earns a star", game.Progress.Stars == starsBefore + 1, $"stars {starsBefore} → {game.Progress.Stars}");
                Check("the answer is saved", game.Store.Progress(game.ChildId).Stars == game.Progress.Stars);
                Check("the game's level is tracked", game.Progress.GameLevels.ContainsKey(BipCore.CountTapGame.GameId));
            }
            if (await Tap("home")) await Expect<GameIslandScreen>("home in a game goes back to its island");

            // Quick Look: a wrong answer first (no star), then a whole visit to the end.
            starsBefore = game.Progress.Stars;
            if (await Tap("quick")) await Expect<QuickLookScreen>("Quick Look opens from the island");
            Check("Quick Look's dots flash, then the numerals show",
                  await WaitFor(() => Screen() is NumeralGameScreen { RightIndex: >= 0 }, 5));
            if (await TapNumeral(right: false))
                Check("a wrong numeral stays on the question", Screen() is QuickLookScreen { InputLocked: false, RightIndex: >= 0 });
            if (await TapNumeral(right: true))
                Check("right after a miss: no star, but the answer still counts", game.Progress.Stars == starsBefore
                      && game.Progress.Skill("subitise_5").TotalAttempts + game.Progress.Skill("subitise_10").TotalAttempts > 0);
            for (var round = 2; round <= 8; round++)
            {
                if (!await WaitFor(() => Screen() is NumeralGameScreen { RightIndex: >= 0, InputLocked: false }, 6)) break;
                await TapNumeral(right: true);
            }
            Check("eight questions make a visit: the stars are counted and Bip goes back to the island",
                  await WaitFor(() => Screen() is GameIslandScreen, 15), $"showing {Screen()?.GetType().Name}");
            Check("seven first-try answers, seven stars", game.Progress.Stars == starsBefore + 7, $"stars {starsBefore} → {game.Progress.Stars}");
            Check("both games are in the recent games", game.Progress.RecentGames.TakeLast(2).SequenceEqual(
                [BipCore.CountTapGame.GameId, BipCore.QuickLookGame.GameId]));
            await Wait(0.5);
            if (await Tap("home")) await Expect<MapScreen>("home from numbers island after the games");
        }

        var json = JsonSerializer.Serialize(steps.Select(s => new { step = s.Name, ok = s.Ok, detail = s.Detail }),
            new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(reportPath, json);
        foreach (var s in steps) GD.Print($"{(s.Ok ? "ok  " : "FAIL")} {s.Name} {s.Detail}");
        return steps.All(s => s.Ok);
    }
}
