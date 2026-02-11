using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HumbleBeginnings.WorldViewer
{
    public sealed class WorldViewMode : MonoBehaviour
    {
        [Header("Auto-wire by name (defaults match your scene)")]
        public string AdminCanvasName = "AdminCanvas";
        public string WorldCameraName = "WorldCamera";
        public string MainCameraName = "Main Camera";

        [Header("Optional: auto-hide these under AdminCanvas (by path)")]
        public string BackgroundPath = "Background";
        public string TitlePath = "Title";
        public string MainMenuPanelPath = "MainMenuPanel";

        [Header("Hotkeys (Input System)")]
        public bool EnableHotkeys = true;
        public Key EnterKey = Key.F1;
        public Key ExitKey = Key.Escape;

        Canvas _adminCanvas;
        Camera _mainCam;
        Camera _worldCam;

        GameObject _bg;
        GameObject _title;
        GameObject _mainMenu;

        bool _wired;
        bool _inWorld;

        void Awake()
        {
            WireOnce();
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (!EnableHotkeys) return;
            if (Keyboard.current == null) return;

            if (!_inWorld && Keyboard.current[EnterKey].wasPressedThisFrame) Enter();
            else if (_inWorld && Keyboard.current[ExitKey].wasPressedThisFrame) Exit();
#endif
        }

        public void Enter()
        {
            WireOnce();
            _inWorld = true;

            // Hide UI safely (without you manually moving panels again)
            if (_bg) _bg.SetActive(false);
            if (_title) _title.SetActive(false);
            if (_mainMenu) _mainMenu.SetActive(false);

            // If you want *all* UI gone while in world view:
            if (_adminCanvas) _adminCanvas.enabled = false;

            // Camera toggle: show world, hide main
            if (_mainCam) _mainCam.enabled = false;
            if (_worldCam) _worldCam.enabled = true;
        }

        public void Exit()
        {
            WireOnce();
            _inWorld = false;

            if (_adminCanvas) _adminCanvas.enabled = true;

            if (_bg) _bg.SetActive(true);
            if (_title) _title.SetActive(true);
            if (_mainMenu) _mainMenu.SetActive(true);

            if (_worldCam) _worldCam.enabled = false;
            if (_mainCam) _mainCam.enabled = true;
        }

        void WireOnce()
        {
            if (_wired) return;

            var adminGo = GameObject.Find(AdminCanvasName);
            if (adminGo)
            {
                _adminCanvas = adminGo.GetComponent<Canvas>();

                _bg = FindChildByPath(adminGo.transform, BackgroundPath)?.gameObject;
                _title = FindChildByPath(adminGo.transform, TitlePath)?.gameObject;
                _mainMenu = FindChildByPath(adminGo.transform, MainMenuPanelPath)?.gameObject;
            }

            var mainGo = GameObject.Find(MainCameraName);
            if (mainGo) _mainCam = mainGo.GetComponent<Camera>();

            var worldGo = GameObject.Find(WorldCameraName);
            if (worldGo) _worldCam = worldGo.GetComponent<Camera>();

            // Default state: UI/main camera on, world camera off (safe)
            if (_worldCam) _worldCam.enabled = false;

            _wired = true;
        }

        static Transform FindChildByPath(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path)) return null;

            var parts = path.Split('/');
            Transform cur = root;
            foreach (var p in parts)
            {
                cur = cur.Find(p);
                if (!cur) return null;
            }
            return cur;
        }
    }
}
