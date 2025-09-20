using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 5f;

    Rigidbody2D rb;
    Vector2 input;
    bool movementLocked;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        // Legge gli assi "cardinali": A/D su X, W/S su Y
        float x = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
        float y = Input.GetAxisRaw("Vertical");   // -1, 0, 1
        input = new Vector2(x, y);

        // Normalizza per velocità uniforme in diagonale (facoltativo ma consigliato)
        if (input.sqrMagnitude > 1f) input = input.normalized;
    }

    void FixedUpdate()
    {
        if (movementLocked) { rb.linearVelocity = Vector2.zero; return; }
        rb.linearVelocity = input * moveSpeed; // mondo cartesiano puro
    }

    public void SetMovementLocked(bool locked) => movementLocked = locked;
}
