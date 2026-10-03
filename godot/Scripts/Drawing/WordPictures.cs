using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Hand-drawn pictures for words whose emoji stand-in a child could name another way (a mug that looks
/// like a cup, a monkey for "tail", coloured squares for "mat" and "rug"). Each one is drawn so the
/// word is the obvious name; a part of something (tail, fin, neck, lid) gets an orange pointing arrow.
/// Same conventions as <see cref="PictureNode"/>: y-up numbers, roughly a 220 × 220 box, fixed seeds.
/// </summary>
public static class WordPictures
{
    public static Node2D? Make(string id) => id switch
    {
        "pic_cup" => Cup(),
        "pic_mug" => Mug(),
        "pic_hill" => Hill(),
        "pic_cliff" => Cliff(),
        "pic_pup" => Pup(),
        "pic_dad" => Dad(),
        "pic_tail" => Tail(),
        "pic_tray" => Tray(),
        "pic_gate" => Gate(),
        "pic_bench" => Bench(),
        "pic_farm" => Farm(),
        "pic_moth" => Moth(),
        "pic_cube" => Cube(),
        "pic_fin" => Fin(),
        "pic_top" => Top(),
        "pic_light" => Light(),
        "pic_desk" => Desk(),
        "pic_cot" => Cot(),
        "pic_lid" => Lid(),
        "pic_neck" => Neck(),
        "pic_mat" => Mat(),
        "pic_rug" => Rug(),
        "pic_mud" => Mud(),
        "pic_plum" => Plum(),
        "pic_belt" => Belt(),
        _ => null,
    };

    public static readonly string[] Ids =
    {
        "pic_cup", "pic_mug", "pic_hill", "pic_cliff", "pic_pup", "pic_dad", "pic_tail", "pic_tray", "pic_gate",
        "pic_bench", "pic_farm", "pic_moth", "pic_cube", "pic_fin", "pic_top", "pic_light", "pic_desk", "pic_cot",
        "pic_lid", "pic_neck", "pic_mat", "pic_rug", "pic_mud", "pic_plum", "pic_belt",
    };

    /// <summary>An orange arrow pointing at <paramref name="tip"/> from <paramref name="from"/>.</summary>
    private static Node2D Pointer(Vector2 from, Vector2 tip, ulong seed)
    {
        var n = new Node2D();
        var dir = (tip - from).Normalized();
        var side = new Vector2(-dir.Y, dir.X);
        var neck = tip - dir * 26;
        n.AddChild(Pen(Polyline(from, neck), seed, ink: Palette.Orange, lineWidth: 9, wobble: 1.5));
        n.AddChild(Pen(Polygon(tip, neck + side * 18, neck - side * 18), seed + 1, fill: Palette.Orange, ink: Palette.Orange, lineWidth: 3, wobble: 1));
        return n;
    }

    /// <summary>A plain box (x, y = bottom-left, y-up). Narrow rounded rects lose their fill, so slats and posts use this.</summary>
    private static SketchShape Box(double x, double y, double w, double h) =>
        Polygon(P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h));

    private static Node2D Ground(double y, Color colour, ulong seed) =>
        Pen(Polyline(P(-110, y), P(110, y)), seed, ink: colour, lineWidth: 7);

    // A teacup on a saucer: small, wide and low, with steam.
    private static Node2D Cup()
    {
        var n = new Node2D();
        n.AddChild(Pen(Ellipse(P(0, -58), 96, 18), 1100, fill: Palette.Ice));
        n.AddChild(Pen(Arc(P(62, -4), 26, 24, -1.4, 1.4), 1101, lineWidth: 8));
        n.AddChild(Pen(Polygon(P(-66, 26), P(66, 26), P(44, -46), P(-44, -46)), 1102, fill: Palette.Teal));
        n.AddChild(Pen(Ellipse(P(0, 26), 66, 12), 1103, fill: Palette.Brown, lineWidth: 4));
        foreach (var (i, x) in Indexed(-24.0, 6.0, 36.0))
            n.AddChild(Pen(Polyline(P(x, 48), P(x + 10, 66), P(x - 4, 84), P(x + 6, 100)), (ulong)(1105 + i), ink: Palette.Stone, lineWidth: 5));
        return n;
    }

    // A tall, straight-sided mug with a big handle and a heart on it.
    private static Node2D Mug()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(40, -40, 66, 84), 30), 1110, lineWidth: 12));
        n.AddChild(Pen(RoundRect(R(-70, -86, 120, 170), 16), 1111, fill: Palette.Orange));
        n.AddChild(Pen(Ellipse(P(-10, 84), 60, 12), 1112, fill: Palette.Brown, lineWidth: 4));
        n.AddChild(Pen(Polygon(P(-10, -30), P(-40, 2), P(-34, 22), P(-18, 26), P(-10, 12), P(-2, 26), P(14, 22), P(20, 2)), 1113, fill: Palette.Red, lineWidth: 4));
        return n;
    }

    // A rounded green hill with a tree on top and the sky behind.
    private static Node2D Hill()
    {
        var n = new Node2D();
        var hump = Enumerable.Range(0, 17).Select(i => {
            var a = (float)i / 16 * Mathf.Pi;
            return P(110 * Mathf.Cos(a), -70 + 120 * Mathf.Sin(a));
        }).ToList();
        n.AddChild(Pen(Polygon(hump), 1120, fill: Palette.Grass));
        n.AddChild(Pen(Polyline(P(0, 46), P(0, 80)), 1121, ink: Palette.Brown, lineWidth: 9));
        n.AddChild(Pen(Ellipse(P(0, 96), 30, 26), 1122, fill: Palette.Leaf));
        n.AddChild(Pen(Polyline(P(-70, -40), P(-40, -30), P(-10, -42)), 1123, ink: Palette.Leaf, lineWidth: 4));
        n.AddChild(Pen(Polyline(P(20, 0), P(50, 10), P(80, -4)), 1124, ink: Palette.Leaf, lineWidth: 4));
        return n;
    }

    // A steep rock face dropping straight into the sea.
    private static Node2D Cliff()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polygon(P(-110, -100), P(110, -100), P(110, -50), P(-110, -50)), 1130, fill: Palette.Sea));
        n.AddChild(Pen(Polygon(P(-110, -100), P(10, -100), P(4, -40), P(20, 10), P(8, 50), P(18, 100), P(-110, 100)), 1131, fill: Palette.Stone));
        n.AddChild(Pen(Polygon(P(-110, 100), P(18, 100), P(14, 84), P(-110, 80)), 1132, fill: Palette.Grass, lineWidth: 4));
        foreach (var (i, (x, y)) in Indexed((-60.0, 20.0), (-20.0, -30.0), (-70.0, -60.0)))
            n.AddChild(Pen(Polyline(P(x, y), P(x + 26, y - 8)), (ulong)(1133 + i), ink: Palette.Ink.WithAlpha(0.6), lineWidth: 4));
        foreach (var (i, x) in Indexed(40.0, 80.0))
            n.AddChild(Pen(Arc(P(x, -64), 14, 8, 0.2, 2.9), (ulong)(1136 + i), ink: Palette.White, lineWidth: 4));
        return n;
    }

    // A tiny puppy: big head, floppy ears, short legs, a ball beside it.
    private static Node2D Pup()
    {
        var n = new Node2D();
        n.AddChild(Pen(Ellipse(P(78, -62), 22, 22), 1140, fill: Palette.Red));
        n.AddChild(Pen(Ellipse(P(-6, -40), 54, 36), 1141, fill: Palette.LightBrown));
        foreach (var (i, x) in Indexed(-40.0, -14.0, 14.0, 34.0))
            n.AddChild(Pen(Box(x - 8, -90, 18, 36), (ulong)(1142 + i), fill: Palette.LightBrown, lineWidth: 4));
        n.AddChild(Pen(Polyline(P(-56, -30), P(-78, -10), P(-84, 8)), 1146, ink: Palette.Ink, lineWidth: 6));
        n.AddChild(Pen(Ellipse(P(10, 34), 54, 48), 1147, fill: Palette.LightBrown));
        n.AddChild(Pen(Ellipse(P(-40, 30), 18, 36), 1148, fill: Palette.Brown, lineWidth: 4));
        n.AddChild(Pen(Ellipse(P(60, 30), 18, 36), 1149, fill: Palette.Brown, lineWidth: 4));
        n.AddChild(Dot(-6, 42, 6));
        n.AddChild(Dot(26, 42, 6));
        n.AddChild(Pen(Ellipse(P(10, 18), 12, 9), 1150, fill: Palette.Ink, lineWidth: 2));
        n.AddChild(Pen(Ellipse(P(10, 0), 8, 10), 1151, fill: Palette.Pink, lineWidth: 3));
        return n;
    }

    // A dad holding his child's hand.
    private static Node2D Dad()
    {
        var n = new Node2D();
        // Dad: tall, short hair, smiling.
        n.AddChild(Pen(Polyline(P(-46, -40), P(-56, -104)), 1160, lineWidth: 9));
        n.AddChild(Pen(Polyline(P(-26, -40), P(-16, -104)), 1161, lineWidth: 9));
        n.AddChild(Pen(RoundRect(R(-72, -44, 72, 100), 18), 1162, fill: Palette.Teal));
        n.AddChild(Pen(Polyline(P(-4, 40), P(26, -8)), 1163, lineWidth: 8));
        n.AddChild(Pen(Ellipse(P(-36, 84), 30, 32), 1164, fill: Palette.Sand));
        n.AddChild(Pen(Polygon(P(-66, 92), P(-60, 116), P(-12, 116), P(-6, 92), P(-36, 104)), 1166, fill: Palette.Brown, lineWidth: 4));
        n.AddChild(Dot(-46, 88, 4));
        n.AddChild(Dot(-26, 88, 4));
        n.AddChild(Pen(Arc(P(-36, 80), 12, 8, 3.5, 5.9), 1165, lineWidth: 4));
        // The child: small, holding Dad's hand.
        n.AddChild(Pen(Polyline(P(52, -60), P(48, -104)), 1167, lineWidth: 7));
        n.AddChild(Pen(Polyline(P(70, -60), P(74, -104)), 1168, lineWidth: 7));
        n.AddChild(Pen(Box(40, -64, 42, 54), 1169, fill: Palette.Sun));
        n.AddChild(Pen(Polyline(P(42, -20), P(26, -8)), 1170, lineWidth: 7));
        n.AddChild(Pen(Ellipse(P(61, 6), 22, 22), 1171, fill: Palette.Sand));
        n.AddChild(Dot(54, 10, 3.5));
        n.AddChild(Dot(68, 10, 3.5));
        return n;
    }

    // A cat seen from the side with a long curly tail, and an arrow at the tail.
    private static Node2D Tail()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(P(40, -10), P(80, 10), P(96, 50), P(80, 80), P(60, 74)), 1180, ink: Palette.Orange.Blend(0.3, Palette.Brown), lineWidth: 16, wobble: 1.5));
        foreach (var (i, x) in Indexed(-60.0, -34.0, 14.0, 36.0))
            n.AddChild(Pen(Box(x - 7, -80, 16, 50), (ulong)(1181 + i), fill: Palette.Orange, lineWidth: 4));
        n.AddChild(Pen(Ellipse(P(-12, -18), 62, 34), 1185, fill: Palette.Orange));
        n.AddChild(Pen(Ellipse(P(-74, 18), 30, 28), 1186, fill: Palette.Orange));
        n.AddChild(Pen(Polygon(P(-96, 34), P(-92, 62), P(-76, 42)), 1187, fill: Palette.Orange, lineWidth: 4));
        n.AddChild(Pen(Polygon(P(-66, 42), P(-52, 62), P(-50, 34)), 1188, fill: Palette.Orange, lineWidth: 4));
        n.AddChild(Dot(-82, 22, 4));
        n.AddChild(Pointer(P(-10, 96), P(64, 84), 1189));
        return n;
    }

    // A flat tray with handles, carrying a cup and a plate.
    private static Node2D Tray()
    {
        var n = new Node2D();
        n.AddChild(Pen(Ellipse(P(-36, -8), 34, 10), 1190, fill: Palette.White, lineWidth: 4));
        n.AddChild(Pen(Polygon(P(20, -10), P(64, -10), P(58, 30), P(26, 30)), 1191, fill: Palette.Teal, lineWidth: 4));
        n.AddChild(Pen(Arc(P(66, 12), 12, 12, -1.4, 1.4), 1192, lineWidth: 5));
        n.AddChild(Pen(Polygon(P(-110, -16), P(110, -16), P(94, -46), P(-94, -46)), 1193, fill: Palette.Brown));
        n.AddChild(Pen(Box(-112, -36, 24, 14), 1194, fill: Palette.LightBrown, lineWidth: 4));
        n.AddChild(Pen(Box(88, -36, 24, 14), 1195, fill: Palette.LightBrown, lineWidth: 4));
        return n;
    }

    // A wooden garden gate with bars and a cross brace, set in a fence.
    private static Node2D Gate()
    {
        var n = new Node2D();
        n.AddChild(Ground(-90, Palette.Grass, 1200));
        n.AddChild(Pen(Box(-110, -88, 22, 170), 1201, fill: Palette.Brown, lineWidth: 4));
        n.AddChild(Pen(Box(88, -88, 22, 170), 1202, fill: Palette.Brown, lineWidth: 4));
        foreach (var (i, y) in Indexed(-60.0, -10.0, 40.0))
            n.AddChild(Pen(Box(-84, y, 168, 16), (ulong)(1203 + i), fill: Palette.LightBrown, lineWidth: 4));
        n.AddChild(Pen(Polyline(P(-78, -56), P(78, 50)), 1206, ink: Palette.LightBrown.Blend(0.3, Palette.Brown), lineWidth: 12));
        n.AddChild(Pen(Box(-84, -60, 14, 116), 1207, fill: Palette.LightBrown, lineWidth: 4));
        n.AddChild(Pen(Box(70, -60, 14, 116), 1208, fill: Palette.LightBrown, lineWidth: 4));
        n.AddChild(Pen(Ellipse(P(62, -2), 7, 7), 1209, fill: Palette.Stone, lineWidth: 3));
        return n;
    }

    // A long park bench with slats, a back and iron legs.
    private static Node2D Bench()
    {
        var n = new Node2D();
        n.AddChild(Ground(-80, Palette.Grass, 1210));
        foreach (var (i, x) in Indexed(-86.0, 62.0))
        {
            n.AddChild(Pen(Box(x, -78, 24, 64), (ulong)(1211 + 2 * i), fill: Palette.Ink.WithAlpha(0.85), lineWidth: 4));
            n.AddChild(Pen(Box(x + 4, 6, 16, 56), (ulong)(1212 + 2 * i), fill: Palette.Ink.WithAlpha(0.85), lineWidth: 4));
        }
        n.AddChild(Pen(Box(-110, -18, 220, 28), 1215, fill: Palette.Orange));
        foreach (var (i, y) in Indexed(26.0, 60.0))
            n.AddChild(Pen(Box(-104, y, 208, 26), (ulong)(1217 + i), fill: Palette.Orange));
        return n;
    }

    // A red barn, a silo and a field with a fence: a whole farm.
    private static Node2D Farm()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polygon(P(-110, -100), P(110, -100), P(110, -50), P(-110, -50)), 1220, fill: Palette.Grass));
        n.AddChild(Pen(Box(60, -60, 40, 130), 1221, fill: Palette.Stone));
        n.AddChild(Pen(Polygon(P(-80, -60), P(40, -60), P(40, 30), P(-20, 74), P(-80, 30)), 1222, fill: Palette.Red));
        n.AddChild(Pen(Box(-40, -60, 40, 54), 1223, fill: Palette.White, lineWidth: 4));
        n.AddChild(Pen(Polyline(P(-40, -60), P(0, -6)), 1224, ink: Palette.Red, lineWidth: 4));
        n.AddChild(Pen(Polyline(P(0, -60), P(-40, -6)), 1225, ink: Palette.Red, lineWidth: 4));
        n.AddChild(Pen(Ellipse(P(-20, 30), 12, 12), 1226, fill: Palette.White, lineWidth: 4));
        foreach (var (i, x) in Indexed(-104.0, -64.0, -24.0, 16.0, 56.0, 96.0))
            n.AddChild(Pen(Polyline(P(x, -100), P(x, -74)), (ulong)(1227 + i), ink: Palette.Brown, lineWidth: 5));
        n.AddChild(Pen(Polyline(P(-108, -86), P(108, -86)), 1233, ink: Palette.Brown, lineWidth: 5));
        return n;
    }

    // A moth: soft grey-brown wings with eye spots and feathery antennae.
    private static Node2D Moth()
    {
        var n = new Node2D();
        var wing = Palette.LightBrown.Blend(0.35, Palette.Stone);
        n.AddChild(Pen(Polygon(P(-6, 20), P(-60, 80), P(-106, 60), P(-96, 6), P(-10, -4)), 1240, fill: wing));
        n.AddChild(Pen(Polygon(P(6, 20), P(60, 80), P(106, 60), P(96, 6), P(10, -4)), 1241, fill: wing));
        n.AddChild(Pen(Polygon(P(-8, -4), P(-80, -20), P(-70, -70), P(-12, -30)), 1242, fill: wing.Darkened(0.12f)));
        n.AddChild(Pen(Polygon(P(8, -4), P(80, -20), P(70, -70), P(12, -30)), 1243, fill: wing.Darkened(0.12f)));
        n.AddChild(Pen(Ellipse(P(-60, 40), 12, 12), 1244, fill: Palette.Card, lineWidth: 3));
        n.AddChild(Pen(Ellipse(P(60, 40), 12, 12), 1245, fill: Palette.Card, lineWidth: 3));
        n.AddChild(Dot(-60, 40, 5));
        n.AddChild(Dot(60, 40, 5));
        n.AddChild(Pen(Ellipse(P(0, 4), 12, 44), 1246, fill: Palette.Brown));
        foreach (var (i, s) in Indexed(-1.0, 1.0))
        {
            n.AddChild(Pen(Polyline(P(4 * s, 46), P(26 * s, 80), P(40 * s, 100)), (ulong)(1247 + i), lineWidth: 4));
            for (var j = 0; j < 4; j++)
            {
                var y = 60 + j * 10;
                var x = (8 + (y - 46) * 0.62) * s;
                n.AddChild(Pen(Polyline(P(x, y), P(x + 12 * s, y - 6)), (ulong)(1250 + 4 * i + j), lineWidth: 2.5, wobble: 0.8));
            }
        }
        return n;
    }

    // A solid cube drawn in three tones so it reads as a 3D shape.
    private static Node2D Cube()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polygon(P(-80, 40), P(0, 80), P(80, 40), P(0, 0)), 1260, fill: Palette.Sun.Lightened(0.25f)));
        n.AddChild(Pen(Polygon(P(-80, 40), P(0, 0), P(0, -96), P(-80, -56)), 1261, fill: Palette.Sun));
        n.AddChild(Pen(Polygon(P(80, 40), P(0, 0), P(0, -96), P(80, -56)), 1262, fill: Palette.Orange));
        return n;
    }

    // A fish with a big back fin, and an arrow at the fin.
    private static Node2D Fin()
    {
        var n = new Node2D();
        var dark = Palette.Sea.Darkened(0.25f);
        n.AddChild(Pen(Polygon(P(-40, 32), P(-30, 62), P(-6, 86), P(4, 80), P(10, 50), P(30, 30)), 1270, fill: dark));
        n.AddChild(Pen(Polygon(P(64, -6), P(108, 34), P(108, -46)), 1271, fill: dark));
        n.AddChild(Pen(Ellipse(P(-6, -6), 80, 46), 1272, fill: Palette.Sea));
        n.AddChild(Pen(Ellipse(P(-56, 4), 10, 10), 1273, fill: Palette.White, lineWidth: 3));
        n.AddChild(Dot(-54, 4, 4));
        n.AddChild(Pen(Arc(P(-74, -22), 10, 6, 3.6, 5.6), 1274, lineWidth: 4));
        n.AddChild(Pointer(P(-96, 100), P(-30, 76), 1275));
        return n;
    }

    // A wooden spinning top on its point, with spin lines.
    private static Node2D Top()
    {
        var n = new Node2D();
        n.AddChild(Ground(-96, Palette.Stone, 1280));
        n.AddChild(Pen(Box(-8, 50, 16, 40), 1281, fill: Palette.Brown, lineWidth: 4));
        n.AddChild(Pen(Polygon(P(-84, 40), P(84, 40), P(0, -92)), 1282, fill: Palette.Red));
        n.AddChild(Pen(Ellipse(P(0, 44), 86, 18), 1283, fill: Palette.Sun));
        n.AddChild(Pen(Polyline(P(-56, -4), P(56, -4)), 1284, ink: Palette.Sun, lineWidth: 8));
        foreach (var (i, s) in Indexed(-1.0, 1.0))
            n.AddChild(Pen(Arc(P(0, 10), 106, 50, s < 0 ? 2.4 : -0.6, s < 0 ? 3.0 : 0.0), (ulong)(1285 + i), ink: Palette.Ink.WithAlpha(0.5), lineWidth: 4));
        return n;
    }

    // A glowing light bulb.
    private static Node2D Light()
    {
        var n = new Node2D();
        for (var i = 0; i < 7; i++)
        {
            var a = Mathf.Pi * (0.1f + 0.8f * i / 6);
            n.AddChild(Pen(Polyline(P(86 * Mathf.Cos(a), 30 + 86 * Mathf.Sin(a)), P(108 * Mathf.Cos(a), 30 + 108 * Mathf.Sin(a))),
                           (ulong)(1290 + i), ink: Palette.Orange, lineWidth: 6));
        }
        n.AddChild(Pen(Box(-26, -96, 52, 50), 1297, fill: Palette.Stone));
        foreach (var (i, y) in Indexed(-84.0, -70.0, -56.0))
            n.AddChild(Pen(Polyline(P(-26, y), P(26, y + 4)), (ulong)(1298 + i), lineWidth: 3));
        n.AddChild(Pen(Polygon(Enumerable.Range(0, 21).Select(i => {
            var a = -0.25f * Mathf.Pi + (float)i / 20 * 1.5f * Mathf.Pi;
            return P(64 * Mathf.Cos(a), 30 + 64 * Mathf.Sin(a));
        }).Concat(new[] { P(-28, -46), P(28, -46) })), 1301, fill: Palette.Sun));
        n.AddChild(Pen(Polyline(P(-14, -40), P(-14, 10), P(-4, 24), P(4, 10), P(14, 24), P(14, -40)), 1302, ink: Palette.Orange, lineWidth: 4));
        return n;
    }

    // A school desk with a drawer, a book and a pencil on top.
    private static Node2D Desk()
    {
        var n = new Node2D();
        n.AddChild(Pen(Box(-96, -96, 16, 112), 1310, fill: Palette.Brown, lineWidth: 4));
        n.AddChild(Pen(Box(80, -96, 16, 112), 1311, fill: Palette.Brown, lineWidth: 4));
        n.AddChild(Pen(Box(14, -30, 66, 40), 1312, fill: Palette.LightBrown, lineWidth: 4));
        n.AddChild(Pen(Box(36, -14, 22, 8), 1313, fill: Palette.Brown, lineWidth: 3));
        n.AddChild(Pen(Box(-110, 10, 220, 20), 1314, fill: Palette.LightBrown));
        n.AddChild(Pen(Polygon(P(-80, 30), P(-12, 30), P(-20, 52), P(-88, 52)), 1315, fill: Palette.Red, lineWidth: 4));
        n.AddChild(Pen(Polyline(P(-82, 40), P(-16, 40)), 1316, ink: Palette.White, lineWidth: 4));
        n.AddChild(Pen(Polygon(P(20, 36), P(84, 36), P(96, 42), P(84, 48), P(20, 48)), 1317, fill: Palette.Sun, lineWidth: 3.5));
        return n;
    }

    // A baby's cot with bars, a pillow and a teddy.
    private static Node2D Cot()
    {
        var n = new Node2D();
        n.AddChild(Pen(Box(-94, -40, 188, 30), 1320, fill: Palette.EggBlue));
        n.AddChild(Pen(Ellipse(P(-56, -4), 26, 14), 1321, fill: Palette.White, lineWidth: 4));
        n.AddChild(Pen(Ellipse(P(30, 4), 22, 20), 1322, fill: Palette.LightBrown, lineWidth: 4));
        n.AddChild(Pen(Ellipse(P(30, 30), 16, 15), 1323, fill: Palette.LightBrown, lineWidth: 4));
        n.AddChild(Dot(24, 32, 3));
        n.AddChild(Dot(36, 32, 3));
        foreach (var (i, x) in Indexed(-104.0, 92.0))
            n.AddChild(Pen(Box(x, -100, 12, 176), (ulong)(1324 + i), fill: Palette.Pink, lineWidth: 4));
        n.AddChild(Pen(Polyline(P(-98, 50), P(98, 50)), 1326, lineWidth: 6));
        n.AddChild(Pen(Polyline(P(-98, -46), P(98, -46)), 1327, lineWidth: 6));
        for (var i = 0; i < 9; i++)
        {
            var x = -78 + i * 19.5;
            n.AddChild(Pen(Polyline(P(x, -46), P(x, 50)), (ulong)(1328 + i), ink: Palette.Ink.WithAlpha(0.75), lineWidth: 4, wobble: 1));
        }
        return n;
    }

    // A pot with its lid lifted off, and an arrow at the lid.
    private static Node2D Lid()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-70, -100, 140, 84), 14), 1340, fill: Palette.Red));
        n.AddChild(Pen(Box(-94, -40, 28, 18), 1341, fill: Palette.Ink.WithAlpha(0.8), lineWidth: 4));
        n.AddChild(Pen(Box(66, -40, 28, 18), 1342, fill: Palette.Ink.WithAlpha(0.8), lineWidth: 4));
        // The lid lifted off the pot: a shallow dome with a knob.
        n.AddChild(Pen(Box(-14, 62, 28, 22), 1345, fill: Palette.Ink.WithAlpha(0.8), lineWidth: 4));
        n.AddChild(Pen(Polygon(Enumerable.Range(0, 13).Select(i => {
            var a = (float)i / 12 * Mathf.Pi;
            return P(80 * Mathf.Cos(a), 20 + 44 * Mathf.Sin(a));
        })), 1344, fill: Palette.Stone));
        n.AddChild(Pointer(P(-104, 100), P(-56, 60), 1346));
        return n;
    }

    // A giraffe's long neck and head, with an arrow at the neck.
    private static Node2D Neck()
    {
        var n = new Node2D();
        var neck = Pen(Polygon(P(-40, -110), P(30, -110), P(42, 40), P(14, 40)), 1350, fill: Palette.Sun);
        n.AddChild(neck);
        foreach (var (i, (x, y)) in Indexed((-14.0, -80.0), (12.0, -46.0), (-4.0, -10.0), (24.0, 14.0)))
            n.AddChild(Pen(Ellipse(P(x, y), 9, 8), (ulong)(1351 + i), fill: Palette.Orange, lineWidth: 2.5));
        n.AddChild(Pen(Polyline(P(18, 72), P(14, 96)), 1355, lineWidth: 5));
        n.AddChild(Pen(Polyline(P(42, 72), P(48, 96)), 1356, lineWidth: 5));
        n.AddChild(Pen(Ellipse(P(44, 54), 46, 24), 1357, fill: Palette.Sun));
        n.AddChild(Dot(46, 62, 5));
        n.AddChild(Dot(84, 50, 3));
        n.AddChild(Pointer(P(-104, 6), P(-30, -20), 1358));
        return n;
    }

    // A bristly doormat in front of a door, with a pair of boots on it.
    private static Node2D Mat()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-54, -40, 108, 140), 6), 1360, fill: Palette.Teal));
        n.AddChild(Pen(Ellipse(P(34, 30), 7, 7), 1361, fill: Palette.Sun, lineWidth: 3));
        n.AddChild(Pen(Polygon(P(-100, -100), P(100, -100), P(80, -46), P(-80, -46)), 1362, fill: Palette.LightBrown));
        for (var i = 0; i < 10; i++)
        {
            var x = -78 + i * 17.3;
            n.AddChild(Pen(Polyline(P(x - 6, -92), P(x, -54)), (ulong)(1363 + i), ink: Palette.Brown, lineWidth: 3, wobble: 1));
        }
        return n;
    }

    // A round patterned rug with a fringe, on the floor.
    private static Node2D Rug()
    {
        var n = new Node2D();
        for (var i = 0; i < 16; i++)
        {
            var a = (float)i / 16 * 2 * Mathf.Pi;
            n.AddChild(Pen(Polyline(P(96 * Mathf.Cos(a), 64 * Mathf.Sin(a)), P(112 * Mathf.Cos(a), 74 * Mathf.Sin(a))),
                           (ulong)(1380 + i), ink: Palette.Sun, lineWidth: 4, wobble: 1));
        }
        n.AddChild(Pen(Ellipse(Vector2.Zero, 100, 66), 1396, fill: Palette.Red));
        n.AddChild(Pen(Ellipse(Vector2.Zero, 72, 46), 1397, fill: Palette.Sun, lineWidth: 4));
        n.AddChild(Pen(Ellipse(Vector2.Zero, 44, 26), 1398, fill: Palette.Teal, lineWidth: 4));
        n.AddChild(Pen(Polygon(Star(Vector2.Zero, 14)), 1399, fill: Palette.White, lineWidth: 3));
        return n;
    }

    // A squelchy brown puddle with splashes and a boot print.
    private static Node2D Mud()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polygon(P(-100, -20), P(-80, -60), P(-20, -70), P(40, -60), P(96, -40), P(104, 0),
                               P(70, 30), P(10, 40), P(-50, 30), P(-96, 14)), 1400, fill: Palette.Brown));
        n.AddChild(Pen(Ellipse(P(-30, -16), 22, 12), 1401, fill: Palette.Brown.Darkened(0.25f), lineWidth: 3));
        n.AddChild(Pen(Ellipse(P(30, -26), 22, 12), 1402, fill: Palette.Brown.Darkened(0.25f), lineWidth: 3));
        foreach (var (i, (x, y, r)) in Indexed((-70.0, 64.0, 10.0), (-20.0, 86.0, 14.0), (40.0, 74.0, 11.0), (84.0, 52.0, 8.0)))
            n.AddChild(Pen(Ellipse(P(x, y), r, r), (ulong)(1403 + i), fill: Palette.Brown, lineWidth: 3.5));
        return n;
    }

    // A plum: a dark purple oval with a crease, a short stalk and a leaf.
    private static Node2D Plum()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(P(4, 60), P(10, 92)), 1410, ink: Palette.Brown, lineWidth: 8));
        var leaf = Pen(Ellipse(Vector2.Zero, 30, 12), 1411, fill: Palette.Leaf, lineWidth: 4);
        leaf.Position = P(38, 86);
        leaf.Rotation = Turn(0.4);
        n.AddChild(leaf);
        n.AddChild(Pen(Ellipse(P(0, -6), 70, 80), 1412, fill: Palette.Purple.Darkened(0.15f)));
        n.AddChild(Pen(Arc(P(-30, -6), 36, 66, -1.2, 1.2), 1413, ink: Palette.Ink.WithAlpha(0.45), lineWidth: 4));
        var shine = Oval(16, 30, Palette.White.WithAlpha(0.45));
        shine.Position = P(26, 20);
        n.AddChild(shine);
        return n;
    }

    // A leather belt with a buckle, curled into a loop.
    private static Node2D Belt()
    {
        var n = new Node2D();
        n.AddChild(Pen(Arc(P(0, 0), 92, 62, 0.5, 2 * Mathf.Pi - 0.15), 1420, ink: Palette.Brown, lineWidth: 26, wobble: 1));
        n.AddChild(Pen(Arc(P(0, 0), 92, 62, 0.5, 2 * Mathf.Pi - 0.15), 1421, ink: Palette.LightBrown, lineWidth: 3, wobble: 1));
        foreach (var (i, a) in Indexed(2.2, 2.7, 3.2))
            n.AddChild(Dot(92 * Mathf.Cos((float)a), 62 * Mathf.Sin((float)a), 4));
        n.AddChild(Pen(Box(62, -4, 44, 58), 1422, ink: Palette.Sun.Darkened(0.2f), lineWidth: 9, wobble: 1));
        n.AddChild(Pen(Polyline(P(84, 2), P(84, 48)), 1423, ink: Palette.Sun.Darkened(0.3f), lineWidth: 5));
        return n;
    }
}
