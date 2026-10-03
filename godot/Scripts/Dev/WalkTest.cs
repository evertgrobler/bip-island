using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BipIsland.App;
using BipCore;
using BipIsland.Game;
using BipIsland.Games;
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
        await PlayLetters();
        if (await Tap("home")) await Expect<MapScreen>("home goes back to the map");

        foreach (var island in new[] { "numbers", "words", "coding" })
        {
            if (await Tap("island:" + island)) await Expect<GameIslandScreen>($"{island} island opens");
            // Tapping Bip starts the island's first game.
            if (await Tap("bip")) await Expect<GameScreen>($"tapping Bip on {island} island starts its first game");
            if (await Tap("home")) await Expect<GameIslandScreen>($"home from that game goes back to {island} island");
            await Wait(2.2);
            if (await Tap("home")) await Expect<MapScreen>($"home from {island} island");
        }

        await PlayNumbers();
        await PlayWords();
        await PlayCoding();

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
        var playedBefore = game.Progress.RecentGames.Count;
        game.StartGame(BipCore.QuickLookGame.GameId);
        await Expect<ChargingScreen>("during a break a game shows Bip charging instead");
        Check("...and doesn't count as played", game.Progress.RecentGames.Count == playedBefore && game.CurrentGameId == null);
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

        async Task PlayWords()
        {
            if (await Tap("island:words")) await Expect<GameIslandScreen>("words island opens for a game");
            var starsBefore = game.Progress.Stars;
            if (await Tap("buttons")) await Expect<SoundButtonsScreen>("Sound Buttons opens from the island");
            Check("Sound Buttons shows letter buttons and three pictures",
                  await WaitFor(() => Screen()?.FindTappable("tile:0") != null && Screen() is SoundButtonsScreen { RightIndex: >= 0 }, 3));
            if (await Tap("tile:0")) Check("a letter button says its sound and stays on the question", Screen() is SoundButtonsScreen { InputLocked: false });
            if (Screen() is SoundButtonsScreen buttons && await Tap($"card:{(buttons.RightIndex + 1) % 3}"))
                Check("a wrong picture stays on the question, no star", Screen() is SoundButtonsScreen { InputLocked: false } && game.Progress.Stars == starsBefore);
            if (Screen() is SoundButtonsScreen again && await Tap($"card:{again.RightIndex}"))
                Check("the right picture after a miss: no star, but the answer is saved",
                      game.Progress.Stars == starsBefore && game.Store.Progress(game.ChildId).Skills.Count > 0);
            Check("the next question comes", await WaitFor(() => Screen() is SoundButtonsScreen { InputLocked: false, RightIndex: >= 0 }, 8));
            if (await Tap("home")) await Expect<GameIslandScreen>("home from Sound Buttons goes back to words island");

            if (await Tap("builder")) await Expect<WordBuilderScreen>("Word Builder opens from the island");
            Check("Word Builder shows a picture, spaces and tiles",
                  await WaitFor(() => Screen() is WordBuilderScreen b && b.AnswerTileNames().Count > 0, 3));
            if (Screen() is WordBuilderScreen builder)
            {
                var answer = builder.AnswerTileNames();
                var spare = builder.SpareTileName();
                // A wrong word first: a spare tile in the last space, or the word backwards.
                var wrong = spare != null ? [.. answer.Take(answer.Count - 1), spare] : Enumerable.Reverse(answer).ToList();
                foreach (var name in wrong) await Tap(name);
                var wrongSpelling = builder.Spelling.ToList();
                var reallyWrong = wrongSpelling.All(s => s.Length > 0) && !wrongSpelling.SequenceEqual(answer.Select(n => builder.FindTappable(n) is LetterTile t ? t.Tile.Id : ""));
                if (reallyWrong && await Tap("next"))
                    Check("a wrong word stays on the question, no star", Screen() is WordBuilderScreen { InputLocked: false } && game.Progress.Stars == starsBefore);
                // Tapping a placed tile sends it back to the bank.
                foreach (var name in wrong) await Tap(name);
                Check("tapping placed tiles sends them back", builder.Spelling.All(s => s.Length == 0), string.Join(",", builder.Spelling));
                // A release the game never saw (outside the window, or behind the gate): the tile drops.
                builder.LostReleaseForTest(answer[0]);
                await Wait(0.4);
                Check("a drag whose release was missed drops the tile instead of following the pointer",
                      !builder.Dragging && builder.Spelling.All(s => s.Length == 0));
                // Drag the first tile into its space; tap the rest in.
                builder.DragForTest(answer[0], 0);
                await Wait(0.4);
                Check("dragging a tile drops it in the space", builder.Spelling[0].Length > 0, string.Join(",", builder.Spelling));
                foreach (var name in answer.Skip(1)) await Tap(name);
                var starsNow = game.Progress.Stars;
                if (await Tap("next"))
                    Check("the right word is sounded out and saved", Screen() is WordBuilderScreen { InputLocked: true }
                          && game.Progress.Stars == starsNow + (reallyWrong ? 0 : 1));
                Check("the next word comes", await WaitFor(() => Screen() is WordBuilderScreen { InputLocked: false } w && w.Spelling.All(s => s.Length == 0), 10));
            }
            if (await Tap("home")) await Expect<GameIslandScreen>("home from Word Builder goes back to words island");
            if (await Tap("home")) await Expect<MapScreen>("home from words island after the games");
        }

        async Task PlayLetters()
        {
            // A sound stone: Meet the Sound, then the green arrow marks it met and goes back.
            if (await Tap("stone:s")) await Expect<MeetSoundScreen>("tapping a sound stone opens Meet the Sound");
            Check("Meet the Sound introduces the sound, then shows the green arrow",
                  await WaitFor(() => Screen() is MeetSoundScreen { IntroDone: true }, 25));
            if (await Tap("next"))
            {
                Check("the sound is marked as met and saved", game.Store.Progress(game.ChildId).Sounds.Stage(game.Course!.Sound("s")!) >= BipCore.SoundStage.Met);
                Check("Meet the Sound goes back to the island", await WaitFor(() => Screen() is LettersIslandScreen, 6));
            }
            await Wait(0.6);

            // Bip's suggestion: a game for a sound in the current group.
            if (await Tap("play")) await Expect<GameScreen>("the play button starts Bip's suggested activity");
            Check("the suggestion is a Letters game", Screen() is MeetSoundScreen or SoundHuntScreen or BubblePopScreen, $"showing {Screen()?.GetType().Name}");
            if (await Tap("home")) await Expect<LettersIslandScreen>("home from the suggested activity goes back to letters island");

            var starsBefore = game.Progress.Stars;
            if (game.StartGame(BipCore.SoundHuntGame.GameId, game.Course!.Sound("s")))
            {
                await Expect<SoundHuntScreen>("Sound Hunt opens for a sound");
                Check("Sound Hunt shows three pictures", await WaitFor(() => Screen() is SoundHuntScreen { RightIndex: >= 0 }, 3));
                if (Screen() is SoundHuntScreen hunt && await Tap($"card:{(hunt.RightIndex + 1) % 3}") && await Tap($"card:{(hunt.RightIndex + 2) % 3}"))
                    Check("two misses: the hint, and still on the question", Screen() is SoundHuntScreen { InputLocked: false });
                if (Screen() is SoundHuntScreen again && await Tap($"card:{again.RightIndex}"))
                    Check("the right picture after misses: no star, but the answer is saved",
                          game.Progress.Stars == starsBefore && game.Store.Progress(game.ChildId).Skill("snd_g1").TotalAttempts > 0);
                if (await Tap("home")) await Expect<LettersIslandScreen>("home from Sound Hunt goes back to letters island");
            }

            starsBefore = game.Progress.Stars;
            if (game.StartGame(BipCore.BubblePopGame.GameId, game.Course!.Sound("s")))
            {
                await Expect<BubblePopScreen>("Bubble Pop opens for a sound");
                Check("Bubble Pop floats a bubble with the right letter", await WaitFor(() => Screen() is BubblePopScreen { InputLocked: false, RightBubble: not null }, 3));
                if (Screen() is BubblePopScreen pop && pop.RightBubble is { } bubble && await Tap(bubble))
                    Check("popping the right bubble earns a star", game.Progress.Stars == starsBefore + 1, $"stars {starsBefore} → {game.Progress.Stars}");
                if (await Tap("home")) await Expect<LettersIslandScreen>("home from Bubble Pop goes back to letters island");
            }

            starsBefore = game.Progress.Stars;
            if (await Tap("trace")) await Expect<LetterTraceScreen>("Letter Trace opens from the island");
            Check("Letter Trace is ready to trace", await WaitFor(() => Screen() is LetterTraceScreen { Started: true }, 3));
            if (Screen() is LetterTraceScreen { Started: true } trace)
            {
                // Trace round the letter with the button held: through every checkpoint.
                var points = trace.CheckpointsOnScreen.ToList();
                trace._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, GlobalPosition = points[0] });
                foreach (var point in points) trace._UnhandledInput(new InputEventMouseMotion { GlobalPosition = point, ButtonMask = MouseButtonMask.Left });
                trace._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, GlobalPosition = points[^1] });
                Check("tracing round the letter earns a star", game.Progress.Stars == starsBefore + 1, $"stars {starsBefore} → {game.Progress.Stars}");
                Check("Letter Trace goes back to the island", await WaitFor(() => Screen() is LettersIslandScreen, 6));
            }
            await Wait(0.6);

            // Feed the Monster's foods start at phonics group 4: before that the monster button
            // meets the sound instead of an empty visit.
            if (await Tap("monster")) await Expect<MeetSoundScreen>("with no foods yet, the monster button meets the sound instead");
            if (await Tap("home")) await Expect<LettersIslandScreen>("home from that goes back to letters island");
            ScreenPreview.LearnGroups(game, 3);
            Check("after learning groups 1-3, group 4 is open", game.LettersProgress?.HighestUnlockedGroup >= 4,
                  $"highest group {game.LettersProgress?.HighestUnlockedGroup}");
            starsBefore = game.Progress.Stars;
            if (await Tap("monster")) await Expect<FeedMonsterScreen>("Feed the Monster opens from the island");
            Check("Feed the Monster shows foods", await WaitFor(() => Screen() is FeedMonsterScreen { RightIndex: >= 0, InputLocked: false }, 3));
            if (Screen() is FeedMonsterScreen monster && monster.RightIndex >= 0)
            {
                monster.DragToMonsterForTest((monster.RightIndex + 1) % 3);
                await Wait(0.5);
                Check("a wrong food goes back, no star", monster is { InputLocked: false } && game.Progress.Stars == starsBefore);
                monster.DragToMonsterForTest(monster.RightIndex);
                await Wait(0.5);
                Check("dragging the right food to the monster is saved", game.Store.Progress(game.ChildId).Skills.ContainsKey("snd_g1"));
                Check("the next food comes", await WaitFor(() => monster is { InputLocked: false }, 8));
            }
            if (await Tap("home")) await Expect<LettersIslandScreen>("home from Feed the Monster goes back to letters island");
        }

        async Task PlayCoding()
        {
            if (await Tap("island:coding")) await Expect<GameIslandScreen>("coding island opens for a game");
            var starsBefore = game.Progress.Stars;
            if (await Tap("order")) await Expect<MorningOrderScreen>("Morning Order opens from the island");
            Check("Morning Order shows its cards", await WaitFor(() => Screen() is MorningOrderScreen m && m.AnswerCardNames().Count >= 3, 3));
            if (Screen() is MorningOrderScreen order)
            {
                var answer = order.AnswerCardNames();
                if (await Tap(answer[0])) Check("tapping a card sends it to space 1", order.PlacedCount == 1);
                if (await Tap(answer[0])) Check("tapping a placed card sends it back", order.PlacedCount == 0);
                // Wrong order first (the last card first), then the right one.
                foreach (var name in answer.Skip(1).Append(answer[0])) await Tap(name);
                Check("a wrong order slides back and stays on the question, no star",
                      await WaitFor(() => order is { InputLocked: false, PlacedCount: 0 }, 5) && game.Progress.Stars == starsBefore);
                foreach (var name in answer) await Tap(name);
                Check("the right order is saved and the next routine comes",
                      await WaitFor(() => order is { InputLocked: false, PlacedCount: 0 }, 10)
                      && game.Progress.Skill("sequencing").TotalAttempts == 1, $"placed {order.PlacedCount}");
            }
            if (await Tap("home")) await Expect<GameIslandScreen>("home from Morning Order goes back to coding island");

            if (await Tap("path")) await Expect<BipsPathScreen>("Bip's Path opens from the island");
            Check("Bip's Path shows a grid and blocks", await WaitFor(() => Screen() is BipsPathScreen p && p.Answer.Count > 0, 3));
            if (Screen() is BipsPathScreen path)
            {
                var blocks = path.PaletteBlocks.ToList();
                if (await Tap("block:0") && await Tap("undo")) Check("a block snaps in, and take-back removes it", path.Strip.Count == 0);
                if (await Tap("go")) Check("Go with no blocks does nothing", path is { InputLocked: false });
                // Keys: an arrow key snaps a block in when the palette has it; Backspace takes it back.
                if (blocks.Contains("right"))
                {
                    path._UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.Right });
                    var snapped = path.Strip.SequenceEqual(["right"]);
                    path._UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.Backspace });
                    Check("the right-arrow key snaps a block in and Backspace takes it back", snapped && path.Strip.Count == 0);
                }
                foreach (var block in path.Answer)
                {
                    var index = blocks.IndexOf(block);
                    if (index >= 0) await Tap($"block:{index}");
                }
                Check("the working program is in the strip", path.Strip.SequenceEqual(path.Answer));
                var stars = game.Progress.Stars;
                if (await Tap("go"))
                    Check("Bip walks the program, leaving footprints, and it counts",
                          await WaitFor(() => game.Progress.Stars == stars + 1, 10) && path.FootprintCount > 0, $"footprints {path.FootprintCount}");
                Check("the next puzzle comes", await WaitFor(() => path is { InputLocked: false } && path.Strip.Count == 0, 8));
            }
            if (await Tap("home")) await Expect<GameIslandScreen>("home from Bip's Path goes back to coding island");
            if (await Tap("home")) await Expect<MapScreen>("home from coding island after the games");
        }

        var json = JsonSerializer.Serialize(steps.Select(s => new { step = s.Name, ok = s.Ok, detail = s.Detail }),
            new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(reportPath, json);
        foreach (var s in steps) GD.Print($"{(s.Ok ? "ok  " : "FAIL")} {s.Name} {s.Detail}");
        return steps.All(s => s.Ok);
    }
}
