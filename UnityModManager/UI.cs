﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Harmony12;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public class UI : MonoBehaviour
        {
            internal static bool Load()
            {
                try
                {
                    new GameObject(typeof(UI).FullName, typeof(UI));

                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                return false;
            }

            private static UI mInstance = null;

            public static UI Instance
            {
                get { return mInstance; }
            }

            public static GUIStyle window = null;
            public static GUIStyle h1 = null;
            public static GUIStyle h2 = null;
            public static GUIStyle bold = null;
            private static GUIStyle settings = null;
            private static GUIStyle status = null;
            private static GUIStyle www = null;
            private static GUIStyle updates = null;

            private bool mFirstLaunch = false;
            private bool mInit = false;

            private bool mOpened = false;
            public bool Opened { get { return mOpened; } }

            private Rect mWindowRect = new Rect(0, 0, 0, 0);
            private Vector2 mWindowSize = Vector2.zero;
            private Vector2 mExpectedWindowSize = Vector2.zero;

            private void Awake()
            {
                mInstance = this;
                DontDestroyOnLoad(this);
                mWindowSize = ClampWindowSize(new Vector2(Params.WindowWidth, Params.WindowHeight));
                mExpectedWindowSize = mWindowSize;
                Textures.Init();
                var harmony = HarmonyInstance.Create("UnityModManager.UI");
                var original = typeof(Screen).GetMethod("set_lockCursor");
                var prefix = typeof(Screen_lockCursor_Patch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(original, new HarmonyMethod(prefix));
            }

            private void Start()
            {
                CalculateWindowPos();
                if (Params.ShowOnStart == 1 && string.IsNullOrEmpty(Config.UIStartingPoint))
                {
                    FirstLaunch();
                }
                if (Params.CheckUpdates == 1)
                {
                    CheckModUpdates();
                }
            }

            private void OnDestroy()
            {
                SaveSettingsAndParams();
                Logger.WriteBuffers();
            }

            private void Update()
            {
                var deltaTime = Time.deltaTime;
                foreach (var mod in modEntries)
                {
                    if (mod.Active && mod.OnUpdate != null)
                    {
                        try
                        {
                            mod.OnUpdate.Invoke(mod, deltaTime);
                        }
                        catch (Exception e)
                        {
                            mod.Logger.Error("OnUpdate: " + e.GetType().Name + " - " + e.Message);
                            Debug.LogException(e);
                        }
                    }
                }

                bool toggle = false;

                switch (Params.ShortcutKeyId)
                {
                    default:
                        if (Input.GetKeyUp(KeyCode.F10) && (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)))
                        {
                            toggle = true;
                        }

                        break;
                    case 1:
                        if (Input.GetKeyUp(KeyCode.ScrollLock))
                        {
                            toggle = true;
                        }

                        break;
                    case 2:
                        if (Input.GetKeyUp(KeyCode.KeypadMultiply))
                        {
                            toggle = true;
                        }

                        break;
                    case 3:
                        if (Input.GetKeyUp(KeyCode.BackQuote))
                        {
                            toggle = true;
                        }

                        break;
                }

                if (toggle)
                {
                    ToggleWindow();
                }
            }

            private void FixedUpdate()
            {
                var deltaTime = Time.fixedDeltaTime;
                foreach (var mod in modEntries)
                {
                    if (mod.Active && mod.OnFixedUpdate != null)
                    {
                        try
                        {
                            mod.OnFixedUpdate.Invoke(mod, deltaTime);
                        }
                        catch (Exception e)
                        {
                            mod.Logger.Error("OnFixedUpdate: " + e.GetType().Name + " - " + e.Message);
                            Debug.LogException(e);
                        }
                    }
                }
            }

            private void LateUpdate()
            {
                var deltaTime = Time.deltaTime;
                foreach (var mod in modEntries)
                {
                    if (mod.Active && mod.OnLateUpdate != null)
                    {
                        try
                        {
                            mod.OnLateUpdate.Invoke(mod, deltaTime);
                        }
                        catch (Exception e)
                        {
                            mod.Logger.Error("OnLateUpdate: " + e.GetType().Name + " - " + e.Message);
                            Debug.LogException(e);
                        }
                    }
                }

                Logger.Watcher(deltaTime);
            }

            private void PrepareGUI()
            {
                window = new GUIStyle();
                window.name = "umm window";
                window.normal.background = Textures.Window;
                window.normal.background.wrapMode = TextureWrapMode.Repeat;
                window.padding = RectOffset(5);

                h1 = new GUIStyle();
                h1.name = "umm h1";
                h1.normal.textColor = Color.white;
                h1.fontSize = 16;
                h1.fontStyle = FontStyle.Bold;
                h1.alignment = TextAnchor.MiddleCenter;
                h1.margin = RectOffset(0, 5);

                h2 = new GUIStyle();
                h2.name = "umm h2";
                h2.normal.textColor = new Color(0.6f, 0.91f, 1f);
                h2.fontSize = 13;
                h2.fontStyle = FontStyle.Bold;
                //                h2.alignment = TextAnchor.MiddleCenter;
                h2.margin = RectOffset(0, 3);

                bold = new GUIStyle(GUI.skin.label);
                bold.name = "umm bold";
                bold.normal.textColor = Color.white;
                bold.fontStyle = FontStyle.Bold;

                int iconHeight = 28;
                settings = new GUIStyle();
                settings.alignment = TextAnchor.MiddleCenter;
                settings.stretchHeight = true;
                settings.fixedWidth = 24;
                settings.fixedHeight = iconHeight;

                status = new GUIStyle();
                status.alignment = TextAnchor.MiddleCenter;
                status.stretchHeight = true;
                status.fixedWidth = 12;
                status.fixedHeight = iconHeight;

                www = new GUIStyle();
                www.alignment = TextAnchor.MiddleCenter;
                www.stretchHeight = true;
                www.fixedWidth = 24;
                www.fixedHeight = iconHeight;

                updates = new GUIStyle();
                updates.alignment = TextAnchor.MiddleCenter;
                updates.stretchHeight = true;
                updates.fixedWidth = 26;
                updates.fixedHeight = iconHeight;
            }

            private void OnGUI()
            {
                if (!mInit)
                {
                    mInit = true;
                    PrepareGUI();
                }

                if (mOpened)
                {
                    var backgroundColor = GUI.backgroundColor;
                    var color = GUI.color;
                    GUI.backgroundColor = Color.white;
                    GUI.color = Color.white;
                    mWindowRect = GUILayout.Window(0, mWindowRect, WindowFunction, "", window, GUILayout.Height(mWindowSize.y));
                    GUI.backgroundColor = backgroundColor;
                    GUI.color = color;
                }
            }

            public int tabId = 0;
            public string[] tabs = { "Mods", "Logs", "Settings" };

            class Column
            {
                public string name;
                public float width;
                public bool expand = false;
                public bool skip = false;
            }

            private readonly List<Column> mColumns = new List<Column>
            {
                new Column {name = "Name", width = 200, expand = true},
                new Column {name = "Version", width = 60},
                new Column {name = "Requirements", width = 150, expand = true},
                new Column {name = "On/Off", width = 50},
                new Column {name = "Status", width = 50}
            };

            private Vector2[] mScrollPosition = new Vector2[0];

            private int mShowModSettings = -1;

            private void CalculateWindowPos()
            {
                mWindowSize = ClampWindowSize(mWindowSize);
                mWindowRect = new Rect((Screen.width - mWindowSize.x) / 2f, (Screen.height - mWindowSize.y) / 2f, 0, 0);
            }

            private Vector2 ClampWindowSize(Vector2 orig)
            {
                return new Vector2(Mathf.Clamp(orig.x, Mathf.Min(960, Screen.width), Screen.width), Mathf.Clamp(orig.y, Mathf.Min(720, Screen.height), Screen.height));
            }

            private void WindowFunction(int windowId)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                    GUI.DragWindow(mWindowRect);

                UnityAction buttons = () => { };

                GUILayout.Label("Mod Manager " + version, h1);

                GUILayout.Space(3);
                int tab = tabId;
                tab = GUILayout.Toolbar(tab, tabs, GUILayout.Width(150 * tabs.Length));
                if (tab != tabId)
                {
                    tabId = tab;
                    //                    CalculateWindowPos();
                }

                GUILayout.Space(5);

                if (mScrollPosition.Length != tabs.Length)
                    mScrollPosition = new Vector2[tabs.Length];

                DrawTab(tabId, ref buttons);

                GUILayout.FlexibleSpace();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Close", GUILayout.Width(150)))
                {
                    ToggleWindow();
                }

                if (GUILayout.Button("Save", GUILayout.Width(150)))
                {
                    SaveSettingsAndParams();
                }

                buttons();
                GUILayout.EndHorizontal();
            }

            private void DrawTab(int tabId, ref UnityAction buttons)
            {
                var minWidth = GUILayout.MinWidth(mWindowSize.x);

                switch (tabs[tabId])
                {
                    case "Mods":
                        {
                            mScrollPosition[tabId] = GUILayout.BeginScrollView(mScrollPosition[tabId], minWidth, GUILayout.ExpandHeight(false));

                            var amountWidth = mColumns.Where(x => !x.skip).Sum(x => x.width);
                            var expandWidth = mColumns.Where(x => x.expand && !x.skip).Sum(x => x.width);

                            var mods = modEntries;
                            var colWidth = mColumns.Select(x =>
                                x.expand
                                    ? GUILayout.Width(x.width / expandWidth * (mWindowSize.x - 60 + expandWidth - amountWidth))
                                    : GUILayout.Width(x.width)).ToArray();

                            GUILayout.BeginVertical("box");

                            GUILayout.BeginHorizontal("box");
                            for (int i = 0; i < mColumns.Count; i++)
                            {
                                if (mColumns[i].skip)
                                    continue;
                                GUILayout.Label(mColumns[i].name, colWidth[i]);
                            }

                            GUILayout.EndHorizontal();

                            for (int i = 0, c = mods.Count; i < c; i++)
                            {
                                int col = -1;
                                GUILayout.BeginVertical("box");
                                GUILayout.BeginHorizontal();

                                GUILayout.BeginHorizontal(colWidth[++col]);
                                if (mods[i].OnGUI != null)
                                {
                                    if (GUILayout.Button(mods[i].Info.DisplayName, GUI.skin.label, GUILayout.ExpandWidth(true)))
                                    {
                                        mShowModSettings = (mShowModSettings == i) ? -1 : i;
                                    }

                                    if (GUILayout.Button(mShowModSettings == i ? Textures.SettingsActive : Textures.SettingsNormal, settings))
                                    {
                                        mShowModSettings = (mShowModSettings == i) ? -1 : i;
                                    }
                                }
                                else
                                {
                                    GUILayout.Label(mods[i].Info.DisplayName);
                                }

                                if (!string.IsNullOrEmpty(mods[i].Info.HomePage))
                                {
                                    GUILayout.Space(10);
                                    if (GUILayout.Button(Textures.WWW, www))
                                    {
                                        Application.OpenURL(mods[i].Info.HomePage);
                                    }
                                }

                                if (mods[i].NewestVersion != null)
                                {
                                    GUILayout.Space(10);
                                    GUILayout.Box(Textures.Updates, updates);
                                }

                                GUILayout.Space(20);

                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal(colWidth[++col]);
                                GUILayout.Label(mods[i].Info.Version, GUILayout.ExpandWidth(false));
                                //                            if (string.IsNullOrEmpty(mods[i].Info.Repository))
                                //                            {
                                //                                GUI.color = new Color32(255, 81, 83, 255);
                                //                                GUILayout.Label("*");
                                //                                GUI.color = Color.white;
                                //                            }
                                GUILayout.EndHorizontal();

                                if (mods[i].ManagerVersion > GetVersion())
                                {
                                    GUI.color = new Color32(255, 81, 83, 255);
                                    GUILayout.Label("Manager-" + mods[i].Info.ManagerVersion, colWidth[++col]);
                                    GUI.color = Color.white;
                                }
                                else if (mods[i].Requirements.Count > 0)
                                {
                                    GUILayout.Label(string.Join("\r\n", mods[i].Info.Requirements), colWidth[++col]);
                                }
                                else
                                {
                                    GUILayout.Label("-", colWidth[++col]);
                                }

                                var action = mods[i].Enabled;
                                action = GUILayout.Toggle(action, "", colWidth[++col]);
                                if (action != mods[i].Enabled)
                                {
                                    mods[i].Enabled = action;
                                    if (mods[i].Toggleable)
                                        mods[i].Active = action;
                                }

                                if (mods[i].Active)
                                {
                                    GUILayout.Box(mods[i].Enabled ? Textures.StatusActive : Textures.StatusNeedRestart, status);
                                }
                                else
                                {
                                    GUILayout.Box(!mods[i].Enabled ? Textures.StatusInactive : Textures.StatusNeedRestart, status);
                                }

                                GUILayout.EndHorizontal();

                                if (mShowModSettings == i && mods[i].Active)
                                {
                                    GUILayout.Label("Options", h2);
                                    try
                                    {
                                        mods[i].OnGUI(mods[i]);
                                    }
                                    catch (Exception e)
                                    {
                                        mShowModSettings = -1;
                                        mods[i].Logger.Error("OnGUI: " + e.GetType().Name + " - " + e.Message);
                                        Debug.LogException(e);
                                    }
                                }

                                GUILayout.EndVertical();
                            }

                            GUILayout.EndVertical();

                            GUILayout.EndScrollView();

                            GUILayout.Space(10);

                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            GUILayout.Box(Textures.SettingsNormal, settings);
                            GUILayout.Space(3);
                            GUILayout.Label("Options", GUILayout.ExpandWidth(false));
                            GUILayout.Space(15);
                            GUILayout.Box(Textures.WWW, www);
                            GUILayout.Space(3);
                            GUILayout.Label("Home page", GUILayout.ExpandWidth(false));
                            GUILayout.Space(15);
                            GUILayout.Box(Textures.Updates, updates);
                            GUILayout.Space(3);
                            GUILayout.Label("Available update", GUILayout.ExpandWidth(false));
                            GUILayout.Space(15);
                            GUILayout.Box(Textures.StatusActive, status);
                            GUILayout.Space(3);
                            GUILayout.Label("Active", GUILayout.ExpandWidth(false));
                            GUILayout.Space(10);
                            GUILayout.Box(Textures.StatusInactive, status);
                            GUILayout.Space(3);
                            GUILayout.Label("Inactive", GUILayout.ExpandWidth(false));
                            GUILayout.Space(10);
                            GUILayout.Box(Textures.StatusNeedRestart, status);
                            GUILayout.Space(3);
                            GUILayout.Label("Need restart", GUILayout.ExpandWidth(false));
                            GUILayout.Space(10);
                            GUILayout.Label("[CTRL + LClick]", bold, GUILayout.ExpandWidth(false));
                            GUILayout.Space(3);
                            GUILayout.Label("Drag window", GUILayout.ExpandWidth(false));
                            //                        GUILayout.Space(10);
                            //                        GUI.color = new Color32(255, 81, 83, 255);
                            //                        GUILayout.Label("*", bold, GUILayout.ExpandWidth(false));
                            //                        GUI.color = Color.white;
                            //                        GUILayout.Space(3);
                            //                        GUILayout.Label("Not support updates", GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();

                            if (GUI.changed)
                            {
                            }

                            break;
                        }

                    case "Logs":
                        {
                            mScrollPosition[tabId] = GUILayout.BeginScrollView(mScrollPosition[tabId], minWidth);

                            GUILayout.BeginVertical("box");

                            for (int c = Logger.history.Count, i = Mathf.Max(0, c - Logger.historyCapacity); i < c; i++)
                            {
                                GUILayout.Label(Logger.history[i]);
                            }

                            GUILayout.EndVertical();
                            GUILayout.EndScrollView();

                            buttons += delegate
                            {
                                if (GUILayout.Button("Clear", GUILayout.Width(150)))
                                {
                                    Logger.Clear();
                                }
                                if (GUILayout.Button("Open detailed log", GUILayout.Width(150)))
                                {
                                    OpenUnityFileLog();
                                }
                            };

                            break;
                        }

                    case "Settings":
                        {
                            mScrollPosition[tabId] = GUILayout.BeginScrollView(mScrollPosition[tabId], minWidth);

                            GUILayout.BeginVertical("box");

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Hotkey", GUILayout.ExpandWidth(false));
                            Params.ShortcutKeyId =
                                GUILayout.Toolbar(Params.ShortcutKeyId, mShortcutNames, GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5);

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Check updates", GUILayout.ExpandWidth(false));
                            Params.CheckUpdates = GUILayout.Toolbar(Params.CheckUpdates, mCheckUpdateStrings,
                                GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Show this window on startup", GUILayout.ExpandWidth(false));
                            Params.ShowOnStart = GUILayout.Toolbar(Params.ShowOnStart, mShowOnStartStrings,
                                GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginVertical("box");
                            GUILayout.Label("Window size", bold, GUILayout.ExpandWidth(false));
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Width ", GUILayout.ExpandWidth(false));
                            mExpectedWindowSize.x = GUILayout.HorizontalSlider(mExpectedWindowSize.x, Mathf.Min(Screen.width, 960), Screen.width, GUILayout.Width(200));
                            GUILayout.Label(" " + mExpectedWindowSize.x.ToString("f0") + " px ", GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Height", GUILayout.ExpandWidth(false));
                            mExpectedWindowSize.y = GUILayout.HorizontalSlider(mExpectedWindowSize.y, Mathf.Min(Screen.height, 720), Screen.height, GUILayout.Width(200));
                            GUILayout.Label(" " + mExpectedWindowSize.y.ToString("f0") + " px ", GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();
                            if (GUILayout.Button("Apply", GUILayout.ExpandWidth(false)))
                            {
                                mWindowSize.x = Mathf.Floor(mExpectedWindowSize.x) % 2 > 0 ? Mathf.Ceil(mExpectedWindowSize.x) : Mathf.Floor(mExpectedWindowSize.x);
                                mWindowSize.y = Mathf.Floor(mExpectedWindowSize.y) % 2 > 0 ? Mathf.Ceil(mExpectedWindowSize.y) : Mathf.Floor(mExpectedWindowSize.y);
                                CalculateWindowPos();
                                Params.WindowWidth = mWindowSize.x;
                                Params.WindowHeight = mWindowSize.y;
                            }
                            GUILayout.EndVertical();

                            GUILayout.EndVertical();
                            GUILayout.EndScrollView();

                            break;
                        }
                }
            }

            private string[] mCheckUpdateStrings = { "Never", "Automatic" };
            
            private string[] mShowOnStartStrings = { "No", "Yes" };

            private string[] mShortcutNames = { "CTRL+F10", "ScrollLock", "Num *", "~" };

            internal bool GameCursorLocked { get; set; }

            public void FirstLaunch()
            {
                if (mFirstLaunch)
                    return;

                mFirstLaunch = true;
                try
                {
                    ToggleWindow(true);
                }
                catch(Exception e)
                {
                    Logger.Error("FirstLaunch: " + e.GetType().Name + " - " + e.Message);
                    Debug.LogException(e);
                }
            }

            public void ToggleWindow()
            {
                ToggleWindow(!mOpened);
            }

            public void ToggleWindow(bool open)
            {
                if (open == mOpened)
                    return;

                mOpened = open;
                BlockGameUI(open);
                if (!open)
                {
                    SaveSettingsAndParams();
                }
                if (open)
                {
                    GameCursorLocked = Cursor.lockState == CursorLockMode.Locked || !Cursor.visible;
                    if (GameCursorLocked)
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;
                    }
                }
                else
                {
                    if (GameCursorLocked)
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                }
            }

            private GameObject mCanvas = null;

            private void BlockGameUI(bool value)
            {
                if (value)
                {
                    mCanvas = new GameObject("", typeof(Canvas), typeof(GraphicRaycaster));
                    mCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    mCanvas.GetComponent<Canvas>().sortingOrder = Int16.MaxValue;
                    DontDestroyOnLoad(mCanvas);
                    var panel = new GameObject("", typeof(Image));
                    panel.transform.SetParent(mCanvas.transform);
                    panel.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
                    panel.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                    panel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                    panel.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                }
                else
                {
                    Destroy(mCanvas);
                }
            }

            private static RectOffset RectOffset(int value)
            {
                return new RectOffset(value, value, value, value);
            }

            private static RectOffset RectOffset(int x, int y)
            {
                return new RectOffset(x, x, y, y);
            }
        }

        //        [HarmonyPatch(typeof(Screen), "lockCursor", MethodType.Setter)]
        static class Screen_lockCursor_Patch
        {
            static bool Prefix(bool value)
            {
                if (UI.Instance != null && UI.Instance.Opened)
                {
                    UI.Instance.GameCursorLocked = value;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    return false;
                }

                return true;
            }
        }

    }
}

