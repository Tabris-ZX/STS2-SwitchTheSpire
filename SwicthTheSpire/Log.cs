using MegaCrit.Sts2.Core.Logging;

namespace SwitchTheSpire;

//统一日志输出,暂时写着,万一有用呢
internal static class Log
{
    private static readonly Logger Logger = new("SwitchTheSpire", LogType.Generic);

    internal static void Info(string msg) => Logger.Info(msg);
    internal static void Warn(string msg) => Logger.Warn(msg);
    internal static void Error(string msg) => Logger.Error(msg);
    internal static void Debug(string msg) => Logger.Debug(msg);
}
