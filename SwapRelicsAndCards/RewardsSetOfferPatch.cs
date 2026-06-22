#nullable enable
using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace swapRelicsAndCards;

/// <summary>
/// 在奖励生成阶段交换 CardReward 和 RelicReward，让玩家看到正确的奖励类型。
/// <para>Monster/Boss 房间：CardReward → RelicReward（战斗卡牌奖励变遗物）</para>
/// <para>Elite 房间：CardReward ↔ RelicReward 互换（两个都有则互相交换）</para>
/// <para>守卫机制：整个 Offer() 期间 SwapGuard.Depth++，底层补丁检测后放行。</para>
/// </summary>
[HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.Offer))]
internal static class RewardsSetOfferPatch
{
    /// <summary>
    /// Prefix：在进入 Offer() 前交换奖励类型，并增加守卫深度。
    /// </summary>
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
            Log.Error($"Swapping rewards failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Postfix：在 Offer() 返回的 Task 完成后减少守卫深度。
    /// 使用 Task.ContinueWith 确保在异步流程结束后才释放。
    /// </summary>
    private static void Postfix(ref Task __result)
    {
        if (!Mod.Config.Enabled) return;

        __result = __result.ContinueWith(_ =>
        {
            SwapGuard.Depth--;
        });
    }

    /// <summary>
    /// 遍历奖励列表，将所有 CardReward 换成 RelicReward，所有 RelicReward 换成 CardReward。
    /// </summary>
    private static void SwapRewards(RewardsSet rewardsSet)
    {
        Player player = rewardsSet.Player;
        AbstractRoom? room = rewardsSet.Room;

        for (int i = 0; i < rewardsSet.Rewards.Count; i++)
        {
            Reward reward = rewardsSet.Rewards[i];

            if (reward is CardReward)
            {
                var newReward = new RelicReward(player);
                newReward.Populate(); // 立即填充确保 IsPopulated=true，防 NRewardsScreen NRE
                Log.Info($"CardReward → RelicReward at index {i}");
                rewardsSet.Rewards[i] = newReward;
            }
            else if (reward is RelicReward)
            {
                CardCreationOptions options;
                if (room != null)
                {
                    options = CardCreationOptions.ForRoom(player, room.RoomType);
                }
                else
                {
                    // 非房间奖励（如 ExtraRewards），使用玩家角色卡池
                    options = new CardCreationOptions(
                        new[] { player.Character.CardPool },
                        CardCreationSource.Other,
                        CardRarityOddsType.RegularEncounter);
                }

                var newReward = new CardReward(options, 3, player);
                newReward.Populate(); // 立即填充确保 IsPopulated=true
                Log.Info($"RelicReward → CardReward at index {i}");
                rewardsSet.Rewards[i] = newReward;
            }
        }
    }
}
