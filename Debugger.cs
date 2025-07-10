using Crestron.SimplSharp;

namespace PanasonicMediaProductionSuite
{
    public static class Debugger
    {
        public static bool DebugEnable { get; set; }

        public static void Log(object obj, string message, string details)
        {
            if (DebugEnable)
            {
                CrestronConsole.PrintLine($"{nameof(PanasonicMediaProductionSuite)}.{obj} {message}: {details}");
            }
        }
    }
}
