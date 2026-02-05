using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Automatically remove missing component entries when entering Play Mode
// This prevents SerializedObject/Inspector errors caused by null component slots
[InitializeOnLoad]
public static class AutoRemoveMissingComponentsOnPlay
{
    static AutoRemoveMissingComponentsOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            // Run on entering play to ensure scene is clean
            RemoveMissingComponentsInActiveScene();
        }
    }

    [MenuItem("Tools/AutoFix/Remove Missing Components In Active Scene Now")]
    public static void MenuRemoveNow()
    {
        RemoveMissingComponentsInActiveScene();
    }

    private static void RemoveMissingComponentsInActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
            return;

        var roots = scene.GetRootGameObjects();
        int totalRemoved = 0;

        foreach (var root in roots)
        {
            totalRemoved += RemoveMissingComponentsRecursively(root);
        }

        if (totalRemoved > 0)
        {
            Debug.Log($"[AutoRemoveMissingComponentsOnPlay] Removed {totalRemoved} missing component entries in scene '{scene.name}'.");
            EditorSceneManager.MarkSceneDirty(scene);
        }
        else
        {
            Debug.Log("[AutoRemoveMissingComponentsOnPlay] No missing components found.");
        }
    }

    private static int RemoveMissingComponentsRecursively(GameObject go)
    {
        int removed = 0;
        removed += RemoveMissingComponentsFromGameObject(go);
        for (int i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i).gameObject;
            removed += RemoveMissingComponentsRecursively(child);
        }
        return removed;
    }

    private static int RemoveMissingComponentsFromGameObject(GameObject go)
    {
        int removedCount = 0;
        // Use SerializedObject to edit the internal component list safely
        SerializedObject so = new SerializedObject(go);
        SerializedProperty prop = so.FindProperty("m_Component");
        if (prop == null || !prop.isArray)
            return 0;

        // Iterate from end to start so deletions don't shift remaining elements
        for (int i = prop.arraySize - 1; i >= 0; i--)
        {
            var element = prop.GetArrayElementAtIndex(i);
            if (element == null)
                continue;
            var objRef = element.FindPropertyRelative("component").objectReferenceValue;
            if (objRef == null)
            {
                prop.DeleteArrayElementAtIndex(i);
                removedCount++;
            }
        }

        if (removedCount > 0)
            so.ApplyModifiedProperties();

        return removedCount;
    }
}
