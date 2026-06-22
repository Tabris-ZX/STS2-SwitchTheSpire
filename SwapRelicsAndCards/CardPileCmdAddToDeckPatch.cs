#nullable enable
using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;

namespace swapRelicsAndCards;

/// <summary>
/// 事件安全网：拦截非奖励流程中的卡牌获取，改为授予随机遗物。
/// <para>当 SwapGuard.Depth &gt; 0 时放行（此时卡牌获取来自奖励层交换，不应再次交换）。</para>
/// <para>覆盖：事件中直接调用 CardPileCmd.Add 给牌的场合。</para>
/// </summary>
[HarmonyPatch]
internal static class CardPileCmdAddToDeckPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardPileCmd), "Add", new Type[]
        {
            typeof(CardModel),
            typeof(PileType),
            typeof(CardPilePosition),
            typeof(AbstractModel),
            typeof(bool)
        });
    }

    private static bool Prefix(
        CardModel card,
        PileType newPileType,
        CardPilePosition position,
        AbstractModel? clonedBy,
        bool skipVisuals,
        ref Task<CardPileAddResult> __result)
    {
        // 全局开关关闭，放行
        if (!Mod.Config.Enabled)
            return true;

        // 奖励选择流程中，放行
        if (SwapGuard.Depth > 0)
            return true;

        // 商店购买不互换
        if (card.Owner?.RunState?.CurrentRoom is MerchantRoom)
            return true;

        if (newPileType != PileType.Deck)
            return true;
        if (card.Type == CardType.Curse)
            return true;
        if (card.Owner == null)
            return true;

        // 第0层初始牌组不替换
        var runState = card.Owner.RunState;
        if (runState != null)
        {
            try
            {
                if (runState.TotalFloor <= 0)
                    return true;
            }
            catch { }
        }

        try
        {
            Log.Debug($"Card '{card.Id?.Entry}' → relic");
        }
        catch { }

        // 增加守卫深度防止 RelicCmdObtainPatch 再换回来
        SwapGuard.Depth++;
        __result = GrantRelicInsteadAsync(card);
        return false;
    }

    private static async Task<CardPileAddResult> GrantRelicInsteadAsync(CardModel card)
    {
        Player owner = card.Owner!;
        try
        {
            RelicModel randomRelic = RelicFactory.PullNextRelicFromFront(owner).ToMutable();
            await RelicCmd.Obtain(randomRelic, owner);
            Log.Debug($"OK: card '{card.Id.Entry}' → relic '{randomRelic.Id.Entry}'");
        }
        catch (Exception ex)
        {
            Log.Error($"Card→relic failed: {ex.Message}");
        }
        finally
        {
            SwapGuard.Depth--;
        }

        return new CardPileAddResult { success = true, cardAdded = card };
    }
}
