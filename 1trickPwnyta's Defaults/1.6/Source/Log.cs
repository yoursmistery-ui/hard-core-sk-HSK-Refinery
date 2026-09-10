namespace Defaults
{
    public static class Log
    {
        private static string TagMessage(object message) => $"[{DefaultsMod.PACKAGE_NAME}] {message}";

        public static void Debug(object message)
        {
#if DEBUG
            Verse.Log.Message(TagMessage($"(debug) {message}"));
#endif
        }

        public static void Info(object message)
        {
            Verse.Log.Message(TagMessage(message));
        }

        public static void Warn(object message)
        {
            Verse.Log.Warning(TagMessage(message));
        }

        public static void Error(object message)
        {
            Verse.Log.Error(TagMessage(message));
        }
    }
}
