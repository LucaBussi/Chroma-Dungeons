using UnityEngine;

public class SpriteFlipper : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer bodySR;   // il renderer del corpo
    [SerializeField] private Rigidbody2D rb;          // opzionale
    [SerializeField] private PlayerDash dash;         // opzionale (per aim col mouse)

    [Header("Preferenze")]
    [SerializeField] private bool faceWithMouseWhenAiming = true;

    void Awake()
    {
        if (!bodySR) bodySR = GetComponentInChildren<SpriteRenderer>(true);
        if (!rb)     rb     = GetComponent<Rigidbody2D>();
        if (!dash)   dash   = GetComponent<PlayerDash>();
    }

    void LateUpdate()
    {
        if (!bodySR) return;

        float x = 0f;

        // 1) Se stai mirando col mouse, guardo verso il mouse
        if (dash && dash.aimWithMouse && faceWithMouseWhenAiming && Camera.main)
            x = Camera.main.ScreenToWorldPoint(Input.mousePosition).x - transform.position.x;

        // 2) Altrimenti oriento in base all’input orizzontale
        if (Mathf.Abs(x) < 0.001f)
        {
            float h = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(h) > 0.01f) x = h;
        }

        // 3) Fallback: velocità fisica
        if (Mathf.Abs(x) < 0.001f && rb && Mathf.Abs(rb.linearVelocity.x) > 0.01f)
            x = rb.linearVelocity.x;

        if (Mathf.Abs(x) < 0.001f) return; // nessun cambiamento

        bodySR.flipX = (x < 0f);
    }
}
