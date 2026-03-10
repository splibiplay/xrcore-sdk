using UnityEngine;

namespace XRCore.Core
{
    public static class XRCoreDebug
    {
        public static bool EnableLogs { get; set; } = true;
        public static bool EnableWarnings { get; set; } = true;
        public static bool EnableErrors { get; set; } = true;

        public static void Log(string message)
        {
            if (EnableLogs)
            {
                Debug.Log($"[XRCore] {message}");
            }
        }

        public static void Warning(string message)
        {
            if (EnableWarnings)
            {
                Debug.LogWarning($"[XRCore] {message}");
            }
        }

        public static void Error(string message)
        {
            if (EnableErrors)
            {
                Debug.LogError($"[XRCore] {message}");
            }
        }
    }
}
