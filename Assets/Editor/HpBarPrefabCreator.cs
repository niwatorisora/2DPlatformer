using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HpBarPrefabCreator
{
    const string HpBarPrefabPath = "Assets/Prefab/UI/HpBar.prefab";
    static readonly string[] EnemyPrefabPaths =
    {
        "Assets/Prefab/Enemy.prefab",
        "Assets/Prefab/JumpingEnemy.prefab",
    };

    [MenuItem("Tools/Setup HP Bars")]
    static void SetupAll()
    {
        var hpBarPrefab = CreateHpBarPrefab();
        if (hpBarPrefab == null) return;

        foreach (var path in EnemyPrefabPaths)
            AddHpBarToPrefab(path, hpBarPrefab);

        AssetDatabase.SaveAssets();
        Debug.Log("[HpBarPrefabCreator] Done. Add HpBar to the Player GameObject manually via the Scene Hierarchy.");
    }

    static GameObject CreateHpBarPrefab()
    {
        // Root — Canvas (World Space)
        var root = new GameObject("HpBar");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 20f);
        root.transform.localScale = new Vector3(0.005f, 0.005f, 1f);

        // A filled Image only honours fillAmount when it has a sprite; with a null
        // sprite Unity draws a plain full-rect quad and ignores fillAmount entirely.
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Background (lost HP — red)
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(root.transform, false);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = uiSprite;
        bgImg.color = new Color(0.8f, 0.13f, 0.13f);
        StretchRect(bgGo.GetComponent<RectTransform>());

        // Fill (remaining HP — green)
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(root.transform, false);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.sprite = uiSprite;
        fillImg.color = new Color(0.13f, 0.8f, 0.27f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;
        StretchRect(fillGo.GetComponent<RectTransform>());

        // HpBarUI component — wire fill reference
        var hpBar = root.AddComponent<HpBarUI>();
        var so = new SerializedObject(hpBar);
        so.FindProperty("fill").objectReferenceValue = fillImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        System.IO.Directory.CreateDirectory("Assets/Prefab/UI");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, HpBarPrefabPath);
        Object.DestroyImmediate(root);

        if (prefab != null)
        {
            Debug.Log($"[HpBarPrefabCreator] HP bar prefab saved to {HpBarPrefabPath}");
            return prefab;
        }

        Debug.LogError("[HpBarPrefabCreator] Failed to save HP bar prefab.");
        return null;
    }

    static void AddHpBarToPrefab(string prefabPath, GameObject hpBarPrefab)
    {
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[HpBarPrefabCreator] Prefab not found: {prefabPath}");
            return;
        }

        // Skip if already has an HpBar child
        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var root = scope.prefabContentsRoot;
        if (root.GetComponentInChildren<HpBarUI>() != null)
        {
            Debug.Log($"[HpBarPrefabCreator] {prefabPath} already has HpBarUI — skipped.");
            return;
        }

        var instance = PrefabUtility.InstantiatePrefab(hpBarPrefab, root.transform) as GameObject;
        instance.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        Debug.Log($"[HpBarPrefabCreator] Added HpBar to {prefabPath}");
    }

    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
