using HumbleBeginnings.Debugging;

namespace HumbleBeginnings.Admin.Stubs
{
    public static class WorldGeneratorStub
    {
        public static void GenerateNewWorld(int seed)
        {
            DebugLog.Emit(
                DebugLogRealm.WorldSeeder,
                $"GenerateNewWorld called with seed={seed}"
            );

            // No world mutation (Phase 2A)
        }
    }
}
