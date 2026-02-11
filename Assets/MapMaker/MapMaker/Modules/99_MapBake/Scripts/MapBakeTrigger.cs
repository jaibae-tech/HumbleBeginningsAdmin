using UnityEngine;
using MapMaker.Modules.MapBake5.Config;

namespace MapMaker.Modules.MapBake5.Scripts
{
    /// <summary>
    /// Attach this to a GameObject in your Admin Tool scene. Hook its RunBake() to a UI button.
    /// </summary>
    public sealed class MapBakeTrigger : MonoBehaviour
    {
        public HB_MapBakeConfig Config;

        [ContextMenu("Run MapBake")]
        public void RunBake()
        {
            if (Config == null)
            {
                Debug.LogError("[MapBakeTrigger] Config is not assigned.");
                return;
            }

            var driver = new MapBakeDriver(Config);
            driver.Run();
        }
    }
}
