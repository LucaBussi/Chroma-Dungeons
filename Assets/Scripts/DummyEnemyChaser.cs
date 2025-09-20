using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DummyEnemyChaser : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 2.2f;
    [SerializeField] private float maxChaseDistance = 20f;

    [Header("Contact Damage")]
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float contactTick = 0.5f; // ogni X secondi
    [SerializeField] private string playerTag = "Player";

    [Header("Animation")]
    [Tooltip("Animator con due stati: Idle e Walk. Parametri: IsMoving (bool) oppure Speed (float)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isMovingParam = "IsMoving"; // lascia vuoto se usi Speed
    [SerializeField] private string speedParam = "";            // es. "Speed" se preferisci un float
    [Tooltip("Sprite da flippare orizzontalmente in base alla direzione di movimento (opzionale)")]
    [SerializeField] private SpriteRenderer spriteToFlip;
    [SerializeField] private float flipDeadZone = 0.05f;  // evita flip nervosi quando quasi fermo
    [SerializeField] private float movingSpeedThreshold = 0.05f; // soglia per considerarlo fermo

    private Rigidbody2D _rb;
    private Transform _player;

    // per rate-limit del danno per-collider
    private readonly Dictionary<Collider2D, float> _nextHitTime = new();

    // per calcolare la velocità reale (siccome MovePosition non aggiorna sempre rb.velocity)
    private Vector2 _lastPos;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        var playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj) _player = playerObj.transform;

        // Autowire dell’Animator e dello SpriteRenderer se non assegnati
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!spriteToFlip) spriteToFlip = GetComponentInChildren<SpriteRenderer>();

        _lastPos = _rb.position;
    }

    private void FixedUpdate()
    {
        Vector2 targetPos = _rb.position;
        Vector2 moveDir = Vector2.zero;

        if (_player != null)
        {
            Vector2 toPlayer = _player.position - transform.position;

            if (toPlayer.sqrMagnitude <= maxChaseDistance * maxChaseDistance)
            {
                moveDir = toPlayer.normalized;
                targetPos = _rb.position + moveDir * speed * Time.fixedDeltaTime;
                _rb.MovePosition(targetPos);
            }
        }

        // Stima della velocità effettiva del frame (mag più affidabile di rb.velocity con MovePosition)
        float frameSpeed = (targetPos - _lastPos).magnitude / Time.fixedDeltaTime;
        _lastPos = targetPos;

        // Aggiorna animazione
        UpdateAnimation(frameSpeed, moveDir);
    }

    private void UpdateAnimation(float frameSpeed, Vector2 moveDir)
    {
        bool isMoving = frameSpeed > movingSpeedThreshold;

        // 1) Boolean: IsMoving
        if (animator && !string.IsNullOrEmpty(isMovingParam))
        {
            animator.SetBool(isMovingParam, isMoving);
        }

        // 2) Float: Speed (alternativa)
        if (animator && !string.IsNullOrEmpty(speedParam))
        {
            animator.SetFloat(speedParam, frameSpeed);
        }

        // 3) Flip orizzontale in base alla direzione
        if (spriteToFlip)
        {
            // Se ci stiamo muovendo, usa la direzione di movimento; altrimenti evita flip.
            if (Mathf.Abs(moveDir.x) > flipDeadZone)
                spriteToFlip.flipX = moveDir.x < 0f;
        }
    }

    // Metti questo sul collider TRIGGER dell'hurtbox (stesso oggetto o figlio)
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (!other.TryGetComponent<IDamageable>(out var dmg) || !dmg.IsAlive) return;

        float now = Time.time;
        if (!_nextHitTime.TryGetValue(other, out float next) || now >= next)
        {
            dmg.TakeDamage(contactDamage, (other.transform.position - transform.position).normalized);
            _nextHitTime[other] = now + contactTick;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_nextHitTime.ContainsKey(other))
            _nextHitTime.Remove(other);
    }
}
