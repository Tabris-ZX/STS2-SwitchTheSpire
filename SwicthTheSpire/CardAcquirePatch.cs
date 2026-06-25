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
using MegaCrit.Sts2.Core.Rewards;

namespace SwitchTheSpire;

/// <summary>
/// 拦截非奖励流程的卡牌获取，转为遗物或药水。
/// </summary>
[HarmonyPatch]
internal static class CardAcquirePatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(CardPileCmd), "Add", [
            typeof(CardModel),
            typeof(PileType),
            typeof(CardPilePosition),
            typeof(AbstractModel),
            typeof(bool)
        ]);

    private static bool Prefix(
        CardModel card,
        PileType newPileType,
        CardPilePosition position,
        AbstractModel? clonedBy,
        bool skipVisuals,
        ref Task<CardPileAddResult> __result)
    {
        if (Mod.Config.CardBecomes == SwapTarget.Card) return true;

        if (SwapGuard.ShouldBypass(Mod.Config.CardBecomes, card.Owner))
            return true;

        if (newPileType != PileType.Deck) return true;
        if (card.Type == CardType.Curse) return true;

        Log.Debug($"卡牌 '{card.Id.Entry}' -> {Mod.Config.CardBecomes}");

        SwapGuard.Depth++;
        __result = GrantAsync(card);
        return false;
    }

    private static async Task<CardPileAddResult> GrantAsync(CardModel card)
    {
        var owner = card.Owner!;
        try
        {
            switch (Mod.Config.CardBecomes)
            {
                case SwapTarget.Relic:
                    var relic = RelicFactory.PullNextRelicFromFront(owner).ToMutable();
                    await RelicCmd.Obtain(relic, owner);
                    Log.Debug($"完成: 卡牌 '{card.Id.Entry}' -> 遗物 '{relic.Id.Entry}'");
                    break;
                case SwapTarget.Potion:
                    var reward = new PotionReward(owner);
                    reward.Populate();
                    await reward.SelectUnsynchronized();
                    Log.Debug($"完成: 卡牌 '{card.Id.Entry}' -> 药水");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"卡牌->{Mod.Config.CardBecomes} 失败: {ex.Message}");
        }
        finally
        {
            SwapGuard.Depth--;
        }
        return new CardPileAddResult { success = true, cardAdded = card };
    }
}
