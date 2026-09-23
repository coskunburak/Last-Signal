namespace LastSignal.Persistence
{
    /// <summary>Main-thread, synchronous cross-owner mutation barrier. Never held across a frame/await.</summary>
    public static class OwnershipTransaction
    {
        static int depth;
        public static bool Active => depth != 0;
        internal static void Enter() => depth++;
        internal static void Exit() => depth--;
    }
}
