using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace SwitchTheSpire;

/// <summary>
/// 拦截非奖励流程的药水获取，转为卡牌或遗物。
/// </summary>
[HarmonyPatch]
internal static class PotionAcquirePatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(PotionCmd), "TryToProcure", [
            typeof(PotionModel),
            typeof(Player),
            typeof(int)
        ]);

    private static bool Prefix(
        PotionModel potion,
        Player player,
        int slotIndex,
        ref Task<PotionProcureResult> __result)
    {
        if (Mod.Config.PotionBecomes == SwapTarget.Potion) return true;

        if (SwapGuard.ShouldBypass(Mod.Config.PotionBecomes, player))
            return true;

        Log.Debug($"Potion '{potion.Id.Entry}' → {Mod.Config.PotionBecomes}");

        SwapGuard.Depth++;
        __result = GrantAsync(potion, player);
        return false;
    }

    private static async Task<PotionProcureResult> GrantAsync(PotionModel potion, Player player)
    {
        try
        {
            switch (Mod.Config.PotionBecomes)
            {
                case SwapTarget.Card:
                    var options = new CardCreationOptions(
                        [player.Character.CardPool],
                        CardCreationSource.Other,
                        CardRarityOddsType.RegularEncounter);
                    var cardReward = new CardReward(options, 3, player);
                    cardReward.Populate();
                    await cardReward.SelectUnsynchronized();
                    Log.Debug($"OK: potion '{potion.Id.Entry}' → card");
                    break;
                case SwapTarget.Relic:
                    var relic = RelicFactory.PullNextRelicFromFront(player).ToMutable();
                    await RelicCmd.Obtain(relic, player);
                    Log.Debug($"OK: potion '{potion.Id.Entry}' → relic '{relic.Id.Entry}'");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Potion→{Mod.Config.PotionBecomes} failed: {ex.Message}");
        }
        finally
        {
            SwapGuard.Depth--;
        }
        return new PotionProcureResult { success = true, potion = potion };
    }
}
