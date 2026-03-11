using UnityEngine;

// Spawns the chosen player avatar based on onboarding selection and binds camera.
[DefaultExecutionOrder(-50)]
public class PlayerAvatarSpawner : MonoBehaviour
{
    [Header("Prefabs (assign in inspector)")]
    [SerializeField] private GameObject malePrefab;
    [SerializeField] private GameObject femalePrefab;
    [SerializeField] private GameObject defaultPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool destroyExistingPlayer = true;
    [SerializeField] private bool autoBindCamera = true;

    private IsometricPlayerController spawnedPlayer;

    public IsometricPlayerController SpawnedPlayer => spawnedPlayer;
    public Transform SpawnedTransform => spawnedPlayer != null ? spawnedPlayer.transform : null;

    private void Awake()
    {
        SpawnAvatar();
    }

    private void SpawnAvatar()
    {
        if (spawnedPlayer != null) return;

        GameObject prefab = ResolvePrefab();
        if (prefab == null)
        {
            Debug.LogWarning("PlayerAvatarSpawner: No prefab assigned for spawn; aborting.");
            return;
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;

        if (destroyExistingPlayer)
        {
            GameObject existing = GameObject.FindGameObjectWithTag("Player");
            if (existing != null)
                Destroy(existing);
        }

        GameObject instance = Instantiate(prefab, point.position, point.rotation);
        instance.tag = "Player";

        spawnedPlayer = instance.GetComponent<IsometricPlayerController>();
        if (spawnedPlayer == null)
        {
            Debug.LogError("PlayerAvatarSpawner: Spawned prefab is missing IsometricPlayerController.");
        }

        if (autoBindCamera)
            BindCamera(instance.transform);
    }

    private GameObject ResolvePrefab()
    {
        PlayerData.Gender gender = PlayerData.Gender.NotSpecified;
        if (PlayerData.Instance != null)
            gender = PlayerData.Instance.PlayerGender;

        GameObject prefab = null;
        switch (gender)
        {
            case PlayerData.Gender.Male:
                prefab = malePrefab;
                break;
            case PlayerData.Gender.Female:
                prefab = femalePrefab;
                break;
        }

        if (prefab == null)
            prefab = defaultPrefab ?? malePrefab ?? femalePrefab;

        return prefab;
    }

    private void BindCamera(Transform target)
    {
        IsometricCameraController cameraController = FindObjectOfType<IsometricCameraController>();
        if (cameraController != null)
        {
            cameraController.Target = target;
            cameraController.SnapToTarget();
        }
    }
}
