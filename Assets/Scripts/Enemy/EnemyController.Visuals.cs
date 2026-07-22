using UnityEngine;

public partial class EnemyController
{
    void ApplySkin(Sprite skinSprite, float skinScale)
    {
        if (spriteRenderer != null && !originalSpriteCached) { originalSprite = spriteRenderer.sprite; originalSpriteCached = true; }
        if (spriteRenderer != null) spriteRenderer.sprite = skinSprite != null ? skinSprite : originalSprite;
        if (visualRoot == null) return;
        if (!originalVisualRootScaleCached) { originalVisualRootScale = visualRoot.localScale; originalVisualRootLocalPosition = visualRoot.localPosition; originalVisualRootScaleCached = true; }
        // プール再利用時は元の位置と倍率を戻し、補正の累積を防ぐ。
        visualRoot.localPosition = originalVisualRootLocalPosition;
        visualRoot.localScale = originalVisualRootScale * skinScale;
        if (enemyCollider == null || spriteRenderer == null || spriteRenderer.sprite == null || !TryGetColliderBottomLocal(out float bottom)) return;
        float spriteBottom = visualRoot.localPosition.y + spriteRenderer.sprite.bounds.min.y * visualRoot.localScale.y;
        visualRoot.localPosition += new Vector3(0f, bottom - spriteBottom, 0f);
    }

    bool TryGetColliderBottomLocal(out float bottom)
    {
        if (enemyCollider is BoxCollider2D box) { bottom = box.offset.y - box.size.y * .5f; return true; }
        if (enemyCollider is CapsuleCollider2D capsule) { bottom = capsule.offset.y - capsule.size.y * .5f; return true; }
        bottom = 0f;
        if (!warnedAboutUnsupportedAlignmentCollider)
        {
            warnedAboutUnsupportedAlignmentCollider = true;
            GameLog.Warning(this, $"Visual-root alignment supports BoxCollider2D or CapsuleCollider2D, not {enemyCollider.GetType().Name}.");
        }
        return false;
    }
}
