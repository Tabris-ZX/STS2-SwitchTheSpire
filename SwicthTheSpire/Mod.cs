using System.Reflection;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;

namespace SwitchTheSpire;

//注册
[ModInitializer("Initialize")]
internal static class Mod
{
    internal const string ModId = "SwitchTheSpire";

    internal static Config Config { get; set; } = Config.Default;

    public static void Initialize()
    {
        Config = Config.Load();
        var harmony = new Harmony(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        var original = AccessTools.Method(typeof(NGame), "_Input", [typeof(InputEvent)]);
        harmony.Patch(original,
            postfix: new HarmonyMethod(typeof(KeyPatch), nameof(KeyPatch.OnInput)));

        Log.Info($"Initialized {Config.Enabled}");
    }
}
