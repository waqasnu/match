using UnityEngine;
using System.Collections;
using EditorUtils;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Berry.Contact;
using System.Text.RegularExpressions;

public class ProjectParametersEditor : MetaEditor {
    public override Object FindTarget() {
        if (ProjectParameters.main == null)
            ProjectParameters.main = FindObjectOfType<ProjectParameters>();
        return ProjectParameters.main;
    }

    public override void OnInspectorGUI() {
        if (!metaTarget) {
            EditorGUILayout.HelpBox("SessionAssistant is missing", MessageType.Error);
            return;
        }

        if (ProfileAssistant.main == null)
            ProfileAssistant.main = FindObjectOfType<ProfileAssistant>();
        if (ProfileAssistant.main == null) {
            EditorGUILayout.HelpBox("ProfileAssistant is missing", MessageType.Error);
            return;
        }

        ProjectParameters main = (ProjectParameters) metaTarget;
        Undo.RecordObject(main, "");

        main.square_combination = EditorGUILayout.Toggle("Square Combinations", main.square_combination);

        EditorGUILayout.Space();
        main.chip_acceleration = EditorGUILayout.Slider("Chip Acceleration", main.chip_acceleration, 1f, 100f);
        main.chip_max_velocity = EditorGUILayout.Slider("Chip Velocity Limit", main.chip_max_velocity, 5f, 100f);
        main.swap_duration = EditorGUILayout.Slider("Swap Duration", main.swap_duration, 0.01f, 1f);

        EditorGUILayout.Space();
        main.slot_offset = EditorGUILayout.Slider("Slot Offset", main.slot_offset, 0.01f, 2f);

        EditorGUILayout.Space();
        main.music_volume_max = EditorGUILayout.Slider("Max Music Volume", main.music_volume_max, 0f, 1f);

        EditorGUILayout.Space();
    }
}

public class ContactForm : MetaEditor {
    const string message = "My name: {0}\nReply to: {1}\nMy invoice: free license\n\nMessage:\n{2}\n\n------------\n\n{3}";

    public enum AppealType {
        BugReport,
        Other
    }

    PrefVariable type = new PrefVariable("ContactForm_AppealType");
    PrefVariable myName = new PrefVariable("ContactForm_Name");
    PrefVariable myEmail = new PrefVariable("ContactForm_Email");

    PrefVariable subject = new PrefVariable("ContactForm_Subject");
    PrefVariable body = new PrefVariable("ContactForm_Body");
    PrefVariable log = new PrefVariable("ContactForm_Log");
    PrefVariable attachments = new PrefVariable("ContactForm_Attachments");

    public override void OnInspectorGUI() {
        bool isSending = Contact.IsSending();
        bool isValidate = true;
        Regex regex;

        GUI.enabled = !isSending;

        EditorGUILayout.BeginVertical();

        type.Int = (int) (AppealType) EditorGUILayout.EnumPopup("Appeal Type:", (AppealType) type.Int, GUILayout.ExpandWidth(true));

        EditorGUILayout.Space();

        myName.String = EditorGUILayout.TextField("Name", myName.String, GUILayout.ExpandWidth(true));
        regex = new Regex(@"[\w]{2,}");
        if (!regex.IsMatch(myName.String)) {
            DrawError("Type your name");
            isValidate = false;
        }

        myEmail.String = EditorGUILayout.TextField("Email", myEmail.String, GUILayout.ExpandWidth(true));
        regex = new Regex(@"^([\w\.\-\+]+)@([\w\-]+)((\.(\w){2,3})+)$");
        if (!regex.IsMatch(myEmail.String)) {
            DrawError("Type your email (format: yourname@domain.com)");
            isValidate = false;
        }
        
        EditorGUILayout.Space();

        subject.String = EditorGUILayout.TextField("Subject", subject.String, GUILayout.ExpandWidth(true));

        GUILayout.Label("Message", GUILayout.Width(300));
        body.String = EditorGUILayout.TextArea(body.String, GUI.skin.textArea, GUILayout.ExpandWidth(true), GUILayout.Height(300));
        if (body.String.Length == 0) {
            DrawError("Type a message");
            isValidate = false;
        }

        if (type.Int == (int) AppealType.BugReport) {
            GUILayout.Label("Logs or another technical information", GUILayout.Width(300));
            log.String = EditorGUILayout.TextArea(log.String, GUI.skin.textArea, GUILayout.ExpandWidth(true), GUILayout.Height(100));

            EditorGUILayout.Space();
        }

        List<string> fileList = null;
        if (type.Int == (int) AppealType.BugReport) {
            fileList = new List<string>();
            if (!string.IsNullOrEmpty(attachments.String))
                fileList = attachments.String.Split(';').ToList();
            foreach (string file in fileList) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(30))) {
                    fileList.Remove(file);
                    break;
                }
                GUILayout.Label(new System.IO.FileInfo(file).Name, EditorStyles.miniLabel, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
            }
            if (fileList.Count < 5) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add attachment", EditorStyles.miniButton, GUILayout.Width(100))) {
                    string path = EditorUtility.OpenFilePanel("Select file", "", "");
                    if (path.Length > 0)
                        fileList.Add(path);
                }
                EditorGUILayout.EndHorizontal();
            }
            attachments.String = string.Join(";", fileList.ToArray());
        }
        GUI.enabled = !isSending && isValidate && !EditorApplication.isCompiling;
        
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (isSending)
            GUILayout.Label("Sending...", GUILayout.Width(90));
        if (EditorApplication.isCompiling)
            GUILayout.Label("Compiling...", GUILayout.Width(90));
        GUILayout.FlexibleSpace();
        if (Contact.IsSending()) {
            bool active = GUI.enabled;
            GUI.enabled = true;
            if (GUILayout.Button("Break", GUILayout.Width(70)))
                Contact.Break();
            GUI.enabled = active;
        } else {
            if (GUILayout.Button("Send", GUILayout.Width(70))) {
                EditorGUI.FocusTextInControl("");
                if (string.IsNullOrEmpty(subject.String))
                    subject.String = "No Subject";
                Contact.Send(
                    myName.String,
                    ((AppealType) type.Int).ToString() + ": " + subject.String,
                    string.Format(message, myName.String, myEmail.String, body.String, log.String),
                    OnSent,
                    fileList
                    );
            }
        }
        EditorGUILayout.EndHorizontal();

        foreach (string error in Contact.GetErrors())
            DrawError(error);

        GUI.enabled = true;
        EditorGUILayout.EndVertical();
    }

    void DrawError(string error) {
        Color color = GUI.color;
        GUI.color = Color.red;
        GUILayout.Label(error, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
        GUI.color = color;
    }

    void OnSent() {
        EditorCoroutine.start(ClearOnSent());
    }

    IEnumerator ClearOnSent() {
        body.String = "";
        subject.String = "";
        attachments.String = "";
        log.String = "";
        RepaintIt();
        EditorUtility.DisplayDialog("Contact", "Your email has been sent", "Ok");
        yield break;
    }

    public override Object FindTarget() {
        return null;
    }
}

public class PRORequired : MetaEditor {
    public override Object FindTarget() {
        return null;
    }

    #region Styles
    GUIStyle _bgStyle = null;
    GUIStyle bgStyle {
        get {
            if (_bgStyle == null) {
                _bgStyle = new GUIStyle(EditorStyles.textArea);
                _bgStyle.normal.background = Texture2D.whiteTexture;
                _bgStyle.padding = new RectOffset(0, 0, 0, 0);
                _bgStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            return _bgStyle;
        }
    }

    GUIStyle _titleStyle = null;
    GUIStyle titleStyle {
        get {
            if (_titleStyle == null) {
                _titleStyle = new GUIStyle(EditorStyles.largeLabel);
                _titleStyle.normal.textColor = Color.black;
                _titleStyle.fontStyle = FontStyle.Bold;
                _titleStyle.alignment = TextAnchor.MiddleCenter;
                _titleStyle.wordWrap = true;
            }
            return _titleStyle;
        }
    }

    GUIStyle _textStyle = null;
    GUIStyle textStyle {
        get {
            if (_textStyle == null) {
                _textStyle = new GUIStyle(EditorStyles.label);
                _textStyle.normal.textColor = Color.black;
                _textStyle.alignment = TextAnchor.UpperLeft;
                _textStyle.wordWrap = true;
            }
            return _textStyle;
        }
    }

    GUIStyle _tableStyle = null;
    GUIStyle tableStyle {
        get {
            if (_tableStyle == null) {
                _tableStyle = new GUIStyle(EditorStyles.label);
                _tableStyle.normal.textColor = Color.black;
                _tableStyle.alignment = TextAnchor.MiddleCenter;
                _tableStyle.padding = new RectOffset(5, 5, 5, 5);
                _tableStyle.wordWrap = true;
            }
            return _tableStyle;
        }
    }

    GUIStyle _buttonStyle = null;
    GUIStyle buttonStyle {
        get {
            if (_buttonStyle == null) {
                _buttonStyle = new GUIStyle(GUI.skin.FindStyle("Button"));
                _buttonStyle.fontSize = 20;

                _buttonStyle.normal.background = new Texture2D(1, 1);
                _buttonStyle.normal.background.SetPixel(0, 0, new Color(103f / 256, 194f / 256, 116f / 256, 1));
                _buttonStyle.normal.background.Apply();
                _buttonStyle.normal.textColor = Color.white;

                _buttonStyle.hover = _buttonStyle.hover;

                _buttonStyle.active.background = new Texture2D(1, 1);
                _buttonStyle.active.background.SetPixel(0, 0, new Color(62f / 256, 130f / 256, 73f / 256, 1));
                _buttonStyle.active.background.Apply();
                _buttonStyle.active.textColor = Color.white;


                _buttonStyle.alignment = TextAnchor.MiddleCenter;
                _buttonStyle.wordWrap = true;
            }
            return _buttonStyle;
        }
    }
    #endregion

    #region Images
    Dictionary<string, Texture> images = new Dictionary<string, Texture>();
    string[] imageNames = new string[] {
        "HeaderLogo", "DuelWithTed", "AdsLogos",
        "CutScenes", "Syncing", "Leaderboard",
        "Notifications", "Facebook",
        "YesMark", "NoMark",
        "IconLITE", "IconSTANDARD", "IconPRO"
    };
    #endregion

    #region Colors
    Color[] cell_color = new Color[] {
        Color.white,
        new Color(196f / 256, 179f / 256, 175f / 256, 1f), //bronze
        new Color(171f / 256, 196f / 256, 214f / 256, 1f), //silver
        new Color(243f / 256, 229f / 256, 171f / 256, 1f) //gold
    };
    #endregion

    void OnEnable() {
        images = new Dictionary<string, Texture>();
        foreach (string _name in imageNames)
            images.Add(_name, EditorGUIUtility.Load("PRO/" + _name + ".png") as Texture);
    }

    public override void OnInspectorGUI() {
        Color bgcolor = GUI.backgroundColor;
        Color color = GUI.color;

        GUI.backgroundColor = Color.white;
        EditorGUILayout.BeginVertical(bgStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        GUILayout.Label("This feature isn't avaliable in LITE version", titleStyle);

        #region Features Table
        float width = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(0)).width;

        DrawTable(width, titleStyle,
            new GUIContent[] {
                null,
                new GUIContent(images["IconLITE"]),
                new GUIContent(images["IconSTANDARD"]),
                new GUIContent(images["IconPRO"])
        });

        DrawTable(width, titleStyle,
            new GUIContent[] {
                new GUIContent("Project name"),
                new GUIContent("Berry Match-Three:\nLITE"),
                new GUIContent("Berry Match-Three"),
                new GUIContent("Berry Match-Three:\nPRO")
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Gameplay modes"),
                new GUIContent("Target Score"),
                new GUIContent("Target Score\n+\nJelly Crush\nBlock Crush\nSugar Drop\nColor Collection\nFill with Jam"),
                new GUIContent("Target Score\n+\nJelly Crush\nBlock Crush\nSugar Drop\nColor Collection\nFill with Jam\n+\nDuel with Ted")
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Level limitations"),
                new GUIContent("Moves"),
                new GUIContent("Moves\nTimer"),
                new GUIContent("Moves\nTimer")
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Powerups"),
                new GUIContent("Simple Bomb\nCross Bomb"),
                new GUIContent("Simple Bomb\nCross Bomb\n+\nColor Bomb\nLadybird\nHorizontal Line Bomb\nVertical Line Bomb\nLightning Bomb\nUltra Color Bomb\nRainbows"),
                new GUIContent("Simple Bomb\nCross Bomb\n+\nColor Bomb\nLadybird\nHorizontal Line Bomb\nVertical Line Bomb\nLightning Bomb\nUltra Color Bomb\nRainbows")
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Obstacles"),
                new GUIContent("Wooden Block"),
                new GUIContent("Wooden Block\n+\nBranch\nWeed\nStone\nWall"),
                new GUIContent("Wooden Block\n+\nBranch\nWeed\nStone\nWall")
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Level map size"),
                new GUIContent("1 location"),
                new GUIContent("5+ locations"),
                new GUIContent("5+ locations")
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Boosters"),
                new GUIContent(images["NoMark"]),
                new GUIContent("Spoon\nMagic Finger\nHand\nBombs\nLadybirds\nRainbows\nShaker", images["YesMark"]),
                new GUIContent("Spoon\nMagic Finger\nHand\nBombs\nLadybirds\nRainbows\nShaker", images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Energy system"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Daily reward\n(Spin Wheel)"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("User Inventory"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("In-game store\n(including Hard Currency and In-App Purchases)"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("The Fixer\n(Automatic solving of bugs)"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Customer support"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Free updates"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Localization system"),
                new GUIContent(images["NoMark"]),
                new GUIContent("+\nEnglish\nand\nRussian\nlanguages", images["YesMark"]),
                new GUIContent("+\nEnglish\nand\nRussian\nlanguages", images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Advertising"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["NoMark"]),
                new GUIContent("UnityAds\nChartboost\nAdColony\nAdMob", images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Cut-scenes"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("User progression syncing"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Facebook coonnect\n(User avatars, sharing, invitations)"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Leaderboards"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, tableStyle,
            new GUIContent[] {
                new GUIContent("Local notifications\n(Android and iOS)"),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["NoMark"]),
                new GUIContent(images["YesMark"])
        });

        DrawTable(width, titleStyle,
            new GUIContent[] {
                new GUIContent("COST"),
                new GUIContent("FREE"),
                new GUIContent("100$"),
                new GUIContent("300$")
        });

        DrawTableLinks(width, buttonStyle,
            new GUIContent[] {
                null,
                null,
                new GUIContent("GET IT"),
                new GUIContent("GET IT")
        });

        GUILayout.Space(50);
        #endregion

        #region About the PRO

        EditorGUILayout.Space();
        DrawImage("HeaderLogo");
        EditorGUILayout.Space();

        #region Duel with Ted
        GUILayout.Label("Duel with Ted", titleStyle);
        GUILayout.Label("\"Duel\" is an exclusive gameplay mode, where the player will need to fill the level with jam like in the Jam mode. But he/she will compete with AI (one of in-game characters is Ted). It is really funny game play mode.", textStyle);
        DrawImage("DuelWithTed");
        GUILayout.Space(20);
        #endregion

        #region Cut-scenes
        GUILayout.Label("Cut-Scenes", titleStyle);
        GUILayout.Label("You may write a short dialog between the two characters that will play back every time you launch a pre-selected level. This allows to introduce exciting tutorial moments into the game as well as enrich the game with a plot.", textStyle);
        DrawImage("CutScenes");
        GUILayout.Space(20);
        #endregion

        #region Leaderboards
        GUILayout.Label("Leaderboards", titleStyle);
        GUILayout.Label("Each level has its own list of leaders, where the player may compete with his Facebook friends as well as with fake players from the Berry Match-Three universe.", textStyle);
        DrawImage("Leaderboard");
        GUILayout.Space(20);
        #endregion

        #region Facebook
        GUILayout.Label("Facebook SDK", titleStyle);
        GUILayout.Label("The game uses the Facebook SDK for syncing user profiles. Also this SDK allows to share scores and rewards as well as invite player's friends into the game. This is a great virality driver for your game!", textStyle);
        DrawImage("Facebook");
        GUILayout.Space(20);
        #endregion

        #region Local Notifications
        GUILayout.Label("Local Notifications", titleStyle);
        GUILayout.Label("The player will always know it’s time to play again. Each time when the player’s lives will be refilled, he will receive a message on his/her telephone. This concerns the daily rewards too. Also, the game will wish the player good morning if he ever forgets to play it :)", textStyle);
        DrawImage("Notifications");
        GUILayout.Space(20);
        #endregion

        #region Syncing
        GUILayout.Label("Syncing", titleStyle);
        GUILayout.Label("By player’s wish he can connect the game to his Facebook profile and save the progress in the cloud and, as a result, play on multiple devices. This is made possible by the Backendless cloud database service.", textStyle);
        DrawImage("Syncing");
        GUILayout.Space(20);
        #endregion

        #region Ads Networks
        GUILayout.Label("Ads Networks", titleStyle);
        GUILayout.Label("The PRO version contains built-in advertisement networks: Chartboost, UnityAds, AdColony and AdMob. They allow to monetize your application by means of showing video ads. There is also a way to reward the player for viewing an extra ad video.", textStyle);
        DrawImage("AdsLogos");
        GUILayout.Space(20);
        #endregion
        
        #region Buttons
        GUILayout.Space(30);
        float buttonWidth = 150;
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(70));
        Rect buttonRect = new Rect(rect);
        buttonRect.x = rect.width / 2 - buttonWidth - 5;
        buttonRect.width = buttonWidth;
        if (GUI.Button(buttonRect, "TRY DEMO", buttonStyle))
            Application.OpenURL("https://" + "play.google.com/store/apps/details?id=com.yurowm.berrymatchpro");

        buttonRect.x = rect.width / 2 + 5;
        if (GUI.Button(buttonRect, "GET PRO", buttonStyle))
            Application.OpenURL("https://" + "www.sellmyapp.com/downloads/berry-match-three-pro/");
        GUILayout.Space(100);
        #endregion
        #endregion

        EditorGUILayout.EndVertical();
    }

    void DrawImage(string _name) {
        Texture texture = images[_name];
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(texture.height));
        float width = Mathf.Min(rect.width, texture.width);
        rect.x = rect.width / 2 - width / 2;
        rect.width = width;
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
    }

    void DrawTable(float width, GUIStyle style, GUIContent[] content) {
        Rect[] cells = GetRects(width, style, content);

        for (int i = 0; i < 4; i++) {
            if (content[i] != null) {
                Handles.DrawSolidRectangleWithOutline(cells[i], cell_color[i], Color.gray);
                GUI.Box(cells[i], content[i], style);
            }
        }
    }

    void DrawTableLinks(float width, GUIStyle style, GUIContent[] content) {
        Rect[] cells = GetRects(width, style, content);
        string[] links = new string[] {
            null,
            null,
            "http://u3d.as/c3R",
            "https://www.sellmyapp.com/downloads/berry-match-three-pro/",
        };

        for (int i = 0; i < 4; i++) {
            cells[i].x += cells[i].width / 2;
            cells[i].width = 90;
            cells[i].x -= cells[i].width / 2;
        }

        for (int i = 0; i < 4; i++) {
            if (content[i] != null) {
                if (GUI.Button(cells[i], content[i], style))
                    Application.OpenURL(links[i]);
            }
        }
    }

    Rect[] GetRects(float width, GUIStyle style, GUIContent[] content) {
        float height = 0;

        float table_width = Mathf.Clamp(width, 400f, 700f);
        foreach (GUIContent c in content)
            if (c != null)
                height = Mathf.Max(height, style.CalcHeight(c, table_width / 4));

        Rect row = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(height));
        row.x = row.x + row.width / 2 - table_width / 2;
        row.width = table_width;
        row.height += 2;

        Rect[] result = new Rect[4];

        result[0] = new Rect(row);
        result[1] = new Rect(row);
        result[2] = new Rect(row);
        result[3] = new Rect(row);

        result[0].x = row.x + row.width / 2 - row.width / 2;
        result[0].width = row.width / result.Length;

        result[1].x = result[0].x + result[0].width;
        result[1].width = result[0].width;

        result[2].x = result[1].x + result[1].width;
        result[2].width = result[0].width;

        result[3].x = result[2].x + result[2].width;
        result[3].width = result[0].width;

        return result;
    }
}