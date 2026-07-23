namespace Synora.Gameplay.Combat
{
    /// <summary>
    /// Minimal contract for anything that can receive damage. It only RECEIVES:
    /// reading or querying health belongs to <see cref="Health"/>, not to this
    /// generic receiver. No Heal/Kill/Revive/Reset/IsDead/GetHealth here.
    /// </summary>
    public interface IDamageable
    {
        void ApplyDamage(in DamageInfo damage);
    }
}
