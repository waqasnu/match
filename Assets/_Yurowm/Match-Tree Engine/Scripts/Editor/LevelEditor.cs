using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System;
using EditorUtils;
using Berry.Utils;

[CustomEditor(typeof(Level))]
public class LevelEditor : MetaEditor {

    LevelProfile profile;
    Level level;
    Rect rect;

    static int cellSize = 40;
    static int legendSize = 20;
    static int slotOffset = 4;

    static Color defaultColor;
    Color[] chipColor;

    static GUIStyle mLabelStyle;
    public static GUIStyle labelStyle {
        get {
            if (mLabelStyle == null) {
                mLabelStyle = new GUIStyle(GUI.skin.button);
                mLabelStyle.wordWrap = true;
;
                mLabelStyle.normal.background = null;
                mLabelStyle.focused.background = null;
                mLabelStyle.active.background = null;

                mLabelStyle.normal.textColor = Color.black;
                mLabelStyle.focused.textColor = mLabelStyle.normal.textColor;
                mLabelStyle.active.textColor = mLabelStyle.normal.textColor;

                mLabelStyle.fontSize = 8;
                mLabelStyle.margin = new RectOffset();
                mLabelStyle.padding = new RectOffset();
            }
            return mLabelStyle;
        }
    }

    Dictionary<int2, SlotSettings> slots = new Dictionary<int2, SlotSettings>();
    List<int2> teleportTargets = new List<int2>();
    SlotSettings target_selection;
    bool wait_target = false;
    Dictionary<string, SessionAssistant.ChipInfo> chipInfos = new Dictionary<string, SessionAssistant.ChipInfo>();
    Dictionary<string, SessionAssistant.BlockInfo> blockInfos = new Dictionary<string, SessionAssistant.BlockInfo>();

    List<int2> selected = new List<int2>();
    #region Icons
    public static Texture slotIcon;
    public static Texture chipIcon;
    public static Texture jellyIcon;
    public static Texture blockIcon;
    public static Texture generatorIcon;
    public static Texture wallhIcon;
    public static Texture wallvIcon;
    public static Dictionary<Side, Texture> gravityIcon = new Dictionary<Side, Texture>();


    static string[] alphabet = { "A", "B", "C", "D", "E", "F" };

    Texture LoadIcon(string resource) {
        return EditorGUIUtility.Load(resource) as Texture;
    }
    #endregion

    void OnEnable() {
        if (!metaTarget)
            return;
        level = (Level) metaTarget;

        if (SessionAssistant.main == null)
            SessionAssistant.main = FindObjectOfType<SessionAssistant>();

        Level.all = FindObjectsOfType<Level>().ToDictionary(x => x.profile.level, x => x.profile);

        if (slotIcon == null) slotIcon = LoadIcon("LevelEditor/SlotIcon.png");
        if (chipIcon == null) chipIcon = LoadIcon("LevelEditor/ChipIcon.png");
        if (blockIcon == null) blockIcon = LoadIcon("LevelEditor/BlockIcon.png");
        if (generatorIcon == null) generatorIcon = LoadIcon("LevelEditor/GeneratorIcon.png");

        slots.Clear();

        chipColor = Chip.colors.Select(x => Color.Lerp(x, Color.white, 0.4f)).ToArray();
    }

    public override void OnInspectorGUI() {
        if (!metaTarget)
            return;
        level = (Level) metaTarget;

        if (!level) {
            EditorGUILayout.HelpBox("No level selected", MessageType.Info);
            return;
        }

        if (level.profile == null)
            level.profile = new LevelProfile();

        profile = level.profile;

        #region Temporary arrays
        slots = profile.slots.ToDictionary(x => x.position, x => x);
        chipInfos = SessionAssistant.main.chipInfos.ToDictionary(x => x.name, x => x);
        blockInfos = SessionAssistant.main.blockInfos.ToDictionary(x => x.name, x => x);
        #endregion

        if (profile.levelID == 0) {
            profile = new LevelProfile();
            profile.levelID = level.gameObject.GetInstanceID();
            ResetField();
        }

        if (profile.levelID != level.gameObject.GetInstanceID()) {
            if (profile.levelID != level.gameObject.GetInstanceID())
                profile = profile.GetClone();
            profile.levelID = level.gameObject.GetInstanceID();
        }

        Undo.RecordObject(level, "Level design changed");

        #region Level parameters
        GUILayout.Label("Level Parameters", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandWidth(true));
        profile.level = level.transform.GetSiblingIndex() + 1;

        EditorGUILayout.BeginVertical(EditorStyles.textArea);

        #region Navigation Panel
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("<<", EditorStyles.miniButtonLeft, GUILayout.Width(30))) {
            SelectLevel(1);
            return;
        }
        if (GUILayout.Button("<", EditorStyles.miniButtonMid, GUILayout.Width(30))) {
            SelectLevel(profile.level - 1);
            return;
        }

        GUILayout.Label("Level #" + profile.level, EditorStyles.miniButtonMid, GUILayout.Width(70));

        if (GUILayout.Button(">", EditorStyles.miniButtonMid, GUILayout.Width(30))) {
            SelectLevel(profile.level + 1);
            return;
        }
        if (GUILayout.Button(">>", EditorStyles.miniButtonRight, GUILayout.Width(30))) {
            SelectLevel(Level.all.Count);
            return;
        }

        EditorGUILayout.EndHorizontal();
        #endregion
        profile.width = Mathf.RoundToInt(EditorGUILayout.Slider("Width", 1f * profile.width, 5f, LevelProfile.maxSize));
        profile.height = Mathf.RoundToInt(EditorGUILayout.Slider("Height", 1f * profile.height, 5f, LevelProfile.maxSize));
        profile.colorCount = Mathf.RoundToInt(EditorGUILayout.Slider("Count of Possible Colors", 1f * profile.colorCount, 3f, chipColor.Length));
   
        #region Stars
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Score Stars", GUILayout.ExpandWidth(true));
        profile.firstStarScore = Mathf.Max(EditorGUILayout.IntField(profile.firstStarScore, GUILayout.ExpandWidth(true)), 1);
        profile.secondStarScore = Mathf.Max(EditorGUILayout.IntField(profile.secondStarScore, GUILayout.ExpandWidth(true)), profile.firstStarScore + 1);
        profile.thirdStarScore = Mathf.Max(EditorGUILayout.IntField(profile.thirdStarScore, GUILayout.ExpandWidth(true)), profile.secondStarScore + 1);
        EditorGUILayout.EndHorizontal();
        #endregion

        #region Limitation
        profile.limit = Mathf.RoundToInt(EditorGUILayout.Slider("Move Count", profile.limit, 5, 100));
        #endregion

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();
        #endregion

        UpdateName(level);

        #region Slot parameters
        GUILayout.Label("Slot Parameters", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandWidth(true));
        EditorGUILayout.BeginHorizontal();

        DrawSlotSettings();

        EditorGUILayout.EndHorizontal();

        #endregion

        GUILayout.Label("Level Layout", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandWidth(true));

        DrawActionBar();

        defaultColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.gray;
        EditorGUILayout.BeginHorizontal(EditorStyles.textArea, GUILayout.MinWidth(10));
        GUI.backgroundColor = defaultColor;

        rect = GUILayoutUtility.GetRect(
            profile.width * (cellSize + slotOffset) + legendSize + EditorStyles.textArea.margin.left + EditorStyles.textArea.margin.right,
            profile.height * (cellSize + slotOffset) + legendSize + EditorStyles.textArea.margin.top + EditorStyles.textArea.margin.bottom);
       

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        DrawFieldView();

        profile.slots = slots.Values.Where(x => x.position.IsItHit(0, 0, profile.width - 1, profile.height - 1)).ToList();

        level.profile = profile;
    }

    public void SelectLevel(int v) {
        Level _level = FindObjectsOfType<Level>().ToList().Find(x => x.profile.level == v);
        if (_level) {
            level = _level;
            BerryPanel.lastSelectedLevel = level.transform.GetSiblingIndex();
            BerryPanel.currentLevel = level;
        }
    }

    void ResetField() {
        slots.Clear();
        for (int x = 0; x < profile.width; x++)
            for (int y = 0; y < profile.height; y++)
                NewSlotSettings(new int2(x, y));
    }

    void RunLevel() {
        EditorApplication.isPlaying = true;
        PlayerPrefs.SetInt("TestLevel", level.profile.level);
    }

    SlotSettings NewSlotSettings(int2 coord) {
        if (!slots.ContainsKey(coord)) {
            slots.Add(coord, new SlotSettings(coord));
            if (coord.y == profile.height - 1)
                slots[coord].generator = true;
            return slots[coord];
        }
        return null;
    }

    void DrawActionBar() {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Select all active", EditorStyles.toolbarButton, GUILayout.Width(100))) {
            selected = new List<int2>(slots.Keys);
        }

        if (GUILayout.Button("Select all", EditorStyles.toolbarButton, GUILayout.Width(80))) {
            selected.Clear();
            for (int x = 0; x < profile.width; x++)
                for (int y = 0; y < profile.height; y++)
                    selected.Add(new int2(x, y));
        }

        if (GUILayout.Button("Unselect all", EditorStyles.toolbarButton, GUILayout.Width(80)))
            selected.Clear();

        GUILayout.FlexibleSpace();

        defaultColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.Lerp(defaultColor, Color.green, 0.6f);
        if (!EditorApplication.isPlayingOrWillChangePlaymode && GUILayout.Button("Run", EditorStyles.toolbarButton, GUILayout.Width(50)))
            RunLevel();

        GUI.backgroundColor = Color.Lerp(defaultColor, Color.red, 0.6f);
        if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(50)))
            ResetField();
        GUI.backgroundColor = defaultColor;

        EditorGUILayout.EndHorizontal();
    }

    void DrawMixedProperty<T>(Func<int2, bool> mask, Func<int2, T> getValue, Action<int2, T> setValue, Func<T, T> drawSingle, Func<Action<T>, bool> drawMixed, Action drawEmpty = null) {
        bool multiple = false;
        bool assigned = false;
        T value = default(T);
        T temp;
        foreach (int2 coord in selected) {
            if (!mask.Invoke(coord))
                continue;
            if (!assigned) {
                value = getValue.Invoke(coord);
                assigned = true;
                continue;
            }
            temp = getValue.Invoke(coord);
            if (!value.Equals(temp)) {
                multiple = true;
                break;
            }
        }

        if (!assigned) {
            if (drawEmpty != null)
                drawEmpty.Invoke();
            return;
        }

        if (multiple) {
            EditorGUI.showMixedValue = true;
            Action<T> setDefault = (T t) => {
                value = t;
            };
            if (drawMixed.Invoke(setDefault)) {
                multiple = false;
            }
            EditorGUI.showMixedValue = false;
        } else
            value = drawSingle(value);

        if (!multiple)
            foreach (int2 coord in selected)
                if (mask.Invoke(coord))
                    setValue(coord, value);
    }

    public static void UpdateName(Level level) {
        level.name = "Level:" + level.profile.level.ToString();
    }

    void DrawSlotSettings() {
        EditorGUILayout.BeginVertical(EditorStyles.textArea, GUILayout.ExpandWidth(true), GUILayout.Height(170));

        if (selected.Count == 0) {
            GUILayout.Label("Nothing selected", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();
            return;
        }

        #region Slots property
        DrawMixedProperty(
            mask: (int2 coord) => {
                return true;
            },
            getValue: (int2 coord) => { return slots.ContainsKey(coord); },
            setValue: (int2 coord, bool value) => {
                if (value && !slots.ContainsKey(coord))
                    NewSlotSettings(coord);
                if (!value && slots.ContainsKey(coord))
                    slots.Remove(coord);
            },
            drawSingle: (bool value) => { return EditorGUILayout.Toggle("Active", value); },
            drawMixed: (Action<bool> setDefault) => {
                if (EditorGUILayout.Toggle("Active", false)) {
                    setDefault(true);
                    return true;
                }
                return false;
            });
        #endregion

        #region Generators property
        DrawMixedProperty(
            mask: (int2 coord) => {
                return slots.ContainsKey(coord);
            },
            getValue: (int2 coord) => {return slots[coord].generator;},
            setValue: (int2 coord, bool value) => {
                slots[coord].generator = value;
            },
            drawSingle: (bool value) => {
                return EditorGUILayout.Toggle("Generator", value);
            },
            drawMixed: (Action<bool> setDefault) => {
                if (EditorGUILayout.Toggle("Generator", false)) {
                    setDefault(true);
                    return true;
                }
                return false;
            });
        #endregion

        #region Chip property
        if (SessionAssistant.main.chipInfos.Count > 0) {
            List<string> chips = new List<string>();
            chips.Add("Empty");
            foreach (SessionAssistant.ChipInfo chip in SessionAssistant.main.chipInfos)
                if (!chips.Contains(chip.name))
                    chips.Add(chip.name);


            DrawMixedProperty(
                mask: (int2 coord) => {
                    return slots.ContainsKey(coord) && (slots[coord].block_type == "" || blockInfos[slots[coord].block_type].chip);
                },
                getValue: (int2 coord) => {return slots[coord].chip;},
                setValue: (int2 coord, string value) => {
                    slots[coord].chip = value;
                    if (!chipInfos.ContainsKey(value) || !chipInfos[value].color)
                        slots[coord].color_id = -1;
                },
                drawSingle: (string value) => {
                    int id = chips.IndexOf(value);
                    if (id == -1) id = 0;
                    id = EditorGUILayout.Popup("Chip type", id, chips.ToArray());
                    return chips[id] == "Empty" ? "" : chips[id];
                    
                },
                drawMixed: (Action<string> setDefault) => {
                    int id = EditorGUILayout.Popup("Chip type", -1, chips.ToArray());
                    if (id != -1) {
                        setDefault(chips[id] == "Empty" ? "" : chips[id]);
                        return true;
                    }
                    return false;
                });
        }
        #endregion

        #region Chip color property
        List<string> colors = new List<string>();
        colors.Add("Random");
        for (int i = 0; i < profile.colorCount; i++)
            colors.Add(Chip.chipTypes[i]);
        DrawMixedProperty(
                mask: (int2 coord) => {
                    return slots.ContainsKey(coord) && slots[coord].chip != "" && chipInfos[slots[coord].chip].color;
                },
                getValue: (int2 coord) => {return slots[coord].color_id;},
                setValue: (int2 coord, int value) => {
                    slots[coord].color_id = value;
                },
                drawSingle: (int value) => {
                    return EditorGUILayout.Popup("Chip color group", Mathf.Max(0, value), colors.ToArray());
                    
                },
                drawMixed: (Action<int> setDefault) => {
                    int id = EditorGUILayout.Popup("Chip color group", -1, colors.ToArray());
                    if (id != -1) {
                        setDefault(id);
                        return true;
                    }
                    return false;
                });
        #endregion

        #region Block property
        if (SessionAssistant.main.blockInfos.Count > 0) {
            Dictionary<string, SessionAssistant.BlockInfo> blocks = new Dictionary<string, SessionAssistant.BlockInfo>();
            blocks.Add("Empty", null);
            foreach (SessionAssistant.BlockInfo block in SessionAssistant.main.blockInfos)
                if (!blocks.ContainsKey(block.name))
                    blocks.Add(block.name, block);
            List<string> block_keys = new List<string>(blocks.Keys);
            
            #region Block type
            DrawMixedProperty(
                mask: (int2 coord) => {
                    return slots.ContainsKey(coord);
                },
                getValue: (int2 coord) => {
                    return slots[coord].block_type;
                },
                setValue: (int2 coord, string value) => {
                    slots[coord].block_type = value;
                    if (value != "" && !blockInfos[value].chip)
                        slots[coord].chip = "";
                },
                drawSingle: (string value) => {
                    int id = block_keys.IndexOf(value);
                    if (id == -1)
                        id = 0;
                    id = EditorGUILayout.Popup("Block type", id, block_keys.ToArray());
                    return block_keys[id] == "Empty" ? "" : block_keys[id];

                },
                drawMixed: (Action<string> setDefault) => {
                    int id = EditorGUILayout.Popup("Block type", -1, block_keys.ToArray());
                    if (id != -1) {
                        setDefault(block_keys[id] == "Empty" ? "" : block_keys[id]);
                        return true;
                    }
                    return false;
                });
            #endregion

            #region Block level
            int max = 1000;
            DrawMixedProperty(
                    mask: (int2 coord) => {
                        if (!slots.ContainsKey(coord) || slots[coord].block_type == "" || blocks[slots[coord].block_type].levelCount <= 1)
                            return false;
                        max = Mathf.Min(max, blocks[slots[coord].block_type].levelCount);
                        return true;
                    },
                    getValue: (int2 coord) => {
                        return slots[coord].block_level;
                    },
                    setValue: (int2 coord, int value) => {
                        slots[coord].block_level = value;
                    },
                    drawSingle: (int value) => {
                        return Mathf.RoundToInt(EditorGUILayout.Slider("Block level", value  + 1, 1, max)) - 1;

                    },
                    drawMixed: (Action<int> setDefault) => {
                        float level = EditorGUILayout.Slider("Block level", -1, -1, max);
                        if (level != -1) {
                            setDefault(Mathf.RoundToInt(level) - 1);
                            return true;
                        }
                        return false;
                    });
            #endregion
        }
        #endregion

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();
    }

    void DrawFieldView() {
        int2 key = null;
        for (int x = 0; x < profile.width; x++) {
            for (int y = 0; y < profile.height; y++) {
                key = new int2(x, y);
                if (DrawSlotButton(key, rect)) {
                    if (wait_target) {
                        if (slots.ContainsKey(key)) {
                            target_selection = slots[key];
                            continue;
                        } else
                            wait_target = false;
                    }

                    if (Event.current.shift && selected.Count > 0) {
                        int2 start = selected.Last().GetClone();
                        int2 delta = new int2();
                        delta.x = start.x < x ? 1 : -1;
                        delta.y = start.y < y ? 1 : -1;
                        int2 cursor = new int2();
                        for (cursor.x = start.x; cursor.x != x + delta.x; cursor.x += delta.x)
                            for (cursor.y = start.y; cursor.y != y + delta.y; cursor.y += delta.y)
                                if (!selected.Contains(cursor))
                                    selected.Add(cursor.GetClone());
                        

                    } else {
                        if (!Event.current.control)
                            selected.Clear();
                        if (selected.Contains(key))
                            selected.Remove(key);
                        else
                            selected.Add(key);
                    }
                }
            }
        }
           
        for (int x = 0; x < profile.width; x++)
            GUI.Box(new Rect(rect.xMin + x * (cellSize + slotOffset) + legendSize,
                            rect.yMin + (profile.height) * (cellSize + slotOffset), cellSize, legendSize), x.ToString(), EditorStyles.centeredGreyMiniLabel);
        for (int y = 0; y < profile.height; y++)
            GUI.Box(new Rect(rect.xMin, rect.yMin + (profile.height - y - 1) * (cellSize + slotOffset) + slotOffset,
                legendSize, cellSize), y.ToString(), EditorStyles.centeredGreyMiniLabel);
    }

    bool DrawSlotButton(int2 coord, Rect r) {
        defaultColor = GUI.color;
        bool btn = false;

        Rect rect = new Rect(r.xMin + coord.x * (cellSize + slotOffset) + legendSize,
            r.yMin + (profile.height - coord.y - 1) * (cellSize + slotOffset) + slotOffset,
            cellSize, cellSize);

        if (slots.ContainsKey(coord))
            GUI.color = (wait_target || teleportTargets.Contains(coord)) ? Color.cyan : Color.gray;
        else
            GUI.color *= new Color(0, 0, 0, 0.1f);

        GUI.DrawTexture(rect, slotIcon);
        btn = GUI.Button(rect, "", labelStyle);
        GUI.color = defaultColor;

        // Draw chip
        if (slots.ContainsKey(coord) && slots[coord].chip != "") {
            defaultColor = GUI.color;
            if (slots[coord].color_id > profile.colorCount)
                slots[coord].color_id = 0;
            int color_id = slots[coord].color_id;
            GUI.color = color_id > 0 && color_id <= profile.colorCount ? chipColor[color_id - 1] : Color.white;
            GUI.DrawTexture(rect, chipIcon);
            GUI.Box(rect, chipInfos[slots[coord].chip].shirtName, labelStyle);
            GUI.color = defaultColor;
        }

        // Draw block
        if (slots.ContainsKey(coord) && slots[coord].block_type != "") {
            GUI.DrawTexture(rect, blockIcon);
            SessionAssistant.BlockInfo info = blockInfos[slots[coord].block_type];
            GUI.Box(new Rect(rect.x, rect.y + rect.height / 2, rect.width, rect.height / 2),
                info.shirtName + (info.levelCount > 1 ? (":" + (slots[coord].block_level + 1).ToString()) : ""), labelStyle);
        }

        // Draw labels
        if (slots.ContainsKey(coord)) {
            Rect label_rect = new Rect(rect);
            label_rect.width = 10;
            label_rect.height = 10;
            label_rect.x += rect.width - label_rect.width;

            if (slots[coord].generator) {
                GUI.DrawTexture(label_rect, generatorIcon);
                label_rect.y += label_rect.height;
            }
        }


        if (selected.Contains(coord))
            GUI.Toggle(new Rect(rect.xMin, rect.yMin, 10, 10), true, "");

        GUI.backgroundColor = defaultColor;
        return btn;
    }

    public override UnityEngine.Object FindTarget() {
        return BerryPanel.currentLevel;
    }
}