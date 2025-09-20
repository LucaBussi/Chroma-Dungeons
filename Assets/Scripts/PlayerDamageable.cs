using UnityEngine;

public class PlayerDamageable : MonoBehaviour, IDamageable
{
    [SerializeField] private PlayerHealth health;
    public bool IsInvincible { get; private set; }

    private void Reset()
    {
        if (!health) health = GetComponent<PlayerHealth>();
    }

    public void SetInvincible(bool value)
    {
        IsInvincible = value;
    }

    // IDamageable
    public void TakeDamage(float amount, Vector2 hitFrom)
    {
        if (IsInvincible) return;
        if (health) health.TakeDamage(amount, hitFrom);
    }

    public bool IsAlive => health && health.IsAlive;

}
