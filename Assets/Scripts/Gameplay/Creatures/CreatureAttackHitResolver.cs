using System.Collections.Generic;
using UnityEngine;
using Synora.Gameplay.Combat;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Physical hit channel of the Altered Verak's attack (M5 Fase 6), the offensive
    /// mirror of the player's PlayerAttackHitResolver. It is the ONLY piece that
    /// touches Physics2D on the creature attack side. It is driven explicitly by
    /// <see cref="CreatureAttackController"/> (BeginWindow on the active edge,
    /// ResolveHits each active tick) — no own Update — so there is no cross-component
    /// frame latency (the F5 order-of-execution risk is avoided by design).
    ///
    /// It resolves each overlapped Collider2D to an <see cref="IDamageable"/> and
    /// applies one <see cref="DamageInfo"/> (SourceKind = Creature) per target per
    /// window. It never accesses Health directly, never uses Animator/AnimationEvents,
    /// and has no "if target is Player" special case. Layers: logical LayerMask filter
    /// (no new layers, no ProjectSettings change); the mask is wired in F7.
    /// </summary>
    public sealed class CreatureAttackHitResolver : MonoBehaviour
    {
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private float attackRange = 1f;
        [SerializeField] private float attackWidth = 1f;
        [SerializeField] private float damageAmount = 1f;

        private readonly Collider2D[] overlapBuffer = new Collider2D[8];

        // Reset at the start of every window (BeginWindow) and on disable: bounded to
        // one attack, never retains stale references across attacks (F5 lesson).
        private readonly HashSet<IDamageable> damagedThisWindow = new HashSet<IDamageable>();
        private readonly Dictionary<Collider2D, IDamageable> resolveCache =
            new Dictionary<Collider2D, IDamageable>();

        private ContactFilter2D targetFilter;
        private bool hasLoggedBufferFull;

        // The attacker's own damageable (if any), excluded from hits so a misconfigured
        // targetLayers mask can never make the creature damage itself. Resolved by
        // hierarchy, not by name/tag.
        private IDamageable self;

        private void Awake()
        {
            self = GetComponentInParent<IDamageable>();

            if (targetLayers.value == 0)
            {
                Debug.LogWarning("CreatureAttackHitResolver: targetLayers mask is empty; no target will be hit.", this);
            }

            if (damageAmount <= 0f)
            {
                Debug.LogWarning("CreatureAttackHitResolver: damageAmount should be greater than zero.", this);
            }

            if (attackRange <= 0f || attackWidth <= 0f)
            {
                Debug.LogWarning("CreatureAttackHitResolver: attackRange and attackWidth should be greater than zero.", this);
            }

            targetFilter = new ContactFilter2D();
            targetFilter.useLayerMask = true;
            targetFilter.SetLayerMask(targetLayers);
            targetFilter.useTriggers = true;
        }

        /// <summary>Opens a fresh window: clears per-window dedupe and resolution cache.</summary>
        public void BeginWindow()
        {
            damagedThisWindow.Clear();
            resolveCache.Clear();
        }

        /// <summary>
        /// Overlaps the frontal box in the captured cardinal direction and applies one
        /// hit per resolved target. Called each active-window tick by the controller.
        /// </summary>
        public void ResolveHits(Vector2Int facingInt)
        {
            if (facingInt == Vector2Int.zero)
            {
                return;
            }

            Vector2 origin = transform.position;
            Vector2 facing = new Vector2(facingInt.x, facingInt.y);
            Vector2 center = origin + facing * (attackRange * 0.5f);
            Vector2 size = (facingInt.x != 0)
                ? new Vector2(attackRange, attackWidth)
                : new Vector2(attackWidth, attackRange);

            int count = Physics2D.OverlapBox(center, size, 0f, targetFilter, overlapBuffer);

            if (count == overlapBuffer.Length && !hasLoggedBufferFull)
            {
                Debug.LogWarning("CreatureAttackHitResolver: overlap buffer full; some targets may be ignored.", this);
                hasLoggedBufferFull = true;
            }

            for (int i = 0; i < count; i++)
            {
                IDamageable target = Resolve(overlapBuffer[i]);
                if (target == null || ReferenceEquals(target, self) || damagedThisWindow.Contains(target))
                {
                    continue; // ignore no-damageable colliders and the attacker itself
                }

                target.ApplyDamage(new DamageInfo(damageAmount, DamageSourceKind.Creature));
                damagedThisWindow.Add(target);
            }
        }

        private IDamageable Resolve(Collider2D collider)
        {
            if (collider == null)
            {
                return null;
            }

            if (resolveCache.TryGetValue(collider, out IDamageable cached))
            {
                return cached;
            }

            IDamageable resolved = collider.GetComponentInParent<IDamageable>();
            resolveCache[collider] = resolved;
            return resolved;
        }

        private void OnDisable()
        {
            damagedThisWindow.Clear();
            resolveCache.Clear();
        }
    }
}
