using MegaCrit.Sts2.Core.Logging;

namespace swapRelicsAndCards;

internal static class Log
{
    private static readonly Logger Logger = new("SwapRelicsAndCards", LogType.Generic);

    internal static void Info(string msg) => Logger.Info(msg);
    internal static void Warn(string msg) => Logger.Warn(msg);
    internal static void Error(string msg) => Logger.Error(msg);
    internal static void Debug(string msg) => Logger.Debug(msg);
}
