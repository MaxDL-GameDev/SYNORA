using System;
using UnityEngine;

namespace Synora.Gameplay.Combat
{
    /// <summary>
    /// Per-instance health. Single responsibility: store max/current health, apply
    /// damage, clamp to the valid range, and expose its state read-only. Reusable by
    /// the player and by the Altered Verak; the MEANING of reaching zero (temporary
    /// defeat vs Subdued) belongs to the owner, not to Health.
    ///
    /// Deliberately excluded in M5 Fase 3: invulnerability, cooldown, regeneration,
    /// healing, UI, GameObject deactivation, animation, state transitions, defeat,
    /// restoration, persistence, per-window damage dedupe.
    ///
    /// Zero signal: a local C# instance event <see cref="Depleted"/> (never a global
    /// event, static event, singleton or SO channel). It is NOT interpreted as death;
    /// Health does not run Subdued or temporary defeat.
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 3f;

        private float currentHealth;

        /// <summary>Raised exactly once when health transitions from above zero to zero.</summary>
        public event Action Depleted;

        /// <summary>Normalized, always-valid maximum (a non-positive serialized value becomes 1).</summary>
        public float Max => NormalizeMaxHealth(maxHealth);

        public float Current => currentHealth;

        public bool IsZero => currentHealth <= 0f;

        /// <summary>Current fraction in [0,1]. Zero when max is somehow non-positive.</summary>
        public float Normalized
        {
            get
            {
                float max = Max;
                return max > 0f ? currentHealth / max : 0f;
            }
        }

        private void Awake()
        {
            ResetHealth();
        }

        /// <summary>
        /// Normalizes the maximum and refills to full. This is both the deterministic
        /// initialization path (called from Awake) and the explicit encounter reset.
        /// Idempotent. It does NOT raise <see cref="Depleted"/>, and it re-arms the
        /// signal for a future transition to zero. Health is not restored automatically
        /// on enable/disable (there is no OnEnable/OnDisable).
        /// </summary>
        public void ResetHealth()
        {
            maxHealth = NormalizeMaxHealth(maxHealth);
            currentHealth = maxHealth;
        }

        public void ApplyDamage(in DamageInfo damage)
        {
            bool wasAboveZero = currentHealth > 0f;
            currentHealth = ComputeDamaged(currentHealth, damage.Amount, Max);

            // Emit only on the >0 -> 0 transition: never on zero/negative (no-op) damage,
            // and never again while already at zero.
            if (wasAboveZero && currentHealth <= 0f)
            {
                Depleted?.Invoke();
            }
        }

        // ── Pure logic (no MonoBehaviour/physics/Animator needed; unit-testable) ──

        /// <summary>A non-positive maximum is normalized to 1 (deterministic).</summary>
        public static float NormalizeMaxHealth(float max) => max > 0f ? max : 1f;

        /// <summary>
        /// Computes the resulting health after damage: a negative amount is treated as
        /// zero (never heals) and the result is clamped to [0, max].
        /// </summary>
        public static float ComputeDamaged(float current, float amount, float max)
        {
            float applied = amount > 0f ? amount : 0f;
            float result = current - applied;
            if (result < 0f)
            {
                result = 0f;
            }
            else if (result > max)
            {
                result = max;
            }

            return result;
        }
    }
}
