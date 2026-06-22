using System.Reflection;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;

namespace swapRelicsAndCards;

[ModInitializer("Initialize")]
internal static class Mod
{
    internal const string ModId = "SwapRelicsAndCards";

    internal static Config Config { get; set; } = Config.Default;

    public static void Initialize()
    {
        Config = Config.Load();
        var harmony = new Harmony(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        // NGame._Input 走 Godot variant 分发，PatchAll 匹配不到，手动注册
        var original = AccessTools.Method(typeof(NGame), "_Input", new[] { typeof(InputEvent) });
        harmony.Patch(original,
            postfix: new HarmonyMethod(typeof(NGameInputPatch), nameof(NGameInputPatch.OnInput)));

        Log.Info($"Initialized — enabled={Config.Enabled}");
    }
}
