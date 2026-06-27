#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Upgrades all materials in the city asset folder to properly work with URP.
/// The materials already reference URP/Lit shader, but their keywords and
/// render queue may not be set correctly after conversion.
/// This script re-validates each material so Unity recognizes the shader properties.
/// </summary>
public static class UpgradeCityMaterials
{
    [MenuItem("Surveyor/Upgrade City Materials to URP", false, 40)]
    public static void UpgradeMaterials()
    {
        string[] searchPaths = new string[]
        {
            "Assets/Versatile Studio Assets"
        };

        // Find the URP/Lit shader
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Error", "URP/Lit shader not found!", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", searchPaths);
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Force-set the shader to URP/Lit (re-validates all properties)
            mat.shader = urpLit;

            // Ensure _BaseMap is set if _MainTex exists
            if (mat.HasProperty("_MainTex") && mat.HasProperty("_BaseMap"))
            {
                var mainTex = mat.GetTexture("_MainTex");
                if (mainTex != null && mat.GetTexture("_BaseMap") == null)
                {
                    mat.SetTexture("_BaseMap", mainTex);
                }
            }

            // Ensure _BaseColor is set if _Color exists
            if (mat.HasProperty("_Color") && mat.HasProperty("_BaseColor"))
            {
                var col = mat.GetColor("_Color");
                mat.SetColor("_BaseColor", col);
            }

            // Re-enable keywords based on assigned textures
            if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null)
                mat.EnableKeyword("_NORMALMAP");
            else
                mat.DisableKeyword("_NORMALMAP");

            if (mat.HasProperty("_EmissionMap") && mat.GetTexture("_EmissionMap") != null)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            if (mat.HasProperty("_MetallicGlossMap") && mat.GetTexture("_MetallicGlossMap") != null)
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");

            if (mat.HasProperty("_OcclusionMap") && mat.GetTexture("_OcclusionMap") != null)
                mat.EnableKeyword("_OCCLUSIONMAP");

            // Ensure surface type is Opaque and render queue is correct
            if (mat.HasProperty("_Surface"))
            {
                float surfaceType = mat.GetFloat("_Surface");
                if (surfaceType == 0) // Opaque
                {
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    mat.SetOverrideTag("RenderType", "Opaque");
                    mat.SetFloat("_Blend", 0);
                    mat.SetFloat("_SrcBlend", 1);
                    mat.SetFloat("_DstBlend", 0);
                    mat.SetFloat("_ZWrite", 1);
                }
            }

            EditorUtility.SetDirty(mat);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Upgrade Complete! 🎨",
            $"Re-validated {fixedCount} materials with URP/Lit shader.\n" +
            "Keywords, textures, and render settings have been refreshed.\n\n" +
            "Press Play to see the city with full textures!",
            "Awesome!");

        Debug.Log($"[UpgradeCityMaterials] Re-validated {fixedCount} materials for URP.");
    }
}
#endif
