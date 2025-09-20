using UnityEngine;

public class WeaponAim : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Transform player;           // Player
    [SerializeField] SpriteRenderer body;        // SpriteBody
    [SerializeField] SpriteRenderer gun;         // Gun

    void Reset() {
        cam = Camera.main;
    }

    void Update()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 dir = (mouseWorld - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Ruota SOLO l’arma (il pivot)
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Flip: corpo e fucile quando guardi a sinistra
        bool lookingLeft = mouseWorld.x < player.position.x;
        if (body) body.flipX = lookingLeft;
        if (gun)  gun.flipY = lookingLeft;

        // Davanti quando mira in giù, dietro quando mira in su (facoltativo)
        //if (gun) gun.sortingOrder = (angle > -90f && angle < 90f) ? 2 : -1;
    }
}
