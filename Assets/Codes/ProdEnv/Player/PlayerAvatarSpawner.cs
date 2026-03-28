using UnityEngine;

// Spawns the chosen player avatar based on onboarding selection and binds camera.
[DefaultExecutionOrder(-50)]
public class PlayerAvatarSpawner : MonoBehaviour
{
    private enum SpawnMode
    {
        UsePrefabs,
        UseSceneInstances
    }

    [Header("Prefabs (assign in inspector)")]
    [SerializeField] private GameObject malePrefab;
    [SerializeField] private GameObject femalePrefab;
    [SerializeField] private GameObject defaultPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool destroyExistingPlayer = true;
    [SerializeField] private bool autoBindCamera = true;

    [Header("Spawn Mode")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.UsePrefabs;

    [Header("Scene Avatar References (optional)")]
    [SerializeField] private IsometricPlayerController maleSceneAvatar;
    [SerializeField] private IsometricPlayerController femaleSceneAvatar;
    [SerializeField] private IsometricPlayerController defaultSceneAvatar;

    [Header("Joystick Binding")]
    [SerializeField] private bool autoBindJoystick = true;
    [SerializeField] private FixedJoystick overrideJoystick;

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

        switch (spawnMode)
        {
            case SpawnMode.UseSceneInstances:
                ActivateSceneAvatar();
                break;
            case SpawnMode.UsePrefabs:
            default:
                SpawnFromPrefabs();
                break;
        }
    }

    private void SpawnFromPrefabs()
    {
        GameObject prefab = ResolvePrefab();
        if (prefab == null)
        {
            Debug.LogWarning("PlayerAvatarSpawner: No prefab assigned for spawn in prefab mode; aborting.");
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

        if (autoBindJoystick)
            BindJoystick(spawnedPlayer);
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

    private void ActivateSceneAvatar()
    {
        IsometricPlayerController controller = ResolveSceneAvatar();
        if (controller == null)
        {
            Debug.LogWarning("PlayerAvatarSpawner: No scene avatar assigned for spawn in scene-instance mode; aborting.");
            return;
        }

        if (destroyExistingPlayer)
        {
            GameObject existing = GameObject.FindGameObjectWithTag("Player");
            if (existing != null && existing != controller.gameObject)
                Destroy(existing);
        }

        // Disable unused scene avatars
        if (controller != maleSceneAvatar && maleSceneAvatar != null)
            maleSceneAvatar.gameObject.SetActive(false);
        if (controller != femaleSceneAvatar && femaleSceneAvatar != null)
            femaleSceneAvatar.gameObject.SetActive(false);
        if (controller != defaultSceneAvatar && defaultSceneAvatar != null)
            defaultSceneAvatar.gameObject.SetActive(false);

        if (spawnPoint != null)
        {
            controller.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }

        controller.gameObject.tag = "Player";
        controller.gameObject.SetActive(true);

        spawnedPlayer = controller;

        if (autoBindCamera)
            BindCamera(controller.transform);

        if (autoBindJoystick)
            BindJoystick(spawnedPlayer);
    }

    private IsometricPlayerController ResolveSceneAvatar()
    {
        PlayerData.Gender gender = PlayerData.Gender.NotSpecified;
        if (PlayerData.Instance != null)
            gender = PlayerData.Instance.PlayerGender;

        IsometricPlayerController controller = null;
        switch (gender)
        {
            case PlayerData.Gender.Male:
                controller = maleSceneAvatar;
                break;
            case PlayerData.Gender.Female:
                controller = femaleSceneAvatar;
                break;
        }

        if (controller == null)
            controller = defaultSceneAvatar ?? maleSceneAvatar ?? femaleSceneAvatar;

        return controller;
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

    private void BindJoystick(IsometricPlayerController controller)
    {
        if (controller == null) return;

        FixedJoystick joystick = overrideJoystick != null ? overrideJoystick : FindObjectOfType<FixedJoystick>();
        if (joystick != null)
        {
            controller.SetJoystick(joystick);
        }
        else
        {
            Debug.LogWarning("PlayerAvatarSpawner: No FixedJoystick found to bind.");
        }
    }
}
