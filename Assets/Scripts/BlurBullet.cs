using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BlurBullet : MonoBehaviour
{
    public float lifeTime = 3f;

    private Rigidbody2D rb;
    private float blurAmount;

    public void Init(float speed, float blurPerHit)
    {
        blurAmount = blurPerHit;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = transform.right * speed; // firePoint orientato su +X
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out BossController boss))
        {
            boss.ApplyBlur(blurAmount); // avrà effetto solo se il boss è Stunned
        }
        Destroy(gameObject);
    }
}
