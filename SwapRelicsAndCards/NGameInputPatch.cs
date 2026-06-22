using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace swapRelicsAndCards;

/// <summary>
/// F7 热切换奖励互换开关。按 F7 屏幕顶部短暂提示当前状态。
/// </summary>
internal static class NGameInputPatch
{
    private const long F7 = 4194338;
    private static Label? _toast;
    private static Tween? _tween;

    public static void OnInput(NGame __instance, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey val || !val.Pressed || val.Echo)
            return;

        if ((long)val.Keycode == F7)
        {
            Mod.Config.Enabled = !Mod.Config.Enabled;
            Mod.Config.Save();
            var msg = Mod.Config.Enabled ? "互换遗物与卡牌获取: 开启" : "互换遗物与卡牌获取: 关闭";
            Log.Info(Mod.Config.Enabled ? "开启" : "关闭");
            ShowToast(__instance, msg);
            __instance.GetViewport().SetInputAsHandled();
        }
    }

    private static void ShowToast(Node host, string text)
    {
        // 快速点击时直接杀掉旧提示和动画
        _tween?.Kill();
        _toast?.QueueFree();

        _toast = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _toast.AddThemeColorOverride("font_color", new Color("F5DE7A"));
        _toast.AddThemeFontSizeOverride("font_size", 36);

        var size = host.GetViewport().GetVisibleRect().Size;
        _toast.SetAnchorsPreset(Control.LayoutPreset.CenterTop, false);
        _toast.Position = new Vector2(size.X * 0.12f, size.Y * 0.18f);
        _toast.SetSize(new Vector2(200f, 50f));
        _toast.MouseFilter = Control.MouseFilterEnum.Ignore;

        host.AddChild(_toast);

        _tween = host.CreateTween();
        _tween.TweenProperty(_toast, "modulate:a", 0f, 2.0f).SetDelay(0.5f);
        _tween.TweenCallback(Callable.From(() =>
        {
            _toast?.QueueFree();
            _toast = null;
            _tween = null;
        }));
    }
}
