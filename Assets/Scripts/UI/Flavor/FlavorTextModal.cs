using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class FlavorTextModal : ModalPanel
{
    static readonly Color BackgroundColor = new(0x0A / 255f, 0x08 / 255f, 0x10 / 255f, .96f);
    [SerializeField] HudTheme theme;
    [SerializeField] RectTransform titleRoot;
    [SerializeField] Text textLabel;
    [SerializeField, Tooltip("フェードイン時間（秒）")] float fadeInDuration = .4f;
    [SerializeField, Tooltip("タイトル文字ごとの出現間隔（秒）")] float titleInterval = .6f;
    [SerializeField, Tooltip("タイトル文字の不透明度です。")] float titleAlpha = .20f;
    [SerializeField, Tooltip("衝撃スケールの合計時間（秒）")] float impactDuration = .1f;
    [SerializeField, Tooltip("タイトル後の待機時間（秒）")] float titlePause = .8f;
    [SerializeField, Tooltip("本文表示後の待機時間（秒）")] float bodyHoldDuration = 2.5f;
    [SerializeField, Tooltip("アウトロのフェード時間（秒）")] float outroDuration = 2f;
    [SerializeField, Tooltip("衝撃スケール演出のON/OFF。OFFでも出現タイミングは同じ")] bool enableImpact = true;
    readonly List<Text> titleCharacters = new();
    string title;
    string body;
    bool isOpen;
    bool isClosing;
    bool hasWarnedMissingTitleRoot;
    Phase phase;

    enum Phase { Closed, Intro, Title, Pause, Body, Hold, Outro }
    public void Show(string title, string bodyText)
    {
        this.title = title ?? string.Empty;
        body = bodyText ?? string.Empty;
        if (isOpen && !isClosing) StartSequence();
    }
    void Update()
    {
        if (!isOpen || phase == Phase.Outro) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            phase = Phase.Outro;
            RequestClose();
        }
    }
    protected override void OnOpened()
    {
        isOpen = true;
        isClosing = false;
        ApplyTheme();
        StartSequence();
    }
    internal override void Close()
    {
        if (isClosing) return;
        isClosing = true;
        phase = Phase.Outro;
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }
    protected override void OnClosed()
    {
        isOpen = false;
        isClosing = false;
        phase = Phase.Closed;
    }
    void StartSequence()
    {
        StopAllCoroutines();
        isClosing = false;
        BuildTitle();
        if (textLabel != null) { textLabel.text = body; textLabel.gameObject.SetActive(false); }
        CanvasGroup.alpha = 0f;
        StartCoroutine(PlaySequence());
    }
    IEnumerator PlaySequence()
    {
        phase = Phase.Intro;
        yield return Fade(CanvasGroup.alpha, 1f, fadeInDuration);
        phase = Phase.Title;
        for (int i = 0; i < titleCharacters.Count; i++)
        {
            Show(titleCharacters[i]);
            yield return Wait(titleInterval);
        }
        phase = Phase.Pause;
        yield return Wait(titlePause);
        phase = Phase.Body;
        if (textLabel != null) { textLabel.gameObject.SetActive(true); yield return Impact(textLabel.rectTransform); }
        phase = Phase.Hold;
        yield return Wait(bodyHoldDuration);
        phase = Phase.Outro;
        RequestClose();
    }
    IEnumerator FadeOut()
    {
        yield return Fade(CanvasGroup.alpha, 0f, outroDuration);
        base.Close();
    }
    IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f) { CanvasGroup.alpha = to; yield break; }
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            CanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        CanvasGroup.alpha = to;
    }

    IEnumerator Impact(RectTransform rect)
    {
        if (!enableImpact) { yield return Wait(impactDuration); yield break; }
        float step = impactDuration * .5f;
        rect.localScale = Vector3.one * 1.4f;
        yield return Wait(step);
        rect.localScale = Vector3.one * 1.15f;
        yield return Wait(step);
        rect.localScale = Vector3.one;
    }

    IEnumerator Wait(float duration)
    {
        if (duration > 0f) yield return new WaitForSecondsRealtime(duration);
    }

    void Show(Text label)
    {
        label.gameObject.SetActive(true);
        StartCoroutine(Impact(label.rectTransform));
    }

    void BuildTitle()
    {
        if (titleRoot == null)
        {
            if (!hasWarnedMissingTitleRoot)
            {
                hasWarnedMissingTitleRoot = true;
                Debug.LogWarning("FlavorTextModal has no TitleRoot assigned. Run Tools/HUD/Rebuild Modal Layer to retrofit this modal.", this);
            }
            return;
        }
        int count = title.Length;
        while (titleCharacters.Count < count) titleCharacters.Add(CreateTitleCharacter());
        float size = Mathf.Clamp(2400f / Mathf.Max(1, count), 160f, 1100f);
        float width = 1920f / Mathf.Max(1, count);
        Color color = theme != null ? theme.Gold() : new Color(0xF0 / 255f, 0xC0 / 255f, 0x30 / 255f);
        color.a *= titleAlpha;
        for (int i = 0; i < titleCharacters.Count; i++)
        {
            bool used = i < count;
            var label = titleCharacters[i];
            label.gameObject.SetActive(false);
            if (!used) continue;
            label.text = title[i].ToString(); label.fontSize = Mathf.RoundToInt(size);
            label.color = color; label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
            if (theme != null) theme.ApplyFlavorText(label, color);
            label.rectTransform.pivot = new Vector2(.5f, .5f); label.rectTransform.sizeDelta = new Vector2(Mathf.Max(width, size), size * 1.2f);
            label.rectTransform.anchoredPosition = new Vector2((i - (count - 1) * .5f) * width, 0f);
        }
    }

    Text CreateTitleCharacter()
    {
        var go = new GameObject("Title Character", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(titleRoot, false);
        var label = go.GetComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        if (theme != null) theme.ApplyFlavorText(label, theme.Gold());
        return label;
    }

    void ApplyTheme()
    {
        if (theme == null) return;
        theme.ApplyImage(GetComponent<Image>(), null, BackgroundColor);
        theme.ApplyFlavorText(textLabel, theme.FlavorBodyColor());
    }
}
