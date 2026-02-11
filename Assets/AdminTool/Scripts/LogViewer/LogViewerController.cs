using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using HumbleBeginnings.Admin.Logging;

namespace HumbleBeginnings.Admin.LogViewer
{
    public sealed class LogViewerController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private LogSourceRegistry logSourceRegistry;

        [Header("UI")]
        [SerializeField] private TMP_Dropdown sourceDropdown;
        [SerializeField] private TMP_Text logText;

        [Header("Scroll")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform scrollContent;

        [Header("Polling")]
        [SerializeField] private float pollIntervalSeconds = 0.5f;

        [Tooltip("How close to the bottom counts as 'following live output'")]
        [SerializeField] private float bottomSnapThreshold = 0.02f;

        private LogSourceDefinition activeSource;
        private float pollTimer;
        private long lastFilePosition;

        private FileStream activeStream;
        private StreamReader activeReader;

        private bool viewerVisible;

        private void Awake()
        {
            PopulateSourceDropdown();
        }

        private void Update()
        {
            if (!viewerVisible)
                return;

            if (activeReader == null)
                return;

            pollTimer += Time.unscaledDeltaTime;

            if (pollTimer < pollIntervalSeconds)
                return;

            pollTimer = 0f;
            PollFileForNewContent();
        }

        // Called by LogViewerUI
        public void OnViewerShown()
        {
            viewerVisible = true;

            if (activeSource != null)
                OpenFileAndLoadInitial(activeSource);
        }

        // Called by LogViewerUI
        public void OnViewerHidden()
        {
            viewerVisible = false;
            CloseActiveFile();
        }

        private void PopulateSourceDropdown()
        {
            if (sourceDropdown == null || logSourceRegistry == null)
                return;

            sourceDropdown.ClearOptions();
            sourceDropdown.onValueChanged.RemoveAllListeners();

            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var source in logSourceRegistry.Sources)
            {
                if (source == null)
                    continue;

                options.Add(new TMP_Dropdown.OptionData(source.DisplayName));
            }

            sourceDropdown.AddOptions(options);
            sourceDropdown.onValueChanged.AddListener(OnSourceSelected);

            if (logSourceRegistry.Sources.Count > 0)
                activeSource = logSourceRegistry.Sources[0];
        }

        private void OnSourceSelected(int index)
        {
            if (logSourceRegistry == null)
                return;

            if (index < 0 || index >= logSourceRegistry.Sources.Count)
                return;

            activeSource = logSourceRegistry.Sources[index];

            if (viewerVisible)
                OpenFileAndLoadInitial(activeSource);
        }

        private void OpenFileAndLoadInitial(LogSourceDefinition source)
        {
            CloseActiveFile();

            if (logText == null || scrollContent == null || scrollRect == null)
                return;

            logText.text = string.Empty;
            lastFilePosition = 0;

            if (source == null || string.IsNullOrEmpty(source.LogFilePath))
                return;

            if (!File.Exists(source.LogFilePath))
            {
                logText.text = $"[LogViewer] File not found:\n{source.LogFilePath}";
                UpdateContentAndScroll(forceToBottom: true);
                return;
            }

            activeStream = new FileStream(
                source.LogFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

            activeReader = new StreamReader(activeStream);

            string contents = activeReader.ReadToEnd();
            lastFilePosition = activeStream.Position;

            logText.text = contents;
            UpdateContentAndScroll(forceToBottom: true);
        }

        private void PollFileForNewContent()
        {
            if (activeStream == null || activeReader == null)
                return;

            if (activeStream.Length <= lastFilePosition)
                return;

            // Check scroll position BEFORE modifying content
            bool wasAtBottom = IsNearBottom();

            activeStream.Seek(lastFilePosition, SeekOrigin.Begin);
            string newData = activeReader.ReadToEnd();
            lastFilePosition = activeStream.Position;

            if (string.IsNullOrEmpty(newData))
                return;

            logText.text += newData;
            UpdateContentAndScroll(forceToBottom: wasAtBottom);
        }

        private bool IsNearBottom()
        {
            if (scrollRect == null)
                return true;

            // VerticalNormalizedPosition: 1 = top, 0 = bottom
            return scrollRect.verticalNormalizedPosition <= bottomSnapThreshold;
        }

        private void CloseActiveFile()
        {
            if (activeReader != null)
            {
                activeReader.Close();
                activeReader = null;
            }

            if (activeStream != null)
            {
                activeStream.Close();
                activeStream = null;
            }

            lastFilePosition = 0;
        }

        private void UpdateContentAndScroll(bool forceToBottom)
        {
            logText.ForceMeshUpdate();

            scrollContent.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                logText.preferredHeight
            );

            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();

            if (forceToBottom)
                scrollRect.verticalNormalizedPosition = 0f; // bottom
        }
    }
}
