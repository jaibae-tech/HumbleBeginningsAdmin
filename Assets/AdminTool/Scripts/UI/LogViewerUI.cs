using UnityEngine;
using HumbleBeginnings.Admin.LogViewer;

namespace HumbleBeginnings.Admin.UI
{
    public class LogViewerUI : MonoBehaviour
    {
        [SerializeField] private LogViewerController logViewerController;

        private bool isVisible;

        private void Awake()
        {
            gameObject.SetActive(false);
            isVisible = false;
        }

        public void Toggle()
        {
            if (isVisible)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            isVisible = true;
            gameObject.SetActive(true);

            if (logViewerController != null)
                logViewerController.OnViewerShown();
        }

        public void Hide()
        {
            isVisible = false;

            if (logViewerController != null)
                logViewerController.OnViewerHidden();

            gameObject.SetActive(false);
        }
    }
}
