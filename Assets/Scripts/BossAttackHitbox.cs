using System.Collections.Generic;
using UnityEngine;

public class BossAttackHitboxDamage2D : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float damage = 20f;
    [SerializeField] private float tick = 0.15f; // rate-limit durante la finestra

    private readonly Dictionary<Collider2D, float> _next = new();

    private void OnEnable()
    {
        _next.Clear(); // nuova finestra di attacco
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!enabled) return; // se la finestra è chiusa, niente
        if (!other.CompareTag(playerTag)) return;
        if (!other.TryGetComponent<IDamageable>(out var dmg) || !dmg.IsAlive) return;

        float t = Time.time;
        if (!_next.TryGetValue(other, out var next) || t >= next)
        {
            Vector2 from = (other.transform.position - transform.position).normalized;
            dmg.TakeDamage(damage, from);
            _next[other] = t + tick;
            // Debug.Log("[Boss] Hit!");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_next.ContainsKey(other)) _next.Remove(other);
    }
}
