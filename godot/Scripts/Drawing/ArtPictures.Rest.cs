using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>The other word pictures of <see cref="ArtPictures"/>.</summary>
public static partial class ArtPictures
{
    private static readonly Dictionary<string, Func<Node2D>> Rest = new()
    {
        ["pic_bag"] = Bag,
        ["pic_ball"] = Ball,
        ["pic_banana"] = Banana,
        ["pic_bat"] = Bat,
        ["pic_bed"] = Bed,
        ["pic_bike"] = Bike,
        ["pic_bin"] = Bin,
        ["pic_biscuit"] = Biscuit,
        ["pic_naartjie"] = Naartjie,
        ["pic_strawberry"] = Strawberry,
        ["pic_boat"] = Boat,
        ["pic_bowl"] = Bowl,
        ["pic_bug"] = Bug,
        ["pic_bun"] = Bun,
        ["pic_camp"] = Camp,
        ["pic_can"] = Can,
        ["pic_cap"] = Cap,
        ["pic_chick"] = Chick,
        ["pic_chin"] = Chin,
        ["pic_coat"] = Coat,
        ["pic_crab"] = Crab,
        ["pic_dish"] = Dish,
        ["pic_doll"] = Doll,
        ["pic_drum"] = Drum,
        ["pic_feet"] = () => Feet(true),
        ["pic_foot"] = () => Feet(false),
        ["pic_flag"] = Flag,
        ["pic_fog"] = Fog,
        ["pic_fox"] = Fox,
        ["pic_frog"] = Frog,
        ["pic_girl"] = Girl,
        ["pic_hand"] = Hand,
        ["pic_hen"] = Hen,
        ["pic_home"] = Home,
        ["pic_hook"] = Hook,
        ["pic_hut"] = Hut,
        ["pic_jet"] = Jet,
        ["pic_jug"] = Jug,
        ["pic_juice"] = Juice,
        ["pic_king"] = King,
        ["pic_lamp"] = Lamp,
        ["pic_ladybird"] = Ladybird,
        ["pic_lip"] = Lip,
        ["pic_log"] = Log,
        ["pic_lollipop"] = Lollipop,
        ["pic_lunch"] = Lunch,
        ["pic_man"] = Man,
        ["pic_milk"] = Milk,
        ["pic_mop"] = Mop,
        ["pic_mouth"] = Mouth,
        ["pic_mum"] = Mum,
        ["pic_night"] = Night,
        ["pic_nut"] = Nut,
        ["pic_owl"] = Owl,
        ["pic_park"] = Park,
        ["pic_paw"] = Paw,
        ["pic_pen"] = Pen_,
        ["pic_pencil"] = Pencil,
        ["pic_fan"] = Fan,
        ["pic_pod"] = Pod,
        ["pic_pot"] = Pot,
        ["pic_rat"] = Rat,
        ["pic_rock"] = Rock,
        ["pic_rope"] = Rope,
        ["pic_sandwich"] = Sandwich,
        ["pic_sea"] = Sea,
        ["pic_shed"] = Shed,
        ["pic_sheep"] = Sheep,
        ["pic_shell"] = Shell,
        ["pic_shirt"] = Shirt,
        ["pic_shop"] = Shop,
        ["pic_sister"] = Sister,
        ["pic_snail"] = Snail,
        ["pic_snake"] = Snake,
        ["pic_soap"] = Soap,
        ["pic_sock"] = Sock,
        ["pic_spoon"] = Spoon,
        ["pic_star"] = Star_,
        ["pic_storm"] = Storm,
        ["pic_teddy"] = Teddy,
        ["pic_town"] = Town,
        ["pic_train"] = Train,
        ["pic_tree"] = Tree,
        ["pic_truck"] = Truck,
        ["pic_tube"] = Tube,
        ["pic_vet"] = Vet,
        ["pic_wig"] = Wig,
        ["pic_wing"] = Wing,
        ["pic_wood"] = Wood,
        ["pic_zoo"] = Zoo,
    };

    // A handbag with a handle and a clasp.
    private static Node2D Bag()
    {
        var p = new Pic(3000);
        p.Line(Arc(P(0, 10), 50, 56, 0.1, Math.PI - 0.1), Palette.Ink, 16, 1).Line(Arc(P(0, 10), 50, 56, 0.1, Math.PI - 0.1), Palette.Purple.Darkened(0.2f), 8, 1);
        p.Fill(Polygon(P(-74, 20), P(74, 20), P(96, -84), P(-96, -84)), Palette.Purple, 5);
        p.Fill(Polygon(P(-74, 20), P(74, 20), P(80, -10), P(0, -24), P(-80, -10)), Palette.Purple.Darkened(0.12f), 4);
        p.Fill(RoundRect(R(-12, -32, 24, 18), 5), Palette.Sun, 3);
        return p.Node;
    }

    // A soccer ball.
    private static Node2D Ball()
    {
        var p = new Pic(3010);
        p.Fill(Circle(0, 0, 92), Palette.White, 5);
        var middle = Enumerable.Range(0, 5).Select(i => Math.PI / 2 + i * 2 * Math.PI / 5).ToArray();
        p.Fill(Polygon(middle.Select(a => P(26 * Math.Cos(a), 26 * Math.Sin(a)))), Palette.Ink, 3);
        foreach (var a in middle)
        {
            var (x, y) = (74 * Math.Cos(a), 74 * Math.Sin(a));
            p.Line(Polyline(P(26 * Math.Cos(a), 26 * Math.Sin(a)), P(x * 0.78, y * 0.78)), line: 4);
            p.Fill(Polygon(Enumerable.Range(0, 5).Select(i => a + Math.PI + i * 2 * Math.PI / 5).Select(b => P(x + 18 * Math.Cos(b), y + 18 * Math.Sin(b)))), Palette.Ink, 3);
        }
        return p.Node;
    }

    private static Node2D Banana()
    {
        var p = new Pic(3020);
        p.Fill(Polygon(Around(0, 60, 104, 130, Math.PI * 1.18, Math.PI * 1.82, 16).Concat(Around(0, 104, 94, 132, Math.PI * 1.78, Math.PI * 1.22, 16))),
               Palette.Sun, 5);
        p.Line(Polyline(Around(0, 80, 100, 130, Math.PI * 1.3, Math.PI * 1.7, 10)), Palette.Sun.Darkened(0.2f), 3);
        p.Fill(Circle(-74, -6, 7), Palette.Brown, 2).Fill(Box(66, -10, 14, 12), Palette.Brown, 3);
        return p.Node;
    }

    // A bat with scalloped wings.
    private static Node2D Bat()
    {
        var p = new Pic(3030);
        var fur = new Color("6d6478");
        foreach (var side in new[] { -1.0, 1.0 })
            p.Fill(Polygon(P(side * 20, 30), P(side * 70, 54), P(side * 110, 40), P(side * 100, -4), P(side * 82, 6), P(side * 72, -16), P(side * 52, -4), P(side * 36, -22), P(side * 20, -10)), fur, 5);
        p.Fill(Ellipse(P(0, 0), 30, 42), fur.Lightened(0.15f), 5);
        p.Fill(Polygon(P(-22, 30), P(-26, 66), P(-6, 42), P(6, 42), P(26, 66), P(22, 30)), fur.Lightened(0.15f), 4);
        p.Fill(Circle(-11, 20, 8), Palette.White, 2).Fill(Circle(11, 20, 8), Palette.White, 2).Dot(-10, 19, 3.5).Dot(12, 19, 3.5);
        p.Fill(Polygon(P(-6, 4), P(-2, -4), P(2, 4)), Palette.White, 1);
        return p.Node;
    }

    private static Node2D Bed()
    {
        var p = new Pic(3040);
        p.Fill(Box(-108, -60, 22, 140), Palette.Brown, 5);
        p.Fill(Box(86, -60, 20, 80), Palette.Brown, 5);
        p.Fill(Box(-88, -30, 194, 34), Palette.White, 5);
        p.Fill(Ellipse(P(-58, 14), 30, 16), Palette.White, 4);
        p.Fill(Polygon(P(-28, 4), P(106, 4), P(106, -40), P(-28, -40)), Palette.Teal, 5);
        foreach (var x in new[] { 10.0, 50, 90 }) p.Line(Polyline(P(x - 10, 4), P(x - 10, -40)), Palette.Teal.Lightened(0.3f), 4, 1);
        return p.Node;
    }

    private static Node2D Bike()
    {
        var p = new Pic(3050);
        foreach (var x in new[] { -62.0, 62 })
        {
            p.Line(Circle(x, -40, 44), line: 7);
            for (var i = 0; i < 4; i++)
                p.Line(Polyline(P(x + 40 * Math.Cos(i * Math.PI / 4), -40 + 40 * Math.Sin(i * Math.PI / 4)), P(x - 40 * Math.Cos(i * Math.PI / 4), -40 - 40 * Math.Sin(i * Math.PI / 4))), Grey, 2, 0.5);
        }
        Tube(p, 7, Palette.Red, P(-62, -40), P(-10, -40), P(30, 26), P(-24, 26), P(-10, -40));
        Tube(p, 7, Palette.Red, P(30, 26), P(62, -40));
        Tube(p, 7, Palette.Red, P(-24, 26), P(-62, -40));
        p.Line(Polyline(P(30, 26), P(40, 54), P(56, 56)), line: 6);
        p.Fill(RoundRect(R(-44, 30, 40, 14), 7), Palette.Ink, 3);
        return p.Node;
    }

    // A dustbin with a lid.
    private static Node2D Bin()
    {
        var p = new Pic(3060);
        p.Fill(Polygon(P(-66, 56), P(66, 56), P(52, -100), P(-52, -100)), Grey, 5);
        foreach (var x in new[] { -30.0, 0, 30 }) p.Line(Polyline(P(x, 40), P(x * 0.8, -84)), Grey.Darkened(0.25f), 4);
        p.Fill(RoundRect(R(-78, 56, 156, 24), 10), Grey.Darkened(0.1f), 5);
        p.Fill(RoundRect(R(-20, 80, 40, 18), 8), Grey.Darkened(0.1f), 4);
        return p.Node;
    }

    private static Node2D Biscuit()
    {
        var p = new Pic(3070);
        p.Fill(Polygon(Enumerable.Range(0, 24).Select(i => i * 2 * Math.PI / 24).Select(a => P((86 + 4 * Math.Sin(5 * a)) * Math.Cos(a), (86 + 4 * Math.Sin(5 * a)) * Math.Sin(a)))),
               new Color("d9a35a"), 5);
        foreach (var (x, y) in new[] { (-34.0, 30.0), (20.0, 46.0), (40.0, -6.0), (-10.0, -30.0), (-46.0, -20.0), (14.0, 8.0), (30.0, -46.0) })
            p.Fill(Ellipse(P(x, y), 9, 7), Palette.Brown.Darkened(0.3f), 2);
        return p.Node;
    }

    // A naartjie: a squat orange with a dimpled peel, its stalk and a leaf (Picnic Plates).
    private static Node2D Naartjie()
    {
        var p = new Pic(3995);
        p.Fill(Ellipse(P(0, -6), 88, 76), Palette.Orange, 5);
        foreach (var (x, y) in new[] { (-40.0, 20.0), (10.0, 34.0), (44.0, 6.0), (-18.0, -24.0), (26.0, -36.0), (-52.0, -16.0), (2.0, -2.0) })
            p.Fill(Ellipse(P(x, y), 4, 3), Palette.Orange.Darkened(0.2f), 1);
        p.Line(Polyline(P(0, 64), P(4, 82)), Palette.Brown, 6);
        p.Fill(Polygon(P(6, 76), P(40, 98), P(66, 84), P(34, 70)), Palette.Leaf, 4);
        return p.Node;
    }

    // A strawberry: red, with seeds and a green top (Picnic Plates).
    private static Node2D Strawberry()
    {
        var p = new Pic(3996);
        p.Fill(Polygon(P(-80, 46), P(-56, 70), P(0, 62), P(56, 70), P(80, 46), P(74, 4), P(48, -44), P(0, -96), P(-48, -44), P(-74, 4)),
               Palette.Red, 5);
        foreach (var (x, y) in new[] { (-44.0, 34.0), (0.0, 38.0), (44.0, 34.0), (-26.0, 2.0), (24.0, 2.0), (-40.0, -22.0), (40.0, -22.0), (0.0, -20.0), (-14.0, -52.0), (14.0, -52.0) })
            p.Fill(Ellipse(P(x, y), 4, 6), Palette.Sun, 1);
        p.Fill(Polygon(P(-60, 64), P(-30, 92), P(-10, 70), P(0, 100), P(10, 70), P(30, 92), P(60, 64), P(0, 58)), Palette.Leaf, 4);
        return p.Node;
    }

    // A sailing boat on the sea.
    private static Node2D Boat()
    {
        var p = new Pic(3080);
        p.Line(Polyline(P(0, -30), P(0, 100)), Palette.Brown, 7);
        p.Fill(Polygon(P(6, 96), P(6, -10), P(84, -10)), Palette.White, 5);
        p.Fill(Polygon(P(-6, 80), P(-6, -10), P(-70, -10)), Palette.Sun, 5);
        p.Fill(Polygon(P(-100, -22), P(100, -22), P(70, -70), P(-74, -70)), Palette.Red, 5);
        p.Line(Polyline(Enumerable.Range(0, 12).Select(i => P(-110 + i * 20, i % 2 == 0 ? -78.0 : -88))), Palette.Sea, 6);
        p.Fill(Polygon(P(2, 100), P(2, 86), P(30, 93)), Palette.Teal, 2);
        return p.Node;
    }

    // A bowl of porridge with a spoon.
    private static Node2D Bowl()
    {
        var p = new Pic(3090);
        p.Fill(RoundRect(R(30, 10, 18, 92), 9), Silver, 4);
        p.Fill(Ellipse(P(0, 10), 100, 26), new Color("efe1c0"), 5);
        p.Fill(Polygon(Around(0, 10, 100, 96, Math.PI, 2 * Math.PI, 16)), Palette.Sea, 5);
        p.Line(Polyline(Around(0, -10, 90, 60, Math.PI * 1.15, Math.PI * 1.85, 10)), Palette.White, 6);
        return p.Node;
    }

    // A green beetle with six legs.
    private static Node2D Bug()
    {
        var p = new Pic(3100);
        foreach (var side in new[] { -1.0, 1.0 })
            foreach (var y in new[] { 30.0, 0, -30 })
                p.Line(Polyline(P(side * 40, y), P(side * 80, y + 14), P(side * 96, y - 10)), line: 5);
        p.Line(Polyline(P(-10, 66), P(-30, 98)), line: 4).Line(Polyline(P(10, 66), P(30, 98)), line: 4);
        p.Fill(Circle(0, 56, 26), Palette.Ink, 4);
        p.Fill(Ellipse(P(0, -6), 58, 72), Palette.Leaf, 5);
        p.Line(Polyline(P(0, 64), P(0, -76)), line: 4);
        p.Dot(-26, 10, 8, Palette.Leaf.Darkened(0.3f)).Dot(24, -20, 8, Palette.Leaf.Darkened(0.3f)).Dot(-20, -40, 6, Palette.Leaf.Darkened(0.3f));
        p.Dot(-10, 60, 4, Palette.White).Dot(10, 60, 4, Palette.White);
        return p.Node;
    }

    // A bread roll with seeds.
    private static Node2D Bun()
    {
        var p = new Pic(3110);
        p.Fill(Polygon(Around(0, -40, 100, 120, 0, Math.PI, 18)), new Color("d99a4e"), 5);
        p.Line(Polyline(P(-100, -40), P(100, -40)), line: 5);
        foreach (var (x, y) in new[] { (-50.0, 20.0), (-10.0, 50.0), (30.0, 30.0), (56.0, 0.0), (-30.0, -10.0), (10.0, 10.0) })
            p.Fill(Ellipse(P(x, y), 6, 3), Palette.Card, 1.5);
        return p.Node;
    }

    // Camping: a tent and a little fire.
    private static Node2D Camp()
    {
        var p = new Pic(3120);
        p.Line(Polyline(P(-110, -70), P(110, -70)), Palette.Leaf, 7);
        p.Fill(Polygon(P(-104, -70), P(-30, 70), P(44, -70)), Palette.Orange, 5);
        p.Fill(Polygon(P(-30, -70), P(-30, 20), P(-6, -70)), Palette.Ink, 3);
        foreach (var (x, a) in new[] { (60.0, 0.4), (96.0, -0.4) }) p.Line(Polyline(P(x, -70), P(x + 30 * Math.Sin(-a), -40)), Palette.Brown, 8);
        p.Fill(Polygon(P(62, -56), P(70, -20), P(78, -36), P(88, 0), P(98, -40), P(104, -56)), Palette.Orange, 4);
        p.Fill(Polygon(P(72, -56), P(80, -36), P(88, -24), P(94, -56)), Palette.Sun, 3);
        return p.Node;
    }

    // A cooldrink can.
    private static Node2D Can()
    {
        var p = new Pic(3130);
        p.Fill(RoundRect(R(-50, -96, 100, 182), 18), Palette.Red, 5);
        p.Fill(Ellipse(P(0, 82), 44, 12), Silver, 4);
        p.Fill(Ellipse(P(10, 84), 14, 5), Grey, 2);
        p.Line(Polyline(P(-50, 0), P(-20, 20), P(10, -10), P(50, 10)), Palette.White, 8);
        p.Shine(-30, 40, 10, 50, 0);
        return p.Node;
    }

    // A cap with a peak.
    private static Node2D Cap()
    {
        var p = new Pic(3140);
        p.Fill(Polygon(P(20, -20), P(110, -30), P(110, -46), P(30, -44)), Palette.Red.Darkened(0.1f), 5);
        p.Fill(Polygon(Around(-10, -20, 80, 80, 0, Math.PI, 16)), Palette.Red, 5);
        p.Line(Polyline(P(-10, 60), P(-10, -20)), Palette.Red.Darkened(0.25f), 3);
        p.Fill(Circle(-10, 62, 8), Palette.Red.Darkened(0.1f), 3);
        p.Fill(Box(-90, -26, 140, 12), Palette.White, 3);
        return p.Node;
    }

    // A fluffy yellow chick.
    private static Node2D Chick()
    {
        var p = new Pic(3150);
        foreach (var x in new[] { -16.0, 16 }) p.Line(Polyline(P(x, -60), P(x, -84), P(x - 10, -92)), Palette.Orange, 5).Line(Polyline(P(x, -84), P(x + 10, -92)), Palette.Orange, 5);
        p.Fill(Ellipse(P(0, -20), 60, 48), Palette.Sun, 5);
        p.Fill(Circle(0, 40, 40), Palette.Sun, 5);
        p.Fill(Polygon(P(-14, 34), P(14, 34), P(0, 18)), Palette.Orange, 3);
        p.Dot(-14, 50, 6).Dot(14, 50, 6);
        p.Fill(Polygon(P(-46, -6), P(-70, -30), P(-30, -32)), Palette.Sun.Darkened(0.12f), 3);
        p.Line(Polyline(P(-6, 78), P(0, 94), P(8, 80)), line: 4);
        return p.Node;
    }

    // A face with an arrow pointing at the chin.
    private static Node2D Chin()
    {
        var p = new Pic(3160);
        p.Fill(Ellipse(P(-10, 10), 70, 88), Skin, 5);
        p.Fill(Polygon(Around(-10, 30, 74, 72, 0, Math.PI, 12)), Palette.Ink, 4);
        p.Dot(-34, 20, 7).Dot(14, 20, 7);
        p.Line(Arc(P(-10, -20), 26, 14, Math.PI + 0.4, 2 * Math.PI - 0.4), line: 5);
        p.Fill(Ellipse(P(-10, -64), 26, 14), Skin.Darkened(0.12f), 3);
        p.Add(WordPictures.Pointer(P(100, -100), P(30, -72), 3165));
        return p.Node;
    }

    // A warm coat with buttons and a collar.
    private static Node2D Coat()
    {
        var p = new Pic(3170);
        foreach (var side in new[] { -1.0, 1.0 })
            p.Fill(Polygon(P(side * 46, 80), P(side * 96, 50), P(side * 110, -60), P(side * 82, -64), P(side * 70, 20)), Palette.Purple.Darkened(0.1f), 5);
        p.Fill(Polygon(P(-50, 84), P(50, 84), P(70, -100), P(-70, -100)), Palette.Purple, 5);
        p.Fill(Polygon(P(-50, 84), P(0, 40), P(-20, 96)), Palette.Purple.Lightened(0.2f), 4).Fill(Polygon(P(50, 84), P(0, 40), P(20, 96)), Palette.Purple.Lightened(0.2f), 4);
        p.Line(Polyline(P(0, 40), P(0, -100)), line: 4);
        foreach (var y in new[] { 20.0, -20, -60 }) p.Fill(Circle(12, y, 7), Palette.Sun, 3);
        return p.Node;
    }

    private static Node2D Crab()
    {
        var p = new Pic(3180);
        foreach (var side in new[] { -1.0, 1.0 })
        {
            foreach (var y in new[] { -10.0, -30, -50 }) p.Line(Polyline(P(side * 50, y), P(side * 90, y - 10), P(side * 100, y - 34)), Palette.Red.Darkened(0.2f), 6);
            p.Line(Polyline(P(side * 40, 20), P(side * 70, 60)), Palette.Red.Darkened(0.2f), 7);
            p.Fill(Polygon(P(side * 60, 60), P(side * 100, 100), P(side * 104, 66), P(side * 86, 76), P(side * 90, 50)), Palette.Red, 4);
            p.Line(Polyline(P(side * 22, 30), P(side * 22, 52)), line: 4).Fill(Circle(side * 22, 56, 9), Palette.White, 3).Dot(side * 22, 56, 4);
        }
        p.Fill(Ellipse(P(0, -10), 66, 44), Palette.Red, 5);
        p.Line(Arc(P(0, -6), 18, 10, Math.PI + 0.4, 2 * Math.PI - 0.4), line: 4);
        return p.Node;
    }

    // A dish of stew.
    private static Node2D Dish()
    {
        var p = new Pic(3190);
        p.Fill(Ellipse(P(0, -10), 108, 62), Palette.EggBlue, 5);
        p.Fill(Ellipse(P(0, -4), 84, 44), new Color("c96a3b"), 4);
        foreach (var (x, y, c) in new[] { (-36.0, 4.0, Palette.Orange), (10.0, 14.0, Palette.Leaf), (40.0, -10.0, Palette.Orange), (-10.0, -20.0, Palette.Sun), (-50.0, -18.0, Palette.Leaf) })
            p.Fill(Ellipse(P(x, y), 12, 9), c, 2);
        foreach (var x in new[] { -20.0, 20 }) p.Line(Polyline(P(x, 50), P(x + 8, 66), P(x - 4, 82), P(x + 6, 98)), Palette.Stone, 5);
        return p.Node;
    }

    // A rag doll in a dress.
    private static Node2D Doll()
    {
        var p = new Pic(3200);
        foreach (var x in new[] { -20.0, 20 }) Tube(p, 12, Skin, P(x, -50), P(x, -96));
        foreach (var side in new[] { -1.0, 1.0 }) Tube(p, 11, Skin, P(side * 30, 20), P(side * 70, -20));
        p.Fill(Polygon(P(-30, 30), P(30, 30), P(64, -56), P(-64, -56)), Palette.Pink, 5);
        p.Fill(Polygon(Around(0, 60, 46, 46, -0.3, Math.PI + 0.3, 12).Append(P(-50, 20)).Prepend(P(50, 20))), Palette.Orange.Darkened(0.2f), 4);
        p.Fill(Circle(0, 60, 34), Skin, 5);
        p.Dot(-12, 64, 4).Dot(12, 64, 4);
        p.Line(Arc(P(0, 54), 12, 8, Math.PI + 0.4, 2 * Math.PI - 0.4), line: 3);
        foreach (var x in new[] { -30.0, 0, 30 }) p.Fill(Circle(x, -30, 6), Palette.White, 2);
        return p.Node;
    }

    // A drum with drumsticks.
    private static Node2D Drum()
    {
        var p = new Pic(3210);
        p.Fill(Polygon(Around(0, -60, 90, 24, Math.PI, 2 * Math.PI, 12).Concat(Around(0, 30, 90, 24, 0, Math.PI, 12))), Palette.Red, 5);
        p.Line(Polyline(Enumerable.Range(0, 7).Select(i => P(-90 + i * 30, i % 2 == 0 ? 20.0 : -70))), Palette.Sun, 5);
        p.Fill(Ellipse(P(0, 30), 90, 24), Palette.Card, 5);
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Line(Polyline(P(side * 20, 40), P(side * 90, 100)), Palette.LightBrown, 8);
            p.Fill(Circle(side * 20, 40, 9), Palette.LightBrown, 3);
        }
        return p.Node;
    }

    /// <summary>A bare foot seen from above, toes up.</summary>
    private static void FootSole(Pic p, double x, double y, double turn)
    {
        var s = Math.Sin(turn);
        p.Fill(Polygon(Enumerable.Range(0, 20).Select(i => i * 2 * Math.PI / 20)
                                .Select(a => P(x + 30 * Math.Cos(a) + s * 10 * Math.Sin(a), y + (Math.Sin(a) > 0 ? 54 : 62) * Math.Sin(a)))), Skin, 5);
        foreach (var (i, (dx, dy, r)) in Indexed((-22.0, 62.0, 11.0), (-4.0, 70.0, 9.0), (12.0, 68.0, 8.0), (25.0, 60.0, 7.0), (34.0, 48.0, 6.0)))
            p.Fill(Circle(x + dx * Math.Sign(turn == 0 ? 1 : turn), y + dy, r), Skin, 3);
    }

    // Feet: a pair; foot: just one.
    private static Node2D Feet(bool pair)
    {
        var p = new Pic(pair ? 3220UL : 3240UL);
        if (pair)
        {
            FootSole(p, -46, -30, -1);
            FootSole(p, 46, -30, 1);
        }
        else FootSole(p, 0, -30, 1);
        return p.Node;
    }

    // A red flag on a pole.
    private static Node2D Flag()
    {
        var p = new Pic(3260);
        p.Line(Polyline(P(-60, -104), P(-60, 100)), Palette.Brown, 9);
        p.Fill(Circle(-60, 102, 8), Palette.Sun, 3);
        p.Fill(Polygon(P(-56, 92), P(-10, 104), P(40, 88), P(96, 98), P(96, 16), P(40, 6), P(-10, 22), P(-56, 10)), Palette.Red, 5);
        return p.Node;
    }

    // Fog: hills and trees half hidden behind grey bands.
    private static Node2D Fog()
    {
        var p = new Pic(3270);
        p.Fill(Polygon(Around(-30, -60, 110, 110, 0, Math.PI, 14)), Palette.Grass.Lightened(0.3f), 4);
        p.Fill(Polygon(P(50, -60), P(70, 30), P(90, -60)), Palette.Leaf.Lightened(0.3f), 4);
        foreach (var (i, y) in Indexed(50.0, 10.0, -30.0, -70.0))
            p.Line(Polyline(Enumerable.Range(0, 9).Select(k => P(-104 + k * 26, y + (k % 2 == 0 ? 5.0 : -5)))), Palette.Stone.WithAlpha(0.95), 18, 1);
        return p.Node;
    }

    // A fox's face: orange with white cheeks and pointed ears.
    private static Node2D Fox()
    {
        var p = new Pic(3280);
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Fill(Polygon(P(side * 24, 40), P(side * 76, 100), P(side * 80, 20)), Palette.Orange, 5);
            p.Fill(Polygon(P(side * 40, 44), P(side * 70, 82), P(side * 72, 36)), Palette.Ink, 2);
        }
        p.Fill(Polygon(P(-96, 30), P(0, 50), P(96, 30), P(60, -30), P(0, -90), P(-60, -30)), Palette.Orange, 5);
        p.Fill(Polygon(P(-90, 24), P(-30, 0), P(0, -86), P(-50, -40)), Palette.White, 4);
        p.Fill(Polygon(P(90, 24), P(30, 0), P(0, -86), P(50, -40)), Palette.White, 4);
        p.Fill(Ellipse(P(0, -80), 14, 10), Palette.Ink, 3);
        p.Dot(-30, 16, 7).Dot(30, 16, 7);
        return p.Node;
    }

    // A green frog with big eyes.
    private static Node2D Frog()
    {
        var p = new Pic(3290);
        foreach (var side in new[] { -1.0, 1.0 })
            p.Fill(Polygon(P(side * 40, -60), P(side * 100, -80), P(side * 104, -60), P(side * 80, -44)), Palette.Leaf, 4);
        p.Fill(Ellipse(P(0, -20), 90, 64), Palette.Leaf, 5);
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Fill(Circle(side * 44, 44, 30), Palette.Leaf, 5);
            p.Fill(Circle(side * 44, 46, 18), Palette.White, 3).Dot(side * 44, 46, 8);
        }
        p.Line(Arc(P(0, -6), 50, 26, Math.PI + 0.3, 2 * Math.PI - 0.3), line: 5);
        p.Fill(Ellipse(P(0, -50), 40, 16), Palette.Grass.Lightened(0.3f), 3);
        return p.Node;
    }

    /// <summary>A child's or grown-up's face: skin, eyes, smile and cheeks, at (x, y).</summary>
    private static void Face(Pic p, double x, double y, double rx, double ry)
    {
        p.Fill(Ellipse(P(x, y), rx, ry), Skin, 5);
        p.Dot(x - rx * 0.36, y + ry * 0.1, 6).Dot(x + rx * 0.36, y + ry * 0.1, 6);
        p.Line(Arc(P(x, y - ry * 0.3), rx * 0.4, ry * 0.24, Math.PI + 0.4, 2 * Math.PI - 0.4), line: 4);
        p.Dot(x - rx * 0.6, y - ry * 0.2, 7, Palette.Pink.WithAlpha(0.7)).Dot(x + rx * 0.6, y - ry * 0.2, 7, Palette.Pink.WithAlpha(0.7));
    }

    // A girl with two puffs of hair and bows.
    private static Node2D Girl()
    {
        var p = new Pic(3300);
        var hair = Palette.Ink.Lightened(0.15f);
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Fill(Circle(side * 68, 46, 30), hair, 5);
            p.Fill(Polygon(P(side * 44, 70), P(side * 30, 90), P(side * 30, 62)), Palette.Pink, 3);
        }
        p.Fill(Polygon(Around(0, 0, 66, 76, 0, Math.PI, 12)), hair, 5);
        Face(p, 0, -6, 56, 66);
        p.Fill(Polygon(P(-60, -84), P(60, -84), P(70, -110), P(-70, -110)), Palette.Pink, 4);
        return p.Node;
    }

    // An open hand.
    private static Node2D Hand()
    {
        var p = new Pic(3320);
        // Fingers as round-topped polygons: narrow rounded rectangles lose their fill.
        foreach (var (x, top) in new[] { (-42.0, 64.0), (-14.0, 86.0), (14.0, 90.0), (42.0, 74.0) })
            p.Fill(Polygon(Around(x, top - 13, 13, 13, 0, Math.PI, 6).Prepend(P(x + 13, -10)).Append(P(x - 13, -10))), Skin, 4);
        p.Fill(Polygon(P(-50, 0), P(-84, 46), P(-104, 40), P(-96, 24), P(-70, -40)), Skin, 4);
        p.Fill(RoundRect(R(-58, -96, 116, 116), 40), Skin, 5);
        foreach (var x in new[] { -42.0, -14, 14, 42 }) p.Line(Arc(P(x, 8), 8, 4, Math.PI + 0.5, 2 * Math.PI - 0.5), Skin.Darkened(0.3f), 3);
        return p.Node;
    }

    // A brown hen with a red comb.
    private static Node2D Hen()
    {
        var p = new Pic(3340);
        var feathers = new Color("b9733f");
        foreach (var x in new[] { -16.0, 20 }) p.Line(Polyline(P(x, -40), P(x, -84), P(x - 12, -94)), Palette.Orange, 5).Line(Polyline(P(x, -84), P(x + 12, -94)), Palette.Orange, 5);
        p.Fill(Polygon(P(60, 0), P(110, 60), P(96, 0), P(108, -20)), feathers.Darkened(0.2f), 5);
        p.Fill(Ellipse(P(10, -10), 76, 54), feathers, 5);
        p.Fill(Polygon(P(-30, 0), P(30, -4), P(0, -30)), feathers.Darkened(0.15f), 4);
        p.Fill(Polygon(P(-74, 74), P(-64, 92), P(-52, 80), P(-40, 92), P(-34, 70)), Palette.Red, 4);
        p.Fill(Circle(-52, 46, 30), feathers, 5);
        p.Fill(Polygon(P(-80, 50), P(-104, 40), P(-80, 32)), Palette.Sun, 3);
        p.Fill(Ellipse(P(-76, 22), 7, 11), Palette.Red, 2);
        p.Dot(-58, 54, 5);
        return p.Node;
    }

    // A house with a red roof.
    private static Node2D Home()
    {
        var p = new Pic(3360);
        p.Fill(Box(44, 40, 22, 54), Palette.Brown, 4);
        p.Fill(Box(-80, -90, 160, 110), Palette.Sand, 5);
        p.Fill(Polygon(P(-104, 14), P(0, 98), P(104, 14)), Palette.Red, 5);
        p.Fill(Box(-20, -90, 40, 66), Palette.Brown, 4);
        p.Dot(10, -58, 4, Palette.Sun);
        foreach (var x in new[] { -62.0, 34 }) p.Fill(Box(x, -50, 30, 30), Palette.Ice, 4);
        return p.Node;
    }

    // A hook on the wall with a cap hanging on it.
    private static Node2D Hook()
    {
        var p = new Pic(3370);
        p.Fill(RoundRect(R(-26, 50, 52, 50), 10), Palette.LightBrown, 4);
        p.Dot(0, 84, 4, Grey).Dot(0, 64, 4, Grey);
        p.Line(Polyline(Around(-6, -10, 40, 40, 0, -Math.PI, 12).Prepend(P(34, 60))), Palette.Ink, 20, 0.6);
        p.Line(Polyline(Around(-6, -10, 40, 40, 0, -Math.PI, 12).Prepend(P(34, 60))), Grey, 12, 0.6);
        p.Fill(Circle(-46, -10, 9), Grey, 3);
        return p.Node;
    }

    // A round hut with a thatched roof, like a rondavel.
    private static Node2D Hut()
    {
        var p = new Pic(3380);
        p.Line(Polyline(P(-110, -84), P(110, -84)), Palette.Leaf, 7);
        p.Fill(Polygon(Around(0, -84, 78, 14, Math.PI, 2 * Math.PI, 10).Concat(Around(0, 4, 78, 14, 0, Math.PI, 10))), new Color("ecd6b0"), 5);
        p.Fill(Polygon(Around(0, -84, 20, 4, Math.PI, 2 * Math.PI, 4).Concat(new[] { P(20, -30), P(-20, -30) }).Reverse()), Palette.Brown, 4);
        p.Fill(Polygon(P(-104, 0), P(0, 104), P(104, 0), P(60, -12), P(0, -16), P(-60, -12)), new Color("c8a05a"), 5);
        foreach (var x in new[] { -60.0, -30, 0, 30, 60 }) p.Line(Polyline(P(x, -10), P(x * 0.2, 80)), Palette.Brown, 3, 1);
        return p.Node;
    }

    // An aeroplane.
    private static Node2D Jet()
    {
        var p = new Pic(3390);
        p.Fill(Polygon(P(-10, 10), P(40, 90), P(64, 90), P(40, 10)), Palette.Sea, 5);
        p.Fill(Polygon(P(-84, 10), P(-104, 60), P(-88, 60), P(-60, 16)), Palette.Sea, 4);
        p.Fill(Polygon(P(-100, 6), P(80, 6), P(108, -10), P(80, -26), P(-90, -26), P(-108, -6)), Palette.White, 5);
        foreach (var x in new[] { -50.0, -26, -2, 22, 46 }) p.Fill(Circle(x, -8, 6), Palette.Sea, 2);
        p.Fill(Polygon(P(-10, -14), P(40, -90), P(64, -90), P(40, -14)), Palette.Sea.Darkened(0.15f), 5);
        return p.Node;
    }

    // A glass jug of water.
    private static Node2D Jug()
    {
        var p = new Pic(3400);
        p.Line(Arc(P(60, 10), 34, 46, -Math.PI / 2, Math.PI / 2), line: 14).Line(Arc(P(60, 10), 34, 46, -Math.PI / 2, Math.PI / 2), Palette.Ice, 6);
        p.Fill(Polygon(P(-64, 84), P(60, 84), P(66, -96), P(-66, -96), P(-60, 60), P(-90, 84)), Palette.Ice, 5);
        p.Fill(Polygon(P(-62, 20), P(62, 20), P(66, -92), P(-66, -92)), Palette.Sea, 3);
        p.Shine(-40, 40, 10, 60, 0);
        return p.Node;
    }

    // A glass of orange juice with a straw.
    private static Node2D Juice()
    {
        var p = new Pic(3410);
        p.Line(Polyline(P(10, 20), P(40, 106), P(70, 106)), Palette.Red, 9);
        p.Fill(Polygon(P(-60, 60), P(60, 60), P(46, -96), P(-46, -96)), Palette.Ice, 5);
        p.Fill(Polygon(P(-56, 30), P(56, 30), P(46, -92), P(-46, -92)), Palette.Orange, 3);
        p.Fill(Polygon(Around(66, 60, 30, 30, Math.PI, 2 * Math.PI, 8)), Palette.Orange, 4);
        p.Line(Polyline(P(66, 60), P(66, 34)), Palette.Sun, 3);
        return p.Node;
    }

    // A king: a face with a beard and a golden crown.
    private static Node2D King()
    {
        var p = new Pic(3420);
        p.Fill(Polygon(Around(0, -20, 60, 80, Math.PI, 2 * Math.PI, 12)), Palette.Ink.Lightened(0.2f), 5);
        Face(p, 0, 0, 56, 62);
        p.Fill(Polygon(P(-56, 50), P(-62, 104), P(-30, 76), P(0, 108), P(30, 76), P(62, 104), P(56, 50)), Palette.Sun, 5);
        foreach (var (i, x) in Indexed(-30.0, 0.0, 30.0)) p.Fill(Circle(x, 62, 7), i == 1 ? Palette.Teal : Palette.Red, 3);
        return p.Node;
    }

    // A table lamp, switched on.
    private static Node2D Lamp()
    {
        var p = new Pic(3430);
        p.Fill(Ellipse(P(0, -92), 54, 12), Palette.Teal, 4);
        p.Line(Polyline(P(0, -86), P(0, 10)), Palette.Teal, 9);
        p.Fill(Polygon(P(-74, 10), P(74, 10), P(44, 90), P(-44, 90)), Palette.Sun, 5);
        foreach (var (i, a) in Indexed(-2.4, -1.57, -0.74))
            p.Line(Polyline(P(100 * Math.Cos(a) * 0.7, 10 + 100 * Math.Sin(a) * 0.4), P(100 * Math.Cos(a), 10 + 100 * Math.Sin(a) * 0.6)), Palette.Sun, 5);
        return p.Node;
    }

    private static Node2D Ladybird()
    {
        var p = new Pic(3440);
        foreach (var side in new[] { -1.0, 1.0 })
            foreach (var y in new[] { 10.0, -20, -50 })
                p.Line(Polyline(P(side * 50, y), P(side * 86, y - 10)), line: 5);
        p.Fill(Circle(0, 54, 30), Palette.Ink, 4);
        p.Fill(Ellipse(P(0, -14), 74, 76), Palette.Red, 5);
        p.Line(Polyline(P(0, 60), P(0, -88)), line: 4);
        foreach (var (x, y) in new[] { (-36.0, 20.0), (36.0, 20.0), (-44.0, -30.0), (44.0, -30.0), (-18.0, -60.0), (18.0, -60.0) })
            p.Fill(Circle(x, y, 12), Palette.Ink, 2);
        p.Dot(-12, 60, 5, Palette.White).Dot(12, 60, 5, Palette.White);
        return p.Node;
    }

    // Lips, with an arrow pointing at the bottom lip.
    private static Node2D Lip()
    {
        var p = new Pic(3460);
        p.Fill(Polygon(P(-90, 20), P(-44, 54), P(-14, 48), P(0, 36), P(14, 48), P(44, 54), P(90, 20), P(0, 18)), Palette.Red, 5);
        p.Fill(Polygon(P(-90, 20), P(0, 18), P(90, 20), P(48, -20), P(0, -30), P(-48, -20)), Palette.Red.Darkened(0.08f), 5);
        p.Shine(-30, -4, 20, 8, 0.1);
        p.Add(WordPictures.Pointer(P(70, -100), P(20, -40), 3465));
        return p.Node;
    }

    // A wooden log with rings on its end.
    private static Node2D Log()
    {
        var p = new Pic(3470);
        p.Fill(Polygon(P(-80, 40), P(80, 40), P(80, -40), P(-80, -40)), Palette.Brown, 5);
        foreach (var y in new[] { 20.0, -4, -24 }) p.Line(Polyline(P(-70, y), P(-20, y + 4), P(30, y - 2), P(60, y)), Palette.Brown.Darkened(0.25f), 3, 1);
        p.Fill(Ellipse(P(80, 0), 24, 40), Palette.Sand, 5);
        p.Line(Ellipse(P(80, 0), 14, 24), Palette.Brown, 3).Line(Ellipse(P(80, 0), 6, 10), Palette.Brown, 3);
        p.Fill(Polygon(P(-40, 40), P(-50, 74), P(-28, 66), P(-26, 40)), Palette.Leaf, 3);
        return p.Node;
    }

    // A swirly lollipop.
    private static Node2D Lollipop()
    {
        var p = new Pic(3480);
        p.Fill(RoundRect(R(-6, -104, 12, 110), 6), Palette.White, 4);
        p.Fill(Circle(0, 36, 62), Palette.Pink, 5);
        p.Line(Polyline(Enumerable.Range(0, 40).Select(i => i * 0.4).Select(a => P(a * 3.6 * Math.Cos(a), 36 + a * 3.6 * Math.Sin(a)))), Palette.DeepPink, 7, 0.8);
        return p.Node;
    }

    // A lunchbox with a sandwich and an apple.
    private static Node2D Lunch()
    {
        var p = new Pic(3490);
        p.Fill(RoundRect(R(-104, -80, 208, 120), 18), Palette.Teal, 5);
        p.Fill(RoundRect(R(-92, -68, 196, 96), 12), Palette.Ice, 3);
        p.Fill(Polygon(P(-80, -50), P(10, -50), P(-34, 20)), new Color("e6c38a"), 4);
        p.Fill(Polygon(P(-74, -40), P(4, -40), P(-34, 10)), Palette.Leaf, 2);
        p.Fill(Circle(50, -16, 30), Palette.Red, 4);
        p.Line(Polyline(P(50, 14), P(56, 30)), Palette.Brown, 4);
        return p.Node;
    }

    /// <summary>A grown-up standing: legs, a shirt, arms and a head with hair of the given style.</summary>
    private static void Person(Pic p, Color shirt, Color legs, bool dress)
    {
        foreach (var x in new[] { -18.0, 18 }) Tube(p, 14, dress ? Skin : legs, P(x, -40), P(x, -96));
        foreach (var side in new[] { -1.0, 1.0 }) Tube(p, 12, Skin, P(side * 34, 24), P(side * 60, -30));
        if (dress) p.Fill(Polygon(P(-34, 36), P(34, 36), P(60, -52), P(-60, -52)), shirt, 5);
        else
        {
            p.Fill(Box(-34, -50, 68, 22), legs, 4);
            p.Fill(Polygon(P(-38, 36), P(38, 36), P(34, -36), P(-34, -36)), shirt, 5);
        }
    }

    // A man.
    private static Node2D Man()
    {
        var p = new Pic(3500);
        Person(p, Palette.Sea, Palette.Ink.Lightened(0.3f), false);
        p.Fill(Polygon(Around(0, 70, 36, 40, -0.2, Math.PI + 0.2, 8)), Palette.Ink.Lightened(0.15f), 4);
        Face(p, 0, 64, 32, 36);
        p.Node.Scale = Vector2.One * 1.1f;
        p.Node.Position = P(0, -12);
        return Group(p.Node);
    }

    // A carton of milk.
    private static Node2D Milk()
    {
        var p = new Pic(3510);
        p.Fill(Polygon(P(-50, 40), P(50, 40), P(50, -96), P(-50, -96)), Palette.White, 5);
        p.Fill(Polygon(P(-50, 40), P(0, 80), P(50, 40)), Palette.Sea, 5);
        p.Fill(Box(-14, 80, 28, 20), Palette.Sea, 4);
        p.Fill(Polygon(P(-50, -30), P(50, -10), P(50, -60), P(-50, -70)), Palette.Sea, 3);
        p.Fill(Circle(0, 0, 18), Palette.Sea.Lightened(0.4f), 3);
        return p.Node;
    }

    // A mop and bucket.
    private static Node2D Mop()
    {
        var p = new Pic(3520);
        p.Line(Polyline(P(-50, 104), P(10, -46)), Palette.LightBrown, 10);
        foreach (var i in Enumerable.Range(0, 7)) p.Line(Polyline(P(10 - 18 + i * 6, -46), P(-24 + i * 10, -100)), Palette.Card.Darkened(0.1f), 7, 1);
        p.Fill(Polygon(P(36, -20), P(104, -20), P(96, -96), P(44, -96)), Palette.Sea, 5);
        p.Line(Arc(P(70, -20), 34, 26, 0.1, Math.PI - 0.1), line: 4);
        return p.Node;
    }

    // A big smiling mouth with teeth and a tongue.
    private static Node2D Mouth()
    {
        var p = new Pic(3530);
        p.Fill(Polygon(new[] { P(-96, 30), P(96, 30) }.Concat(Around(0, 30, 96, 96, 0, -Math.PI, 16).Skip(1))), Palette.Red.Darkened(0.4f), 5);
        p.Fill(Polygon(P(-90, 30), P(90, 30), P(84, 10), P(-84, 10)), Palette.White, 3);
        p.Fill(Ellipse(P(0, -40), 46, 26), Palette.Pink, 3);
        p.Line(Polyline(Around(0, 30, 108, 108, -0.05, -Math.PI + 0.05, 16)), Palette.Red, 12, 1);
        return p.Node;
    }

    // Mum: a smiling woman with her hair up in a bun.
    private static Node2D Mum()
    {
        var p = new Pic(3540);
        p.Fill(Circle(0, 84, 26), Palette.Brown.Darkened(0.3f), 4);
        p.Fill(Polygon(Around(0, 0, 64, 74, -0.1, Math.PI + 0.1, 12)), Palette.Brown.Darkened(0.3f), 5);
        Face(p, 0, -6, 54, 64);
        p.Fill(Polygon(P(-60, -80), P(60, -80), P(80, -110), P(-80, -110)), Palette.Teal, 4);
        foreach (var side in new[] { -1.0, 1.0 }) p.Fill(Circle(side * 56, -30, 7), Palette.Sun, 2);
        return p.Node;
    }

    // Night: a dark sky with the moon and stars over a sleeping house.
    private static Node2D Night()
    {
        var p = new Pic(3550);
        p.Fill(RoundRect(R(-108, -104, 216, 208), 20), new Color("2f3566"), 4);
        foreach (var (x, y, r) in new[] { (-70.0, 70.0, 9.0), (-20.0, 40.0, 6.0), (70.0, 30.0, 7.0), (-80.0, 10.0, 5.0), (20.0, 80.0, 5.0) })
            p.Fill(Polygon(Star(P(x, y), r)), Palette.Sun, 2);
        p.Add(Moon(), 54, 64, 0.38);
        p.Fill(Box(-60, -104, 90, 60), new Color("4a4f86"), 4);
        p.Fill(Polygon(P(-72, -44), P(-15, 4), P(42, -44)), new Color("4a4f86"), 4);
        p.Fill(Box(-36, -84, 22, 22), Palette.Sun, 3);
        return p.Node;
    }

    // A peanut in its shell.
    private static Node2D Nut()
    {
        var p = new Pic(3560);
        p.Blob(new Color("d9b07a"), 5, Ellipse(P(-36, 20), 52, 44), Ellipse(P(36, -20), 52, 44), Polygon(P(-40, -10), P(-6, 40), P(40, 10), P(6, -40)));
        foreach (var (x, y) in new[] { (-50.0, 30.0), (-30.0, 6.0), (-20.0, 40.0), (30.0, -10.0), (50.0, -30.0), (20.0, -40.0) })
            p.Dot(x, y, 3, Palette.Brown);
        return p.Node;
    }

    // An owl with big round eyes.
    private static Node2D Owl()
    {
        var p = new Pic(3570);
        var feathers = new Color("a77b52");
        p.Line(Polyline(P(-110, -90), P(110, -90)), Palette.Brown, 9);
        p.Fill(Polygon(P(-60, 70), P(-64, 100), P(-30, 76), P(30, 76), P(64, 100), P(60, 70), P(70, -20), P(50, -84), P(-50, -84), P(-70, -20)), feathers, 5);
        p.Fill(Ellipse(P(0, -36), 40, 40), feathers.Lightened(0.35f), 3);
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Fill(Circle(side * 30, 36, 26), Palette.Card, 4);
            p.Fill(Circle(side * 30, 36, 13), Palette.Sun, 2).Dot(side * 30, 36, 7);
        }
        p.Fill(Polygon(P(-8, 18), P(8, 18), P(0, 0)), Palette.Orange, 3);
        foreach (var x in new[] { -20.0, 0, 20 }) p.Line(Polyline(P(x - 6, -84), P(x, -96)), Palette.Orange, 4);
        return p.Node;
    }

    // A park: grass, a tree and a swing.
    private static Node2D Park()
    {
        var p = new Pic(3580);
        p.Fill(Polygon(P(-110, -60), P(110, -60), P(110, -100), P(-110, -100)), Palette.Grass, 4);
        p.Fill(Box(-76, -60, 16, 70), Palette.Brown, 4);
        p.Blob(Palette.Leaf, 5, Circle(-68, 30, 36), Circle(-40, 56, 30), Circle(-94, 56, 28), Circle(-68, 76, 26));
        p.Line(Polyline(P(10, -60), P(30, 70), P(90, 70), P(110, -60)), Palette.Red, 8);
        p.Line(Polyline(P(46, 70), P(46, -20)), line: 3).Line(Polyline(P(74, 70), P(74, -20)), line: 3);
        p.Fill(Box(38, -26, 44, 10), Palette.Sun, 3);
        return p.Node;
    }

    // A paw print.
    private static Node2D Paw()
    {
        var p = new Pic(3590);
        p.Fill(Polygon(Around(0, -40, 56, 46, 0, 2 * Math.PI, 16)), Palette.Brown, 5);
        foreach (var (x, y) in new[] { (-62.0, 28.0), (-24.0, 62.0), (24.0, 62.0), (62.0, 28.0) })
            p.Fill(Ellipse(P(x, y), 22, 28), Palette.Brown, 4);
        return p.Node;
    }

    // A pen with its cap on the end.
    private static Node2D Pen_()
    {
        var p = new Pic(3600);
        var n = new Pic(3601);
        n.Fill(Polygon(P(-14, 70), P(14, 70), P(14, -60), P(-14, -60)), Palette.Sea, 5);
        n.Fill(Polygon(P(-14, -60), P(14, -60), P(0, -100)), Silver, 4);
        n.Dot(0, -96, 3);
        n.Fill(RoundRect(R(-16, 40, 32, 64), 10), Palette.Sea.Darkened(0.25f), 4);
        n.Fill(Box(14, 50, 8, 44), Silver, 3);
        p.Add(n.Node);
        p.Node.GetChild<Node2D>(0).Rotation = Turn(-0.6);
        return p.Node;
    }

    // A yellow pencil with a pink rubber.
    private static Node2D Pencil()
    {
        var n = new Pic(3610);
        n.Fill(RoundRect(R(-16, 70, 32, 34), 10), Palette.Pink, 4);
        n.Fill(Box(-16, 56, 32, 18), Silver, 4);
        n.Fill(Polygon(P(-16, 58), P(16, 58), P(16, -50), P(-16, -50)), Palette.Sun, 5);
        n.Line(Polyline(P(0, 56), P(0, -48)), Palette.Sun.Darkened(0.2f), 3);
        n.Fill(Polygon(P(-16, -50), P(16, -50), P(0, -100)), new Color("efd5ad"), 4);
        n.Fill(Polygon(P(-6, -84), P(6, -84), P(0, -100)), Palette.Ink, 2);
        var p = new Pic(3619);
        p.Add(n.Node);
        n.Node.Rotation = Turn(-0.6);
        return p.Node;
    }

    // An electric fan on a stand.
    private static Node2D Fan()
    {
        var p = new Pic(3700);
        p.Fill(Ellipse(P(0, -96), 50, 12), Palette.Teal, 4);
        p.Line(Polyline(P(0, -90), P(0, -10)), Palette.Teal, 10);
        p.Fill(Circle(0, 40, 64), Palette.Ice, 5);
        foreach (var a in new[] { 0.3, 0.3 + 2 * Math.PI / 3, 0.3 + 4 * Math.PI / 3 })
            p.Fill(Polygon(P(0, 40), P(50 * Math.Cos(a - 0.35), 40 + 50 * Math.Sin(a - 0.35)), P(56 * Math.Cos(a + 0.2), 40 + 56 * Math.Sin(a + 0.2))), Palette.Sea, 3);
        foreach (var r in new[] { 30.0, 50 }) p.Line(Circle(0, 40, r), Grey, 2, 0.8);
        p.Fill(Circle(0, 40, 12), Palette.Teal, 3);
        return p.Node;
    }

    // An open pea pod.
    private static Node2D Pod()
    {
        var p = new Pic(3710);
        p.Fill(Polygon(Around(0, 60, 110, 90, Math.PI * 1.15, Math.PI * 1.85, 14).Concat(Around(0, 20, 110, 60, Math.PI * 1.85, Math.PI * 1.15, 14))), Palette.Leaf, 5);
        foreach (var x in new[] { -54.0, -18, 18, 54 }) p.Fill(Circle(x, -14 - Math.Abs(x) * 0.08, 16), Palette.Grass, 3);
        p.Line(Polyline(P(-96, 22), P(-110, 40)), Palette.Leaf.Darkened(0.2f), 5);
        return p.Node;
    }

    // A cooking pot with a lid and steam.
    private static Node2D Pot()
    {
        var p = new Pic(3720);
        foreach (var side in new[] { -1.0, 1.0 }) p.Fill(RoundRect(R(side > 0 ? 70 : -104, -10, 34, 16), 8), Grey, 3);
        p.Fill(Polygon(P(-76, 10), P(76, 10), P(70, -86), P(-70, -86)), Grey.Lightened(0.15f), 5);
        p.Fill(Polygon(Around(0, 10, 80, 26, 0, Math.PI, 12)), Grey, 5);
        p.Fill(Circle(0, 40, 10), Palette.Ink, 3);
        foreach (var x in new[] { -30.0, 30 }) p.Line(Polyline(P(x, 56), P(x + 8, 72), P(x - 4, 88), P(x + 6, 104)), Palette.Stone, 5);
        return p.Node;
    }

    // A grey rat with a long tail.
    private static Node2D Rat()
    {
        var p = new Pic(3730);
        var fur = new Color("a39e9a");
        p.Line(Polyline(P(60, -30), P(96, -50), P(104, -10), P(86, 20)), Palette.Pink, 6);
        p.Fill(Ellipse(P(10, -20), 64, 42), fur, 5);
        p.Fill(Polygon(P(-40, 10), P(-104, -20), P(-50, -50)), fur, 5);
        p.Fill(Circle(-34, 20, 18), fur, 4).Fill(Circle(-34, 20, 9), Palette.Pink, 2);
        p.Dot(-104, -20, 6, Palette.Pink).Dot(-66, -12, 5);
        foreach (var x in new[] { -20.0, 30 }) p.Line(Polyline(P(x, -58), P(x, -70)), Palette.Pink, 5);
        return p.Node;
    }

    private static Node2D Rock()
    {
        var p = new Pic(3740);
        p.Fill(Polygon(P(-100, -70), P(-90, 0), P(-50, 50), P(10, 70), P(70, 40), P(100, -20), P(96, -70)), Grey, 5);
        p.Line(Polyline(P(-40, 30), P(-10, 0), P(-20, -30)), Grey.Darkened(0.3f), 4).Line(Polyline(P(40, 20), P(60, -20)), Grey.Darkened(0.3f), 4);
        p.Fill(Polygon(P(-110, -70), P(110, -70), P(110, -84), P(-110, -84)), Palette.Grass, 3);
        return p.Node;
    }

    // A coiled rope.
    private static Node2D Rope()
    {
        var p = new Pic(3750);
        var rope = new Color("d2b27a");
        var coil = Enumerable.Range(0, 90).Select(i => i * 0.2).Select(a => P((14 + a * 4.6) * Math.Cos(a), (14 + a * 4.6) * Math.Sin(a) * 0.62)).ToArray();
        p.Line(Polyline(coil.Append(P(110, -30))), Palette.Ink, 22, 0.6).Line(Polyline(coil.Append(P(110, -30))), rope, 14, 0.6);
        return p.Node;
    }

    // A sandwich from the side: bread, lettuce, tomato, cheese, bread.
    private static Node2D Sandwich()
    {
        var p = new Pic(3760);
        var bread = new Color("ecd09a");
        p.Fill(RoundRect(R(-100, -76, 200, 36), 12), bread, 5);
        p.Fill(Polygon(Enumerable.Range(0, 21).Select(k => P(-104 + k * 10.4, -40 + (k % 2 == 0 ? 10.0 : 0))).Append(P(100, -46)).Append(P(-100, -46))), Palette.Leaf, 4);
        p.Fill(Box(-92, -30, 184, 16), Palette.Red, 4);
        p.Fill(Polygon(P(-96, -14), P(96, -14), P(96, 2), P(60, 2), P(50, -26), P(40, 2), P(-96, 2)), Palette.Sun, 4);
        p.Fill(Polygon(Around(0, 2, 100, 74, 0, Math.PI, 16)), bread, 5);
        foreach (var (x, y) in new[] { (-40.0, 40.0), (0.0, 56.0), (40.0, 36.0) }) p.Fill(Ellipse(P(x, y), 6, 3), Palette.Card, 1.5);
        return p.Node;
    }

    // The sea: waves under the sun.
    private static Node2D Sea()
    {
        var p = new Pic(3770);
        p.Fill(Circle(56, 60, 34), Palette.Sun, 4);
        foreach (var (i, y) in Indexed(0.0, -36.0, -72.0))
        {
            var wave = Enumerable.Range(0, 23).Select(k => P(-110 + k * 10, y + 10 * Math.Sin(k * 0.8 + i)));
            p.Fill(Polygon(wave.Append(P(110, -104)).Append(P(-110, -104))), Palette.Sea.Darkened(0.1f * i), 4);
        }
        return p.Node;
    }

    // A wooden garden shed.
    private static Node2D Shed()
    {
        var p = new Pic(3780);
        p.Fill(Box(-84, -90, 168, 120), Palette.LightBrown, 5);
        foreach (var x in new[] { -56.0, -28, 28, 56 }) p.Line(Polyline(P(x, -86), P(x, 26)), Palette.Brown, 3, 1);
        p.Fill(Polygon(P(-100, 24), P(0, 90), P(100, 24)), Palette.Brown, 5);
        p.Fill(Box(-22, -90, 44, 80), Palette.Brown.Darkened(0.15f), 4);
        p.Dot(14, -50, 4, Palette.Sun);
        return p.Node;
    }

    // A woolly sheep.
    private static Node2D Sheep()
    {
        var p = new Pic(3790);
        foreach (var x in new[] { -46.0, -20, 24, 50 }) Tube(p, 11, Palette.Ink, P(x, -20), P(x, -90));
        p.Blob(Palette.White, 5, Circle(-50, 10, 34), Circle(-14, 34, 36), Circle(30, 30, 34), Circle(60, 0, 30), Circle(26, -20, 34), Circle(-26, -16, 34));
        p.Fill(Ellipse(P(-84, 20), 26, 34), Palette.Ink.Lightened(0.15f), 4);
        p.Fill(Ellipse(P(-100, 40), 12, 8), Palette.Ink.Lightened(0.15f), 3);
        p.Dot(-90, 26, 5, Palette.White);
        return p.Node;
    }

    // A scallop seashell.
    private static Node2D Shell()
    {
        var p = new Pic(3800);
        var shell = new Color("f5b08a");
        p.Fill(Polygon(Around(0, -40, 104, 130, 0.25, Math.PI - 0.25, 14).Prepend(P(30, -70)).Append(P(-30, -70))), shell, 5);
        foreach (var a in new[] { 0.6, 0.95, 1.3, 1.57, 1.84, 2.2, 2.55 })
            p.Line(Polyline(P(0, -66), P(100 * Math.Cos(a), -40 + 120 * Math.Sin(a))), shell.Darkened(0.25f), 3);
        p.Fill(Polygon(P(-30, -70), P(30, -70), P(40, -96), P(-40, -96)), shell.Darkened(0.1f), 4);
        return p.Node;
    }

    // A T-shirt.
    private static Node2D Shirt()
    {
        var p = new Pic(3810);
        p.Fill(Polygon(P(-30, 84), P(-104, 50), P(-80, 6), P(-56, 18), P(-56, -96), P(56, -96), P(56, 18), P(80, 6), P(104, 50), P(30, 84), P(0, 66)), Palette.Teal, 5);
        p.Line(Arc(P(0, 84), 30, 20, Math.PI + 0.1, 2 * Math.PI - 0.1), line: 4);
        p.Fill(Polygon(Star(P(0, -20), 28)), Palette.Sun, 3);
        return p.Node;
    }

    // A corner shop with a striped awning.
    private static Node2D Shop()
    {
        var p = new Pic(3820);
        p.Fill(Box(-96, -96, 192, 150), Palette.Card.Darkened(0.06f), 5);
        p.Fill(Box(-96, 54, 192, 40), Palette.Teal, 5);
        for (var i = 0; i < 6; i++)
            p.Fill(Polygon(Around(-80 + i * 32, 30, 16, 16, 0, -Math.PI, 6).Prepend(P(-64 + i * 32, 54)).Append(P(-96 + i * 32, 54))), i % 2 == 0 ? Palette.Red : Palette.White, 3);
        p.Fill(Box(-80, -60, 90, 70), Palette.Ice, 4);
        foreach (var (x, c) in new[] { (-60.0, Palette.Red), (-34.0, Palette.Sun), (-8.0, Palette.Leaf) }) p.Fill(Circle(x, -40, 10), c, 2);
        p.Fill(Box(28, -96, 50, 96), Palette.Brown, 4);
        p.Dot(66, -50, 4, Palette.Sun);
        return p.Node;
    }

    // Two sisters holding hands.
    private static Node2D Sister()
    {
        var p = new Pic(3830);
        foreach (var (i, (x, dress, scale)) in Indexed((-46.0, Palette.Pink, 1.0), (50.0, Palette.Purple, 0.86)))
        {
            var top = -96 + 150 * scale;
            foreach (var dx in new[] { -10.0, 10 }) Tube(p, 9, Skin, P(x + dx, -60), P(x + dx, -96));
            p.Fill(Polygon(P(x - 18, top - 34 * scale), P(x + 18, top - 34 * scale), P(x + 40, -64), P(x - 40, -64)), dress, 5);
            p.Fill(Polygon(Around(x, top, 34 * scale, 38 * scale, -0.2, Math.PI + 0.2, 8)), Palette.Ink.Lightened(0.15f), 4);
            Face(p, x, top - 6 * scale, 28 * scale, 32 * scale);
        }
        Tube(p, 9, Skin, P(-20, 0), P(4, -20), P(28, 0));
        return p.Node;
    }

    private static Node2D Snail()
    {
        var p = new Pic(3840);
        p.Fill(Polygon(P(-104, -70), P(80, -70), P(96, -56), P(-60, -50), P(-80, -20), P(-104, -40)), Palette.Sand.Darkened(0.1f), 5);
        p.Line(Polyline(P(-90, -24), P(-100, 20)), line: 4).Line(Polyline(P(-76, -20), P(-74, 24)), line: 4);
        p.Dot(-100, 22, 6).Dot(-74, 26, 6);
        p.Fill(Circle(16, 0, 62), Palette.Orange, 5);
        p.Line(Polyline(Enumerable.Range(0, 34).Select(i => i * 0.4).Select(a => P(16 + (54 - a * 3.8) * Math.Cos(-a), (54 - a * 3.8) * Math.Sin(-a)))), Palette.Brown, 5, 0.8);
        return p.Node;
    }

    private static Node2D Snake()
    {
        var p = new Pic(3850);
        var body = Enumerable.Range(0, 20).Select(i => P(90 - i * 9, -60 + 34 * Math.Sin(i * 0.55))).ToArray();
        p.Line(Polyline(body), Palette.Ink, 32, 0.6).Line(Polyline(body), Palette.Leaf, 22, 0.6);
        foreach (var q in body.Where((_, i) => i % 3 == 1)) p.Node.AddChild(new Disc(5, Palette.Sun) { Position = q });
        p.Fill(Ellipse(P(-90, 30), 34, 24), Palette.Leaf, 5);
        p.Line(Polyline(P(-90, 10), P(-90, -20)), Palette.Leaf, 22, 0.6);
        p.Dot(-96, 38, 5).Dot(-80, 38, 5);
        p.Line(Polyline(P(-120, 24), P(-134, 30)), Palette.Red, 4);
        return p.Node;
    }

    private static Node2D Soap()
    {
        var p = new Pic(3860);
        p.Fill(RoundRect(R(-80, -60, 160, 80), 30), Palette.Pink, 5);
        p.Fill(RoundRect(R(-60, -46, 120, 52), 22), Palette.Pink.Lightened(0.2f), 3);
        foreach (var (x, y, r) in new[] { (-50.0, 50.0, 20.0), (-10.0, 70.0, 14.0), (30.0, 46.0, 18.0), (64.0, 76.0, 12.0), (10.0, 94.0, 9.0) })
            p.Fill(Circle(x, y, r), Palette.Bubble.WithAlpha(0.8), 3).Shine(x - r * 0.3, y + r * 0.3, r * 0.4, r * 0.25);
        return p.Node;
    }

    private static Node2D Sock()
    {
        var p = new Pic(3870);
        p.Fill(Polygon(P(-20, 100), P(40, 100), P(40, -20), P(60, -50), P(40, -96), P(-60, -96), P(-84, -70), P(-70, -40), P(-20, -30)), Palette.Red, 5);
        foreach (var y in new[] { 70.0, 40, 10 }) p.Fill(Box(-20, y, 60, 12), Palette.White, 2);
        p.Fill(Polygon(P(-60, -96), P(-84, -70), P(-70, -46), P(-46, -70)), Palette.White, 3);
        p.Fill(Polygon(P(30, -96), P(60, -50), P(46, -36), P(14, -80)), Palette.White, 3);
        return p.Node;
    }

    private static Node2D Spoon()
    {
        var p = new Pic(3880);
        var n = new Pic(3881);
        n.Fill(Polygon(P(-8, 20), P(8, 20), P(12, -100), P(-12, -100)), Silver, 4);
        n.Fill(Ellipse(P(0, 56), 36, 50), Silver, 5);
        n.Shine(-10, 70, 10, 26, 0);
        p.Add(n.Node);
        n.Node.Rotation = Turn(-0.5);
        return p.Node;
    }

    private static Node2D Star_()
    {
        var p = new Pic(3890);
        p.Fill(Polygon(Star(P(0, -6), 100)), Palette.Sun, 6);
        p.Dot(-22, 4, 7).Dot(22, 4, 7);
        p.Line(Arc(P(0, -16), 18, 12, Math.PI + 0.4, 2 * Math.PI - 0.4), line: 4);
        return p.Node;
    }

    // A storm: a dark cloud, lightning and rain.
    private static Node2D Storm()
    {
        var p = new Pic(3900);
        Cloud(0, 44, 0.9, Grey, 0, p);
        p.Fill(Polygon(P(-6, 10), P(-34, -46), P(-8, -46), P(-26, -100), P(30, -30), P(4, -30), P(20, 10)), Palette.Sun, 4);
        foreach (var (x, y) in new[] { (-70.0, -30.0), (-56.0, -76.0), (54.0, -40.0), (74.0, -84.0) })
            p.Line(Polyline(P(x, y), P(x - 8, y - 22)), Palette.Sea, 5);
        return p.Node;
    }

    // A teddy bear.
    private static Node2D Teddy()
    {
        var p = new Pic(3910);
        var fur = new Color("c08a54");
        foreach (var side in new[] { -1.0, 1.0 })
        {
            p.Fill(Circle(side * 40, -80, 24), fur, 5);
            p.Fill(Circle(side * 62, -16, 20), fur, 5);
            p.Fill(Circle(side * 40, 82, 20), fur, 5).Fill(Circle(side * 40, 82, 10), fur.Lightened(0.3f), 2);
        }
        p.Fill(Ellipse(P(0, -40), 52, 56), fur, 5);
        p.Fill(Ellipse(P(0, -44), 30, 34), fur.Lightened(0.3f), 3);
        p.Fill(Circle(0, 44, 50), fur, 5);
        p.Fill(Ellipse(P(0, 30), 22, 16), fur.Lightened(0.3f), 3);
        p.Fill(Ellipse(P(0, 38), 8, 6), Palette.Ink, 2);
        p.Dot(-18, 58, 6).Dot(18, 58, 6);
        p.Fill(Polygon(P(-20, 2), P(0, -8), P(20, 2), P(20, -16), P(0, -6), P(-20, -16)), Palette.Red, 3);
        return p.Node;
    }

    // A town: houses and a tall building.
    private static Node2D Town()
    {
        var p = new Pic(3920);
        p.Fill(Box(-20, -90, 60, 180), Palette.Sea.Lightened(0.3f), 5);
        for (var r = 0; r < 5; r++) foreach (var x in new[] { -8.0, 16 }) p.Fill(Box(x, -70 + r * 32, 14, 18), Palette.Sun, 2);
        p.Fill(Box(-104, -90, 70, 70), Palette.Sand, 5).Fill(Polygon(P(-112, -20), P(-69, 30), P(-26, -20)), Palette.Red, 5);
        p.Fill(Box(-80, -90, 22, 36), Palette.Brown, 3);
        p.Fill(Box(52, -90, 56, 90), Palette.Pink, 5).Fill(Polygon(P(46, 0), P(80, 40), P(114, 0)), Palette.Teal, 5);
        p.Fill(Box(68, -50, 22, 22), Palette.Ice, 3);
        return p.Node;
    }

    // A toy steam train engine.
    private static Node2D Train()
    {
        var p = new Pic(3930);
        p.Fill(Box(-40, 26, 22, 44), Palette.Ink, 4);
        p.Fill(Polygon(P(-48, 70), P(-10, 70), P(-20, 84), P(-38, 84)), Palette.Ink, 3);
        foreach (var (x, y, r) in new[] { (-20.0, 94.0, 12.0), (6.0, 104.0, 9.0) }) p.Fill(Circle(x, y, r), Palette.Stone, 3);
        p.Fill(Box(-96, -40, 130, 66), Palette.Red, 5);
        p.Fill(Box(30, -40, 70, 104), Palette.Teal, 5);
        p.Fill(Box(44, 14, 40, 34), Palette.Ice, 3);
        p.Fill(Box(20, 60, 92, 16), Palette.Ink, 3);
        p.Fill(Polygon(P(-96, -40), P(-110, -60), P(-96, -60)), Palette.Sun, 3);
        foreach (var (x, r) in new[] { (-70.0, 22.0), (-20.0, 22.0), (60.0, 30.0) }) Wheel(p, x, -56, r);
        return p.Node;
    }

    // A big leafy tree.
    private static Node2D Tree()
    {
        var p = new Pic(3940);
        p.Fill(Polygon(P(-18, -100), P(18, -100), P(14, 0), P(36, 30), P(4, 14), P(-30, 30), P(-14, 0)), Palette.Brown, 5);
        p.Blob(Palette.Leaf, 5, Circle(0, 50, 54), Circle(-50, 30, 40), Circle(50, 30, 40), Circle(-36, 76, 34), Circle(36, 76, 34), Circle(0, 92, 30));
        foreach (var (x, y) in new[] { (-40.0, 40.0), (30.0, 64.0), (10.0, 20.0) }) p.Fill(Circle(x, y, 9), Palette.Red, 2);
        return p.Node;
    }

    // A delivery truck.
    private static Node2D Truck()
    {
        var p = new Pic(3950);
        p.Fill(Box(-108, -40, 140, 110), Palette.Sun, 5);
        p.Fill(Polygon(P(32, -40), P(108, -40), P(108, 10), P(84, 44), P(32, 44)), Palette.Red, 5);
        p.Fill(Polygon(P(46, 4), P(92, 4), P(76, 32), P(46, 32)), Palette.Ice, 3);
        foreach (var x in new[] { -68.0, -10, 76 }) Wheel(p, x, -44, 22);
        return p.Node;
    }

    // A tube of toothpaste.
    private static Node2D Tube()
    {
        var p = new Pic(3960);
        var n = new Pic(3961);
        n.Fill(Polygon(P(-40, 90), P(40, 90), P(40, 80), P(30, -60), P(-30, -60), P(-40, 80)), Palette.White, 5);
        n.Fill(Polygon(P(-30, 30), P(36, 30), P(32, -20), P(-32, -20)), Palette.Sea, 3);
        n.Fill(RoundRect(R(-22, -100, 44, 40), 8), Palette.Red, 4);
        p.Add(n.Node);
        n.Node.Rotation = Turn(-0.5);
        return p.Node;
    }

    // At the vet: a puppy and a stethoscope.
    private static Node2D Vet()
    {
        var p = new Pic(3970);
        p.Add(Dog(), -26, 20, 0.7);
        p.Line(Polyline(P(64, 90), P(70, 20), P(50, -30), P(10, -60)), Palette.Ink, 7);
        p.Line(Polyline(P(64, 90), P(96, 64), P(90, 10), P(50, -30)), Palette.Ink, 7);
        p.Fill(Circle(0, -70, 20), Silver, 5);
        return p.Node;
    }

    // A wig on a stand.
    private static Node2D Wig()
    {
        var p = new Pic(3980);
        p.Fill(Box(-14, -100, 28, 60), Grey, 4).Fill(Ellipse(P(0, -100), 46, 10), Grey, 4);
        p.Fill(Ellipse(P(0, 4), 50, 56), Palette.Stone, 5);
        p.Blob(Palette.Sun, 5, Circle(-46, 40, 30), Circle(0, 62, 36), Circle(46, 40, 30), Circle(-62, 0, 26), Circle(62, 0, 26), Circle(-58, -40, 22), Circle(58, -40, 22));
        p.Fill(Ellipse(P(0, 8), 36, 36), Palette.Stone, 3);
        return p.Node;
    }

    // Wing: the bird with an orange arrow at its wing (a part of something gets a pointer, as in WordPictures).
    private static Node2D Wing()
    {
        var p = new Pic(3990);
        p.Add(Bird(), -10, 0, 0.9);
        p.Add(WordPictures.Pointer(P(-60, -100), P(-24, -6), 3995));
        return p.Node;
    }

    // Wood: logs stacked for the fire.
    private static Node2D Wood()
    {
        var p = new Pic(4000);
        foreach (var (x, y) in new[] { (-60.0, -60.0), (0.0, -60.0), (60.0, -60.0), (-30.0, -4.0), (30.0, -4.0), (0.0, 52.0) })
        {
            p.Fill(Circle(x, y, 30), Palette.Sand, 5);
            p.Line(Circle(x, y, 18), Palette.Brown, 3).Line(Circle(x, y, 7), Palette.Brown, 3);
        }
        return p.Node;
    }

    // The zoo: a lion behind the bars of its enclosure.
    private static Node2D Zoo()
    {
        var p = new Pic(4010);
        var mane = Palette.Orange.Darkened(0.15f);
        // The mane: a ring of points, every other one further out.
        p.Fill(Polygon(Enumerable.Range(0, 24).Select(k => (a: k * 2 * Math.PI / 24, r: k % 2 == 0 ? 92.0 : 80))
                                  .Select(q => P(q.r * Math.Cos(q.a), q.r * Math.Sin(q.a)))), mane, 5);
        p.Fill(Circle(0, 0, 56), Palette.Sun, 5);
        p.Dot(-20, 12, 6).Dot(20, 12, 6);
        p.Fill(Polygon(P(-12, -8), P(12, -8), P(0, -22)), Palette.Brown, 3);
        p.Line(Polyline(P(0, -22), P(0, -30), P(-12, -36)), line: 3).Line(Polyline(P(0, -30), P(12, -36)), line: 3);
        foreach (var x in new[] { -90.0, -54, -18, 18, 54, 90 }) p.Fill(Box(x - 5, -104, 10, 208), Grey, 3);
        p.Fill(Box(-108, 86, 216, 16), Grey, 3);
        return p.Node;
    }

    private static Node2D? MakeRest(string id) => Rest.TryGetValue(id, out var draw) ? draw() : null;

    private static IEnumerable<string> RestIds => Rest.Keys;
}
