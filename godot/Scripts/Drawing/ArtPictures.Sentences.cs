using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// The sentence pictures of <see cref="ArtPictures"/> (Content/words/sentences.json): each a small scene
/// built from the word pictures, so "a cat sat on a mat" shows the same cat and mat the child knows.
/// The wrong picture of each pair changes one thing (a hat for the mat, a box on the fox).
/// </summary>
public static partial class ArtPictures
{
    private static readonly Dictionary<string, Func<Node2D>> Sentences = new()
    {
        ["pic_sent_01_a"] = () => Scene(("pic_mat", 0, -60, 0.9), ("pic_cat", 0, 14, 0.66)),
        ["pic_sent_01_b"] = () => Scene(("pic_hat", 0, -52, 0.8), ("pic_cat", 0, 34, 0.56)),
        ["pic_sent_02_a"] = PigInWig,
        ["pic_sent_02_b"] = () => Scene(("pic_pig", 0, 34, 0.62), ("pic_box", 0, -40, 0.8)),
        ["pic_sent_03_a"] = () => Group(Scene(("pic_duck", 10, -10, 0.85)), Placed(Hat(Palette.Red), -30, 64, 0.38)),
        ["pic_sent_03_b"] = () => Group(Scene(("pic_duck", -6, 40, 0.5)), Placed(Sock(), 0, -34, 0.78)),
        ["pic_sent_04_a"] = () => Scene(("pic_fox", 0, 40, 0.62), ("pic_box", 0, -40, 0.8)),
        ["pic_sent_04_b"] = () => Scene(("pic_fox", 0, -40, 0.72), ("pic_box", 0, 56, 0.5)),
        ["pic_sent_05_a"] = () => Scene(("pic_log", 0, -50, 0.95), ("pic_frog", 0, 30, 0.55)),
        ["pic_sent_05_b"] = () => Scene(("pic_dog", 0, -30, 0.8), ("pic_frog", 0, 72, 0.42)),
        ["pic_sent_06_a"] = () => SixFish("pic_dish", -16, 1.0, 4200),
        ["pic_sent_06_b"] = () => SixFish("pic_cup", -24, 1.0, 4210),
        ["pic_sent_07_a"] = () => Scene(("pic_boat", 0, -10, 0.95), ("pic_goat", 44, -16, 0.42)),
        ["pic_sent_07_b"] = () => Scene(("pic_bus", 0, -40, 0.85), ("pic_goat", 0, 60, 0.5)),
        ["pic_sent_08_a"] = () => HenAnd("pic_chick", 4220),
        ["pic_sent_08_b"] = () => HenAnd("pic_duck", 4240),
        ["pic_sent_09_a"] = () => Scene(("pic_bun", 0, -40, 0.9), ("pic_bug", 0, 56, 0.4)),
        ["pic_sent_09_b"] = () => Scene(("pic_bug", 0, -30, 0.8), ("pic_bun", 0, 62, 0.45)),
        ["pic_sent_10_a"] = () => Scene(("pic_tree", 0, 0, 1.0), ("pic_sheep", 0, 52, 0.4)),
        ["pic_sent_10_b"] = () => Scene(("pic_tree", -36, 14, 0.85), ("pic_sheep", 52, -62, 0.45)),
        ["pic_sent_11_a"] = () => Scene(("pic_train", 0, -30, 0.85), ("pic_snail", -34, 56, 0.4)),
        ["pic_sent_11_b"] = () => Scene(("pic_rain", 0, 36, 0.75), ("pic_snail", 0, -72, 0.5)),
        ["pic_sent_12_a"] = () => Scene(("pic_sea", 0, -10, 1.0), ("pic_moon", 0, -40, 0.42)),
        ["pic_sent_12_b"] = () => Scene(("pic_moon", 0, 40, 0.45), ("pic_cup", 0, -20, 1.0)),
        ["pic_sent_13_a"] = () => Scene(("pic_cow", -6, 34, 0.5), ("pic_car", 0, -40, 0.85)),
        ["pic_sent_13_b"] = () => Scene(("pic_farm", 0, 10, 0.95), ("pic_cow", 40, -60, 0.4)),
        ["pic_sent_14_a"] = () => Scene(("pic_king", -24, 0, 0.82), ("pic_fork", 72, -10, 0.55)),
        ["pic_sent_14_b"] = () => Scene(("pic_king", -24, 0, 0.82), ("pic_ring", 72, -40, 0.45)),
        ["pic_sent_15_a"] = () => Scene(("pic_cake", 0, -30, 0.9), ("pic_snake", 0, 60, 0.45)),
        ["pic_sent_15_b"] = () => Scene(("pic_bike", 0, -30, 0.9), ("pic_snake", 0, 46, 0.45)),
        ["pic_sent_16_a"] = () => Scene(("pic_bird", 0, 76, 0.45), ("pic_shirt", 0, -16, 1.0)),
        ["pic_sent_16_b"] = () => Scene(("pic_nest", 0, -30, 1.0), ("pic_bird", 0, 34, 0.45)),
    };

    private static Node2D? MakeSentence(string id) => Sentences.TryGetValue(id, out var draw) ? draw() : null;

    private static IEnumerable<string> SentenceIds => Sentences.Keys;

    /// <summary>Word pictures placed in a scene, in drawing order (later ones on top).</summary>
    private static Node2D Scene(params (string Id, double X, double Y, double Scale)[] parts)
    {
        var scene = new Node2D();
        foreach (var (id, x, y, scale) in parts) scene.AddChild(Placed(PictureNode.Make(id, ""), x, y, scale));
        return scene;
    }

    private static Node2D Placed(Node2D picture, double x, double y, double scale)
    {
        var holder = new Node2D { Position = P(x, y), Scale = Vector2.One * (float)scale };
        holder.AddChild(picture);
        return holder;
    }

    // A pig wearing a curly yellow wig.
    private static Node2D PigInWig()
    {
        var p = new Pic(4190);
        p.Add(PictureNode.Make("pic_pig", ""), 0, -20, 0.85);
        p.Blob(Palette.Sun, 5, Circle(-50, 70, 22), Circle(-20, 84, 24), Circle(16, 86, 24), Circle(48, 72, 22), Circle(-64, 44, 18), Circle(64, 46, 18));
        return p.Node;
    }

    // Six little fish in a dish (or a cup).
    private static Node2D SixFish(string holder, double y, double scale, ulong seed)
    {
        var p = new Pic(seed);
        p.Add(PictureNode.Make(holder, ""), 0, y, scale);
        foreach (var (i, (x, fy)) in Indexed((-54.0, 30.0), (-10.0, 44.0), (34.0, 30.0), (-34.0, 70.0), (14.0, 80.0), (58.0, 64.0)))
            p.Add(Fish(), x, fy, 0.26);
        return p.Node;
    }

    // A hen with ten little ones in two rows (chicks, or ducklings for the wrong picture).
    private static Node2D HenAnd(string young, ulong seed)
    {
        var p = new Pic(seed);
        p.Add(PictureNode.Make("pic_hen", ""), -40, 40, 0.6);
        for (var i = 0; i < 10; i++)
            p.Add(PictureNode.Make(young, ""), -84 + (i % 5) * 42, (i < 5 ? -34 : -82), 0.2);
        return p.Node;
    }
}
