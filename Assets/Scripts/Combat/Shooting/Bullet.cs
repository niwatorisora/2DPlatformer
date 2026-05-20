using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    Rigidbody2D rb;
    Action releaseCallback;
    float remainingLifeTime;
    bool isLaunched;
    bool isReleased = true;
    int damage;
    int hitMask;
    GameObject owner;
    TeamId ownerTeam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // BulletData decides whether gravity is enabled when the bullet is launched.
        rb.gravityScale = 0f;
    }

    void Update()
    {
        if (!isLaunched) return;

        remainingLifeTime -= Time.deltaTime;
        if (remainingLifeTime <= 0f) Release();
    }

    public void Launch(Action onRelease, Vector2 direction, BulletConfig config, GameObject owner, TeamId ownerTeam)
    {
        releaseCallback   = onRelease;
        remainingLifeTime = config.lifeTime;
        isLaunched        = true;
        isReleased        = false;
        damage            = config.damage;
        hitMask           = config.hitMask;
        this.owner        = owner;
        this.ownerTeam    = ownerTeam;
        rb.gravityScale   = config.useGravity ? config.gravityScale : 0f;
        rb.linearVelocity = direction.normalized * config.speed;

        // If the bullet spawns already overlapping a target, OnTriggerEnter2D may never fire.
        // Resolve overlaps once on launch so close-range shots still apply damage.
        ResolveOverlapsAfterLaunch();
    }

    void ResolveOverlapsAfterLaunch()
    {
        if (isReleased) return;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        var filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = (LayerMask)hitMask
        };

        var overlaps = new List<Collider2D>(8);
        int count = col.Overlap(filter, overlaps);
        for (int i = 0; i < count; i++)
        {
            ProcessHit(overlaps[i]);
            if (isReleased) break;
        }
    }

    /// <summary>
    /// Clears runtime-only state before returning the object to the pool.
    /// </summary>
    public void ResetForPool()
    {
        isLaunched        = false;
        isReleased        = true;
        remainingLifeTime = 0f;
        releaseCallback   = null;
        damage            = 0;
        hitMask           = 0;
        owner             = null;
        ownerTeam         = TeamId.Neutral;

        if (rb != null)
        {
            rb.linearVelocity  = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale    = 0f;
        }
    }

    void OnTriggerEnter2D(Collider2D other) => ProcessHit(other);

    void ProcessHit(Collider2D other)
    {
        if (isReleased) return;

        // LayerMask is the broad collision filter; team and owner checks are runtime filters.
        if ((hitMask & (1 << other.gameObject.layer)) == 0) return;
        if (IsOwnerCollider(other)) return;

        var damageContext = new DamageContext(damage, owner, ownerTeam, other);
        if (other.GetComponentInParent<IDamageable>() is { } target)
        {
            if (!target.CanReceiveDamage(damageContext)) return;
            target.TakeDamage(damageContext);
            Release();
            return;
        }

        if (IsFriendlyCollider(other)) return;

        // Non-damageable objects in the hitMask still stop the bullet, such as terrain.
        Release();
    }

    bool IsOwnerCollider(Collider2D other)
    {
        if (owner == null) return false;

        // Handles colliders placed on either the shooter or its child objects.
        Transform otherTransform = other.transform;
        Transform ownerTransform = owner.transform;
        return other.gameObject == owner
            || otherTransform.IsChildOf(ownerTransform)
            || ownerTransform.IsChildOf(otherTransform);
    }

    bool IsFriendlyCollider(Collider2D other)
    {
        // TeamAffiliation is required for friendly-fire prevention; layers alone do not decide teams.
        var otherTeam = other.GetComponentInParent<TeamAffiliation>();
        return otherTeam != null && TeamAffiliation.AreFriendly(ownerTeam, otherTeam.TeamId);
    }

    void Release()
    {
        if (isReleased) return;
        isReleased = true;
        releaseCallback?.Invoke();
    }
}
