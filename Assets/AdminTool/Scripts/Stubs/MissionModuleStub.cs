using HumbleBeginnings.Debugging;

namespace HumbleBeginnings.Admin.Stubs
{
    public static class MissionModuleStub
    {
        public static void GenerateMissions()
        {
            DebugLog.Emit(
                DebugLogRealm.MissionModule,
                "Mission generation requested"
            );
        }
    }
}
