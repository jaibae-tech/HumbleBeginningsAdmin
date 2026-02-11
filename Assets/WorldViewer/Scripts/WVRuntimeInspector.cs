using UnityEngine;
using UnityEngine.InputSystem;

namespace HumbleBeginnings.WorldViewer
{
    public class WVRuntimeInspector : MonoBehaviour
    {
        void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                DumpState();
            }
        }

        void DumpState()
        {
            Debug.Log("==== WV RUNTIME DUMP START ====");

            var rig = Object.FindFirstObjectByType<WorldCameraRig>();
            var controller = Object.FindFirstObjectByType<WorldViewerController>();
            var chunk = GameObject.Find("WV_TestChunk");

            Debug.Log($"Rig Found: {rig != null}");
            Debug.Log($"Controller Found: {controller != null}");
            Debug.Log($"Chunk Found: {chunk != null}");

            if (rig != null)
            {
                Debug.Log($"Rig Position: {rig.transform.position}");
                Debug.Log($"Pivot Rotation: {rig.Pivot?.rotation.eulerAngles}");
                Debug.Log($"Camera Pos: {rig.Cam?.transform.position}");
            }

            if (controller != null)
            {
                Debug.Log($"World ID: {controller.WorldId}");
                Debug.Log($"Tile Size: {controller.TileSize}");
                Debug.Log($"Height Scale: {controller.HeightScale}");
            }

            if (chunk != null)
            {
                Debug.Log($"Chunk Position: {chunk.transform.position}");

                var meshFilter = chunk.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Debug.Log($"Vertices: {meshFilter.sharedMesh.vertexCount}");
                    Debug.Log($"Bounds: {meshFilter.sharedMesh.bounds}");
                }
                else
                {
                    Debug.Log("Chunk mesh missing.");
                }
            }

            Debug.Log("==== WV RUNTIME DUMP END ====");
        }
    }
}
