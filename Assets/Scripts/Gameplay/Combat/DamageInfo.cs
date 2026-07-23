namespace Synora.Gameplay.Combat
{
    /// <summary>
    /// Immutable payload of a single damage application (M5 SPEC §3). Carries the
    /// amount and a small, decoupled <see cref="DamageSourceKind"/>. No critical hit,
    /// element, armor, knockback, faction, hit position, direction, weapon id,
    /// timestamp or side effect (HitDirection is deferred until actually needed).
    ///
    /// Policy: a negative amount is normalized to zero at construction — damage never
    /// heals. Zero is a valid amount and is a no-op when applied. The default value of
    /// the struct has amount 0 and source <see cref="DamageSourceKind.Unknown"/>.
    /// </summary>
    public readonly struct DamageInfo
    {
        public float Amount { get; }
        public DamageSourceKind SourceKind { get; }

        public DamageInfo(float amount, DamageSourceKind sourceKind = DamageSourceKind.Unknown)
        {
            Amount = amount > 0f ? amount : 0f;
            SourceKind = sourceKind;
        }
    }
}
