using System;
using BipIsland.Drawing;
using Godot;

namespace BipIsland.Parent;

/// <summary>
/// The look of the grown-ups' panels (the Swift app's SwiftUI parent area): cream paper, ink
/// outlines, Atkinson Hyperlegible, always light so text never turns white-on-cream.
/// </summary>
public static class ParentUi
{
    public static readonly Color Secondary = new(Palette.Ink, 0.65f);
    public static readonly Color Problem = Palette.Hex(0xC0392B);

    private static Theme? _theme;

    public static Theme Theme => _theme ??= MakeTheme();

    private static Theme MakeTheme()
    {
        var theme = new Theme { DefaultFont = Fonts.Regular, DefaultFontSize = 18 };
        foreach (var type in new[] { "Label", "Button", "LineEdit", "OptionButton", "CheckBox", "SpinBox", "TabBar", "LinkButton", "PopupMenu", "TooltipLabel" })
        {
            theme.SetColor("font_color", type, Palette.Ink);
            theme.SetColor("font_hover_color", type, Palette.Ink);
            theme.SetColor("font_pressed_color", type, Palette.Ink);
            theme.SetColor("font_focus_color", type, Palette.Ink);
            theme.SetColor("font_hover_pressed_color", type, Palette.Ink);
        }
        theme.SetColor("font_disabled_color", "Button", new Color(Palette.Ink, 0.35f));
        theme.SetColor("font_unselected_color", "TabBar", Secondary);
        theme.SetColor("font_selected_color", "TabBar", Palette.Ink);
        theme.SetColor("font_placeholder_color", "LineEdit", new Color(Palette.Ink, 0.4f));
        theme.SetColor("caret_color", "LineEdit", Palette.Ink);
        theme.SetColor("selection_color", "LineEdit", new Color(Palette.Sea, 0.6f));

        var button = Box(Palette.Card, Palette.Ink, 2, 12, 14, 8);
        theme.SetStylebox("normal", "Button", button);
        theme.SetStylebox("hover", "Button", Box(new Color(Palette.Sun, 0.35f), Palette.Ink, 2, 12, 14, 8));
        theme.SetStylebox("pressed", "Button", Box(new Color(Palette.Sun, 0.6f), Palette.Ink, 2, 12, 14, 8));
        theme.SetStylebox("focus", "Button", Box(new Color(0, 0, 0, 0), Palette.Orange, 3, 12, 14, 8));
        theme.SetStylebox("disabled", "Button", Box(new Color(Palette.Card, 0.6f), new Color(Palette.Ink, 0.3f), 2, 12, 14, 8));
        foreach (var state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
            theme.SetStylebox(state, "OptionButton", theme.GetStylebox(state, "Button"));

        foreach (var state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
            theme.SetStylebox(state, "CheckBox", new StyleBoxEmpty());
        theme.SetStylebox("focus", "CheckBox", Box(new Color(0, 0, 0, 0), Palette.Orange, 2, 8, 4, 2));
        theme.SetStylebox("normal", "LineEdit", Box(Palette.White, Palette.Ink, 2, 10, 10, 6));
        theme.SetStylebox("focus", "LineEdit", Box(new Color(0, 0, 0, 0), Palette.Orange, 3, 10, 10, 6));
        theme.SetStylebox("panel", "PopupMenu", Box(Palette.Card, Palette.Ink, 2, 10, 8, 6));
        theme.SetStylebox("panel", "TooltipPanel", Box(Palette.Card, Palette.Ink, 1, 8, 8, 4));
        theme.SetStylebox("panel", "AcceptDialog", Box(Palette.Paper, Palette.Ink, 3, 18, 18, 14));

        theme.SetStylebox("tab_selected", "TabBar", Box(Palette.Sun, Palette.Ink, 2, 12, 22, 8));
        theme.SetStylebox("tab_unselected", "TabBar", Box(Palette.Card, new Color(Palette.Ink, 0.4f), 2, 12, 22, 8));
        theme.SetStylebox("tab_hovered", "TabBar", Box(new Color(Palette.Sun, 0.4f), Palette.Ink, 2, 12, 22, 8));
        theme.SetStylebox("tab_focus", "TabBar", Box(new Color(0, 0, 0, 0), Palette.Orange, 3, 12, 22, 8));
        theme.SetConstant("h_separation", "TabBar", 8);
        theme.SetFontSize("font_size", "TabBar", 20);
        return theme;
    }

    /// <summary>A rounded box with an outline and padding.</summary>
    public static StyleBoxFlat Box(Color fill, Color border, int borderWidth, int radius, int padX = 14, int padY = 14)
    {
        var box = new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            ContentMarginLeft = padX,
            ContentMarginRight = padX,
            ContentMarginTop = padY,
            ContentMarginBottom = padY,
            AntiAliasing = true,
        };
        box.SetBorderWidthAll(borderWidth);
        box.SetCornerRadiusAll(radius);
        return box;
    }

    public static Label Text(string text, int size = 18, bool bold = false, Color? colour = null, bool wrap = false)
    {
        var label = new Label { Text = text, AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off };
        label.AddThemeFontSizeOverride("font_size", size);
        if (bold) label.AddThemeFontOverride("font", Fonts.Bold);
        if (colour is { } c) label.AddThemeColorOverride("font_color", c);
        return label;
    }

    public static Button Button(string text, Action pressed, bool danger = false)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.All };
        if (danger) button.AddThemeColorOverride("font_color", Problem);
        button.Pressed += pressed;
        return button;
    }

    public static HBoxContainer Row(int gap = 12, params Control[] children)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", gap);
        foreach (var child in children) row.AddChild(child);
        return row;
    }

    public static VBoxContainer Column(int gap = 10)
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", gap);
        return column;
    }

    /// <summary>A titled card (the Swift ParentSection). Add rows to the returned column.</summary>
    public static VBoxContainer Section(Container parent, string title)
    {
        var card = new PanelContainer();
        card.AddThemeStyleboxOverride("panel", Box(Palette.Card, new Color(Palette.Ink, 0.25f), 2, 14));
        card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var column = Column(8);
        column.AddChild(Text(title, 20, bold: true));
        card.AddChild(column);
        parent.AddChild(card);
        return column;
    }

    /// <summary>A coloured pill with a little bold text (status badges, sound chips).</summary>
    public static PanelContainer Pill(string text, Color fill, int size = 13, int radius = 10, int minWidth = 0)
    {
        var pill = new PanelContainer { CustomMinimumSize = new Vector2(minWidth, 0) };
        pill.AddThemeStyleboxOverride("panel", Box(fill, new Color(0, 0, 0, 0), 0, radius, 8, 3));
        var label = Text(text, size, bold: true);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        pill.AddChild(label);
        return pill;
    }

    public static Control Spacer() => new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
}
