using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Berry.Utils;

public class ProfileAssistant : MonoBehaviour {

    public static ProfileAssistant main;

    public UserProfile local_profile;
        
    public void UnlockAllLevels() {
        local_profile = UserProfileUtils.ReadProfileFromDevice();
        local_profile.current_level = 9999;
        UserProfileUtils.WriteProfileOnDevice(local_profile);
        if (LevelMap.main)
            LevelMap.main.Refresh();
    }

    public void ClearData() {
        local_profile = new UserProfile();
        PlayerPrefs.DeleteAll();
        UserProfileUtils.WriteProfileOnDevice(local_profile);
        if (LevelMap.main)
            LevelMap.main.UpdateMapParameters();
    }

    void Awake() {
        main = this;

        UIAssistant.onShowPage += TryToSaveProfile;
        
        DebugPanel.AddDelegate("Clear Data", ClearData);
        DebugPanel.AddDelegate("Unlock all levels", UnlockAllLevels);
    }

    void Start() {
        local_profile = UserProfileUtils.ReadProfileFromDevice();

		#if UNITY_EDITOR
        if (PlayerPrefs.GetInt("TestLevel") != 0) {
            Level.TestLevel(PlayerPrefs.GetInt("TestLevel"));
            return;
        }
        #endif
    }

    void TryToSaveProfile(string page) {
        UserProfileUtils.WriteProfileOnDevice(local_profile);
    }
    
    public void SaveUserInventory() {
        StartCoroutine(SaveUserInventoryRoutine());
    }

    IEnumerator SaveUserInventoryRoutine() {
        yield return 0;
        UserProfileUtils.WriteProfileOnDevice(local_profile);
    }
}

public class UserProfile {

    public System.DateTime lastSave = new DateTime();
    
    public int current_level = 1;
    
    public Dictionary<int, int> score = new Dictionary<int, int>();
    public override string ToString() {
        string report = "";
        report += "Current level: " + current_level + ", ";
        report += "Score count: " + score.Count + ", ";
        report += "Last save: " + lastSave.ToString() + ", ";
        return report;
    }

    public int GetScore(int level_number) {
        if (!score.ContainsKey(level_number))
            return 0;
        return score[level_number];
    }

    public void SetScore(int level_number, int value) {
        if (!score.ContainsKey(level_number))
            score.Add(level_number, 0);
        score[level_number] = Mathf.Max(score[level_number], value);
    }
}

public class UserProfileUtils {
    public static void WriteProfileOnDevice(UserProfile profile) {
        PlayerPrefs.SetInt("Profile_current_level", profile.current_level);

        profile.lastSave = System.DateTime.UtcNow;
        PlayerPrefs.SetString("Profile_last_save", profile.lastSave.ToBinary().ToString());

        string score = string.Join(";", profile.score.Select(
            p => string.Format(
                "{0}:{1}",
                p.Key,
                p.Value.ToString()
                )).ToArray<string>());
        PlayerPrefs.SetString("Profile_score", score);
        PlayerPrefs.Save();
    }

    public static UserProfile ReadProfileFromDevice() {
        UserProfile profile = new UserProfile();
        
        profile.current_level = PlayerPrefs.GetInt("Profile_current_level");
        if (profile.current_level == 0)
            profile.current_level = 1;

        string lastSave = PlayerPrefs.GetString("Profile_last_save");
        if (lastSave.Length > 0)
            profile.lastSave = System.DateTime.FromBinary(long.Parse(lastSave));

        string score = PlayerPrefs.GetString("Profile_score");
        if (score.Length > 0)
            profile.score = score
             .Split(';')
             .Select(s => s.Split(':'))
             .ToDictionary(
                p => int.Parse(p[0]),
                p => int.Parse(p[1])
            );
        return profile;
    }
}
