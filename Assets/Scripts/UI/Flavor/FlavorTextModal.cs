using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class FlavorTextModal : ModalPanel
{
    const float CharactersPerSecond = 30f;
    const float BlinkInterval = .5f;

    [SerializeField] HudTheme theme;
    [SerializeField] Text titleLabel;
    [SerializeField] Text textLabel;
    [SerializeField] Text continueIndicator;

    readonly List<string> pages = new();
    int pageIndex;
    float typedCharacters;
    float blinkTime;
    bool isOpen;
    bool isTyping;

    public void Show(string title, string bodyText)
    {
        if (titleLabel != null) titleLabel.text = title ?? string.Empty;
        pages.Clear();
        foreach (string page in Regex.Split(bodyText ?? string.Empty, @"\r?\n[ \t]*\r?\n"))
            pages.Add(page);
        if (pages.Count == 0) pages.Add(string.Empty);

        pageIndex = 0;
        if (isOpen) BeginPage();
    }

    void Update()
    {
        if (!isOpen) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            HandleAdvance();
            return;
        }

        if (isTyping)
        {
            typedCharacters += CharactersPerSecond * Time.unscaledDeltaTime;
            int count = Mathf.Min(pages[pageIndex].Length, Mathf.FloorToInt(typedCharacters));
            if (textLabel != null) textLabel.text = pages[pageIndex].Substring(0, count);
            if (count == pages[pageIndex].Length) CompletePage();
        }
        else
        {
            blinkTime += Time.unscaledDeltaTime;
            if (blinkTime >= BlinkInterval)
            {
                blinkTime -= BlinkInterval;
                SetIndicator(continueIndicator == null || !continueIndicator.enabled);
            }
        }
    }

    protected override void OnOpened()
    {
        isOpen = true;
        ApplyTheme();
        BeginPage();
    }

    protected override void OnClosed()
    {
        isOpen = false;
        isTyping = false;
        SetIndicator(false);
    }

    void ApplyTheme()
    {
        if (theme == null) return;
        theme.ApplyImage(GetComponent<Image>(), null, theme.PanelDark());
        theme.ApplyText(titleLabel, theme.Gold());
        theme.ApplyText(textLabel, theme.BoneCream());
        theme.ApplyText(continueIndicator, theme.BoneCream());
    }

    void HandleAdvance()
    {
        if (isTyping) CompletePage();
        else if (pageIndex < pages.Count - 1)
        {
            pageIndex++;
            BeginPage();
        }
        else RequestClose();
    }

    void BeginPage()
    {
        typedCharacters = 0f;
        blinkTime = 0f;
        isTyping = true;
        if (textLabel != null) textLabel.text = string.Empty;
        SetIndicator(false);
        if (pages[pageIndex].Length == 0) CompletePage();
    }

    void CompletePage()
    {
        isTyping = false;
        if (textLabel != null) textLabel.text = pages[pageIndex];
        blinkTime = 0f;
        SetIndicator(true);
    }

    void SetIndicator(bool visible)
    {
        if (continueIndicator != null) continueIndicator.enabled = visible;
    }
}
