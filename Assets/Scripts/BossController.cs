using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum BossState { Normal, Stunned, Recovering, Defeated }

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class BossController : MonoBehaviour, IAbsorptionReceiver
{
    [Header("Color Match")]
    [Range(0f, 1f)] public float stunThreshold = 0.9f;
    [Range(0f, 1f)] public float colorRecoverJitter = 0.1f;

    [Header("Terrain Ref (per ripresa cromatica)")]
    public Tilemap terrainTilemap;
    public SpriteRenderer bossSprite;

    [Header("Ripresa cromatica (fine stun)")]
    public bool recolorOnRecover = true;
    [Range(0f, 1f)] public float recolorMinDistanceFromTile = 0.25f;
    [Range(0f, 1f)] public float recolorMaxJitter = 0.20f;
    public int recolorMaxTries = 8;

    [Header("Stun")]
    public float stunDuration = 5f;
    public bool freezeDuringStun = true;

    [Header("Blur (seconda barra)")]
    [Range(0f, 1f)] public float blurLevel = 0f;
    public float blurPerHit = 0.2f;
    public float maxBlurToDefeat = 1f;
    public AnimationCurve blurEase = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Refs")]
    public BossVFX vfx;
    public MonoBehaviour[] aiScriptsToDisableWhileStunned;
    private DummyEnemy dummy;

    public BossState State { get; private set; } = BossState.Normal;

    // Animator
    [Header("Animator")]
    [SerializeField] private Animator animator;
    static readonly int HashDie = Animator.StringToHash("Die");
    static readonly int HashStunned = Animator.StringToHash("Stunned");
    static readonly int HashDead = Animator.StringToHash("Dead");

    [SerializeField] private string deathStateName = "Death"; // nome esatto dello stato nello State Machine
    [SerializeField] private float deathTimeout = 5f;         // sicurezza
    private bool _destroyed;

    // API colore
    public float ColorMatchRatio { get; private set; } = 0f;

    void Awake()
    {
        if (!bossSprite) bossSprite = GetComponent<SpriteRenderer>();
        if (!vfx) vfx = GetComponent<BossVFX>();
        if (!animator) animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        dummy = GetComponent<DummyEnemy>();
    }

    void SetBossColor(Color c)
    {
        if (bossSprite) bossSprite.color = c;
        if (dummy) dummy.ForceColor(c);
    }

    public void UpdateColorMatch(float ratio01)
    {
        ColorMatchRatio = Mathf.Clamp01(ratio01);
        if (State == BossState.Normal && ColorMatchRatio >= stunThreshold)
            StartCoroutine(StunRoutine());
    }

    // ==== Blur ====
    public void ApplyBlur(float amount)
    {
        if (State != BossState.Stunned) return;
        blurLevel = Mathf.Clamp01(blurLevel + amount);
        vfx?.SetBlur(blurEase.Evaluate(blurLevel));

        if (blurLevel >= maxBlurToDefeat)
            Defeat();
    }

    void Defeat()
    {
        if (State == BossState.Defeated) return;
        State = BossState.Defeated;

        foreach (var ai in aiScriptsToDisableWhileStunned)
            if (ai) ai.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        vfx?.OnDefeat();

        if (animator)
        {
            animator.SetBool("Dead", true); // o usa l’hash se già lo hai
            animator.SetTrigger("Die");
            StartCoroutine(DeathDestroyRoutine());   // <-- fallback automatico
        }
        else
        {
            Destroy(gameObject, 2f);
        }
    }


    IEnumerator StunRoutine()
    {
        State = BossState.Stunned;
        SetStunFrozen(true);
        vfx?.OnStunStart();
        if (animator) animator.SetBool(HashStunned, true);

        yield return new WaitForSeconds(stunDuration);

        if (State == BossState.Defeated) yield break;

        RecoverColorsSlightly();
        vfx?.OnStunEnd();

        State = BossState.Normal;
        SetStunFrozen(false);
        if (animator) animator.SetBool(HashStunned, false);
    }

    void SetStunFrozen(bool on)
    {
        foreach (var ai in aiScriptsToDisableWhileStunned)
            if (ai) ai.enabled = !on;

        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            if (on) rb.constraints |= RigidbodyConstraints2D.FreezePosition;
            else rb.constraints &= ~RigidbodyConstraints2D.FreezePosition;
        }
    }

    void RecoverColorsSlightly()
    {
        // riduci un po' il match "logico"
        ColorMatchRatio = Mathf.Clamp01(ColorMatchRatio - colorRecoverJitter);

        if (!recolorOnRecover || terrainTilemap == null || bossSprite == null || bossSprite.sprite == null)
            return;

        // scegli un colore lontano dal tile (preferibile rispetto al jitter)
        float minDist01 = Mathf.Clamp01(recolorMinDistanceFromTile);
        if (TryPickRandomFarColorFromTile(minDist01, recolorMaxTries, out Color newColor))
        {
            SetBossColor(newColor);
            if (TryGetTileColorUnderBoss(out var tile))
                ColorMatchRatio = ColorSimilarity01(newColor, tile);
        }
    }


    public void OnAbsorptionThresholdReached(float match01)
    {
        UpdateColorMatch(Mathf.Clamp01(match01));
        if (State == BossState.Normal)
            StartCoroutine(StunRoutine());
    }

    bool TryGetTileColorUnderBoss(out Color tileColor)
    {
        tileColor = Color.black;
        if (terrainTilemap == null) return false;

        Vector3Int cellPos = terrainTilemap.WorldToCell(transform.position);
        TileBase tile = terrainTilemap.GetTile(cellPos);
        if (!(tile is Tile t) || t.sprite == null) return false;

        // Colore "intrinseco" del tile
        Color baseAvg = GetAverageColorFromSprite(t.sprite);

        // >>> NEW: tint per-cella (viene impostato dall’onda)
        Color cellTint = terrainTilemap.GetColor(cellPos);

        // Colore effettivo percepito del pavimento sotto il boss
        tileColor = new Color(baseAvg.r * cellTint.r,
                            baseAvg.g * cellTint.g,
                            baseAvg.b * cellTint.b,
                            1f);
        return true;
    }

    Color GetAverageColorFromSprite(Sprite sprite)
    {
        Texture2D texture = sprite.texture;
        Rect rect = sprite.textureRect;

        Color[] pixels = texture.GetPixels(
            Mathf.RoundToInt(rect.x),
            Mathf.RoundToInt(rect.y),
            Mathf.RoundToInt(rect.width),
            Mathf.RoundToInt(rect.height)
        );
        if (pixels.Length == 0) return Color.black;

        float r = 0, g = 0, b = 0;
        foreach (var p in pixels) { r += p.r; g += p.g; b += p.b; }
        return new Color(r / pixels.Length, g / pixels.Length, b / pixels.Length, 1f);
    }

    bool TryPickRecoveredColor(Color tileColor, float minDist, float maxJitter, int tries, out Color outColor)
    {
        for (int i = 0; i < tries; i++)
        {
            Color candidate = new Color(
                Mathf.Clamp01(tileColor.r + Random.Range(-maxJitter, maxJitter)),
                Mathf.Clamp01(tileColor.g + Random.Range(-maxJitter, maxJitter)),
                Mathf.Clamp01(tileColor.b + Random.Range(-maxJitter, maxJitter)),
                1f
            );
            if (ColorDistance(candidate, tileColor) >= minDist)
            {
                outColor = candidate;
                return true;
            }
        }
        for (int i = 0; i < tries; i++)
        {
            Color candidate = new Color(Random.value, Random.value, Random.value, 1f);
            if (ColorDistance(candidate, tileColor) >= minDist)
            {
                outColor = candidate;
                return true;
            }
        }
        outColor = default;
        return false;
    }

    float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    float ColorSimilarity(Color a, Color b)
    {
        return 1f - Mathf.Clamp01(ColorDistance(a, b));
    }

    // ========= ANIMATION EVENT (fine death clip) =========
    // Chiama questo alla fine della death animation.
    public void AE_Death_End() { SafeDestroy(); }

    private void SafeDestroy()
    {
        if (_destroyed) return;
        _destroyed = true;
        Destroy(gameObject);
    }


    private IEnumerator DeathDestroyRoutine()
    {
        float t = 0f;
        int layer = 0;

        // aspetta che entri nello stato Death
        while (t < deathTimeout)
        {
            var st = animator.GetCurrentAnimatorStateInfo(layer);
            if (st.IsName(deathStateName))
                break;
            t += Time.deltaTime;
            yield return null;
        }

        // ora aspetta che il clip finisca
        while (t < deathTimeout)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.IsName(deathStateName) && st.normalizedTime >= 0.99f)
                break;
            t += Time.deltaTime;
            yield return null;
        }

        SafeDestroy();
    }

    // ===== Util: distanza normalizzata [0..1] (0 = identici, 1 ≈ massima differenza RGB) =====
    static float ColorDistance01(Color a, Color b)
    {
        float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
        // distanza euclidea normalizzata dividendo per sqrt(3)
        return Mathf.Sqrt(dr * dr + dg * dg + db * db) / 1.7320508f;
    }

    static float ColorSimilarity01(Color a, Color b) => 1f - Mathf.Clamp01(ColorDistance01(a, b));

    // ===== Picker: colore casuale "vivo" e lontano dal tile =====
    bool TryPickRandomFarColorFromTile(float minDist01, int tries, out Color outColor)
    {
        outColor = default;
        if (!TryGetTileColorUnderBoss(out Color tile)) return false;

        // 1) prova con colori "vividi"
        for (int i = 0; i < Mathf.Max(1, tries); i++)
        {
            var c = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f, 1f, 1f);
            if (ColorDistance01(c, tile) >= minDist01) { outColor = c; return true; }
        }
        // 2) allarga la rete (anche colori meno vividi)
        for (int i = 0; i < tries; i++)
        {
            var c = new Color(Random.value, Random.value, Random.value, 1f);
            if (ColorDistance01(c, tile) >= minDist01) { outColor = c; return true; }
        }
        // 3) fallback: scegli il più lontano tra alcuni campioni
        Color best = Color.black; float bestD = -1f;
        for (int i = 0; i < 16; i++)
        {
            var c = Random.ColorHSV(0f, 1f, 0.4f, 1f, 0.4f, 1f, 1f, 1f);
            float d = ColorDistance01(c, tile);
            if (d > bestD) { bestD = d; best = c; }
        }
        outColor = best;
        return true;
    }

    // ===== Call esplicita dal QTE: usa questa quando il giocatore FALLISCE =====
    public void OnStunQTEFailed()
    {
        // Se il boss è in stun e il QTE fallisce, ricolora SUBITO
        if (State == BossState.Stunned && terrainTilemap && bossSprite && bossSprite.sprite)
        {
            if (TryPickRandomFarColorFromTile(Mathf.Clamp01(recolorMinDistanceFromTile), recolorMaxTries, out Color c))
            {
                SetBossColor(c);
                if (TryGetTileColorUnderBoss(out var tile))
                    ColorMatchRatio = ColorSimilarity01(c, tile);
            }
        }
    }


}
