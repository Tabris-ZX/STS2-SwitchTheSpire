#nullable enable
using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace swapRelicsAndCards;

/// <summary>
/// 事件安全网：拦截非奖励流程中的遗物获取，弹出完整的卡牌奖励选择界面（3选1）。
/// <para>当 SwapGuard.Depth &gt; 0 时放行（此时遗物获取来自战斗奖励层交换，不应再次交换）。</para>
/// <para>覆盖：事件中直接调用 RelicCmd.Obtain 给遗物的场合、Boss 宝箱等。</para>
/// </summary>
[HarmonyPatch]
internal static class RelicCmdObtainPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(RelicCmd), "Obtain", new Type[]
        {
            typeof(RelicModel),
            typeof(Player),
            typeof(int)
        });
    }

    private static bool Prefix(
        RelicModel relic,
        Player player,
        int index,
        ref Task<RelicModel> __result)
    {
        // 全局开关关闭，放行
        if (!Mod.Config.Enabled)
            return true;

        // 奖励选择流程中，放行（奖励层已经交换过了）
        if (SwapGuard.Depth > 0)
            return true;

        // 商店购买不互换
        if (player?.RunState?.CurrentRoom is MerchantRoom)
            return true;

        if (player == null)
            return true;

        // 第0层初始遗物不替换
        var runState = player.RunState;
        if (runState != null)
        {
            try
            {
                if (runState.TotalFloor <= 0)
                    return true;
            }
            catch { }
        }

        Log.Debug($"Relic '{relic.Id.Entry}' → Card");

        // 增加守卫深度，防止选牌时 CardPileCmdAddToDeckPatch 再换回来
        SwapGuard.Depth++;
        __result = ShowCardRewardInsteadAsync(relic, player);
        return false;
    }

    /// <summary>
    /// 创建完整的 CardReward 并显示选牌界面，替代遗物获取。
    /// 玩家可从 3 张牌中选择 1 张（或跳过），与正常的战斗卡牌奖励体验一致。
    /// </summary>
    private static async Task<RelicModel> ShowCardRewardInsteadAsync(RelicModel relic, Player player)
    {
        try
        {
            // 使用玩家角色卡池，标准遭遇战稀有度概率
            var options = new CardCreationOptions(
                new[] { player.Character.CardPool },
                CardCreationSource.Other,
                CardRarityOddsType.RegularEncounter);

            var cardReward = new CardReward(options, 3, player);
            cardReward.Populate();
            await cardReward.SelectUnsynchronized();

            Log.Debug($"OK: relic '{relic.Id.Entry}' → card");
        }
        catch (Exception ex)
        {
            Log.Error($"Relic→card reward failed: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            SwapGuard.Depth--;
        }
        return relic;
    }
}
