using UnityEngine;
using HumbleBeginnings.Admin.Core;

namespace HumbleBeginnings.Admin.Modules
{
    public abstract class AdminModuleBase : MonoBehaviour, IAdminModule
    {
        [SerializeField] protected GameObject rootPanel;

        public virtual void Enter()
        {
            rootPanel.SetActive(true);
        }

        public virtual void Exit()
        {
            rootPanel.SetActive(false);
        }

        public virtual void Tick() { }
    }
}
