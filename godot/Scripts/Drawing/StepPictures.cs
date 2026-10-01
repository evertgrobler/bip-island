using System;
using System.Linq;
using Godot;
using static BipIsland.Drawing.Up;

namespace BipIsland.Drawing;

/// <summary>
/// Pictures for the Morning Order steps (pic_&lt;set&gt;_&lt;n&gt;), ported from the Swift app's StepPictures
/// with the same numbers and seeds (Swift y-up, flipped by <see cref="Up"/>). A single emoji can't show
/// a step, so each card is a small scene: a hand-drawn ground the whole set shares (soil, sand, a pond)
/// with emoji and drawn pieces on top. Within a set the pictures build on each other, so a child can
/// see what changes from one step to the next. Each fits in roughly a 220 × 220 box around the origin.
/// </summary>
public static class StepPictures
{
    public static Node2D? Make(string id)
    {
        switch (id)
        {
        // Morning
        case "pic_morning_1": return Scene(Glyph("🛌", 120, -5, -40), Glyph("⏰", 72, -60, 62), Glyph("☀️", 62, 62, 66));
        case "pic_morning_2": return Scene(Glyph("😁", 120, -20, 12), Glyph("🪥", 84, 58, -48, turn: 0.4));
        case "pic_morning_3": case "pic_school_2": return Breakfast();

        // Washing hands
        case "pic_hands_1": return Scene(Tap(), Drops((14, -4)), Glyph("👐", 110, 6, -58));
        case "pic_hands_2": return Scene(Glyph("🧼", 76, -52, 58), Glyph("👐", 110, 8, -48), Bubbles((62, 52), (78, 6), (-70, -20)));
        case "pic_hands_3": return Scene(Tap(), Drops((14, -4), (-30, -22), (44, -24)), Glyph("👐", 110, 6, -58),
                                         Bubbles((-80, -84), (82, -80), (-84, -40)));
        case "pic_hands_4": return Scene(Glyph("🧻", 80, -56, 52), Glyph("🙌", 110, 14, -40), Glyph("✨", 46, 72, 58));

        // Sandwich: the same bread, a little more on it each time.
        case "pic_sandwich_1": return Scene(Slice(at: P(-54, 0), scale: 0.62), Slice(at: P(54, 0), scale: 0.62));
        case "pic_sandwich_2": return Scene(Slice(at: P(0, -4), scale: 1), Butter(), Glyph("🧈", 52, 72, -72));
        case "pic_sandwich_3": return Scene(Slice(at: P(0, -4), scale: 1), Butter(), Cheese());
        case "pic_sandwich_4": return Scene(Glyph("🥪", 160, 0, 0));

        // Planting a seed: the same patch of soil each time.
        case "pic_seed_1": return Scene(Soil(), SeedInSoil(), Glyph("👇", 72, 0, 50));
        case "pic_seed_2": return Scene(Soil(), SeedInSoil(), Glyph("🌧️", 120, 0, 36));
        case "pic_seed_3": return Scene(Soil(), Glyph("🌱", 90, 0, -18));
        case "pic_seed_4": return Scene(Soil(), Glyph("🌻", 130, 0, 6));

        // Sandcastle: one bucket on one beach.
        case "pic_sandcastle_1": return Scene(Sand(), Glyph("🪣", 110, 14, -16), Glyph("🤲", 64, -62, 64), SandGrains());
        case "pic_sandcastle_2": return Scene(Sand(), Glyph("🪣", 110, 0, -16), Glyph("✋", 72, 4, 74, turn: Mathf.Pi));
        case "pic_sandcastle_3": return Scene(Sand(), Glyph("🪣", 110, 0, -18, turn: Mathf.Pi), TurnArrow());
        case "pic_sandcastle_4": return Scene(Sand(), SandTower(), Glyph("🪣", 66, 64, 76, turn: 0.3), UpArrow());
        case "pic_sandcastle_5": return Scene(Sand(), SandTower(), Glyph("🐚", 56, 0, 56), Glyph("🚩", 40, -60, 64));

        // Frog life cycle: drawn by hand, in the same pond.
        case "pic_frog_1": return Scene(Pond(), Frogspawn());
        case "pic_frog_2": return Scene(Pond(), Tadpole(legs: false));
        case "pic_frog_3": return Scene(Pond(), Tadpole(legs: true));
        case "pic_frog_4": return Scene(Pond(), Frog(tail: true, scale: 0.7));
        case "pic_frog_5": return Scene(Pond(), LilyPad(), Frog(tail: false, scale: 1));

        // Getting ready for school
        case "pic_school_1": return Scene(Glyph("👕", 100, -40, 34), Glyph("👖", 100, 44, -30));
        case "pic_school_3": return Scene(Glyph("🎒", 130, 10, -24), Glyph("📚", 62, -66, 60), Glyph("✏️", 52, 64, 70, turn: 0.3));
        case "pic_school_4": return Scene(Glyph("🧦", 64, -64, 58), Glyph("👟", 92, -40, -30), Glyph("👟", 92, 50, -10));
        case "pic_school_5": return Scene(Path(), Glyph("🏫", 110, -40, 32), Glyph("🚶", 92, 60, -36));

        // Baking biscuits
        case "pic_biscuits_1": return Scene(Glyph("🧼", 72, -56, 58), Glyph("👐", 110, 8, -42), Drops((62, 64), (74, 28)));
        case "pic_biscuits_2": return Scene(Glyph("🥣", 130, 0, -28), Glyph("🥄", 70, 46, 54, turn: -0.5), Glyph("🥚", 52, -60, 60));
        case "pic_biscuits_3": return Scene(DoughWithShapes());
        case "pic_biscuits_4": return Scene(Oven());
        case "pic_biscuits_5": return Scene(Tray(), Steam(), Glyph("🍪", 56, -54, -40), Glyph("🍪", 56, 0, -40), Glyph("🍪", 56, 54, -40),
                                            Glyph("🌬️", 66, -56, 62));
        case "pic_biscuits_6": return Scene(Glyph("😋", 116, -26, 22), Glyph("🍪", 76, 56, -48));
        // Bedtime
        case "pic_bedtime_1": return Scene(Glyph("😁", 120, -20, 12), Glyph("🪥", 84, 58, -48, turn: 0.4));
        case "pic_bedtime_2": return Scene(Glyph("📖", 120, 10, -24), Glyph("🧸", 72, -62, 56));
        case "pic_bedtime_3": return Scene(LightSwitch(), Glyph("👆", 66, 62, -56), Glyph("🌙", 56, -64, 66));
        case "pic_bedtime_4": return Scene(Glyph("🛌", 124, 0, -34), Glyph("💤", 58, 52, 60), Glyph("🌙", 54, -62, 64));

        // Bath time: the same bath, tap at the corner.
        case "pic_bath_1": return Scene(Glyph("🛁", 130, 0, -46), Drops((-34, 66), (-6, 44), (22, 70)));
        case "pic_bath_2": return Scene(Glyph("🧒", 70, -16, 6), Glyph("🛁", 130, 0, -36), Glyph("🦆", 40, 46, -4));
        case "pic_bath_3": return Scene(Glyph("🧒", 70, -16, 6), Glyph("🛁", 130, 0, -36), Glyph("🧼", 50, 62, 60),
                                        Bubbles((-70, 40), (30, 30), (-40, 64)));
        case "pic_bath_4": return Scene(Glyph("🛁", 130, 0, -36), Glyph("🌀", 54, 0, 62), Glyph("🦆", 40, -70, -80));

        // Chick hatching
        case "pic_chick_1": return Scene(Glyph("🪺", 140, 0, -8));
        case "pic_chick_2": return Scene(Glyph("🥚", 130, 0, -4), Crack());
        case "pic_chick_3": return Scene(Glyph("🐣", 140, 0, -4));
        case "pic_chick_4": return Scene(Glyph("🐔", 130, 10, 6), Glyph("🌾", 56, -68, -64));

        // Butterfly life cycle
        case "pic_butterfly_1": return Scene(Leaf(at: P(0, -4), scale: 1), TinyEggs());
        case "pic_butterfly_2": return Scene(Leaf(at: P(0, -30), scale: 0.8), Glyph("🐛", 84, 6, 30));
        case "pic_butterfly_3": return Scene(Chrysalis());
        case "pic_butterfly_4": return Scene(Glyph("🦋", 120, 0, 18), Glyph("🌸", 56, -62, -64), Glyph("🌼", 50, 62, -66));

        // Weather: the same puddle fills, then a rainbow.
        case "pic_rainbow_1": return Scene(Glyph("☁️", 104, -38, 34), Glyph("☁️", 84, 48, -14));
        case "pic_rainbow_2": return Scene(Puddle(), Glyph("🌧️", 124, 0, 24));
        case "pic_rainbow_3": return Scene(Puddle(), Glyph("🌦️", 124, 0, 24));
        case "pic_rainbow_4": return Scene(Puddle(), Glyph("🌈", 150, 0, 22));

        // Birthday
        case "pic_birthday_1": return Scene(Oven());
        case "pic_birthday_2": return Scene(Glyph("🎂", 140, 0, -18), Glyph("🕯️", 52, 66, 64));
        case "pic_birthday_3": return Scene(Glyph("🥳", 76, -54, 54), Glyph("🎂", 110, 10, -34), Glyph("🎶", 52, 64, 62));
        case "pic_birthday_4": return Scene(Glyph("🎂", 110, 26, -34), Glyph("🌬️", 70, -60, 52), Glyph("💨", 46, -6, 34));

        // Walking the dog
        case "pic_dogwalk_1": return Scene(Lead(), Glyph("🐕", 124, 10, -24));
        case "pic_dogwalk_2": return Scene(Path(), Glyph("🌳", 96, 54, 42), Glyph("🚶", 84, -52, -10), Glyph("🐕", 66, 14, -52));
        case "pic_dogwalk_3": return Scene(ThrowArc(), Ball(), Glyph("🐕", 104, -30, -40));
        case "pic_dogwalk_4": return Scene(Glyph("🐕", 104, -34, 16), WaterBowl(), Drops((56, -20)));

        // Ice cream on a hot day
        case "pic_icecream_1": return Scene(Glyph("🍦", 150, 0, 0));
        case "pic_icecream_2": return Scene(Glyph("☀️", 80, -56, 60), Glyph("🍦", 124, 24, -20), CreamDrips());
        case "pic_icecream_3": return Scene(Glyph("☀️", 80, -56, 60), MeltedIceCream());

        // Pizza: the same base, a little more on it each time.
        case "pic_pizza_1": return Scene(PizzaBase(), RollingPin());
        case "pic_pizza_2": return Scene(PizzaBase(), Sauce());
        case "pic_pizza_3": return Scene(PizzaBase(), Sauce(), CheeseBits());
        case "pic_pizza_4": return Scene(Glyph("🍕", 130, 10, -20), Glyph("😋", 64, -60, 60));

        // Building a tower
        case "pic_tower_1": return Scene(Floor(), Block(0, -66, Palette.Red, seed: 1400));
        case "pic_tower_2": return Scene(Floor(), Block(0, -66, Palette.Red, seed: 1400), Block(0, -2, Palette.Sea, seed: 1401));
        case "pic_tower_3": return Scene(Floor(), Block(0, -66, Palette.Red, seed: 1400), Block(0, -2, Palette.Sea, seed: 1401),
                                         Block(0, 62, Palette.Sun, seed: 1402));
        case "pic_tower_4": return Scene(Floor(), Block(-58, -66, Palette.Red, seed: 1400, turn: 0.2), Block(12, -70, Palette.Sea, seed: 1401, turn: -0.5),
                                         Block(74, -60, Palette.Sun, seed: 1402, turn: 0.9), Glyph("💥", 50, -10, 30));

        // Apple tree: the same tree through the seasons.
        case "pic_appletree_1": return Scene(Glyph("🌳", 170, 0, 6), Glyph("🌸", 36, -30, 40), Glyph("🌸", 34, 28, 56), Glyph("🌸", 34, 8, 16));
        case "pic_appletree_2": return Scene(Glyph("🌳", 170, 0, 6), Glyph("🍏", 30, -30, 40), Glyph("🍏", 30, 28, 56), Glyph("🍏", 30, 8, 16));
        case "pic_appletree_3": return Scene(Glyph("🌳", 170, 0, 6), Glyph("🍎", 44, -30, 40), Glyph("🍎", 44, 28, 56), Glyph("🍎", 44, 8, 16));
        case "pic_appletree_4": return Scene(Glyph("🍎", 72, -40, 46), Glyph("🧺", 110, 18, -40));

        // Cereal for breakfast: the same bowl fills up.
        case "pic_cereal_1": return Scene(Bowl(), Glyph("🥄", 60, 74, 40, turn: -0.5));
        case "pic_cereal_2": return Scene(CerealBits(), Bowl(), FallingBits());
        case "pic_cereal_3": return Scene(Milk(), CerealBits(), Bowl(), Glyph("🥛", 66, -64, 60));
        case "pic_cereal_4": return Scene(Milk(), CerealBits(), Bowl(), Glyph("🥄", 60, 40, 50, turn: -0.5), Glyph("😋", 60, -60, 62));

        // Getting ready to go out
        case "pic_goingout_1": return Scene(Glyph("🧦", 130, 0, 0));
        case "pic_goingout_2": return Scene(Glyph("🧦", 68, -58, 54), Glyph("👟", 112, 14, -26));
        case "pic_goingout_3": return Scene(Glyph("🧢", 100, 0, 40), Glyph("👟", 80, 0, -58));
        case "pic_goingout_4": return Scene(Glyph("🌳", 96, -52, 30), Glyph("🧒", 84, 30, 10), Glyph("⚽", 56, 60, -62), Glyph("☀️", 46, 66, 70));

        // Car wash: the same car gets cleaner.
        case "pic_carwash_1": return Scene(Glyph("🚗", 140, 0, -14), Mud());
        case "pic_carwash_2": return Scene(Glyph("🚗", 140, 0, -14), Mud(), Glyph("💦", 60, -56, 62), Drops((-10, 54), (30, 70)));
        case "pic_carwash_3": return Scene(Glyph("🚗", 140, 0, -14), Glyph("🧽", 60, 52, 58), Bubbles((-60, 30), (-10, 46), (20, 24)));
        case "pic_carwash_4": return Scene(Glyph("🚗", 140, 0, -14), Glyph("✨", 50, -60, 58), Glyph("✨", 42, 62, 44));

        // A day: the same hill, the sun moving across the sky.
        case "pic_day_1": return Scene(Sky(Palette.Pink), Glyph("☀️", 74, -66, -40), Hill());
        case "pic_day_2": return Scene(Sky(Palette.Sea), Glyph("☀️", 84, 0, 54), Hill());
        case "pic_day_3": return Scene(Sky(Palette.Orange), Glyph("☀️", 74, 66, -40), Hill());
        case "pic_day_4": return Scene(Sky(Palette.Ink.Blend(0.25, Palette.Sea)), Glyph("🌙", 70, 40, 50),
                                       Glyph("⭐", 30, -60, 60), Glyph("⭐", 24, -20, 30), Hill());

        // Playground slide: the same slide, the child moving along it.
        case "pic_slide_1": return Scene(Slide(), Glyph("🧒", 58, -76, -6));
        case "pic_slide_2": return Scene(Slide(), Glyph("🧒", 58, -30, 84));
        case "pic_slide_3": return Scene(Slide(), Glyph("🧒", 58, 34, 22));
        case "pic_slide_4": return Scene(Slide(), Glyph("🧒", 58, 84, -60));

        // Washing clothes
        case "pic_laundry_1": return Scene(Glyph("👕", 130, 0, 0), Mud());
        case "pic_laundry_2": return Scene(WashingMachine());
        case "pic_laundry_3": return Scene(WashingLine(), Glyph("👕", 66, -40, 6), Glyph("👖", 66, 42, -2), Glyph("☀️", 44, 70, 76));
        case "pic_laundry_4": return Scene(FoldedPile(), Glyph("✨", 44, 66, 56));

        // A picnic: the same blanket under the same tree.
        case "pic_picnic_1": return Scene(Glyph("🧺", 120, 0, -26), Glyph("🥪", 52, -56, 62), Glyph("🍎", 46, 56, 62));
        case "pic_picnic_2": return Scene(Glyph("🌳", 96, -50, 50), Blanket());
        case "pic_picnic_3": return Scene(Glyph("🌳", 96, -50, 50), Blanket(), Glyph("🥪", 48, -30, -40), Glyph("🍎", 40, 26, -34),
                                          Glyph("🧃", 44, 62, -56));
        case "pic_picnic_4": return Scene(Glyph("🌳", 96, -50, 50), Glyph("🗑️", 100, 46, -24), Glyph("🧃", 40, 46, 56));

        // Sending a letter
        case "pic_letter_1": return Scene(Glyph("📄", 140, -10, 0), Scribble(), Glyph("🖍️", 58, 66, -60));
        case "pic_letter_2": return Scene(Glyph("✉️", 140, 0, 0));
        case "pic_letter_3": return Scene(Glyph("📮", 140, 0, -6), Glyph("✉️", 46, 66, 70, turn: -0.3));
        case "pic_letter_4": return Scene(Glyph("👵", 110, -30, 14), Glyph("💌", 70, 56, -46));

        // Growing up
        case "pic_growing_1": return Scene(Glyph("👶", 140, 0, 0));
        case "pic_growing_2": return Scene(Glyph("🧒", 140, 0, 0));
        case "pic_growing_3": return Scene(Glyph("🧑", 140, 0, 0));
        case "pic_growing_4": return Scene(Glyph("👵", 140, 0, 0));

        // Building a house: the same house goes up.
        case "pic_house_1": return Scene(Soil(), Glyph("🚜", 104, 0, 10));
        case "pic_house_2": return Scene(HouseWalls());
        case "pic_house_3": return Scene(HouseWalls(), Roof());
        case "pic_house_4": return Scene(HouseWalls(), Roof(), HouseFront(), Glyph("👨‍👩‍👧", 52, 60, -74));

        // Rocket to the moon (the rocket emoji leans; a quarter turn left stands it up).
        case "pic_rocket_1": return Scene(Glyph("🚀", 120, 40, 20, turn: Mathf.Pi / 4), Glyph("🧑‍🚀", 86, -50, -34));
        case "pic_rocket_2": return Scene(Glyph("3️⃣", 56, -64, 64), Glyph("2️⃣", 56, 0, 64), Glyph("1️⃣", 56, 64, 64),
                                          Glyph("🚀", 96, 0, -40, turn: Mathf.Pi / 4));
        case "pic_rocket_3": return Scene(Glyph("🚀", 100, 0, 30, turn: Mathf.Pi / 4), Glyph("🔥", 50, 0, -46), Glyph("💨", 50, -54, -70),
                                          Glyph("💨", 50, 54, -70, turn: Mathf.Pi));
        case "pic_rocket_4": return Scene(Glyph("⭐", 30, -70, 70), Glyph("⭐", 24, 70, 40), Glyph("🌕", 140, 0, -50), Glyph("🚀", 76, 0, 56, turn: Mathf.Pi / 4));
        default: return null;
        }
    }

    /// <summary>The parts, each drawn over the ones before it.</summary>
    private static Node2D Scene(params Node2D[] parts) => Group(parts);

    /// An emoji at a size and spot, optionally turned (radians).
    private static Node2D Glyph(string text, double size, double x, double y, double turn = 0)
    {
        var label = EmojiPictures.Glyph(text, (int)Math.Round(size));
        var holder = new Node2D();
        holder.AddChild(label);
        holder.Position = P(x, y);
        holder.Rotation = Turn(turn);
        return holder;
    }

    private static Node2D Breakfast()
    {
        return Scene(Glyph("🥣", 124, 0, -32), Glyph("🥛", 64, -62, 58), Glyph("🍌", 64, 62, 58));
    }

    private static Node2D Drops(params (double X, double Y)[] spots)
    {
        var n = new Node2D();
        foreach (var (x, y) in spots) n.AddChild(Glyph("💧", 34, x, y));
        return n;
    }

    private static Node2D Bubbles(params (double X, double Y)[] spots)
    {
        var n = new Node2D();
        foreach (var (x, y) in spots) n.AddChild(Glyph("🫧", 40, x, y));
        return n;
    }

    /// The hand-drawn tap from PictureNode, smaller and up in the corner.
    private static Node2D Tap()
    {
        var n = PictureNode.Tap();
        n.Scale = Vector2.One * (float)(0.6);
        n.Position = P(-20, 62);
        return n;
    }

    /// A slice of bread: puffy top, golden crust, soft middle.
    private static Node2D Slice(Vector2 at, double scale)
    {
        Vector2[] outline =
        {
            P(-66, -80), P(66, -80), P(68, 26), P(88, 46),
            P(84, 76), P(50, 92), P(0, 86), P(-50, 92),
            P(-84, 76), P(-88, 46), P(-68, 26),
        };
        var n = new Node2D();
        n.AddChild(Pen(Polygon(outline), fill: Palette.LightBrown, seed: 1195));
        n.AddChild(Pen(Polygon(outline.Select(p => new Vector2(p.X * 0.8f, p.Y * 0.8f + 2))),
                               fill: Palette.Paper.Blend(0.2, Palette.Sand), lineWidth: 0, seed: 1196));
        n.Scale = Vector2.One * (float)(scale);
        n.Position = at;
        return n;
    }

    private static Node2D Butter()
    {
        return Pen(Polygon(new[] { P(-50, -30), P(-10, -44), P(46, -36), P(52, 10),
                              P(40, 42), P(-20, 48), P(-54, 20) }),
                    fill: Palette.Sun.Blend(0.45, Palette.White), ink: Palette.Sun, lineWidth: 3, wobble: 4, seed: 1197);
    }

    /// A square cheese slice with holes, lying a little crooked.
    private static Node2D Cheese()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-50, -50, 100, 100), 6), fill: Palette.Sun, lineWidth: 4.5, seed: 1198));
        foreach (var (i, (x, y, r)) in Indexed(((float)(-20), (float)(18), (float)(10)), (22, -14, 12), (-14, -26, 7), (26, 26, 6)))
        {
            n.AddChild(Pen(Ellipse(P(x, y), r, r), fill: Palette.Orange.WithAlpha(0.6), lineWidth: 2.5, seed: (ulong)(1199 + i) * 3));
        }
        n.Rotation = Turn(0.18);
        n.Position = P(6, 6);
        return n;
    }

    /// A low mound along the bottom of the card.
    private static Node2D Mound(Color fill, ulong seed)
    {
        var top = Enumerable.Range(0, 11).Select(i =>
        {
            var t = (float)(i) / 10;
            return P(-108 + 216 * t, -66 + 22 * Mathf.Sin(t * Mathf.Pi));
        }).ToList();
        top.Add(P(108, -104));
        top.Add(P(-108, -104));
        return Pen(Polygon(top), fill: fill, lineWidth: 4.5, seed: seed);
    }

    private static Node2D Soil() => Mound(fill: Palette.Brown, seed: 1200);
    private static Node2D Sand() => Mound(fill: Palette.Sand, seed: 1201);

    private static Node2D SeedInSoil()
    {
        var n = Pen(Ellipse(Vector2.Zero, 13, 9), fill: Palette.Sun, lineWidth: 3.5, seed: 1202);
        n.Position = P(0, -66);
        return n;
    }

    private static Node2D Path()
    {
        return Pen(Polyline(new[] { P(-100, -60), P(-20, -72), P(60, -86), P(104, -94) }),
                    ink: Palette.Stone.Blend(0.3, Palette.Ink), lineWidth: 14, wobble: 3, seed: 1203);
    }

    private static Node2D SandGrains()
    {
        var n = new Node2D();
        foreach (var (i, (x, y)) in Indexed((-34, 40), (-20, 22), (-6, 34), (-28, 8)))
        {
            n.AddChild(Pen(Ellipse(P((float)(x), (float)(y)), 5, 5), fill: Palette.Sand, lineWidth: 2.5, seed: (ulong)(1204 + i)));
        }
        return n;
    }

    /// A curved arrow over the top: "turn it over".
    private static Node2D TurnArrow()
    {
        var n = new Node2D();
        n.AddChild(Pen(Arc(P(0, 40), 76, 56, 0.25, Mathf.Pi - 0.25), ink: Palette.Orange, lineWidth: 9, seed: 1210));
        n.AddChild(Pen(Polygon(new[] { P(-92, 58), P(-52, 62), P(-78, 30) }), fill: Palette.Orange, ink: Palette.Orange, lineWidth: 4, seed: 1211));
        return n;
    }

    private static Node2D UpArrow()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(new[] { P(0, 46), P(0, 92) }), ink: Palette.Orange, lineWidth: 9, seed: 1212));
        n.AddChild(Pen(Polygon(new[] { P(-18, 84), P(0, 106), P(18, 84) }), fill: Palette.Orange, ink: Palette.Orange, lineWidth: 4, seed: 1213));
        n.Position = P(-64, -4);
        return n;
    }

    /// The bucket's shape in sand: wide at the bottom, little turrets on top.
    private static Node2D SandTower()
    {
        return Pen(Polygon(new[] {
            P(-62, -62), P(62, -62), P(46, 28),
            P(46, 42), P(28, 42), P(28, 30), P(9, 30),
            P(9, 42), P(-9, 42), P(-9, 30), P(-28, 30),
            P(-28, 42), P(-46, 42), P(-46, 28),
         }), fill: Palette.Sand.Blend(0.15, Palette.LightBrown), seed: 1214);
    }

    private static Node2D Pond()
    {
        return Pen(Ellipse(P(0, -10), 112, 92), fill: Palette.Sea.WithAlpha(0.55),
                    ink: Palette.Sea.Blend(0.4, Palette.Ink), lineWidth: 4, seed: 1220);
    }

    private static Node2D Frogspawn()
    {
        var n = new Node2D();
        (double X, double Y)[] spots = { (-46, 18), (-6, 30), (34, 20), (-28, -16), (12, -8), (52, -20), (-60, -40), (-12, -50), (28, -52) };
        foreach (var (i, (x, y)) in Indexed(spots))
        {
            n.AddChild(Pen(Ellipse(P(x, y), 22, 22), fill: Palette.Ice.WithAlpha(0.85),
                                   ink: Palette.Ink.WithAlpha(0.5), lineWidth: 3, seed: (ulong)(1221 + i)));
            var egg = new Disc(7, Palette.Ink);
            egg.Position = P(x + 2, y - 1);
            n.AddChild(egg);
        }
        return n;
    }

    private static Node2D Tadpole(bool legs)
    {
        var n = new Node2D();
        var dark = Palette.Ink.WithAlpha(0.85);
        n.AddChild(Pen(Polyline(new[] { P(0, 0), P(34, 16), P(64, -10), P(96, 8) }),
                               ink: dark, lineWidth: 12, wobble: 1.5, seed: 1240));
        if (legs)
        {
            n.AddChild(Pen(Polyline(new[] { P(12, -14), P(30, -44), P(18, -60) }), ink: dark, lineWidth: 7, seed: 1241));
            n.AddChild(Pen(Polyline(new[] { P(12, 14), P(30, 44), P(18, 60) }), ink: dark, lineWidth: 7, seed: 1242));
        }
        n.AddChild(Pen(Ellipse(P(-26, 0), 46, 36), fill: dark, seed: 1243));
        var eye = new Disc(8, Palette.White);
        eye.Position = P(-48, 12);
        n.AddChild(eye);
        n.Position = P(-10, -6);
        return n;
    }

    private static Node2D LilyPad()
    {
        // A round leaf with a notch cut out towards the right.
        var rim = Enumerable.Range(0, 17).Select(i =>
        {
            var a = 0.35 + (float)(i) / 16 * (2 * Mathf.Pi - 0.7);
            return P(100 * Mathf.Cos(a), -46 + 40 * Mathf.Sin(a));
        }).ToList();
        return Pen(Polygon(rim.Append(P(0, -46))), fill: Palette.Grass.Blend(0.25, Palette.Leaf), seed: 1250);
    }

    /// A frog seen from the front. A froglet still has a stubby tail.
    private static Node2D Frog(bool tail, double scale)
    {
        var n = new Node2D();
        var green = Palette.Leaf;
        if (tail)
        {
            n.AddChild(Pen(Polyline(new[] { P(50, -30), P(86, -40), P(108, -30) }), ink: green, lineWidth: 12, seed: 1260));
        }
        // Back legs folded at the sides, then the body, front feet and the eyes on top.
        n.AddChild(Pen(Ellipse(P(-64, -40), 34, 22), fill: green, lineWidth: 4.5, seed: 1261));
        n.AddChild(Pen(Ellipse(P(64, -40), 34, 22), fill: green, lineWidth: 4.5, seed: 1262));
        n.AddChild(Pen(Ellipse(P(0, -8), 72, 52), fill: Palette.Grass, seed: 1263));
        n.AddChild(Pen(Ellipse(P(-26, -58), 18, 10), fill: green, lineWidth: 4, seed: 1264));
        n.AddChild(Pen(Ellipse(P(26, -58), 18, 10), fill: green, lineWidth: 4, seed: 1265));
        foreach (var (i, x) in Indexed((float)(-34), 34))
        {
            n.AddChild(Pen(Ellipse(P(x, 40), 22, 22), fill: Palette.White, lineWidth: 4.5, seed: (ulong)(1266 + i)));
            var pupil = new Disc(9, Palette.Ink);
            pupil.Position = P(x, 38);
            n.AddChild(pupil);
        }
        n.AddChild(Pen(Arc(P(0, 0), 34, 20, Mathf.Pi * 1.15, Mathf.Pi * 1.85), lineWidth: 4.5, seed: 1268));
        n.Scale = Vector2.One * (float)(scale);
        n.Position = P(tail ? -14 : 0, tail ? -6 : 4);
        return n;
    }

    /// Rolled-out dough with a star and a circle already cut out.
    private static Node2D DoughWithShapes()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-100, -78, 200, 150), 30), fill: Palette.Sand, seed: 1270));
        var hole = Palette.LightBrown;
        n.AddChild(Pen(Polygon(Star(P(-40, 2), 40)), fill: hole, lineWidth: 4, seed: 1271));
        n.AddChild(Pen(Ellipse(P(46, 22), 26, 26), fill: hole, lineWidth: 4, seed: 1272));
        n.AddChild(Pen(Ellipse(P(46, -40), 20, 20), fill: hole, lineWidth: 4, seed: 1273));
        return n;
    }

    /// An oven with biscuits glowing behind the glass.
    private static Node2D Oven()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-96, -100, 192, 196), 16), fill: Palette.Stone, seed: 1280));
        n.AddChild(Pen(Polyline(new[] { P(-96, 52), P(96, 52) }), lineWidth: 4.5, seed: 1281));
        foreach (var (i, x) in Indexed((float)(-56), -16, 24, 64))
        {
            n.AddChild(Pen(Ellipse(P(x, 74), 11, 11), fill: i == 3 ? Palette.Red : Palette.Ink.WithAlpha(0.7), lineWidth: 3, seed: (ulong)(1282 + i)));
        }
        n.AddChild(Pen(RoundRect(R(-60, 26, 120, 14), 7), fill: Palette.Ink.WithAlpha(0.7), lineWidth: 3.5, seed: 1286));
        n.AddChild(Pen(RoundRect(R(-72, -82, 144, 92), 12), fill: Palette.Orange.WithAlpha(0.85), lineWidth: 4.5, seed: 1287));
        foreach (var (i, x) in Indexed((float)(-36), 0, 36))
        {
            n.AddChild(Pen(Ellipse(P(x, -40), 15, 15), fill: Palette.LightBrown, lineWidth: 3, seed: (ulong)(1288 + i)));
        }
        return n;
    }

    private static Node2D Tray()
    {
        return Pen(RoundRect(R(-100, -74, 200, 22), 8), fill: Palette.Stone, lineWidth: 4.5, seed: 1291);
    }

    /// Wavy lines rising: still warm.
    private static Node2D Steam()
    {
        var n = new Node2D();
        foreach (var (i, x) in Indexed((float)(-20), 24, 68))
        {
            n.AddChild(Pen(Polyline(new[] { P(x, -6), P(x + 10, 14), P(x - 6, 34), P(x + 6, 54) }),
                                   ink: Palette.Ink.WithAlpha(0.3), lineWidth: 5, wobble: 1.5, seed: (ulong)(1292 + i)));
        }
        return n;
    }

    /// A big green leaf with a vein down the middle.
    private static Node2D Leaf(Vector2 at, double scale)
    {
        var n = new Node2D();
        n.AddChild(Pen(Polygon(new[] { P(-100, -10), P(-60, 46), P(0, 62), P(60, 46),
                                         P(100, 0), P(60, -50), P(0, -66), P(-60, -52) }),
                               fill: Palette.Grass, seed: 1430));
        n.AddChild(Pen(Polyline(new[] { P(-100, -10), P(0, -2), P(92, 0) }), ink: Palette.Leaf, lineWidth: 4, seed: 1431));
        n.Rotation = Turn(0.15);
        n.Scale = Vector2.One * (float)(scale);
        n.Position = at;
        return n;
    }

    private static Node2D Ball()
    {
        var n = Pen(Ellipse(Vector2.Zero, 20, 20), fill: Palette.Sun.Blend(0.3, Palette.Grass), lineWidth: 4, seed: 1432);
        n.Position = P(60, 66);
        return n;
    }

    private static Node2D LightSwitch()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-42, -62, 84, 124), 12), fill: Palette.Card, lineWidth: 5, seed: 1300));
        n.AddChild(Pen(RoundRect(R(-16, -40, 32, 44), 8), fill: Palette.Stone, lineWidth: 4, seed: 1301));
        n.Position = P(-6, 4);
        return n;
    }

    /// A zigzag crack across the egg.
    private static Node2D Crack()
    {
        return Pen(Polyline(new[] { P(-40, 6), P(-22, 20), P(-8, 0), P(8, 22),
                               P(22, 2), P(40, 16) }), lineWidth: 4.5, wobble: 1, seed: 1302);
    }

    private static Node2D TinyEggs()
    {
        var n = new Node2D();
        foreach (var (i, (x, y)) in Indexed((-14, 4), (6, 12), (2, -10), (20, -4)))
        {
            n.AddChild(Pen(Ellipse(P((float)(x), (float)(y)), 7, 9), fill: Palette.White, lineWidth: 2.5, seed: (ulong)(1303 + i)));
        }
        return n;
    }

    /// A green chrysalis hanging from a twig.
    private static Node2D Chrysalis()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(new[] { P(-104, 80), P(0, 72), P(104, 84) }), ink: Palette.Brown, lineWidth: 10, seed: 1310));
        n.AddChild(Pen(Polyline(new[] { P(0, 74), P(0, 54) }), lineWidth: 3.5, seed: 1311));
        n.AddChild(Pen(Polygon(new[] { P(0, 56), P(22, 40), P(30, 0), P(22, -44),
                                         P(0, -74), P(-22, -44), P(-30, 0), P(-22, 40) }),
                               fill: Palette.Grass, seed: 1312));
        foreach (var (i, y) in Indexed((float)(20), -6, -32))
        {
            n.AddChild(Pen(Polyline(new[] { P(-20, y), P(20, y - 6) }), ink: Palette.Leaf, lineWidth: 3, seed: (ulong)(1313 + i)));
        }
        return n;
    }

    private static Node2D Puddle()
    {
        return Pen(Ellipse(P(0, -78), 92, 22), fill: Palette.Sea, lineWidth: 4, seed: 1320);
    }

    private static Node2D Lead()
    {
        return Pen(Polyline(new[] { P(-96, 96), P(-60, 70), P(-26, 30) }), ink: Palette.Red, lineWidth: 7, seed: 1321);
    }

    private static Node2D ThrowArc()
    {
        return Pen(Arc(P(10, 10), 60, 56, 0.4, 2.2), ink: Palette.Ink.WithAlpha(0.4), lineWidth: 4, wobble: 1.5, seed: 1322);
    }

    private static Node2D WaterBowl()
    {
        return Pen(Polygon(new[] { P(18, -50), P(98, -50), P(88, -84), P(28, -84) }), fill: Palette.Sea, lineWidth: 4.5, seed: 1323);
    }

    private static Node2D CreamDrips()
    {
        var n = new Node2D();
        foreach (var (i, (x, y)) in Indexed((4, -92), (40, -84)))
        {
            n.AddChild(Pen(Ellipse(P((float)(x), (float)(y)), 7, 10), fill: Palette.Pink, lineWidth: 2.5, seed: (ulong)(1324 + i)));
        }
        return n;
    }

    /// A pink puddle of melted ice cream with the empty cone lying in it.
    private static Node2D MeltedIceCream()
    {
        var n = new Node2D();
        n.AddChild(Pen(Ellipse(P(6, -54), 92, 30), fill: Palette.Pink, lineWidth: 4, wobble: 4, seed: 1326));
        var cone = Pen(Polygon(new[] { P(-30, 18), P(30, 18), P(0, -60) }), fill: Palette.LightBrown, lineWidth: 4.5, seed: 1327);
        cone.Rotation = Turn(1.4);
        cone.Position = P(26, -40);
        n.AddChild(cone);
        return n;
    }

    private static Node2D PizzaBase()
    {
        return Pen(Ellipse(P(0, -10), 100, 82), fill: Palette.Sand, seed: 1330);
    }

    private static Node2D RollingPin()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-70, -16, 140, 32), 14), fill: Palette.LightBrown, lineWidth: 4.5, seed: 1331));
        n.AddChild(Pen(RoundRect(R(-104, -7, 36, 14), 6), fill: Palette.Brown, lineWidth: 3.5, seed: 1332));
        n.AddChild(Pen(RoundRect(R(68, -7, 36, 14), 6), fill: Palette.Brown, lineWidth: 3.5, seed: 1333));
        n.Rotation = Turn(0.3);
        n.Position = P(0, 6);
        return n;
    }

    private static Node2D Sauce()
    {
        return Pen(Ellipse(P(0, -10), 78, 62), fill: Palette.Red, lineWidth: 3, wobble: 4, seed: 1334);
    }

    private static Node2D CheeseBits()
    {
        var n = new Node2D();
        (double X, double Y)[] spots = { (-40, 10), (-6, 30), (34, 14), (-30, -30), (8, -10), (44, -30), (-4, -50) };
        foreach (var (i, (x, y)) in Indexed(spots))
        {
            n.AddChild(Pen(RoundRect(R(x - 12, y - 8, 24, 16), 4), fill: Palette.Sun, lineWidth: 2.5, seed: (ulong)(1335 + i)));
        }
        return n;
    }

    private static Node2D Floor()
    {
        return Pen(Polyline(new[] { P(-110, -100), P(110, -100) }), ink: Palette.Brown, lineWidth: 6, seed: 1342);
    }

    /// A toy block, sitting with its middle at (x, y).
    private static Node2D Block(double x, double y, Color colour, ulong seed, double turn = 0)
    {
        var n = Pen(RoundRect(R(-32, -32, 64, 64), 8), fill: colour, seed: seed);
        n.AddChild(Pen(Ellipse(Vector2.Zero, 12, 12), fill: Palette.White, lineWidth: 3, seed: seed &+ 20));
        n.Position = P(x, y);
        n.Rotation = Turn(turn);
        return n;
    }

    /// An empty bowl seen from the side.
    private static Node2D Bowl()
    {
        var rim = Enumerable.Range(0, 13).Select(i =>
        {
            var a = Mathf.Pi + (float)(i) / 12 * Mathf.Pi;
            return P(90 * Mathf.Cos(a), -20 + 70 * Mathf.Sin(a));
        }).ToList();
        rim.Add(P(-90, -20));
        return Pen(Polygon(rim), fill: Palette.Sea, seed: 1343);
    }

    private static Node2D CerealBits()
    {
        var n = new Node2D();
        (double X, double Y)[] spots = { (-60, -14), (-30, -6), (0, -12), (30, -4), (60, -14), (-44, 8), (-12, 10), (18, 8), (46, 4) };
        foreach (var (i, (x, y)) in Indexed(spots))
        {
            n.AddChild(Pen(Ellipse(P(x, y), 13, 11), fill: Palette.Sun.Blend(0.3, Palette.LightBrown),
                                   lineWidth: 2.5, seed: (ulong)(1344 + i)));
        }
        return n;
    }

    private static Node2D FallingBits()
    {
        var n = new Node2D();
        foreach (var (i, (x, y)) in Indexed((-10, 60), (14, 84), (4, 38)))
        {
            n.AddChild(Pen(Ellipse(P((float)(x), (float)(y)), 12, 10), fill: Palette.Sun.Blend(0.3, Palette.LightBrown),
                                   lineWidth: 2.5, seed: (ulong)(1354 + i)));
        }
        return n;
    }

    private static Node2D Milk()
    {
        return Pen(Ellipse(P(0, -18), 86, 16), fill: Palette.White, lineWidth: 3, seed: 1358);
    }

    private static Node2D Mud()
    {
        var n = new Node2D();
        foreach (var (i, (x, y, r)) in Indexed(((float)(-40), (float)(-20), (float)(14)), (10, -34, 10), (44, -12, 12), (-6, 4, 8)))
        {
            n.AddChild(Pen(Ellipse(P(x, y), r, r * 0.8), fill: Palette.Brown, lineWidth: 2, wobble: 3, seed: (ulong)(1360 + i)));
        }
        return n;
    }

    private static Node2D Sky(Color colour)
    {
        return Pen(RoundRect(R(-112, -104, 224, 212), 22), fill: colour.WithAlpha(0.55), lineWidth: 0, seed: 1365);
    }

    private static Node2D Hill()
    {
        var top = Enumerable.Range(0, 13).Select(i =>
        {
            var t = (float)(i) / 12;
            return P(-112 + 224 * t, -46 + 26 * Mathf.Sin(t * Mathf.Pi));
        }).ToList();
        top.Add(P(112, -104));
        top.Add(P(-112, -104));
        return Pen(Polygon(top), fill: Palette.Grass, lineWidth: 4.5, seed: 1366);
    }

    /// A ladder up the left, a platform on top and the slide sloping down to the right.
    private static Node2D Slide()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(new[] { P(-110, -96), P(110, -96) }), ink: Palette.Leaf, lineWidth: 6, seed: 1370));
        n.AddChild(Pen(Polyline(new[] { P(-96, -96), P(-66, 60) }), ink: Palette.Ink, lineWidth: 6, seed: 1371));
        n.AddChild(Pen(Polyline(new[] { P(-56, -96), P(-26, 60) }), ink: Palette.Ink, lineWidth: 6, seed: 1372));
        for (var i = 0; i < 4; i++)
        {
            var y = -66 + (float)(i) * 36;
            var dx = (y + 96) / 156 * 30;
            n.AddChild(Pen(Polyline(new[] { P(-96 + dx, y), P(-56 + dx, y) }), lineWidth: 4.5, seed: (ulong)(1373 + i)));
        }
        n.AddChild(Pen(RoundRect(R(-70, 52, 56, 14), 5), fill: Palette.Red, lineWidth: 4, seed: 1377));
        n.AddChild(Pen(Polygon(new[] { P(-16, 64), P(0, 64), P(108, -78), P(108, -92),
                                         P(84, -92) }), fill: Palette.Sun, lineWidth: 4.5, seed: 1378));
        return n;
    }

    private static Node2D WashingMachine()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-86, -100, 172, 196), 16), fill: Palette.Card, seed: 1380));
        n.AddChild(Pen(Polyline(new[] { P(-86, 56), P(86, 56) }), lineWidth: 4, seed: 1381));
        n.AddChild(Pen(Ellipse(P(52, 76), 11, 11), fill: Palette.Red, lineWidth: 3, seed: 1382));
        n.AddChild(Pen(Ellipse(P(0, -24), 58, 58), fill: Palette.Stone, lineWidth: 5, seed: 1383));
        n.AddChild(Pen(Ellipse(P(0, -24), 44, 44), fill: Palette.Sea, lineWidth: 3.5, seed: 1384));
        n.AddChild(Glyph("👕", 46, 0, -26));
        n.AddChild(Bubbles((-22, -2), (24, -44)));
        return n;
    }

    private static Node2D WashingLine()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(new[] { P(-96, -100), P(-96, 52) }), ink: Palette.Brown, lineWidth: 8, seed: 1385));
        n.AddChild(Pen(Polyline(new[] { P(96, -100), P(96, 52) }), ink: Palette.Brown, lineWidth: 8, seed: 1386));
        n.AddChild(Pen(Polyline(new[] { P(-96, 46), P(0, 36), P(96, 46) }), lineWidth: 3.5, seed: 1387));
        return n;
    }

    /// Clothes folded in a neat pile.
    private static Node2D FoldedPile()
    {
        var n = new Node2D();
        Color[] colours = { Palette.Sea, Palette.Red, Palette.Sun, Palette.Grass };
        foreach (var (i, colour) in Indexed(colours))
        {
            var y = -84 + (float)(i) * 40;
            n.AddChild(Pen(RoundRect(R(-74 + (float)(i % 2) * 6, y, 148, 36), 10), fill: colour, lineWidth: 4.5, seed: (ulong)(1390 + i)));
        }
        return n;
    }

    /// A red-and-white checked picnic blanket.
    private static Node2D Blanket()
    {
        var n = new Node2D();
        Vector2[] corners = { P(-80, -96), P(108, -96), P(80, -10), P(-56, -10) };
        n.AddChild(Pen(Polygon(corners), fill: Palette.Red, seed: 1395));
        for (var i = 1; i < 4; i++)
        {
            var t = (float)(i) / 4;
            var bottom = P(-80 + 188 * t, -96);
            var top = P(-56 + 136 * t, -10);
            n.AddChild(Pen(Polyline(new[] { bottom, top }), ink: Palette.White, lineWidth: 7, wobble: 1, seed: (ulong)(1396 + i)));
            var y = -96 + 86 * t;
            n.AddChild(Pen(Polyline(new[] { P(-80 + 24 * t, y), P(108 - 28 * t, y) }), ink: Palette.White, lineWidth: 7, wobble: 1, seed: (ulong)(1400 + i)));
        }
        return n;
    }

    /// A sun drawn in crayon on the paper.
    private static Node2D Scribble()
    {
        var n = new Node2D();
        n.AddChild(Pen(Ellipse(P(-10, 4), 22, 22), ink: Palette.Orange, lineWidth: 5, wobble: 3, seed: 1405));
        for (var i = 0; i < 6; i++)
        {
            var a = (float)(i) / 6 * 2 * Mathf.Pi;
            n.AddChild(Pen(Polyline(new[] { P(-10 + 30 * Mathf.Cos(a), 4 + 30 * Mathf.Sin(a)), P(-10 + 44 * Mathf.Cos(a), 4 + 44 * Mathf.Sin(a)) }),
                                   ink: Palette.Orange, lineWidth: 4, seed: (ulong)(1406 + i)));
        }
        return n;
    }

    private static Node2D HouseWalls()
    {
        var n = new Node2D();
        n.AddChild(Pen(Polyline(new[] { P(-110, -96), P(110, -96) }), ink: Palette.Leaf, lineWidth: 6, seed: 1412));
        n.AddChild(Pen(RoundRect(R(-80, -96, 160, 110), 4), fill: Palette.Red.Blend(0.2, Palette.Brown), seed: 1413));
        for (var i = 1; i < 5; i++)
        {
            var y = -96 + (float)(i) * 22;
            n.AddChild(Pen(Polyline(new[] { P(-76, y), P(76, y) }), ink: Palette.Ink.WithAlpha(0.35), lineWidth: 2.5, wobble: 1, seed: (ulong)(1414 + i)));
        }
        return n;
    }

    private static Node2D Roof()
    {
        return Pen(Polygon(new[] { P(-100, 12), P(0, 96), P(100, 12) }), fill: Palette.Brown, seed: 1420);
    }

    private static Node2D HouseFront()
    {
        var n = new Node2D();
        n.AddChild(Pen(RoundRect(R(-16, -96, 34, 60), 6), fill: Palette.Sun, lineWidth: 4, seed: 1421));
        n.AddChild(Pen(RoundRect(R(-66, -50, 36, 36), 4), fill: Palette.Ice, lineWidth: 4, seed: 1422));
        n.AddChild(Pen(RoundRect(R(32, -50, 36, 36), 4), fill: Palette.Ice, lineWidth: 4, seed: 1423));
        return n;
    }
}
