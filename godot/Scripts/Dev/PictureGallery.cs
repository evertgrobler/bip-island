using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BipIsland.App;
using BipIsland.Drawing;
using Godot;

namespace BipIsland.Dev;

/// <summary>
/// Dev check, not part of the game: every picture id in Content/asset_manifest.json on a card, in
/// pages, plus a last page with the buttons, avatars, Bip and the star. Lets a cloud session (no Mac)
/// screenshot the ported drawings and compare them with the Swift app.
/// Run: <c>--bip-scene res://Scenes/Dev/PictureGallery.tscn [--bip-gallery-page N]</c>; left/right keys turn pages.
/// <c>--bip-gallery-ids pic_cup,pic_mug</c> shows just those pictures, big, for checking new drawings.
/// </summary>
public partial class PictureGallery : Node2D
{
    private int Columns = 13;
    private int Rows = 6;
    private int PerPage => Columns * Rows;
    private float CardScale = 0.36f;
    private Vector2 Spacing = new(120, 152);

    private readonly Node2D _stage = new();
    private List<string> _ids = new();
    private int _page;

    public int PageCount => (_ids.Count + PerPage - 1) / PerPage + 1;

    public override void _Ready()
    {
        AddChild(new TextureRect
        {
            Texture = Paper.Texture,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Size = GetViewportRect().Size,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        AddChild(_stage);
        _stage.Position = GetViewportRect().Size / 2;
        _ids = PictureIds();
        var only = Array.IndexOf(Boot.UserArgs, "--bip-gallery-ids");
        if (only >= 0 && only + 1 < Boot.UserArgs.Length)
        {
            _ids = Boot.UserArgs[only + 1].Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            (Columns, Rows, CardScale, Spacing) = (7, 4, 0.72f, new Vector2(215, 235));
        }
        var i = Array.IndexOf(Boot.UserArgs, "--bip-gallery-page");
        if (i >= 0 && i + 1 < Boot.UserArgs.Length && int.TryParse(Boot.UserArgs[i + 1], out var page)) _page = page;
        Show(_page);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true } key) return;
        if (key.Keycode == Key.Right) Show((_page + 1) % PageCount);
        if (key.Keycode == Key.Left) Show((_page + PageCount - 1) % PageCount);
    }

    private static List<string> PictureIds()
    {
        const string path = "res://assets/content/asset_manifest.json";
        if (!FileAccess.FileExists(path)) return new List<string>();
        using var doc = JsonDocument.Parse(FileAccess.GetFileAsString(path));
        return doc.RootElement.GetProperty("pictures").EnumerateArray()
                  .Select(p => p.GetProperty("id").GetString() ?? "").Where(id => id.Length > 0).ToList();
    }

    private void Show(int page)
    {
        _page = Math.Clamp(page, 0, PageCount - 1);
        foreach (var child in _stage.GetChildren()) child.QueueFree();
        var missing = _ids.Count(id => !PictureNode.HasArt(id));
        var title = Sketch.Label($"Pictures {_page + 1}/{PageCount} ({_ids.Count} ids, {missing} without art)", 30, Palette.Ink, Fonts.Bold);
        title.Position += new Vector2(0, -475);
        _stage.AddChild(title);
        if (_page == PageCount - 1) ShowKit();
        else ShowPictures(_ids.Skip(_page * PerPage).Take(PerPage).ToList());
    }

    private void ShowPictures(List<string> ids)
    {
        for (var i = 0; i < ids.Count; i++)
        {
            var at = CellPosition(i % Columns, i / Columns);
            var id = ids[i];
            var card = new PictureCard(id, id.StartsWith("pic_") ? id[4..] : id, 900 + (ulong)i) { Position = at, Scale = Vector2.One * CardScale };
            _stage.AddChild(card);
            var label = Sketch.Label(id.StartsWith("pic_") ? id[4..] : id, CardScale > 0.5f ? 22 : 13, Palette.Ink, Fonts.Regular);
            label.Position += at + new Vector2(0, PictureCard.Size * CardScale / 2 + 12);
            _stage.AddChild(label);
        }
    }

    private void ShowKit()
    {
        Node2D[] buttons = { Buttons.Home(), Buttons.Replay(), Buttons.Next(), Buttons.Play() };
        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].Position = new Vector2(-560 + i * 230, -300);
            _stage.AddChild(buttons[i]);
        }
        var avatars = Avatars.Emoji.Keys.ToList();
        for (var i = 0; i < avatars.Count; i++)
            _stage.AddChild(new Node2D { Position = new Vector2(-630 + i * 180, -60) }.WithChild(Avatars.Badge(avatars[i], 70, (ulong)(700 + i))));
        _stage.AddChild(new Bip { Position = new Vector2(-450, 400) });
        for (var i = 0; i < 4; i++)
            _stage.AddChild(Sketch.Node(new SketchShape.Polygon(Sketch.StarPoints(new Vector2(-100 + i * 70, 250), 28)), (ulong)(561 + i),
                                        fill: i < 2 ? Palette.Sun : new Color(Palette.Stone, 0.5f), lineWidth: 3, wobble: 1));
        _stage.AddChild(EmojiPictures.Make("pic_cat")!.WithPosition(new Vector2(350, 250)));
    }

    private Vector2 CellPosition(int column, int row) =>
        new((column - (Columns - 1) / 2f) * Spacing.X, (row - (Rows - 1) / 2f) * Spacing.Y + 20);
}

internal static class NodeExtensions
{
    public static Node2D WithChild(this Node2D parent, Node child)
    {
        parent.AddChild(child);
        return parent;
    }

    public static Node2D WithPosition(this Node2D node, Vector2 position)
    {
        node.Position = position;
        return node;
    }
}
