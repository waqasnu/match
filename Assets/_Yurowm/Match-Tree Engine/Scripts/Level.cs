using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Berry.Utils;

public class Level : MonoBehaviour {

    public static Dictionary<int, LevelProfile> all = new Dictionary<int, LevelProfile>();
    public LevelProfile profile;

    void Awake() {
        profile.level = transform.GetSiblingIndex() + 1;

        if (!all.ContainsKey(profile.level))
            all.Add(profile.level, profile);

        if (!Application.isEditor)
           Destroy(gameObject);
    }

    public static void LoadLevel(int key) {
        if (CPanel.uiAnimation > 0)
            return;

        if (!all.ContainsKey(key))
            return;

        LevelProfile.main = all[key];

        UIAssistant.main.ShowPage("LevelSelectedPopup");
    }

    #if UNITY_EDITOR
    public static void TestLevel(int l) {
        LevelProfile.main = all[l];
        FieldAssistant.main.StartLevel();
        PlayerPrefs.DeleteKey("TestLevel");
    }
    #endif
}

[System.Serializable]
public class LevelProfile {

    public static LevelProfile main; // current level
    public const int maxSize = 12; // maximal playing field size
    
    public int levelID = 0; // Level ID
    public int level = 0; // Level number
    // field size
    public int width = 9;
    public int height = 9;
    public int colorCount = 6; // count of chip colors
    public int targetSugarDropsCount = 0; // Count of sugar chips in SugaDrop mode
    public int firstStarScore = 100; // number of score points needed to get a first stars
    public int secondStarScore = 200; // number of score points needed to get a second stars
    public int thirdStarScore = 300; // number of score points needed to get a third stars

    // Target score in Score mode = firstStarScore;
    // Count of jellies in Jelly mode colculate automaticly via jellyData array;
    // Count of blocks in Blocks mode colculate automaticly via blockData array;
    // Count of remaining chips in Color mode takes from "countOfEachTargetCount" array, where value is count, index is color ID ;

    // Session duration in time limitation mode = duration value (sec);
    // Count of moves in moves limimtation mode = moveCount value (sec);
    public int limit = 30; // Count of moves in TargetScore and JellyCrush

    public List<SlotSettings> slots = new List<SlotSettings>();
    
    public LevelProfile GetClone() {
        LevelProfile clone = new LevelProfile();
        clone = (LevelProfile) MemberwiseClone();
        clone.levelID = -1;
        return clone;
    }
}

[System.Serializable]
public class SlotSettings {
    public int2 position = new int2();
    public bool generator = false;
    public string chip = "";
    public int color_id = 0;
    public string block_type = "";
    public int block_level = 0;

    public SlotSettings(int _x, int _y) {
        position = new int2(_x, _y);
    }

    public SlotSettings(int2 _position) {
        position = _position;
    }

    public SlotSettings GetClone() {
        return MemberwiseClone() as SlotSettings;
    }
}