using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility to remove ARSession GameObjects from scenes so only the
/// designated gameplay scene keeps the AR Session. Use when multiple scenes
/// accidentally include an AR Session and you need a single persistent session.
/// </summary>
public static class ARSessionSceneCleaner
{
    [MenuItem("Tools/AR/Clean ARSession From Scenes")] 
    private static void CleanARSessionFromScenes()
    {
        if (!EditorUtility.DisplayDialog("Clean AR Sessions",
            "This will remove AR Session GameObjects from all scenes in the Build Settings except scenes named 'BeforeScenario'. Continue?",
            "Yes", "Cancel"))
            return;

        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            var scenePath = scenes[i].path;
            if (string.IsNullOrEmpty(scenePath)) continue;

            // Skip the canonical gameplay scene by name
            if (scenePath.Contains("BeforeScenario"))
            {
                Debug.Log("Skipping gameplay scene: " + scenePath);
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool modified = false;

            var sessions = Object.FindObjectsOfType<UnityEngine.XR.ARFoundation.ARSession>();
            foreach (var s in sessions)
            {
                if (s == null) continue;
                Debug.Log("[ARSessionSceneCleaner] Removing ARSession from scene " + scenePath + ": " + s.gameObject.name);
                Object.DestroyImmediate(s.gameObject);
                modified = true;
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        EditorUtility.DisplayDialog("ARSession Cleaner", "Finished cleaning scenes.", "OK");
    }
}
