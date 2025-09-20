using UnityEngine;

public class TestShooter : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;

    [SerializeField] private PlayerColorState colorState; // trascina qui il Player (o lo trova da solo)

    void Awake()
    {
        if (colorState == null)
            colorState = GetComponentInParent<PlayerColorState>(); // <-- prende quello sul Player
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            // passa il colore al bullet + colora visivamente il bullet
            var bulletScript = bullet.GetComponent<ColorBullet>();
            if (bulletScript != null && colorState != null)
            {
                bulletScript.effectColor = colorState.SelectedColor;

                var sr = bullet.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = bulletScript.effectColor; // così NON resta fucsia
            }

            var rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = (Vector2)firePoint.right * bulletSpeed;
        }
    }
}
