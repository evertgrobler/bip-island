using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BipIsland.App;
using BipCore;
using BipIsland.Game;
using BipIsland.Parent;
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
        var tree = host.GetTree();
        var game = GameCoordinator.Instance;
        var steps = new List<Step>();

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

        // The parent gate: hold Esc, answer, the parent area, then a fresh Esc goes back.
        var parent = game.Parent;
        void Escape(bool down) => Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape, Pressed = down });
        Escape(true);
        await Wait(1.5);
        Check("a short Esc press doesn't open the gate", !parent.IsOpen);
        await Wait(2.0);
        Check("holding Esc for 3 seconds opens the gate", parent.Flow.Phase == GatePhase.Question);
        Check("the game is paused behind the gate", tree.Paused);
        Escape(false);
        await Wait(0.2);
        parent.Submit("1");
        Check("a wrong answer keeps the gate shut", parent.Flow.Phase == GatePhase.Question && parent.Flow.LastAnswerWasWrong);
        parent.Submit(parent.Flow.Challenge.Answer.ToString());
        await Wait(0.2);
        Check("the right answer opens the parent area", parent.Area != null);
        if (parent.Area is { } area)
        {
            Check("a passcode needs two matching boxes", area.SetPasscode("1234", "1243") != null && game.Store.Passcode == null);
            Check("a parent can set a passcode", area.SetPasscode("1234", "1234") == null && game.Store.Passcode != null);
            area.Show(ParentArea.Tab.Children);
            var before = game.Children.Count;
            Check("a parent can add a child", area.AddChild("Mia", 6) && game.Children.Count == before + 1);
            if (game.Children.FirstOrDefault(c => c.Name == "Mia") is { } mia)
            {
                area.RemoveChild(mia.Id);
                Check("a parent can remove a child", game.Children.All(c => c.Id != mia.Id));
            }
            area.Show(ParentArea.Tab.Settings);
            area.Show(ParentArea.Tab.Progress);
            await Wait(0.2);
            Check("every parent tab opens", area.Showing == ParentArea.Tab.Progress);
        }
        Escape(true);
        await Wait(0.2);
        Escape(false);
        await Wait(1.0);
        Check("a fresh Esc press goes back to the game", !parent.IsOpen && !tree.Paused, $"phase {parent.Flow.Phase}, paused {tree.Paused}");
        await Expect<MapScreen>("the map is back after the parent area");

        parent.Flow.Open();
        Check("with a passcode set the gate asks for it", parent.Flow.UsingPasscode);
        parent.Submit("1234");
        Check("the passcode opens the parent area", parent.Area != null);
        parent.Close();

        parent.ShowUpdateButtonForTest();
        await Wait(0.2);
        if (parent.UpdateButton is { } update)
        {
            update.EmitSignal(BaseButton.SignalName.Pressed);
            Check("the update button asks the grown-up question first", parent.Flow.Phase == GatePhase.Question);
            parent.Submit("1234");
            await Wait(0.2);
            // No update was really downloaded, so installing fails and Settings says why.
            Check("after the gate the update installs (or Settings says why not)", parent.Area?.Showing == ParentArea.Tab.Settings);
            parent.Close();
        }
        else Check("the update button shows when an update is ready", false);
        game.Store.Passcode = null;
        await Wait(0.5);
        Check("the game carries on after the parent area", !tree.Paused && Screen() is MapScreen);

        // Games wait for Bip to finish speaking; the gate pauses the sentence rather than cutting it off.
        var spoke = false;
        game.Voice.Play([game.RandomPraise()], completion: () => spoke = true);
        parent.Flow.Open();
        await Wait(4);
        Check("Bip's sentence waits while the gate is open", !spoke && tree.Paused);
        parent.Close();
        await Wait(5);
        Check("...and finishes once the gate closes, so a game never gets stuck", spoke);

        var json = JsonSerializer.Serialize(steps.Select(s => new { step = s.Name, ok = s.Ok, detail = s.Detail }),
            new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(reportPath, json);
        foreach (var s in steps) GD.Print($"{(s.Ok ? "ok  " : "FAIL")} {s.Name} {s.Detail}");
        return steps.All(s => s.Ok);
    }
}
