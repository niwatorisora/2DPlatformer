using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>敵の見た目バリエーションを定義するアーティスト用データ。</summary>
[CreateAssetMenu(fileName = "NewEnemySkin", menuName = "Combat/Enemy Skin")]
public class EnemySkin : ScriptableObject
{
    [Serializable]
    // 将来アニメーション対応する場合はここに frames / frameRate を追加する（既存アセットを壊さず拡張できる）.
    public class Variant
    {
        [SerializeField] Sprite sprite;
        public Sprite Sprite => sprite;
    }

    [SerializeField] List<Variant> variants = new();
    bool warnedAboutEmptyVariants;

    public Sprite PickRandomSprite()
    {
        if (variants == null || variants.Count == 0)
        {
            if (!warnedAboutEmptyVariants)
            {
                warnedAboutEmptyVariants = true;
                GameLog.Warning(this, "EnemySkin に variants がありません。既定のスプライトを使用します。");
            }

            return null;
        }

        Variant variant = variants[UnityEngine.Random.Range(0, variants.Count)];
        return variant != null ? variant.Sprite : null;
    }
}
