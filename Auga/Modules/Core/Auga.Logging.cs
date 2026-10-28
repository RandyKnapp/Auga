using BepInEx.Logging;
using UnityEngine;

namespace Auga
{
    // Auga's own log lines, gated by [Logging] LoggingEnabled and LogLevel (ValConfig). Plain Debug.Log* calls
    // elsewhere always reach Player.log.
    public partial class Auga
    {
        /// <summary>
        /// Whether a message of this level goes out. Before the config is bound, warnings and errors always do; a
        /// failure that early used to throw inside the logger itself and hide the real problem.
        /// </summary>
        private static bool ShouldLog(LogLevel level)
        {
            if (ValConfig.LoggingEnabled == null || ValConfig.LogLevel == null)
                return level == LogLevel.Warning || level == LogLevel.Error;
            return ValConfig.LoggingEnabled.Value && ValConfig.LogLevel.Value <= level;
        }

        public static void Log(string message)
        {
            if (!ShouldLog(LogLevel.Info)) return;
            if (_instance != null && _instance.Logger != null) _instance.Logger.LogInfo(message);
            else Debug.Log($"[Auga] {message}");
        }

        public static void LogWarning(string message)
        {
            if (!ShouldLog(LogLevel.Warning)) return;
            if (_instance != null && _instance.Logger != null) _instance.Logger.LogWarning(message);
            else Debug.LogWarning($"[Auga] {message}");
        }

        public static void LogError(string message)
        {
            if (!ShouldLog(LogLevel.Error)) return;
            if (_instance != null && _instance.Logger != null) _instance.Logger.LogError(message);
            else Debug.LogError($"[Auga] {message}");
        }
    }
}
