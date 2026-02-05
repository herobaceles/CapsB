using UnityEditor;
using UnityEngine;
using System.Linq;

public class AutoAssignURPTextures
{
    [MenuItem("Tools/URP/Auto Assign Textures To Materials")]
    public static void AutoAssignTextures()
    {
        // Get all textures
        var textures = AssetDatabase.FindAssets("t:Texture2D")
            .Select(guid => AssetDatabase.LoadAssetAtPath<Texture2D>(
                AssetDatabase.GUIDToAssetPath(guid)))
            .ToList();

        // Get all materials
        var materials = AssetDatabase.FindAssets("t:Material")
            .Select(guid => AssetDatabase.LoadAssetAtPath<Material>(
                AssetDatabase.GUIDToAssetPath(guid)))
            .ToList();

        int fixedCount = 0;

        foreach (var mat in materials)
        {
            if (mat == null)
                continue;

            // Ensure URP/Lit
            if (mat.shader.name != "Universal Render Pipeline/Lit")
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");

            // Skip if Base Map already assigned
            if (mat.GetTexture("_BaseMap") != null)
                continue;

            string matName = mat.name.ToLower();

            // Try to find matching texture by name
            var match = textures.FirstOrDefault(t =>
                t != null &&
                matName.Contains(t.name.ToLower())
            );

            if (match != null)
            {
                mat.SetTexture("_BaseMap", match);
                EditorUtility.SetDirty(mat);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Auto-Assign Complete. Fixed {fixedCount} materials.");
    }
}
