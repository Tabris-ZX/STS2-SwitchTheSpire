using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace SwitchTheSpire;

/// <summary>
/// F7 打开/关闭互换设置面板，Escape 关闭面板。
/// （通过 Mod.cs 手动注册到 NGame._Input）
/// </summary>
/// 

internal static class KeyPatch
{
    const int TriggerKey = 4194338; //默认F7
    const int EscapeKey = 4194305; //默认esc
    public static void OnInput(NGame __instance, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        if ((int)key.Keycode == TriggerKey)
        {
            SwitchOverlay.Toggle(__instance);
            __instance.GetViewport().SetInputAsHandled();
        }
        else if ((int)key.Keycode == EscapeKey && SwitchOverlay.TryClose())
        {
            __instance.GetViewport().SetInputAsHandled();
        }
    }
}
