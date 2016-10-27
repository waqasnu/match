using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Advertisements;


[RequireComponent (typeof (Button))]
public class StarLevelButton : MonoBehaviour {

	public void OnClick () {
		if (CPanel.uiAnimation > 0) return;
        FieldAssistant.main.StartLevel();
	}
}


public class UnityAdsExample : MonoBehaviour
{
  public void ShowAd()
  {
    if (Advertisement.IsReady())
    {
      Advertisement.Show();
    }
  }
}
