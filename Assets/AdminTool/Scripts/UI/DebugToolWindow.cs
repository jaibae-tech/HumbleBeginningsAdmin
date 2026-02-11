using UnityEngine;

namespace HumbleBeginnings.Admin.UI
{
    public class DebugToolWindow : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        private void Awake()
        {
            root.SetActive(false); // start hidden
        }

        public void Toggle()
        {
            root.SetActive(!root.activeSelf);
        }

        public void Show()
        {
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}
