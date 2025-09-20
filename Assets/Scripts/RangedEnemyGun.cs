using UnityEngine;
using System.Collections;

public class RangedEnemyGun : MonoBehaviour
{
[SerializeField] private Transform firePoint;
    [SerializeField] private EnemyBullet bulletPrefab;
    [SerializeField] private float windup = 0.15f;

    [Header("Telegraph (solo se vuoi)")]
    [SerializeField] private bool useTelegraphOutline = false; // ⬅️ NEW (default: OFF)
    [SerializeField] private Color telegraphColor = new Color(1f, 0.4f, 0.2f, 1f);
    [SerializeField] private float telegraphThickness = 0.8f;

    private BossVFX vfx;
    private bool _isShooting;

    private void Awake()
    {
        vfx = GetComponentInParent<BossVFX>();
        if (firePoint == null)
        {
            var t = transform.Find("FirePoint");
            if (t != null) firePoint = t;
        }
    }

    public void FireAt(Vector2 worldTarget) => FireAt(worldTarget, null);

// ⬇️ nuovo overload con callback
    public void FireAt(Vector2 worldTarget, System.Action onFired)
    {
        if (_isShooting || !CanShoot()) return;
        StartCoroutine(ShootRoutine(worldTarget, onFired));
    }

    private IEnumerator ShootRoutine(Vector2 target, System.Action onFired)
    {
        _isShooting = true;

        // (telegraph opzionale già gestito)
        if (windup > 0f) yield return new WaitForSeconds(windup);

        SpawnBullet(target);
        onFired?.Invoke();             // ⬅️ notifica "colpo effettivamente partito"

        _isShooting = false;
    }

    private void SpawnBullet(Vector2 target)
    {
        if (!CanShoot()) return;

        var b = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Vector2 dir = (target - (Vector2)firePoint.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right; // fallback
        b.Init(dir.normalized);
    }

    private bool CanShoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[RangedEnemyGun] bulletPrefab non assegnato", this);
            return false;
        }
        if (firePoint == null)
        {
            Debug.LogWarning("[RangedEnemyGun] firePoint non assegnato/trovato", this);
            return false;
        }
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;
        Gizmos.DrawWireSphere(firePoint.position, 0.07f);
    }
#endif
}
