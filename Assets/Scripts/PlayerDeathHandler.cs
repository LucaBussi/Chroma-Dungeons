using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Cosa disattivare alla morte")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D[] collidersToDisable;

    [Header("FX opzionali")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private GameObject deathVfxPrefab;

    private void Reset()
    {
        playerHealth = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
        collidersToDisable = GetComponentsInChildren<Collider2D>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath.AddListener(HandleDeath);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath.RemoveListener(HandleDeath);
    }

    private void HandleDeath()
    {
        // disabilita controlli
        foreach (var s in scriptsToDisable)
            if (s != null) s.enabled = false;

        // ferma rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
        }

        // disabilita colliders
        foreach (var c in collidersToDisable)
            if (c != null) c.enabled = false;

        // anim/FX
        if (animator != null && !string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);
        if (deathVfxPrefab != null)
            Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);

        // apri la pagina Game Over (congela tutto e mostra i bottoni)
        GameOver.Open();
    }
}
