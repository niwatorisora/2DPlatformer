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
        [Tooltip("この見た目の表示倍率。インポートのPPUを触らず微調整する用")]
        [SerializeField] float scale = 1f;
        public Sprite Sprite => sprite;
        public float Scale => scale;
    }

    public readonly struct Selection
    {
        public readonly Sprite Sprite;
        public readonly float Scale;
        public Selection(Sprite sprite, float scale) => (Sprite, Scale) = (sprite, scale);
    }

    [SerializeField] List<Variant> variants = new();
    bool warnedAboutEmptyVariants;

    public Selection PickRandom()
    {
        if (variants == null || variants.Count == 0)
        {
            if (!warnedAboutEmptyVariants)
            {
                warnedAboutEmptyVariants = true;
                GameLog.Warning(this, "EnemySkin に variants がありません。既定のスプライトを使用します。");
            }

            return new Selection(null, 1f);
        }

        Variant variant = variants[UnityEngine.Random.Range(0, variants.Count)];
        return variant != null
            ? new Selection(variant.Sprite, variant.Scale)
            : new Selection(null, 1f);
    }
}
