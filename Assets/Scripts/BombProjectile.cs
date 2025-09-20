using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BombProjectile : MonoBehaviour, IThrowable
{
    [Header("Movimento")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float maxRange = 8f;

    [Header("Rotazione")]
    [SerializeField] private float rotationSpeed = 360f; // gradi al secondo

    [Header("Esplosione")]
    [SerializeField] private float explosionRadius = 2.2f;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask collideMask;
    [SerializeField] private GameObject explosionVfxPrefab;

    private Vector2 _dir;
    private Vector2 _startPos;
    private Rigidbody2D _rb;
    private Transform _owner;
    private bool _launched;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
    }

    public void Launch(Vector2 dir, Transform owner)
    {
        _owner = owner;
        _dir = dir.normalized;
        _startPos = transform.position;
        _launched = true;
        _rb.linearVelocity = _dir * speed;
    }

    void Update()
    {
        if (!_launched) return;

        // fai ruotare lo sprite costantemente
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (Vector2.Distance(_startPos, transform.position) >= maxRange)
            Explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_launched) return;
        if (IsOwner(other.transform)) return;

        if (IsInMask(other.gameObject.layer, collideMask))
            Explode();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_launched) return;
        if (IsOwner(collision.transform)) return;

        if (IsInMask(collision.gameObject.layer, collideMask))
            Explode();
    }

    private void Explode()
    {
        _launched = false;
        _rb.linearVelocity = Vector2.zero;

        if (explosionVfxPrefab)
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);

        var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyMask);
        foreach (var h in hits)
        {
            var enemy = h.GetComponentInParent<DummyEnemy>() ?? h.GetComponent<DummyEnemy>();
            if (enemy != null) enemy.ForceColor(Color.black);
        }

        Destroy(gameObject);
    }

    private static bool IsInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    private bool IsOwner(Transform t) => _owner && (t == _owner || t.IsChildOf(_owner));

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
