using UnityEngine;
using HumbleBeginnings.Admin.Debug;

namespace HumbleBeginnings.Admin.Modules
{
    public class DataEditorModule : MonoBehaviour, IAdminModule
    {
        [SerializeField] private GameObject rootPanel;

        public void Enter()
        {
            rootPanel.SetActive(true);
            LogStore.Add(LogRealm.Admin, "Data Editor entered");
        }

        public void Exit()
        {
            rootPanel.SetActive(false);
            LogStore.Add(LogRealm.Admin, "Data Editor exited");
        }
    }
}

