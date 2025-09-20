using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody2D rb;
    public PlayerHealth health;
    public TrailRenderer dashTrail;

    [Header("Dash")]
    public float dashDistance = 4f;
    public float dashDuration = 0.12f;
    public float dashCooldown = 0.35f;
    public float iFrameDuration = 0.18f;
    public AnimationCurve dashEasing = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Collision")]
    [Tooltip("Seleziona TUTTI i layer che devono bloccare il dash: Walls, Obstacles, ecc.")]
    public LayerMask collisionMask;       // ⬅️ UNICO mask (pareti + ostacoli)
    public float skin = 0.05f;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.Space;
    public bool aimWithMouse = false;

    bool isDashing;
    float lastDashTime;
    Vector2 lastNonZeroMove = Vector2.right;

    void Reset() { rb = GetComponent<Rigidbody2D>(); }

    void Update()
    {
        Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (move.sqrMagnitude > 0.0001f) lastNonZeroMove = move.normalized;
        if (Input.GetKeyDown(dashKey)) TryDash(move);
    }

    void TryDash(Vector2 moveInput)
    {
        if (isDashing) return;
        if (Time.time < lastDashTime + dashCooldown) return;

        Vector2 dir;
        if (aimWithMouse)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dir = ((Vector2)(mouseWorld - transform.position)).normalized;
        }
        else
        {
            dir = (moveInput.sqrMagnitude > 0.0001f ? moveInput.normalized : lastNonZeroMove);
        }

        StartCoroutine(DashRoutine(dir));
    }

    IEnumerator DashRoutine(Vector2 dir)
    {
        isDashing = true;
        lastDashTime = Time.time;

        Vector2 start = rb.position;

        // ---- NEW: usa il cast della forma del rigidbody (niente tunneling, rispetta la dimensione del player)
        float dist = dashDistance;
        var filter = new ContactFilter2D
        {
            useTriggers = false
        };
        filter.SetLayerMask(collisionMask);

        // buffer riutilizzabile piccolo va bene
        RaycastHit2D[] hits = new RaycastHit2D[8];
        int count = rb.Cast(dir, filter, hits, dashDistance + skin);
        if (count > 0)
        {
            float min = Mathf.Infinity;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].distance < min) min = hits[i].distance;
            }
            dist = Mathf.Max(0f, min - skin);
        }
        Vector2 end = start + dir * dist;
        // ---- end NEW

        // i-frames ON
        if (health) health.SetInvincible(true);
        if (dashTrail) dashTrail.emitting = true;

        float t = 0f;
        try
        {
            while (t < dashDuration)
            {
                t += Time.deltaTime;
                float p = dashEasing.Evaluate(Mathf.Clamp01(t / dashDuration));
                rb.MovePosition(Vector2.Lerp(start, end, p));
                yield return null;
            }
            rb.MovePosition(end);

            float residuo = iFrameDuration - dashDuration;
            if (residuo > 0f) yield return new WaitForSeconds(residuo);
        }
        finally
        {
            if (dashTrail) dashTrail.emitting = false;
            if (health) health.SetInvincible(false);
            isDashing = false;
        }
    }

    public bool IsDashing() => isDashing;
}
