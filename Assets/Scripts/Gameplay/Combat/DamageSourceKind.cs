namespace Synora.Gameplay.Combat
{
    /// <summary>
    /// Minimal, decoupled origin of a damage application. Deliberately NOT a reference
    /// to a GameObject/Component/Collider2D — just a small deterministic category so a
    /// receiver could distinguish who dealt the damage without coupling to the scene.
    /// <see cref="Unknown"/> is the safe default.
    /// </summary>
    public enum DamageSourceKind
    {
        Unknown,
        Player,
        Creature
    }
}
