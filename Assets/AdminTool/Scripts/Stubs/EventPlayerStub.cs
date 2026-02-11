using HumbleBeginnings.Debugging;

namespace HumbleBeginnings.Admin.Stubs
{
    public static class EventPlayerStub
    {
        public static void PlayEvent(string eventId)
        {
            DebugLog.Emit(
                DebugLogRealm.EventPlayer,
                $"Event playback requested: {eventId}"
            );
        }
    }
}
