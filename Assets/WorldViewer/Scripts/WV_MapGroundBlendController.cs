using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    /// <summary>
    /// Optional helper: drive a smooth crossfade between map material and ground material
    /// based on camera height and pitch. This does not create materials; it only sets floats.
    ///
    /// Expected shader float properties (set on both materials):
    ///   _HB_ModeBlend : 0 = map look, 1 = ground look
    ///
    /// If your map shader does not use _HB_ModeBlend, it will ignore it (safe).
    /// </summary>
    public sealed class WV_MapGroundBlendController : MonoBehaviour
    {
        public Camera TargetCamera;

        [Header("Materials (optional)")]
        public Material MapMaterial;
        public Material GroundMaterial;

        [Header("Blend From Height (Y)")]
        public float HeightAtMap = 900f;
        public float HeightAtGround = 250f;

        [Header("Blend From Pitch (degrees)")]
        [Tooltip("At or above this pitch (looking down), favor map mode.")]
        public float PitchAtMap = 70f;

        [Tooltip("At or below this pitch (near-horizontal), favor ground mode.")]
        public float PitchAtGround = 15f;

        [Header("Smoothing")]
        [Range(0f, 20f)]
        public float BlendSmoothing = 8f;

        float _blend = 1f;
        static readonly int ModeBlendID = Shader.PropertyToID("_HB_ModeBlend");

        void Reset()
        {
            if (!TargetCamera) TargetCamera = Camera.main;
        }

        void Update()
        {
            if (!TargetCamera) return;

            // Height blend
            float h = TargetCamera.transform.position.y;
            float tH = Mathf.InverseLerp(HeightAtMap, HeightAtGround, h);

            // Pitch blend (angle between forward and horizontal plane)
            float pitch = Vector3.Angle(Vector3.ProjectOnPlane(TargetCamera.transform.forward, Vector3.up), TargetCamera.transform.forward);
            float tP = Mathf.InverseLerp(PitchAtMap, PitchAtGround, pitch);

            float target = Mathf.Clamp01(Mathf.Max(tH, tP));
            _blend = Mathf.Lerp(_blend, target, 1f - Mathf.Exp(-BlendSmoothing * Time.deltaTime));

            if (MapMaterial) MapMaterial.SetFloat(ModeBlendID, _blend);
            if (GroundMaterial) GroundMaterial.SetFloat(ModeBlendID, _blend);
        }
    }
}
