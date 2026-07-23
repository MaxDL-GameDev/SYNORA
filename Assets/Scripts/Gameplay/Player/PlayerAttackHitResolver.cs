using System.Collections.Generic;
using UnityEngine;
using Synora.Gameplay.Combat;

namespace Synora.Gameplay.Player
{
    /// <summary>
    /// Physical hit channel of the player's melee attack (M5 Fase 5). It is the ONLY
    /// piece that touches Physics2D on the attack side: while <see cref="PlayerAttack"/>
    /// reports an active hit window, it overlaps a frontal box, resolves each collider
    /// to an <see cref="IDamageable"/> and applies one <see cref="DamageInfo"/> per
    /// target per window. It decides NOTHING about timing, direction or cooldown — that
    /// authority stays in <see cref="PlayerAttack"/>. No Animator, no AnimationEvents,
    /// no direct access to Health (only the IDamageable interface).
    ///
    /// Gate handling is transitive: PlayerAttack cancels its window when the control
    /// gate is blocked, so this resolver never scans while blocked or cancelled — it
    /// only queries physics when the window is active, and never references the gate.
    ///
    /// Layer strategy (SPEC §8, Option B): logical filtering by a serialized LayerMask
    /// over trigger overlaps, no new layers, no ProjectSettings change. Overlap queries
    /// ignore the physics collision matrix, so Player↔Creatures not colliding
    /// (ADR-M3-002) does not prevent detection.
    /// </summary>
    public sealed class PlayerAttackHitResolver : MonoBehaviour
    {
        [SerializeField] private PlayerAttack attack;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private float attackRange = 1f;
        [SerializeField] private float attackWidth = 1f;
        [SerializeField] private float damageAmount = 1f;

        private readonly Collider2D[] overlapBuffer = new Collider2D[8];

        // One IDamageable receives at most one hit per active window; reset on the
        // false -> true transition of the hit window (a new attack).
        private readonly HashSet<IDamageable> damagedThisWindow = new HashSet<IDamageable>();

        // Collider -> IDamageable resolution cache, so GetComponent runs once per
        // collider instead of every frame the window is open (SPEC §4). A null value
        // is a cached "not damageable" result (present key, null value). Scoped to the
        // current window (cleared on the same edge as the dedupe set) so it stays
        // bounded to the few colliders seen during one attack and never retains a
        // destroyed collider across attacks.
        private readonly Dictionary<Collider2D, IDamageable> resolveCache =
            new Dictionary<Collider2D, IDamageable>();

        private ContactFilter2D targetFilter;
        private bool wasWindowActive;
        private bool hasLoggedBufferFull;

        private void Awake()
        {
            if (targetLayers.value == 0)
            {
                Debug.LogWarning("PlayerAttackHitResolver: targetLayers mask is empty; no target will be hit.", this);
            }

            if (attack == null)
            {
                Debug.LogError("PlayerAttackHitResolver: PlayerAttack reference is not assigned.", this);
            }

            targetFilter = new ContactFilter2D();
            targetFilter.useLayerMask = true;
            targetFilter.SetLayerMask(targetLayers);
            targetFilter.useTriggers = true;
        }

        private void Update()
        {
            Resolve();
        }

        /// <summary>
        /// One hit-resolution cycle. Public so tests can drive it deterministically (with
        /// Physics2D.SyncTransforms). Clears the per-window dedupe set when a new window
        /// opens, then — only while the window is active — overlaps the frontal box and
        /// applies damage once per resolved target.
        /// </summary>
        public void Resolve()
        {
            bool active = attack != null && attack.IsHitWindowActive;

            if (active && !wasWindowActive)
            {
                damagedThisWindow.Clear();
                resolveCache.Clear();
            }

            wasWindowActive = active;

            if (!active)
            {
                return; // no physics query outside an active window
            }

            Vector2Int facingInt = attack.CapturedFacing;
            if (facingInt == Vector2Int.zero)
            {
                return;
            }

            Vector2 origin = transform.position;
            Vector2 facing = new Vector2(facingInt.x, facingInt.y);
            Vector2 center = origin + facing * (attackRange * 0.5f);

            // The captured facing is cardinal, so the box is axis-aligned; its long axis
            // follows the attack direction (reach), the short axis is the lateral width.
            Vector2 size = (facingInt.x != 0)
                ? new Vector2(attackRange, attackWidth)
                : new Vector2(attackWidth, attackRange);

            int count = Physics2D.OverlapBox(center, size, 0f, targetFilter, overlapBuffer);

            if (count == overlapBuffer.Length && !hasLoggedBufferFull)
            {
                Debug.LogWarning("PlayerAttackHitResolver: overlap buffer full; some targets may be ignored.", this);
                hasLoggedBufferFull = true;
            }

            for (int i = 0; i < count; i++)
            {
                IDamageable target = Resolve(overlapBuffer[i]);
                if (target == null || damagedThisWindow.Contains(target))
                {
                    continue;
                }

                target.ApplyDamage(new DamageInfo(damageAmount, DamageSourceKind.Player));
                damagedThisWindow.Add(target);
            }
        }

        // Resolves a collider to its IDamageable (self or an ancestor), caching the
        // result — including a null "not damageable" — to avoid repeated GetComponent.
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
            // Force the next enabled window to be treated as fresh.
            wasWindowActive = false;
            damagedThisWindow.Clear();
            resolveCache.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (attack == null)
            {
                return;
            }

            Vector2Int facingInt = attack.CapturedFacing;
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

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
