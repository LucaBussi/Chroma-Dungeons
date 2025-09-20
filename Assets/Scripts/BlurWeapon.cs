using UnityEngine;

public class BlurWeapon : MonoBehaviour
{
    public BlurBullet bulletPrefab;
    public Transform firePoint;

    public float bulletSpeed = 12f;
    public float fireCooldown = 0.15f;
    public float blurPerHit = 0.2f;

    private float cd;

    void Update()
    {
        cd -= Time.deltaTime;

        // stesso tasto della ColorGun, ma solo l'arma attiva riceve input
        if (Input.GetButton("Fire1") && cd <= 0f)
        {
            Shoot();
            cd = fireCooldown;
        }
    }

    void Shoot()
    {
        var b = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        b.Init(bulletSpeed, blurPerHit);
    }
}
