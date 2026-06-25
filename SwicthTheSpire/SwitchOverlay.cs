using System;
using Godot;

namespace SwitchTheSpire;

/// <summary>
/// F7 设置面板：三行按钮分别设置卡牌/遗物/药水获取转换目标。
/// </summary>
internal sealed class SwitchOverlay : CanvasLayer
{
    private static SwitchOverlay? _instance;

    private static readonly SwapTarget[] Values = [SwapTarget.Card, SwapTarget.Relic, SwapTarget.Potion];
    private static readonly string[] Names = ["卡牌", "遗物", "药水"];

    // 每行 3 个按钮
    private NoTooltipButton[] _cardBtns = null!,_relicBtns = null!,_potionBtns = null!;

    private bool _built;

    internal static void Toggle(Node host)
    {
        EnsureAttached(host);
        if (_instance is { Visible: true })
            _instance.HidePanel();
        else
            _instance?.ShowPanel();
    }

    internal static bool TryClose()
    {
        if (_instance is not { Visible: true }) return false;
        _instance.HidePanel();
        return true;
    }

    private static void EnsureAttached(Node host)
    {
        if (_instance != null && GodotObject.IsInstanceValid(_instance))
        {
            if (_instance.GetParent() == null)
                host.AddChild(_instance);
            _instance.EnsureBuilt();
        }
        else
        {
            _instance = new SwitchOverlay { Name = "SwitchTheSpireOverlay" };
            _instance.EnsureBuilt();
            host.AddChild(_instance);
        }
    }

    public override void _ExitTree()
    {
        if (_instance == this) _instance = null;
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        Layer = 50;
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;

        var backdrop = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.72f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(backdrop);

        // 主面板
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(680f, 380f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.Position = new Vector2(-340f, -190f);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("15202B"),
            BorderColor = new Color("E2C15A"),
            BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            ContentMarginBottom = 18f, ContentMarginLeft = 18f, ContentMarginRight = 18f, ContentMarginTop = 18f
        });
        AddChild(panel);

        var vbox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        panel.AddChild(vbox);

        // 标题
        var title = new Label
        {
            Text = "互换设置",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", new Color("F5DE7A"));
        vbox.AddChild(title);

        // 说明
        var hint = new Label
        {
            Text = "F7 打开/关闭  |  Esc 关闭  |  保存后立即生效",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        hint.AddThemeColorOverride("font_color", new Color("B8C7D9"));
        vbox.AddChild(hint);

        vbox.AddChild(new HSeparator());

        // 三行设置
        _cardBtns = BuildRow(vbox, "卡牌获取");
        _relicBtns = BuildRow(vbox, "遗物获取");
        _potionBtns = BuildRow(vbox, "药水获取");

        vbox.AddChild(new HSeparator());

        // 底部按钮
        var btnRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        vbox.AddChild(btnRow);

        var saveBtn = MakeButton("保存", new Color("8FD3A7"));
        saveBtn.Pressed += () =>
        {
            Mod.Config.Save();
            HidePanel();
        };
        btnRow.AddChild(saveBtn);

        var closeBtn = MakeButton("关闭", new Color("D9E4F0"));
        closeBtn.Pressed += HidePanel;
        btnRow.AddChild(closeBtn);
    }

    private NoTooltipButton[] BuildRow(VBoxContainer parent, string label)
    {
        var hbox = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        var lbl = new Label
        {
            Text = label,
            CustomMinimumSize = new Vector2(100f, 48f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        lbl.AddThemeFontSizeOverride("font_size", 20);
        lbl.AddThemeColorOverride("font_color", new Color("F3EEE0"));
        hbox.AddChild(lbl);

        var arrow = new Label
        {
            Text = "→",
            CustomMinimumSize = new Vector2(36f, 48f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        arrow.AddThemeFontSizeOverride("font_size", 20);
        arrow.AddThemeColorOverride("font_color", new Color("E2C15A"));
        hbox.AddChild(arrow);

        var buttons = new NoTooltipButton[3];
        for (int i = 0; i < 3; i++)
        {
            var val = Values[i];
            var btn = MakeOptionButton(Names[i]);
            btn.Pressed += () => OnOptionClicked(buttons, val);
            buttons[i] = btn;
            hbox.AddChild(btn);
        }

        parent.AddChild(hbox);
        return buttons;
    }

    //逻辑

    private void ShowPanel()
    {
        RefreshAll();
        Visible = true;
    }

    private void HidePanel()
    {
        Visible = false;
        GetViewport().GuiReleaseFocus();
    }

    private void RefreshAll()
    {
        RefreshRow(_cardBtns, Mod.Config.CardBecomes);
        RefreshRow(_relicBtns, Mod.Config.RelicBecomes);
        RefreshRow(_potionBtns, Mod.Config.PotionBecomes);
    }

    private void OnOptionClicked(NoTooltipButton[] row, SwapTarget val)
    {
        // 根据哪个 row 被点击来更新对应的 Config
        if (row == _cardBtns)
            Mod.Config.CardBecomes = val;
        else if (row == _relicBtns)
            Mod.Config.RelicBecomes = val;
        else if (row == _potionBtns)
            Mod.Config.PotionBecomes = val;

        RefreshRow(row, val);
    }

    private static void RefreshRow(NoTooltipButton[] row, SwapTarget selected)
    {
        for (int i = 0; i < 3; i++)
        {
            bool on = Values[i] == selected;
            row[i].ButtonPressed = on;
            row[i].AddThemeColorOverride("font_color", on ? new Color("F5DE7A") : new Color("8899AA"));
        }
    }

    private static NoTooltipButton MakeOptionButton(string text)
    {
        var btn = new NoTooltipButton
        {
            Text = text,
            ToggleMode = true,
            CustomMinimumSize = new Vector2(72f, 42f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Flat = true,
            Alignment = HorizontalAlignment.Center,
            FocusMode = Control.FocusModeEnum.None
        };
        btn.AddThemeFontSizeOverride("font_size", 18);
        btn.AddThemeColorOverride("font_color", new Color("8899AA"));
        btn.AddThemeColorOverride("font_hover_color", new Color("F5DE7A"));
        btn.AddThemeColorOverride("font_pressed_color", new Color("C8B46A"));
        btn.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        return btn;
    }

    private static NoTooltipButton MakeButton(string text, Color color)
    {
        var btn = new NoTooltipButton
        {
            Text = text,
            CustomMinimumSize = new Vector2(120f, 44f)
        };
        btn.AddThemeFontSizeOverride("font_size", 20);
        btn.AddThemeColorOverride("font_color", color);
        btn.AddThemeColorOverride("font_hover_color", new Color("F5DE7A"));
        btn.AddThemeColorOverride("font_pressed_color", new Color("C8B46A"));
        return btn;
    }
}
