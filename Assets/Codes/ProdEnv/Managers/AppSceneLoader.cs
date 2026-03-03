using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppSceneLoader : MonoBehaviour
{
	public static AppSceneLoader Instance { get; private set; }

	public bool IsLoading { get; private set; }

	private Coroutine loadRoutine;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public static void EnsureExists()
	{
		if (Instance != null)
			return;

		var existing = FindObjectOfType<AppSceneLoader>();
		if (existing != null)
		{
			Instance = existing;
			DontDestroyOnLoad(existing.gameObject);
			return;
		}

		var go = new GameObject(nameof(AppSceneLoader));
		Instance = go.AddComponent<AppSceneLoader>();
		DontDestroyOnLoad(go);
	}

	public void LoadSceneSingle(string sceneName)
	{
		if (string.IsNullOrEmpty(sceneName))
		{
			Debug.LogError("AppSceneLoader: Scene name is null or empty.");
			return;
		}

		SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
	}

	public void LoadSceneSingleAsync(
		string sceneName,
		Action<float> onProgress = null,
		Action onCompleted = null,
		float minDisplayTime = 0f)
	{
		if (string.IsNullOrEmpty(sceneName))
		{
			Debug.LogError("AppSceneLoader: Scene name is null or empty.");
			return;
		}

		if (loadRoutine != null)
			StopCoroutine(loadRoutine);

		loadRoutine = StartCoroutine(LoadSceneAsyncRoutine(sceneName, onProgress, onCompleted, minDisplayTime));
	}

	private IEnumerator LoadSceneAsyncRoutine(
		string sceneName,
		Action<float> onProgress,
		Action onCompleted,
		float minDisplayTime)
	{
		IsLoading = true;

		var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
		if (asyncOp == null)
		{
			Debug.LogError($"AppSceneLoader: Failed to start async load for '{sceneName}'.");
			IsLoading = false;
			yield break;
		}

		asyncOp.allowSceneActivation = false;

		float elapsed = 0f;
		while (!asyncOp.isDone)
		{
			float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
			onProgress?.Invoke(progress);

			elapsed += Time.unscaledDeltaTime;
			if (asyncOp.progress >= 0.9f && elapsed >= minDisplayTime)
				asyncOp.allowSceneActivation = true;

			yield return null;
		}

		onProgress?.Invoke(1f);
		onCompleted?.Invoke();

		IsLoading = false;
		loadRoutine = null;
	}
}
