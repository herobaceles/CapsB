using UnityEngine;
using UnityEditor;

// Simple Editor utility to find GameObjects with missing (null) components
// which often cause the "SerializedObjectNotCreatableException" and inspector errors.
public static class MissingComponentFinder
{
    [MenuItem("Tools/Find Missing Components in Scene")]
    public static void FindMissingComponentsInScene()
    {
        int missingCount = 0;
        var allGOs = Object.FindObjectsOfType<GameObject>();

        foreach (var go in allGOs)
        {
            // Skip hidden/internal objects
            if ((go.hideFlags & HideFlags.HideAndDontSave) != 0) continue;

            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                    Debug.LogError($"Missing script on GameObject: {GetGameObjectPath(go)} (component index {i})", go);
                }
            }
        }

        Debug.Log($"Missing component scan complete. Found {missingCount} missing components.");
        if (missingCount == 0)
            EditorUtility.DisplayDialog("Find Missing Components", "No missing components found in the active scenes.", "OK");
        else
            EditorUtility.DisplayDialog("Find Missing Components", $"Found {missingCount} missing components. See Console for details.", "OK");
    }

    [MenuItem("Tools/Remove Missing Components in Scene")]
    public static void RemoveMissingComponentsInScene()
    {
        int removedCount = 0;
        var allGOs = Object.FindObjectsOfType<GameObject>();

        // Operate on each GameObject's serialized representation to remove null component entries
        foreach (var go in allGOs)
        {
            if ((go.hideFlags & HideFlags.HideAndDontSave) != 0) continue;

            SerializedObject so = new SerializedObject(go);
            SerializedProperty componentsProp = so.FindProperty("m_Component");
            if (componentsProp == null || !componentsProp.isArray) continue;

            // Iterate backwards when removing array elements
            for (int i = componentsProp.arraySize - 1; i >= 0; i--)
            {
                var element = componentsProp.GetArrayElementAtIndex(i);
                var compProp = element.FindPropertyRelative("component");
                if (compProp != null && compProp.objectReferenceValue == null)
                {
                    componentsProp.DeleteArrayElementAtIndex(i);
                    removedCount++;
                }
            }

            if (removedCount > 0)
                so.ApplyModifiedProperties();
        }

        Debug.Log($"Removed {removedCount} missing component entries from scene GameObjects.");
        if (removedCount == 0)
            EditorUtility.DisplayDialog("Remove Missing Components", "No missing components found to remove.", "OK");
        else
            EditorUtility.DisplayDialog("Remove Missing Components", $"Removed {removedCount} missing component entries. See Console for details.", "OK");
    }

    private static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform current = go.transform;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }
        return path;
    }
}
