using UnityEngine;
using UnityEngine.AI;

public class PlayerSpawner : MonoBehaviour
{
    private GameObject spawnedPlayerInstance;

    public GameObject SpawnPlayerByGender(int playerGender, Transform spawnPoint, GameObject malePrefab, GameObject femalePrefab, bool destroyExisting = true, string spawnedPlayerTag = "Player")
    {
        GameObject prefab = playerGender == 2 ? femalePrefab : malePrefab;

        if (prefab == null) return null;

        if (spawnPoint == null)
        {
            spawnedPlayerInstance = Instantiate(prefab);
            return spawnedPlayerInstance;
        }

        if (destroyExisting)
        {
            GameObject existing = GameObject.FindGameObjectWithTag(spawnedPlayerTag);
            if (existing != null) Destroy(existing);
        }

        spawnedPlayerInstance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        return spawnedPlayerInstance;
    }

    public GameObject GetSpawnedPlayer()
    {
        return spawnedPlayerInstance;
    }
}
