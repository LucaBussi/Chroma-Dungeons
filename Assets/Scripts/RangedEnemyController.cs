using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class RangedEnemyController : MonoBehaviour
{
    public enum State { Chase, Backpedal, ReloadStunned }

    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private WeaponPivot weaponPivot;
    [SerializeField] private RangedEnemyGun gun;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.6f;
    [SerializeField] private float backpedalSpeed = 1.8f;
    [SerializeField] private float jitterStrength = 0.6f;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float retreatTiles = 5f;

    [Header("Combat")]
    [SerializeField] private int magazineSize = 3;
    [SerializeField] private float fireCooldown = 0.55f;
    [SerializeField] private float reloadDuration = 1.0f;

    [Header("Timing")]
    [SerializeField] private float reloadStartDelayAfterShot = 0.06f;

    [Header("Safety")]
    [SerializeField] private float minShootDistance = 1.5f;
    [SerializeField] private float aimLead = 0f;

    [Header("Animation")]
    [SerializeField] private Animator animator;          // param: IsMoving
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private float moveEpsilon = 0.05f;

    [Header("Sprite Flip")]
    [SerializeField] private SpriteRenderer bodyToFlip;  // ⬅️ sprite da flippare
    [SerializeField] private float flipThreshold = 0.02f; // soglia minima su X per cambiare verso
    [SerializeField] private bool invertFlip = false;      // se il tuo sprite è “al contrario”

    private Rigidbody2D rb;
    private BossVFX vfx;
    private State state;
    private float nextFireTime;
    private int bulletsLeft;
    private bool isReloading;

    private Vector2 jitterSeed;
    private int lastFacing = 1; // 1 = destra, -1 = sinistra

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        if (!bodyToFlip) bodyToFlip = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!bodyToFlip) bodyToFlip = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        vfx = GetComponent<BossVFX>();
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!weaponPivot) weaponPivot = GetComponentInChildren<WeaponPivot>();
        if (!gun) gun = GetComponentInChildren<RangedEnemyGun>();

        bulletsLeft = magazineSize;
        state = State.Chase;
        jitterSeed = new Vector2(Random.value * 100f, Random.value * 100f);

        SetIsMoving(false);
        ApplyFlip(lastFacing); // default a destra
    }

    void Update()
    {
        if (!player)
        {
            SetIsMoving(false);
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        float retreatDist = retreatTiles * tileSize;

        State desired = dist < retreatDist ? State.Backpedal : State.Chase;
        if (!isReloading) state = desired;

        if (!isReloading && Time.time >= nextFireTime && bulletsLeft > 0 && dist > minShootDistance)
            ShootTick();
    }

    void FixedUpdate()
    {
        if (!player)
        {
            rb.linearVelocity = Vector2.zero;
            SetIsMoving(false);
            return;
        }

        Vector2 vel = Vector2.zero;

        switch (state)
        {
            case State.Chase:
                vel = DirTo(player.position) * moveSpeed + Jitter();
                break;
            case State.Backpedal:
                vel = -DirTo(player.position) * backpedalSpeed + Jitter() * 0.6f;
                break;
            case State.ReloadStunned:
                vel = Vector2.zero;
                break;
        }

        rb.linearVelocity = vel;

        // Animator
        bool moving = (state != State.ReloadStunned) && (vel.sqrMagnitude >= moveEpsilon * moveEpsilon);
        SetIsMoving(moving);

        // FLIP in base alla camminata
        UpdateFlip(vel.x);
    }

    private void UpdateFlip(float vx)
    {
        // Cambia facing solo se la velocità orizzontale supera la soglia
        if (vx > flipThreshold) lastFacing = 1;
        else if (vx < -flipThreshold) lastFacing = -1;

        ApplyFlip(lastFacing);
    }

    private void ApplyFlip(int facing)
    {
        if (!bodyToFlip) return;
        bool flipX = (facing < 0);
        if (invertFlip) flipX = !flipX;
        bodyToFlip.flipX = flipX;
    }

    private void ShootTick()
    {
        Vector2 target = (Vector2)player.position + PredictLead();

        gun.FireAt(target, () =>
        {
            bulletsLeft--;
            nextFireTime = Time.time + fireCooldown;

            if (bulletsLeft <= 0)
                StartCoroutine(StartReloadAfterDelay(reloadStartDelayAfterShot));
        });
    }

    private IEnumerator StartReloadAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        StartCoroutine(ReloadStun());
    }

    private IEnumerator ReloadStun()
    {
        isReloading = true;
        state = State.ReloadStunned;

        rb.linearVelocity = Vector2.zero;
        SetIsMoving(false);

        if (vfx) vfx.OnStunStart();

        yield return new WaitForSeconds(reloadDuration);

        if (vfx) vfx.OnStunEnd();

        bulletsLeft = magazineSize;
        nextFireTime = Time.time + 0.2f;

        float dist = Vector2.Distance(transform.position, player.position);
        float retreatDist = retreatTiles * tileSize;
        state = (dist < retreatDist) ? State.Backpedal : State.Chase;
        isReloading = false;
    }

    // Helpers
    private void SetIsMoving(bool value)
    {
        if (animator) animator.SetBool(isMovingParam, value);
    }

    private Vector2 DirTo(Vector2 worldPos)
    {
        Vector2 d = (worldPos - (Vector2)transform.position);
        if (d.sqrMagnitude < 0.0001f) return Vector2.zero;
        return d.normalized;
    }

    private Vector2 Jitter()
    {
        float t = Time.time;
        float x = Mathf.PerlinNoise(jitterSeed.x, t) - 0.5f;
        float y = Mathf.PerlinNoise(jitterSeed.y, t + 133.7f) - 0.5f;
        return new Vector2(x, y) * jitterStrength;
    }

    private Vector2 PredictLead()
    {
        if (aimLead <= 0f) return Vector2.zero;
        var playerRB = player.GetComponent<Rigidbody2D>();
        if (playerRB == null) return Vector2.zero;
        return playerRB.linearVelocity * aimLead;
    }

    public void ForceReloadStun(float seconds)
    {
        StopAllCoroutines();
        reloadDuration = seconds;
        StartCoroutine(ReloadStun());
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float retreatDist = retreatTiles * tileSize;
        UnityEditor.Handles.color = new Color(1f, 0.3f, 0.2f, 0.5f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, retreatDist);
    }
#endif
}
