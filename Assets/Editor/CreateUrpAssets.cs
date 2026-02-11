#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class CreateUrpAssets
{
    [MenuItem("Tools/WorldViewer/Create URP 3D Renderer Data")]
    public static void CreateRendererData()
    {
        // Works across URP versions (UniversalRendererData is the modern name)
        var t = Type.GetType("UnityEngine.Rendering.Universal.UniversalRendererData, Unity.RenderPipelines.Universal.Runtime")
             ?? Type.GetType("UnityEngine.Rendering.Universal.ForwardRendererData, Unity.RenderPipelines.Universal.Runtime");

        if (t == null)
        {
            EditorUtility.DisplayDialog(
                "URP Runtime Not Found",
                "Could not find UniversalRendererData/ForwardRendererData.\n" +
                "Check Package Manager: Universal RP must be Installed.",
                "OK");
            return;
        }

        var asset = ScriptableObject.CreateInstance(t);
        if (asset == null)
        {
            EditorUtility.DisplayDialog("Failed", "Could not create renderer data instance.", "OK");
            return;
        }

        var path = AssetDatabase.GenerateUniqueAssetPath("Assets/UniversalRenderer3D.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);

        Debug.Log($"[WorldViewer] Created: {path}");
    }
}
#endif
