using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ModalLayerBuilder
{
    [MenuItem("Tools/HUD/Rebuild Modal Layer")]
    static void Build()
    {
        var root = FindOrCreate("ModalCanvas", null);
        var canvas = Ensure<Canvas>(root);
        canvas.renderMode = Camera.main != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = FindHudSortingOrder() + 1;
        Ensure<GraphicRaycaster>(root);

        var scaler = Ensure<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        var overlay = FindOrCreate("DimOverlay", root.transform);
        Stretch(overlay.GetComponent<RectTransform>());
        var image = Ensure<Image>(overlay);
        image.color = new Color(0f, 0f, 0f, .6f);
        image.raycastTarget = true;
        var group = Ensure<CanvasGroup>(overlay);
        group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
        overlay.SetActive(false);

        var managerObject = FindOrCreate("ModalManager", root.transform);
        var manager = Ensure<ModalManager>(managerObject);
        Set(manager, "dimOverlay", group);
        BuildFlavor(root.transform, manager);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        root.SetActive(true);
        Selection.activeGameObject = root;
    }

    static void BuildFlavor(Transform parent, ModalManager manager)
    {
        var panel = FindOrCreate("FlavorTextModal", parent);
        Stretch(panel.GetComponent<RectTransform>());
        var image = Ensure<Image>(panel);
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = new Color(0x0A / 255f, 0x08 / 255f, 0x10 / 255f, .96f);
        image.raycastTarget = true;
        var canvasGroup = Ensure<CanvasGroup>(panel);
        canvasGroup.alpha = 0f; canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false;

        var modal = Ensure<FlavorTextModal>(panel);
        RemoveChild(panel.transform, "Title");
        RemoveChild(panel.transform, "Continue Indicator");
        var titleRoot = FindOrCreate("TitleRoot", panel.transform).GetComponent<RectTransform>();
        Stretch(titleRoot);
        RemoveMask(titleRoot.gameObject);
        titleRoot.SetSiblingIndex(0);
        var label = Label("Text", panel.transform, 48, TextAnchor.MiddleCenter);
        Rect(label.rectTransform, new Vector2(1100f, 360f), new Vector2(.5f, .45f),
            Vector2.one * .5f, Vector2.zero);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.lineSpacing = 1.25f;

        HudTheme theme = FirstAsset<HudTheme>();
        Set(modal, "theme", theme); Set(modal, "titleRoot", titleRoot);
        Set(modal, "textLabel", label);

        var serviceObject = FindOrCreate("FlavorTextService", parent);
        var service = Ensure<FlavorTextService>(serviceObject);
        Set(service, "modalManager", manager); Set(service, "modal", modal);
        FlavorTextSet textSet = FirstAsset<FlavorTextSet>();
        Set(service, "textSet", textSet);
        var triggerObject = FindOrCreate("WaveFlavorTrigger", parent);
        var trigger = Ensure<WaveFlavorTrigger>(triggerObject);
        Set(trigger, "spawner", Object.FindFirstObjectByType<WaveSpawner>());
        Set(trigger, "flavorTextService", service);
        if (textSet == null)
            Debug.LogWarning("FlavorTextSet がありません。Assets/Create/UI/Flavor Text Set で作成し、再度 Tools/HUD/Rebuild Modal Layer を実行してください。");
        if (theme == null)
            Debug.LogWarning("HudTheme がありません。FlavorTextModal の配色を設定するため作成してください。");
    }

    static int FindHudSortingOrder()
    {
        var hud = GameObject.Find("Retro HUD");
        return hud != null && hud.TryGetComponent<Canvas>(out var canvas) ? canvas.sortingOrder : 100;
    }

    static GameObject FindOrCreate(string name, Transform parent)
    {
        var child = parent == null ? GameObject.Find(name)?.transform : parent.Find(name);
        var go = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform));
        if (child == null) go.transform.SetParent(parent, false);
        return go;
    }

    static void RemoveChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) Undo.DestroyObjectImmediate(child.gameObject);
    }

    static void RemoveMask(GameObject go)
    {
        if (go.TryGetComponent<Mask>(out var mask)) Undo.DestroyObjectImmediate(mask);
        if (go.TryGetComponent<RectMask2D>(out var rectMask)) Undo.DestroyObjectImmediate(rectMask);
    }

    static T Ensure<T>(GameObject go) where T : Component
    { return go.TryGetComponent<T>(out var component) ? component : Undo.AddComponent<T>(go); }

    static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }

    static void Stretch(RectTransform rect, float padding)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * padding; rect.offsetMax = Vector2.one * -padding; }

    static Text Label(string name, Transform parent, int size, TextAnchor alignment)
    {
        var go = FindOrCreate(name, parent);
        var label = Ensure<Text>(go);
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size; label.alignment = alignment; label.raycastTarget = false;
        return label;
    }

    static void Rect(RectTransform rect, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position)
    { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.sizeDelta = size; rect.anchoredPosition = position; }

    static T FirstAsset<T>() where T : Object
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void Set(Object target, string property, Object value)
    { var so = new SerializedObject(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
}
