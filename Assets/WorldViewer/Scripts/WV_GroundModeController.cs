
using UnityEngine;

public class WV_GroundModeController : MonoBehaviour
{
    public Camera TargetCamera;
    public Material MapMaterial;
    public Material GroundMaterial;

    public float MinHeight = 200f;
    public float MaxHeight = 1200f;

    void Update()
    {
        if (!TargetCamera || !MapMaterial || !GroundMaterial) return;

        float h = TargetCamera.transform.position.y;
        float t = Mathf.InverseLerp(MaxHeight, MinHeight, h);

        MapMaterial.SetFloat("_Blend", 1 - t);
        GroundMaterial.SetFloat("_Blend", t);
    }
}
