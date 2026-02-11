using System;
using System.Collections.Generic;
using UnityEngine;
using HumbleBeginnings.Admin.Debug;
using HumbleBeginnings.Admin.Modules;

namespace HumbleBeginnings.Admin.Core
{
    public class AdminApp : MonoBehaviour
    {
        public static AdminApp Instance { get; private set; }

        private readonly Dictionary<Type, IAdminModule> modules = new();
        private IAdminModule currentModule;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterModules();
        }

        private void Start()
        {
            LogStore.Add(LogRealm.Admin, "AdminApp started");
            GoToMainMenu();
        }

        private void RegisterModules()
        {
            modules.Clear();

            var behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var mb in behaviours)
            {
                if (mb is IAdminModule module)
                {
                    modules[module.GetType()] = module;
                    LogStore.Add(
                        LogRealm.Admin,
                        $"Registered module: {module.GetType().Name}"
                    );
                }
            }
        }

        private void SwitchTo<T>() where T : IAdminModule
        {
            var type = typeof(T);

            if (!modules.TryGetValue(type, out var next))
            {
                LogStore.Add(
                    LogRealm.Admin,
                    $"ERROR: Module not registered: {type.Name}"
                );
                return;
            }

            currentModule?.Exit();
            currentModule = next;
            currentModule.Enter();

            LogStore.Add(
                LogRealm.Admin,
                $"Switched to module: {type.Name}"
            );
        }

        // --------------------------------------------------
        // Public navigation API (wired to buttons)
        // --------------------------------------------------

        public void GoToMainMenu()       => SwitchTo<MainMenuModule>();
        public void GoToWorldGenerator() => SwitchTo<WorldGeneratorModule>();
        public void GoToWorldViewer()    => SwitchTo<WorldViewerModule>();
        public void GoToDataEditor()     => SwitchTo<DataEditorModule>();
        public void GoToEventPlayer()    => SwitchTo<EventPlayerModule>();

        public void Quit()
        {
            LogStore.Add(LogRealm.Admin, "Quit requested");
            Application.Quit();
        }
    }
}
