using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>範囲ダメージと簡易演出を行う、攻撃種別に依存しない爆発。</summary>
[Serializable]
public struct ExplosionSpec
{
    public float radius;
    public float damage;
    public float knockbackForce;
    public float knockbackUpwardRatio;
    public float stunSeconds;

    public static ExplosionSpec Default => new()
    {
        radius = 1.5f, damage = 10f, knockbackForce = 7f,
        knockbackUpwardRatio = 0.7f, stunSeconds = 0.3f
    };
}

public class Explosion : MonoBehaviour
{
    const float VisualDuration = 0.25f;
    static Sprite fallbackSprite;
    SpriteRenderer visual;

    public void Detonate(ExplosionSpec spec, GameObject instigator)
    {
        spec.radius = Mathf.Max(0f, spec.radius);
        spec.damage = Mathf.Max(0f, spec.damage);
        TeamId sourceTeam = instigator != null && instigator.TryGetComponent(out TeamAffiliation team)
            ? team.TeamId : TeamId.Neutral;
        var hitHealths = new HashSet<Health>();
        int hitCount = 0;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, spec.radius))
        {
            Health victim = hit.GetComponentInParent<Health>();
            if (victim == null || !hitHealths.Add(victim) || IsInstigator(victim, instigator)) continue;
            var context = new DamageContext(Mathf.RoundToInt(spec.damage), instigator, sourceTeam, hit);
            if (!victim.CanReceiveDamage(context)) continue;
            victim.TakeDamage(context);
            hitCount++;
            KnockbackUtility.Apply(victim, transform.position, spec.knockbackForce,
                spec.knockbackUpwardRatio, spec.stunSeconds);
        }
        GameLog.Debug(this, $"爆発: {hitCount}体に命中");
        StartCoroutine(PlayVisual(spec.radius));
    }

    /// <summary>頻度が上がったらプール化。</summary>
    public static Explosion Spawn(Vector3 position, ExplosionSpec spec, GameObject instigator)
    {
        var explosionObject = new GameObject("Explosion");
        explosionObject.transform.position = position;
        Explosion explosion = explosionObject.AddComponent<Explosion>();
        explosion.Detonate(spec, instigator);
        return explosion;
    }

    static bool IsInstigator(Health victim, GameObject instigator) => instigator != null
        && (victim.gameObject == instigator || victim.transform.IsChildOf(instigator.transform));

    IEnumerator PlayVisual(float radius)
    {
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = fallbackSprite ??= CreateFallbackSprite();
        visual.color = new Color(1f, 0.65f, 0.15f, 0.8f);
        float endScale = Mathf.Max(0.3f, radius);
        for (int step = 0; step < 3; step++)
        {
            float t = step / 2f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.3f, endScale, t);
            yield return new WaitForSeconds(VisualDuration / 3f);
        }
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    static Sprite CreateFallbackSprite()
    {
        const int size = 16;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), Vector2.one * 7.5f) <= 7.5f
                ? Color.white : Color.clear;
        texture.SetPixels(pixels); texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }
}
