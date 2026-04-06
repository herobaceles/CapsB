using UnityEngine;
using UnityEditor;

public static class MissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]
    private static void FindMissingScripts()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects)
        {
            // Only check objects that are part of a scene (ignore prefabs in the Project)
            if (!go.scene.IsValid())
                continue;

            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null)
                {
                    Debug.LogWarning(
                        $"Missing script on GameObject '{go.name}' in scene '{go.scene.name}'",
                        go
                    );
                }
            }
        }
    }
}