namespace Defaults
{
    public static class Debug
    {
        public static void Log(object message)
        {
#if DEBUG
            Verse.Log.Message($"[{DefaultsMod.PACKAGE_NAME}] {message}");
#endif
        }
    }
}
