using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using EditorUtils;
using System.Linq;

[CustomEditor(typeof(ProfileAssistant))]
public class ProfileAssistantEditor : MetaEditor {

    ProfileAssistant main;
    AnimBool inventoryFade = new AnimBool(false);
    AnimBool scoresFade = new AnimBool(false);
    AnimBool localProfileFade = new AnimBool(false);
    AnimBool botsFade = new AnimBool(false);
    AnimBool initialInventoryFade = new AnimBool(false);

    void OnEnable() {
        scoresFade.valueChanged.AddListener(RepaintIt);
        inventoryFade.valueChanged.AddListener(RepaintIt);
        botsFade.valueChanged.AddListener(RepaintIt);
        localProfileFade.valueChanged.AddListener(RepaintIt);
        botsFade.valueChanged.AddListener(RepaintIt);
        initialInventoryFade.valueChanged.AddListener(RepaintIt);
    }

    public override void OnInspectorGUI() {
        if (!metaTarget) {
            EditorGUILayout.HelpBox("ProfileAssistant is missing", MessageType.Error);
            return;
        }
        main = (ProfileAssistant) metaTarget;
        Undo.RecordObject(main, "");

        #region Local Profile
        localProfileFade.target = GUILayout.Toggle(localProfileFade.target, "Local Profile", EditorStyles.foldout);
        if (EditorGUILayout.BeginFadeGroup(localProfileFade.faded)) {
            EditorGUILayout.BeginVertical(EditorStyles.textArea);

            DrawLocalProfile();

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFadeGroup();
        #endregion
    }

    public void DrawLocalProfile() {
        main = (ProfileAssistant) metaTarget;
        Undo.RecordObject(main, "");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Data", GUILayout.Width(80))) {
            main.ClearData();
        }
        if (GUILayout.Button("Unlock All Levels", GUILayout.Width(110))) {
            main.UnlockAllLevels();
        }
        EditorGUILayout.EndHorizontal();

        if (main.local_profile == null)
            main.local_profile = UserProfileUtils.ReadProfileFromDevice();

        EditorGUILayout.LabelField("Current level", main.local_profile.current_level.ToString(), EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Last save", main.local_profile.lastSave.ToShortDateString() + " " + main.local_profile.lastSave.ToLongTimeString(), EditorStyles.boldLabel);

        EditorGUILayout.EndFadeGroup();

        scoresFade.target = GUILayout.Toggle(scoresFade.target, "Score", EditorStyles.foldout);
        if (EditorGUILayout.BeginFadeGroup(scoresFade.faded)) {
            if (main.local_profile.score.Count > 0) {
                foreach (KeyValuePair<int, int> score in main.local_profile.score) {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField("Level " + score.Key.ToString(), score.Value.ToString(), EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                }
            } else
                GUILayout.Label("Empty");
        }
        EditorGUILayout.EndFadeGroup();
    }

    public override Object FindTarget() {
        if (ProfileAssistant.main == null)
            ProfileAssistant.main = FindObjectOfType<ProfileAssistant>();
        return ProfileAssistant.main;
    }
}

