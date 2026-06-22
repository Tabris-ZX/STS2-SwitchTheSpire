namespace swapRelicsAndCards;

/// <summary>
/// 防递归守卫。奖励层交换期间设为非零，底层命令补丁检测后放行，
/// </summary>
internal static class SwapGuard
{
    /// <summary>
    /// 当前嵌套深度。&gt; 0 表示正在奖励选择流程中，命令层补丁应放行。
    /// </summary>
    internal static int Depth;
}
