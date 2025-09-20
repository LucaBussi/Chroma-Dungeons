using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SkeletonBossAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.6f;
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float stopDistance = 1.75f;

    private BossController boss;
    private bool pausedByStun;
    private Coroutine currentAttackCo;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private float windupTime = 0.5f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float aoeRadius = 1.1f;
    [SerializeField] private float aoeOffset = 1.0f;
    [SerializeField] private Color windupOutlineColor = new Color(1f, 0.35f, 0.1f, 1f);
    [SerializeField] private float windupOutlineThickness = 1.4f;

    [Header("Optional Telegraph")]
    [SerializeField] private AoeIndicator aoeIndicator;

    // Tilemap dell’arena (onda colore)
    [Header("Arena Floor")]
    [SerializeField] private ArenaFloorWaveTint arenaFloorWave;

    // --- Collisione/movimento kinematic ---
    [Header("Collision")]
    [Tooltip("Layer contro cui il boss deve collidere (es: Obstacle | Player).")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Range(0f, 0.2f)] private float skin = 0.03f;

    private Rigidbody2D rb;
    private BossVFX vfx;
    private Vector2 lastDir = Vector2.right;
    private bool attacking;
    private float cdTimer;

    // buffer/filtri per cast
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
    private ContactFilter2D _contactFilter;
    private Vector2 _desiredVel; // impostata in Update, usata in FixedUpdate

    // === Animator ===
    [Header("Animator")]
    [SerializeField] private Animator animator;
    static readonly int HashSpeed = Animator.StringToHash("Speed");
    static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    static readonly int HashAttack = Animator.StringToHash("Attack");
    static readonly int HashStunned = Animator.StringToHash("Stunned");
    static readonly int HashDead = Animator.StringToHash("Dead");

    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyToFlip;
    [SerializeField] private bool useScaleFlip = false;

    // Dati cache per gli Animation Events
    private Vector2 cachedDir;
    private Vector2 cachedCenter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // --- Setup KINEMATIC "a prova di push" ---
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true; // importante per i contatti kinematic
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        // filtro collisione
        _contactFilter.useLayerMask = true;
        _contactFilter.layerMask = obstacleMask;
        _contactFilter.useTriggers = false;

        vfx = GetComponent<BossVFX>();
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        boss = GetComponent<BossController>();

        if (!bodyToFlip) bodyToFlip = GetComponentInChildren<SpriteRenderer>();
        if (!animator) animator = GetComponent<Animator>();

        if (!arenaFloorWave) arenaFloorWave = FindObjectOfType<ArenaFloorWaveTint>();
    }

    private void Update()
    {
        // reset desiderata di default
        _desiredVel = Vector2.zero;

        // Morte
        if (boss != null && boss.State == BossState.Defeated)
        {
            if (animator) animator.SetBool(HashDead, true);
            UpdateLocomotionParams(0f);
            return;
        }

        // Stun
        if (boss != null && boss.State == BossState.Stunned)
        {
            if (!pausedByStun)
            {
                pausedByStun = true;
                if (currentAttackCo != null) StopCoroutine(currentAttackCo);
                currentAttackCo = null;

                if (aoeIndicator != null) aoeIndicator.Hide();
                attacking = false;
            }

            if (animator) animator.SetBool(HashStunned, true);
            UpdateLocomotionParams(0f);
            return;
        }
        else
        {
            pausedByStun = false;
            if (animator) animator.SetBool(HashStunned, false);
        }

        if (player == null)
        {
            UpdateLocomotionParams(0f);
            return;
        }

        // Cooldown
        if (cdTimer > 0f) cdTimer -= Time.deltaTime;

        if (attacking)
        {
            UpdateLocomotionParams(0f);
            return;
        }

        // Distanza
        Vector2 pos = transform.position;
        Vector2 toPlayer = (Vector2)player.position - pos;
        float dist = toPlayer.magnitude;

        if (dist > chaseRange)
        {
            UpdateLocomotionParams(0f);
            UpdateFacing(Vector2.zero);
            return;
        }

        // Movimento
        if (dist > stopDistance)
        {
            Vector2 dir = toPlayer.normalized;
            lastDir = dir;
            _desiredVel = dir * moveSpeed; // << niente velocity diretta
            UpdateLocomotionParams(_desiredVel.magnitude);
            UpdateFacing(dir);
        }
        else
        {
            UpdateLocomotionParams(0f);
            UpdateFacing(Vector2.zero);

            if (cdTimer <= 0f)
                StartAttack();
        }
    }

    private void FixedUpdate()
    {
        // esegui il movimento kinematic con cast
        MoveWithCollisions(_desiredVel * Time.fixedDeltaTime);
    }

    private void MoveWithCollisions(Vector2 delta)
    {
        float dist = delta.magnitude;
        if (dist <= 1e-6f)
        {
            rb.MovePosition(rb.position); // mantiene contatti kinematic aggiornati
            return;
        }

        Vector2 dir = delta / Mathf.Max(dist, 1e-6f);

        // Cast del corpo per vedere quanto possiamo muoverci
        int count = rb.Cast(dir, _contactFilter, _hits, dist + skin);
        if (count == 0)
        {
            rb.MovePosition(rb.position + delta);
            return;
        }

        float allowed = dist;
        Vector2 hitNormal = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            float d = Mathf.Max(0f, _hits[i].distance - skin);
            if (d < allowed) allowed = d;
            hitNormal += _hits[i].normal;
        }

        // primo tratto fino all'ostacolo
        Vector2 move = dir * allowed;

        // componente rimanente -> slide lungo la superficie
        Vector2 remaining = delta - move;
        if (hitNormal != Vector2.zero)
        {
            Vector2 n = hitNormal.normalized;
            Vector2 slide = remaining - Vector2.Dot(remaining, n) * n;
            move += slide;
        }

        rb.MovePosition(rb.position + move);
    }

    private void UpdateLocomotionParams(float speed)
    {
        if (!animator) return;
        animator.SetFloat(HashSpeed, speed);
        animator.SetBool(HashIsMoving, speed > 0.01f);
    }

    private void StartAttack()
    {
        attacking = true;
        _desiredVel = Vector2.zero; // ferma il movimento durante l'attacco

        Vector2 dir = (player != null) ? ((Vector2)player.position - (Vector2)transform.position).normalized
                                       : (lastDir == Vector2.zero ? Vector2.right : lastDir);
        lastDir = dir;

        cachedDir = dir;
        cachedCenter = (Vector2)transform.position + dir * aoeOffset;
        UpdateFacing(dir);

        if (animator) animator.SetTrigger(HashAttack);
    }

    // ========= ANIMATION EVENTS =========

    public void AE_Attack_WindupStart()
    {
        if (aoeIndicator != null) aoeIndicator.Show(cachedCenter, aoeRadius, windupTime);
        if (vfx != null) vfx.ShowOutline(windupOutlineColor, windupOutlineThickness);
    }

    public void AE_Attack_Hit()
    {
        DoAoeDamage(cachedCenter, aoeRadius);

        // Onda di colorazione dall’impatto
        if (arenaFloorWave != null)
            arenaFloorWave.SlamWave(cachedCenter);

        if (vfx != null) vfx.HideOutline();
        if (aoeIndicator != null) aoeIndicator.Hide();
    }

    public void AE_Attack_End()
    {
        cdTimer = attackCooldown;
        attacking = false;
    }

    // (Opzionale) Animation Event alla fine della morte per ripulire la pavimentazione
    public void AE_Death_Cleanup()
    {
        if (arenaFloorWave != null)
            arenaFloorWave.ResetAllTo(Color.white);
    }

    private void DoAoeDamage(Vector2 center, float radius)
    {
        var hits = Physics2D.OverlapCircleAll(center, radius, playerLayerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var dmg = hits[i].GetComponent<IDamageable>() ?? hits[i].GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive)
                dmg.TakeDamage(damage, (Vector2)transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.5f);
        Vector2 dir = (lastDir == Vector2.zero ? Vector2.right : lastDir).normalized;
        Vector2 center = (Vector2)transform.position + dir * aoeOffset;
        Gizmos.DrawWireSphere(center, aoeRadius);
    }

    private void UpdateFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = lastDir;
        bool faceLeft = dir.x < 0f;

        if (useScaleFlip)
        {
            Transform t = bodyToFlip ? bodyToFlip.transform : transform;
            Vector3 s = t.localScale;
            float sign = faceLeft ? -1f : 1f;
            s.x = Mathf.Abs(s.x) * sign;
            t.localScale = s;
        }
        else
        {
            if (bodyToFlip) bodyToFlip.flipX = faceLeft;
        }
    }
}
