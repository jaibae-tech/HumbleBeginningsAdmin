using UnityEngine;

namespace MapMaker.Core.Export
{
    [CreateAssetMenu(
        fileName = "HB_ExportConfig_Default",
        menuName = "Humble Beginnings / MapMaker / Export Config")]
    public sealed class HB_ExportConfig : ScriptableObject 
    {
        [Header("Export (Global)")]
        [Tooltip("Export folder path. Use relative path (e.g., 'MapMakerExports') for <ProjectRoot>/Logs/<FolderName> or absolute path (e.g., 'H:/UnityDebug/MapMaker') for custom location.")]
        public string ExportFolderName = "MapMakerExports";

        [Range(1, 8)]
        public int ExportTilePixelSize = 2;

        public bool ExportFlipVertical = true;
    }
}
