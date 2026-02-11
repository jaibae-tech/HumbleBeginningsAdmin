using UnityEngine;
using HumbleBeginnings.Admin.Debug;

namespace HumbleBeginnings.Admin.Modules
{
    public class WorldGeneratorModule : MonoBehaviour, IAdminModule
    {
        [SerializeField] private GameObject rootPanel;

        public void Enter()
        {
            rootPanel.SetActive(true);
            LogStore.Add(LogRealm.WorldSeeder, "World Generator entered");
        }

        public void Exit()
        {
            rootPanel.SetActive(false);
            LogStore.Add(LogRealm.WorldSeeder, "World Generator exited");
        }
    }
}

