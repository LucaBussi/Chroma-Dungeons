using System.Collections;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerColorState colorState; // se vuoto, lo trova
    [SerializeField] private Transform hitOrigin;         // empty davanti al player
    [SerializeField] private Animator animator;           // Animator del Player

    [Header("Hit Settings")]
    [SerializeField] private float range = 1.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField, Range(0f, 1f)] private float removeAmount = 0.2f;

    [Header("Timing / Anim")]
    [SerializeField] private float attackCooldown = 0.35f;   // blocca lo spam
    [SerializeField] private string animatorTrigger = "Melee";// nome del Trigger nell’Animator
    [SerializeField] private bool useAnimationEvent = true;   // true = colpisce via Animation Event
    [SerializeField] private float fallbackHitDelay = 0.12f;  // usato se non imposti l’event

    [Header("FX (opzionali)")]
    [SerializeField] private SpriteRenderer slashSprite;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem particles;

    private bool canAttack = true;
    private Color currentColor = Color.white;

    // buffer per evitare allocazioni
    private readonly Collider2D[] _hits = new Collider2D[16];

    [SerializeField] private Transform aimPivot; // il transform che ruota con la mira (es. lo stesso usato per le pistole)
    [SerializeField] private float forwardOffset = 0.8f; // quanto davanti al player deve avvenire il colpo


    void Awake()
    {
        if (!colorState) colorState = GetComponentInParent<PlayerColorState>();
        if (!hitOrigin)
        {
            // crea al volo un empty figlio del pivot, posizionato in avanti
            if (aimPivot)
            {
                var go = new GameObject("HitOrigin");
                hitOrigin = go.transform;
                hitOrigin.SetParent(aimPivot, false);
                hitOrigin.localPosition = Vector3.right * forwardOffset; // “avanti” = asse X del pivot
            }
            else
            {
                hitOrigin = transform; // fallback
            }
        }
        if (!animator) animator = GetComponentInParent<Animator>();
    }

    void OnEnable()
    {
        if (colorState)
        {
            currentColor = colorState.SelectedColor;
            colorState.OnColorChanged += HandleColorChanged;
        }
        ApplyFxColor(currentColor);
    }

    void OnDisable()
    {
        if (colorState) colorState.OnColorChanged -= HandleColorChanged;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1) && canAttack)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        canAttack = false;

        // 1) Lancia l’animazione
        if (animator && !string.IsNullOrEmpty(animatorTrigger))
            animator.SetTrigger(animatorTrigger);

        // 2) Se NON usi l’Animation Event, colpisci dopo un piccolo delay
        if (!useAnimationEvent)
        {
            yield return new WaitForSeconds(fallbackHitDelay);
            DoMeleeHitInternal();
        }

        // 3) Attendi il cooldown (l’event può essere arrivato prima in mezzo)
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    /// <summary>
    /// CHIAMARE da Animation Event nel frame d’impatto (nome consigliato: AE_DoMeleeHit).
    /// </summary>
    public void AE_DoMeleeHit()
    {
        if (!useAnimationEvent) return; // ignora se stai usando il fallback
        DoMeleeHitInternal();
    }

    private void DoMeleeHitInternal()
    {
        var origin = hitOrigin ? hitOrigin.position : transform.position;
        int count = Physics2D.OverlapCircleNonAlloc(origin, range, _hits, enemyLayer);
        var attackColor = colorState ? colorState.SelectedColor : currentColor;

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (!col) continue;

            // cerca DummyEnemy su collider, parent o figli
            DummyEnemy enemy =
                col.GetComponent<DummyEnemy>() ??
                col.GetComponentInParent<DummyEnemy>() ??
                col.GetComponentInChildren<DummyEnemy>();

            if (enemy != null)
            {
                enemy.RemoveColorComponent(attackColor, removeAmount);
            }
        }

        // Piccolo “ping” FX opzionale
        if (particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.startColor = attackColor;
            particles.Play();
        }
    }

    private void HandleColorChanged(Color c)
    {
        currentColor = c;
        ApplyFxColor(c);
    }

    private void ApplyFxColor(Color c)
    {
        if (slashSprite) slashSprite.color = c;

        if (trail)
        {
            trail.startColor = c;
            trail.endColor = new Color(c.r, c.g, c.b, 0f);
        }

        if (particles)
        {
            var main = particles.main;
            main.startColor = c;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Color c = Color.white;
        var pcs = colorState ? colorState : GetComponentInParent<PlayerColorState>();
        if (pcs) c = pcs.SelectedColor;

        Gizmos.color = c;
        var origin = hitOrigin ? hitOrigin.position : transform.position;
        Gizmos.DrawWireSphere(origin, range);
    }
#endif
}
