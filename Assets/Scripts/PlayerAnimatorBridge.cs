using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorBridge : MonoBehaviour
{
    private Animator anim;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerDash playerDash;

    private const string SpeedParam     = "Speed";
    private const string IsDashingParam = "IsDashing";

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!playerDash) playerDash = GetComponent<PlayerDash>();
    }

    void Update()
    {
        // Legge sia la velocità fisica che l'input (così funziona anche se muovi il Player via transform)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float inputMag = new Vector2(h, v).magnitude;     // 0..1 (diagonale ~1.41)
        float rbMag    = rb ? rb.linearVelocity.magnitude : 0f; // se usi la fisica

        float speed = Mathf.Max(inputMag, rbMag);
        anim.SetFloat(SpeedParam, speed);

        bool isDashing = (playerDash != null) && playerDash.IsDashing();
        anim.SetBool(IsDashingParam, isDashing);
    }
}
