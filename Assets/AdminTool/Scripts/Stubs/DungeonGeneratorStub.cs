using HumbleBeginnings.Debugging;

namespace HumbleBeginnings.Admin.Stubs
{
    public static class DungeonGeneratorStub
    {
        public static void BuildDungeon(string dungeonId)
        {
            DebugLog.Emit(
                DebugLogRealm.DungeonGenerator,
                $"Dungeon build requested: {dungeonId}"
            );
        }
    }
}
