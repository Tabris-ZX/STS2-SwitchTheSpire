using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;

namespace SwitchTheSpire;

/// <summary>
/// 防递归守卫 + 通用拦截跳过检查。
/// </summary>
internal static class SwapGuard
{
    internal static int Depth;

    /// <summary>
    /// 所有 acquire 补丁的公共放行条件：
    /// 全局开关关闭 / 目标为 None / 正在守卫中 / 商店 / 无玩家 / 第0层初始物品。
    /// </summary>
    internal static bool ShouldBypass(SwapTarget target, Player? player)
    {
        if (!Mod.Config.Enabled) return true;
        if (Depth > 0) return true;
        if (player == null) return true;
        if (player.RunState?.CurrentRoom is MerchantRoom) return true;
        if ((player.RunState?.TotalFloor ?? 1) <= 0) return true;
        return false;
    }
}
