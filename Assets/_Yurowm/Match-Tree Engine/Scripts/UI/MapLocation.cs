using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MapLocation : MonoBehaviour {
    public Transform nextLocationConnector;
    public Transform previousLocationConnector;
    MapLocation nextLocation = null;
    MapLocation previousLocation = null;

    LevelMap map;
    Rect mapRect;
    
    void Start() {
        LevelMap.main.location = this;
        CreateButtons();
    }

    void CreateButtons() {
        Transform locators_folder = transform.FindChild("Buttons");
        if (!locators_folder) {
            Debug.LogError("I can't find Buttons folder");
            return;
        }
        Transform connector;
        LevelButton level_button;
        int level;
        int firstLevel = 0;
        int lastCount = GetLevelCount();
        for (int l = 0; l < lastCount; l++) {
            level = firstLevel + 1 + l;
            if (locators_folder.childCount <= l) return;
            connector = locators_folder.GetChild(l);
            if (!connector || !Level.all.ContainsKey(level))
                return;

            level_button = ContentAssistant.main.GetItem<LevelButton>("LevelButton");
            level_button.transform.parent = connector;
            level_button.transform.localPosition = Vector3.zero;
            level_button.level = level;
            //level_button.GetComponent<Canvas>().worldCamera = LevelMap2.main.mapCamera;
            level_button.Initialize();
        }
        
    }

    public int GetLevelCount() {
        return transform.FindChild("Buttons").childCount;    
    }
}
