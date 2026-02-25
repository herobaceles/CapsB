using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance { get; private set; }

    private const string KEY_PLAYER_NAME = "PlayerName";
    private const string KEY_PLAYER_GENDER = "PlayerGender";
    private const string KEY_ONBOARDING_COMPLETE = "OnboardingComplete";
    private const string KEY_LAST_MISSION_ID = "LastMissionId";

    public string PlayerName { get; private set; } = "";
    public Gender PlayerGender { get; private set; } = Gender.NotSpecified;
    public bool IsOnboardingComplete { get; private set; } = false;
    public int LastMissionId { get; private set; } = 0;

    public enum Gender { NotSpecified = 0, Male = 1, Female = 2 }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadPlayerData();
    }

    public bool IsFirstTimePlaying() => !IsOnboardingComplete;

    public void SaveOnboardingData(string playerName, Gender gender)
    {
        PlayerName = playerName;
        PlayerGender = gender;
        IsOnboardingComplete = true;
        PlayerPrefs.SetString(KEY_PLAYER_NAME, PlayerName);
        PlayerPrefs.SetInt(KEY_PLAYER_GENDER, (int)PlayerGender);
        PlayerPrefs.SetInt(KEY_ONBOARDING_COMPLETE, 1);
        PlayerPrefs.Save();
        Debug.Log($"PlayerData: Saved - Name: {PlayerName}, Gender: {PlayerGender}");
    }

    public void SaveLastMission(int missionId)
    {
        LastMissionId = missionId;
        PlayerPrefs.SetInt(KEY_LAST_MISSION_ID, LastMissionId);
        PlayerPrefs.Save();
        Debug.Log($"PlayerData: Saved last mission id: {LastMissionId}");
    }

    public void LoadPlayerData()
    {
        PlayerName = PlayerPrefs.GetString(KEY_PLAYER_NAME, "");
        PlayerGender = (Gender)PlayerPrefs.GetInt(KEY_PLAYER_GENDER, 0);
        IsOnboardingComplete = PlayerPrefs.GetInt(KEY_ONBOARDING_COMPLETE, 0) == 1;
        LastMissionId = PlayerPrefs.GetInt(KEY_LAST_MISSION_ID, 0);
        Debug.Log($"PlayerData: Loaded - OnboardingComplete: {IsOnboardingComplete}");
    }

    public void ResetAllData()
    {
        PlayerPrefs.DeleteKey(KEY_PLAYER_NAME);
        PlayerPrefs.DeleteKey(KEY_PLAYER_GENDER);
        PlayerPrefs.DeleteKey(KEY_ONBOARDING_COMPLETE);
        PlayerPrefs.DeleteKey(KEY_LAST_MISSION_ID);
        PlayerPrefs.Save();
        PlayerName = "";
        PlayerGender = Gender.NotSpecified;
        IsOnboardingComplete = false;
        LastMissionId = 0;
        Debug.Log("PlayerData: All data reset");
    }

    public string GetGreeting()
    {
        int hour = System.DateTime.Now.Hour;
        string time = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
        return string.IsNullOrEmpty(PlayerName) ? time : $"{time}, {PlayerName}";
    }
}
