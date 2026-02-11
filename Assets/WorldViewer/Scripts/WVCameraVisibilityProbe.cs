using UnityEngine;
using UnityEngine.InputSystem;

namespace HumbleBeginnings.WorldViewer
{
    public class WVCameraVisibilityProbe : MonoBehaviour
    {
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
                Dump();
        }

        void Dump()
        {
            Debug.Log("===== WV CAMERA VISIBILITY PROBE =====");

            // List all cameras that could be rendering the Game view
            foreach (var c in Camera.allCameras)
            {
                Debug.Log(
                    $"Cam '{c.name}' enabled={c.enabled} active={c.gameObject.activeInHierarchy} " +
                    $"depth={c.depth} display={c.targetDisplay} clear={c.clearFlags} " +
                    $"pos={c.transform.position} fwd={c.transform.forward} " +
                    $"cullMask=0x{c.cullingMask:X}"
                );
            }

            var chunk = GameObject.Find("WV_TestChunk");
            Debug.Log($"Chunk exists: {chunk != null}");

            if (chunk != null)
            {
                var r = chunk.GetComponent<Renderer>();
                Debug.Log($"Chunk renderer exists: {r != null}");

                if (r != null)
                {
                    Debug.Log($"Chunk layer={chunk.layer} renderer.enabled={r.enabled} isVisible={r.isVisible}");
                    Debug.Log($"Chunk bounds(world): center={r.bounds.center} extents={r.bounds.extents}");
                }

                // Check against the highest-depth enabled camera on Display 1
                Camera best = null;
                foreach (var c in Camera.allCameras)
                {
                    if (!c.enabled || !c.gameObject.activeInHierarchy) continue;
                    if (c.targetDisplay != 0) continue; // Display 1 is index 0
                    if (best == null || c.depth > best.depth) best = c;
                }

                Debug.Log($"Best camera (Display1, highest depth): {(best ? best.name : "NONE")}");

                if (best != null && r != null)
                {
                    var planes = GeometryUtility.CalculateFrustumPlanes(best);
                    bool inFrustum = GeometryUtility.TestPlanesAABB(planes, r.bounds);
                    Debug.Log($"Chunk inside best camera frustum: {inFrustum}");
                }
            }

            Debug.Log("======================================");
        }
    }
}

