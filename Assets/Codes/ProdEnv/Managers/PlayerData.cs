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
    public string LastMissionId { get; private set; } = "";

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
        bool isNewProfile = !IsOnboardingComplete;

        if (isNewProfile)
            ResetMissionProgress();

        PlayerName = playerName;
        PlayerGender = gender;
        IsOnboardingComplete = true;
        PlayerPrefs.SetString(KEY_PLAYER_NAME, PlayerName);
        PlayerPrefs.SetInt(KEY_PLAYER_GENDER, (int)PlayerGender);
        PlayerPrefs.SetInt(KEY_ONBOARDING_COMPLETE, 1);
        PlayerPrefs.Save();
        Debug.Log($"PlayerData: Saved - Name: {PlayerName}, Gender: {PlayerGender}");
    }

    public void SaveLastMission(string missionId)
    {
        LastMissionId = missionId;
        PlayerPrefs.SetString(KEY_LAST_MISSION_ID, LastMissionId);
        PlayerPrefs.Save();
        Debug.Log($"PlayerData: Saved last mission id: {LastMissionId}");
    }

    public void LoadPlayerData()
    {
        PlayerName = PlayerPrefs.GetString(KEY_PLAYER_NAME, "");
        PlayerGender = (Gender)PlayerPrefs.GetInt(KEY_PLAYER_GENDER, 0);
        IsOnboardingComplete = PlayerPrefs.GetInt(KEY_ONBOARDING_COMPLETE, 0) == 1;
        LastMissionId = PlayerPrefs.GetString(KEY_LAST_MISSION_ID, "");
        Debug.Log($"PlayerData: Loaded - OnboardingComplete: {IsOnboardingComplete}");
    }

    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        PlayerName = "";
        PlayerGender = Gender.NotSpecified;
        IsOnboardingComplete = false;
        LastMissionId = "";
        Debug.Log("PlayerData: All data reset");
    }

    public string GetGreeting()
    {
        int hour = System.DateTime.Now.Hour;
        string time = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
        return string.IsNullOrEmpty(PlayerName) ? time : $"{time}, {PlayerName}";
    }

    private void ResetMissionProgress()
    {
        PlayerPrefs.DeleteKey(KEY_LAST_MISSION_ID);

        DeleteMissionKey("before_01");
        DeleteMissionKey("before_02");
        DeleteMissionKey("before_03");
        DeleteMissionKey("mission_during_01");
        DeleteMissionKey("after_01");
        DeleteMissionKey("after_02");
        DeleteMissionKey("after_03");
    }

    private static void DeleteMissionKey(string missionId)
    {
        PlayerPrefs.DeleteKey($"Mission_{missionId}_Completed");
        PlayerPrefs.DeleteKey($"Mission_{missionId}_Unlocked");
    }
}
