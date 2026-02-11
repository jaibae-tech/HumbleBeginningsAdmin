using HumbleBeginnings.Admin.Stubs;

namespace HumbleBeginnings.Admin.Modules
{
    public class EventPlayerModule : AdminModuleBase
    {
        public override void Enter()
        {
            base.Enter();

            EventPlayerStub.PlayEvent("DEBUG_EVENT_001");
        }
    }
}

