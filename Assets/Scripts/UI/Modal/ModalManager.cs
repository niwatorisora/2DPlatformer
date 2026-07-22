using System.Collections.Generic;
using UnityEngine;

public class ModalManager : MonoBehaviour
{
    [SerializeField] CanvasGroup dimOverlay;
    [SerializeField] ModalPanel pauseModal;

    readonly Stack<ModalPanel> panels = new();
    float savedTimeScale;

    public bool AnyOpen => panels.Count > 0;

    void Awake()
    {
        SetDimOverlay(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (AnyOpen) Pop();
        else if (pauseModal != null) Push(pauseModal);
    }

    public void Push(ModalPanel panel)
    {
        if (panel == null)
        {
            GameLog.Warning(this, "Cannot push a null modal panel.");
            return;
        }

        if (panels.Contains(panel))
        {
            GameLog.Warning(this, $"Modal panel '{panel.name}' is already open.");
            return;
        }

        // timeScale の保存復元は、最初と最後の 1 回だけ行う。
        if (!AnyOpen)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            SetDimOverlay(true);
        }

        panel.SetOwner(this);
        panels.Push(panel);
        panel.Open();
    }

    public void Pop()
    {
        if (!AnyOpen) return;

        var panel = panels.Pop();
        panel.Close();
        panel.SetOwner(null);

        if (!AnyOpen)
        {
            Time.timeScale = savedTimeScale;
            SetDimOverlay(false);
        }
    }

    internal bool IsTop(ModalPanel panel)
    {
        return AnyOpen && panels.Peek() == panel;
    }

    void SetDimOverlay(bool visible)
    {
        if (dimOverlay == null) return;

        dimOverlay.alpha = visible ? 1f : 0f;
        dimOverlay.interactable = visible;
        dimOverlay.blocksRaycasts = visible;
        dimOverlay.gameObject.SetActive(visible);
    }

    // スタック式なので「pause→その上に設定」の重ね開きが自然に動く。
}
