using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using System.Collections.Generic;
using Berry.Utils;

[RequireComponent (typeof (Text))]
public class TargetDescriptionDisplay : MonoBehaviour {
	
	Text text;

    void Awake () {
		text = GetComponent<Text> ();	
	}
	
	void OnEnable () {
        text.text = "";
		if (LevelProfile.main == null)
			return;

        string descrition = "You need to reach {0} points a take one star.";

        descrition = string.Format(descrition, LevelProfile.main.firstStarScore);

        text.text += descrition + " ";
        descrition = "You have only {0} moves to accomplish this.";

        descrition = string.Format(descrition, LevelProfile.main.limit);

        text.text += descrition;
	}
}
