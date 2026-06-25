using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace SwitchTheSpire;

/// <summary>
/// 拦截非奖励流程的遗物获取，转为卡牌或药水。
/// </summary>
[HarmonyPatch]
internal static class RelicAcquirePatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(RelicCmd), "Obtain", [
            typeof(RelicModel),
            typeof(Player),
            typeof(int)
        ]);

    private static bool Prefix(
        RelicModel relic,
        Player player,
        int index,
        ref Task<RelicModel> __result)
    {
        if (Mod.Config.RelicBecomes == SwapTarget.Relic) return true;

        if (SwapGuard.ShouldBypass(Mod.Config.RelicBecomes, player))
            return true;

        Log.Debug($"Relic '{relic.Id.Entry}' → {Mod.Config.RelicBecomes}");

        SwapGuard.Depth++;
        __result = GrantAsync(relic, player);
        return false;
    }

    private static async Task<RelicModel> GrantAsync(RelicModel relic, Player player)
    {
        try
        {
            switch (Mod.Config.RelicBecomes)
            {
                case SwapTarget.Card:
                    var options = new CardCreationOptions(
                        [player.Character.CardPool],
                        CardCreationSource.Other,
                        CardRarityOddsType.RegularEncounter);
                    var cardReward = new CardReward(options, 3, player);
                    cardReward.Populate();
                    await cardReward.SelectUnsynchronized();
                    Log.Debug($"OK: relic '{relic.Id.Entry}' → card");
                    break;
                case SwapTarget.Potion:
                    var potionReward = new PotionReward(player);
                    potionReward.Populate();
                    await potionReward.SelectUnsynchronized();
                    Log.Debug($"OK: relic '{relic.Id.Entry}' → potion");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Relic→{Mod.Config.RelicBecomes} failed: {ex.Message}");
        }
        finally
        {
            SwapGuard.Depth--;
        }
        return relic;
    }
}
