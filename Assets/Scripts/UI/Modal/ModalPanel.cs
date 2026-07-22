using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class ModalPanel : MonoBehaviour
{
    protected CanvasGroup CanvasGroup { get; private set; }

    // ModalManager が Push 時に設定する。この参照を直接変更しないこと。
    protected ModalManager Owner { get; private set; }

    void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();
    }

    // ModalManager 専用。各パネルから直接 Open / Close を呼ばないこと。
    internal void Open()
    {
        SetVisible(true);
        OnOpened();
    }

    // ModalManager 専用。各パネルから直接 Open / Close を呼ばないこと。
    internal virtual void Close()
    {
        SetVisible(false);
        OnClosed();
    }

    internal void SetOwner(ModalManager manager)
    {
        Owner = manager;
    }

    // ボタンなどから閉じる場合は、最前面のときだけスタックを Pop する。
    protected void RequestClose()
    {
        if (Owner != null && Owner.IsTop(this))
            Owner.Pop();
    }

    protected virtual void OnOpened() { }
    protected virtual void OnClosed() { }

    void SetVisible(bool visible)
    {
        CanvasGroup.alpha = visible ? 1f : 0f;
        CanvasGroup.interactable = visible;
        CanvasGroup.blocksRaycasts = visible;
    }
}
