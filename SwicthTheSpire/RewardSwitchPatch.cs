using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace SwitchTheSpire;

/// <summary>
/// 奖励层互换：在 Offer() 前根据配置将 CardReward / RelicReward / PotionReward 互换。
/// </summary>
[HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.Offer))]
internal static class RewardSwapPatch
{
    private static void Prefix(RewardsSet __instance)
    {
        if (!Mod.Config.Enabled) return;

        SwapGuard.Depth++;

        try
        {
            SwapRewards(__instance);
        }
        catch (Exception ex)
        {
            Log.Error($"奖励互换失败: {ex.Message}");
        }
    }

    private static void Postfix(ref Task __result)
    {
        if (!Mod.Config.Enabled) return;

        __result = __result.ContinueWith(_ => SwapGuard.Depth--);
    }

    private static void SwapRewards(RewardsSet rewardsSet)
    {
        var player = rewardsSet.Player;
        var room = rewardsSet.Room;

        for (int i = 0; i < rewardsSet.Rewards.Count; i++)
        {
            var reward = rewardsSet.Rewards[i];
            SwapTarget? target = reward switch
            {
                CardReward when Mod.Config.CardBecomes != SwapTarget.Card => Mod.Config.CardBecomes,
                RelicReward when Mod.Config.RelicBecomes != SwapTarget.Relic => Mod.Config.RelicBecomes,
                PotionReward when Mod.Config.PotionBecomes != SwapTarget.Potion => Mod.Config.PotionBecomes,
                _ => null
            };

            if (target is null)
                continue;

            rewardsSet.Rewards[i] = CreateReward(target.Value, player, room);
            Log.Info($"{reward.GetType().Name} -> {target}, index={i}");
        }
    }

    private static Reward CreateReward(SwapTarget target, Player player, AbstractRoom? room)
    {
        Reward reward = target switch
        {
            SwapTarget.Card => new CardReward(
                room != null
                    ? CardCreationOptions.ForRoom(player, room.RoomType)
                    : new CardCreationOptions([player.Character.CardPool], CardCreationSource.Other, CardRarityOddsType.RegularEncounter),
                3, player),
            SwapTarget.Relic => new RelicReward(player),
            SwapTarget.Potion => new PotionReward(player),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };

        reward.Populate();
        return reward;
    }
}
