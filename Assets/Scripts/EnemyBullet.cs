using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float life = 4f;
    [SerializeField] private float damage = 10f;

    private Rigidbody2D rb;
    private Vector2 dir;

    public void Init(Vector2 direction)
    {
        dir = direction.normalized;
        if (!rb) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = dir * speed;
        Invoke(nameof(Die), life);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var dmg = other.GetComponent<IDamageable>();
        if (dmg != null && dmg.IsAlive)
        {
            dmg.TakeDamage(damage, rb ? rb.position : (Vector2)transform.position);
            Die();
            return;
        }

        // distruggilo quando colpisce pareti/ostacoli (assicurati che la Layer Matrix lo consenta)
        if (other.gameObject.layer != gameObject.layer) Die();
    }

    private void Die() => Destroy(gameObject);
}
