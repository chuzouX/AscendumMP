using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class DevConsole
{
    private static DevConsoleOverlay instance;

    public static void Init()
    {
        if (instance != null) return;
        GameObject go = new GameObject("DevConsole");
        instance = go.AddComponent<DevConsoleOverlay>();
        UnityEngine.Object.DontDestroyOnLoad(go);
        Debug.Log("[DEVCON] DevConsoleMod initialized. Toggle with VK_OEM_3(~).");
    }
}

public class DevConsoleOverlay : MonoBehaviour
{
    private struct ConsoleEntry
    {
        public string time;
        public string message;
        public string stack;
        public LogType type;
    }

    private const int MaxEntries = 500;
    private readonly List<ConsoleEntry> entries = new List<ConsoleEntry>(MaxEntries);
    private Vector2 scroll;
    private bool visible;
    private bool toggleDown;
    private bool showInfo = true;
    private bool showWarning = true;
    private bool showError = true;
    private bool showStack;
    private bool autoScroll = true;
    private GUIStyle lineStyle;
    private GUIStyle titleStyle;
    private static readonly GUILayoutOption[] NoOptions = new GUILayoutOption[0];

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        bool down = IsToggleDown();
        if (down && !toggleDown) visible = !visible;
        toggleDown = down;
    }

    private bool IsToggleDown()
    {
        if (Input.GetKey(KeyCode.BackQuote)) return true;
        try
        {
            return (GetAsyncKeyState(0xC0) & 0x8000) != 0;
        }
        catch { return false; }
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (entries.Count >= MaxEntries) entries.RemoveAt(0);
        entries.Add(new ConsoleEntry
        {
            time = DateTime.Now.ToString("HH:mm:ss"),
            message = condition == null ? "" : condition,
            stack = stackTrace == null ? "" : stackTrace,
            type = type
        });
        if (autoScroll) scroll.y = 999999f;
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e != null && e.type == EventType.KeyDown && IsToggleEvent(e))
        {
            visible = !visible;
            e.Use();
        }

        if (!visible) return;
        EnsureStyles();

        float width = Mathf.Min(Screen.width - 40f, 1000f);
        float height = Mathf.Min(Screen.height - 40f, 640f);
        Rect panel = new Rect(20f, 20f, width, height);

        GUI.Box(panel, "");
        GUILayout.BeginArea(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, panel.height - 16f));
        GUILayout.BeginHorizontal(NoOptions);
        GUILayout.Label("Developer Console", titleStyle, GUILayout.Width(180f));
        GUILayout.Label("Build: " + DevConsoleBuildInfo.CompiledAt, GUILayout.Width(190f));
        showInfo = GUILayout.Toggle(showInfo, "Info", "Button", GUILayout.Width(64f));
        showWarning = GUILayout.Toggle(showWarning, "Warning", "Button", GUILayout.Width(82f));
        showError = GUILayout.Toggle(showError, "Error", "Button", GUILayout.Width(70f));
        showStack = GUILayout.Toggle(showStack, "Stack", "Button", GUILayout.Width(70f));
        autoScroll = GUILayout.Toggle(autoScroll, "Auto", "Button", GUILayout.Width(64f));
        if (GUILayout.Button("Clear", GUILayout.Width(64f))) entries.Clear();
        GUILayout.FlexibleSpace();
        GUILayout.Label("~", GUILayout.Width(42f));
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        for (int i = 0; i < entries.Count; i++)
        {
            ConsoleEntry entry = entries[i];
            if (!ShouldShow(entry.type)) continue;
            Color old = GUI.color;
            GUI.color = ColorFor(entry.type);
            GUILayout.Label("[" + entry.time + "] [" + entry.type + "] " + entry.message, lineStyle, NoOptions);
            if (showStack && entry.stack.Length > 0 && entry.type != LogType.Log)
                GUILayout.Label(entry.stack, lineStyle, NoOptions);
            GUI.color = old;
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private bool IsToggleEvent(Event e)
    {
        if (e.keyCode == KeyCode.BackQuote) return true;
        return e.character == (char)96 || e.character == (char)126;
    }

    private bool ShouldShow(LogType type)
    {
        if (type == LogType.Log) return showInfo;
        if (type == LogType.Warning) return showWarning;
        return showError;
    }

    private Color ColorFor(LogType type)
    {
        if (type == LogType.Warning) return new Color(1f, 0.85f, 0.25f, 1f);
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) return new Color(1f, 0.35f, 0.3f, 1f);
        return Color.white;
    }

    private void EnsureStyles()
    {
        if (lineStyle != null) return;
        lineStyle = new GUIStyle(GUI.skin.label);
        lineStyle.wordWrap = true;
        lineStyle.fontSize = 12;
        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 14;
    }
}
