using UnityEngine;

public static class SaveManager
{
    private const string SaveKey = "PLAYER_PROGRESS";

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }

    public static PlayerProgress Load()
    {
        if (!HasSave())
            return null;

        string json = PlayerPrefs.GetString(SaveKey);
        return JsonUtility.FromJson<PlayerProgress>(json);
    }

    public static void Save(PlayerProgress progress)
    {
        string json = JsonUtility.ToJson(progress);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SaveKey);
    }
}
