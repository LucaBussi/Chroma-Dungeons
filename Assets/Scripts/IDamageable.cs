using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount, Vector2 hitFrom);
    bool IsAlive { get; }
    
}
