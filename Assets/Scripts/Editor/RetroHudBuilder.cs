using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class RetroHudBuilder : EditorWindow
{
    [SerializeField] HudTheme theme;
    Font FallbackFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    [MenuItem("Tools/Build Retro HUD")]
    static void Open() => GetWindow<RetroHudBuilder>(true, "Build Retro HUD");

    void OnGUI()
    {
        EditorGUILayout.LabelField("HUD Theme", EditorStyles.boldLabel);
        theme = (HudTheme)EditorGUILayout.ObjectField(theme, typeof(HudTheme), false);
        EditorGUILayout.HelpBox("先に Assets/Create/UI/Hud Theme でテーマを作成してください。", MessageType.Info);
        using (new EditorGUI.DisabledScope(theme == null))
            if (GUILayout.Button("Build Retro HUD")) Build();
    }

    void Build()
    {
        var root = GameObject.Find("Retro HUD");
        if (root == null) root = Ui("Retro HUD", null, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        root.SetActive(false);
        var canvas = Ensure<Canvas>(root);
        canvas.renderMode = Camera.main != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 100;
        Ensure<GraphicRaycaster>(root);
        var scaler = Ensure<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        // HPバーは非表示の方針（チーム決定）。復活させる場合はこの1行を戻す
        // BuildHp(root.transform);
        BuildScore(root.transform);
        BuildAmmo(root.transform);
        BuildWave(root.transform);
        root.SetActive(true);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
    }

    void BuildHp(Transform parent)
    {
        var go = Ui("HP Segments", parent, new Vector2(640f, 24f), Vector2.up, Vector2.up, new Vector2(32f, -32f));
        var layout = Ensure<HorizontalLayoutGroup>(go);
        layout.spacing = 2f; layout.childControlWidth = false; layout.childControlHeight = true;
        layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
        var view = Ensure<PlayerHpHudView>(go);
        Set(view, "theme", theme); Set(view, "segmentContainer", go.GetComponent<RectTransform>());
    }

    void BuildScore(Transform parent)
    {
        var panel = Panel("Score", parent, new Vector2(320f, 64f), Vector2.one, Vector2.one, new Vector2(-32f, -32f));
        var label = Label("Score Label", panel.transform, "SCORE 0", 32, TextAnchor.MiddleRight);
        Stretch(label.rectTransform, 16f);
        var view = Ensure<ScoreHudView>(panel);
        Set(view, "theme", theme); Set(view, "panel", panel.GetComponent<Image>()); Set(view, "scoreLabel", label);
        view.ApplyTheme();
    }

    void BuildAmmo(Transform parent)
    {
        var panel = Panel("Ammo", parent, new Vector2(420f, 160f), Vector2.right, Vector2.right, new Vector2(-32f, 32f));
        var icon = Image("Weapon Icon", panel.transform, new Vector2(128f, 64f), new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f));
        icon.preserveAspect = true;
        var ammo = Label("Ammo Count", panel.transform, "0 / 0", 40, TextAnchor.MiddleRight);
        Rect(ammo.rectTransform, new Vector2(248f, 64f), Vector2.right, Vector2.right, new Vector2(-16f, -12f));
        var weapon = Label("Weapon Name", panel.transform, "", 20, TextAnchor.MiddleCenter);
        Rect(weapon.rectTransform, new Vector2(128f, 24f), new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 44f));
        theme.ApplyText(weapon, theme.BoneCream());
        var view = Ensure<AmmoHudView>(panel);
        Set(view, "theme", theme); Set(view, "panel", panel.GetComponent<Image>());
        Set(view, "ammoLabel", ammo); Set(view, "weaponNameLabel", weapon); Set(view, "weaponIconImage", icon);
        view.ApplyTheme();

        var reload = Label("Reload Indicator", panel.transform, "RELOADING", 26, TextAnchor.MiddleRight);
        Rect(reload.rectTransform, new Vector2(248f, 32f), Vector2.one, Vector2.one, new Vector2(-16f, -16f));
        var reloadView = Ensure<ReloadIndicatorView>(reload.gameObject);
        Set(reloadView, "theme", theme); Set(reloadView, "reloadLabel", reload); reloadView.ApplyTheme();
    }

    void BuildWave(Transform parent)
    {
        var panel = Panel("Wave Banner", parent, new Vector2(400f, 80f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -64f));
        var label = Label("Wave Label", panel.transform, "WAVE 1", 40, TextAnchor.MiddleCenter);
        Stretch(label.rectTransform, 12f);
        var view = Ensure<WaveBannerView>(panel);
        Set(view, "theme", theme); Set(view, "panel", panel.GetComponent<Image>()); Set(view, "waveLabel", label);
        view.ApplyTheme();
    }

    GameObject Panel(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position)
    { var go = Ui(name, parent, size, anchor, pivot, position); Ensure<Image>(go); return go; }

    Text Label(string name, Transform parent, string value, int size, TextAnchor alignment)
    {
        var go = Ui(name, parent, Vector2.zero, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero);
        var text = Ensure<Text>(go); text.text = value; text.font = FallbackFont;
        theme.ApplyText(text, theme.BoneCream());
        text.fontSize = size; text.alignment = alignment; text.raycastTarget = false; return text;
    }

    Image Image(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position)
    { var go = Ui(name, parent, size, anchor, pivot, position); return Ensure<Image>(go); }

    GameObject Ui(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position)
    {
        var child = parent == null ? null : parent.Find(name);
        var go = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform));
        if (child == null) go.transform.SetParent(parent, false);
        Rect(go.GetComponent<RectTransform>(), size, anchor, pivot, position);
        return go;
    }

    static T Ensure<T>(GameObject go) where T : Component
    { return go.TryGetComponent<T>(out var component) ? component : Undo.AddComponent<T>(go); }

    static void Rect(RectTransform rt, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position)
    { rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.sizeDelta = size; rt.anchoredPosition = position; }

    static void Stretch(RectTransform rt, float padding)
    { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.one * padding; rt.offsetMax = Vector2.one * -padding; }

    static void Set(Object target, string property, Object value)
    { var so = new SerializedObject(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
}
