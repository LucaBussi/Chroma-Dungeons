using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsAlive => CurrentHealth > 0f;

    [Header("Events")]
    public UnityEvent OnDeath;
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnMaxHealthChanged;

    // ===== OVERSHIELD =====
    [Header("Overshield (cuore speciale)")]
    [Tooltip("Valore totale dello scudo quando presente. 20 = 1 cuore.")]
    [SerializeField] private float shieldMax = 20f;
    public float ShieldCurrent { get; private set; }
    public float ShieldMax => shieldMax;
    public bool HasShield => ShieldCurrent > 0f;

    /// <summary>Event payload: current, max</summary>
    public event Action<float, float> OnShieldChanged;
    public UnityEvent OnShieldGained;
    public UnityEvent OnShieldBroken;

    [Header("Invulnerability (i-frames)")]
    [SerializeField] private bool swapLayerOnIFrames = true;
    [SerializeField] private string iFrameLayerName = "PlayerIFrame";
    [SerializeField] private GameObject layerSwapRoot;

    public bool IsInvincible { get; private set; }

    int _originalLayer = -1;
    int _iFrameLayer = -1;
    bool _isDead;

    void Awake()
    {
        CurrentHealth = maxHealth;
        if (!layerSwapRoot) layerSwapRoot = this.gameObject;

        _originalLayer = layerSwapRoot.layer;
        _iFrameLayer = LayerMask.NameToLayer(iFrameLayerName);
        if (swapLayerOnIFrames && _iFrameLayer < 0)
        {
            Debug.LogWarning($"[PlayerHealth] Layer '{iFrameLayerName}' non trovato. " +
                             $"Crea il layer o disattiva 'swapLayerOnIFrames'.");
            swapLayerOnIFrames = false;
        }
    }

    void Start()
    {
        // Notifiche iniziali a UI
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnShieldChanged?.Invoke(ShieldCurrent, shieldMax);
    }

    // ====== Invulnerabilità ======
    public void SetInvincible(bool value)
    {
        if (IsInvincible == value) return;
        IsInvincible = value;
        if (!swapLayerOnIFrames) return;

        int target = value ? _iFrameLayer : _originalLayer;
        SetLayerRecursively(layerSwapRoot, target);
    }

    static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        var t = obj.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }

    // ====== Danno ======
    public void TakeDamage(float amount, Vector2 hitFrom)
    {
        if (_isDead) return;
        if (IsInvincible) return;

        amount = Mathf.Max(0f, amount);
        if (amount <= 0f) return;

        // 1) Lo scudo assorbe prima
        if (HasShield)
        {
            float before = ShieldCurrent;
            float absorbed = Mathf.Min(ShieldCurrent, amount);
            ShieldCurrent -= absorbed;
            amount -= absorbed;

            if (!Mathf.Approximately(ShieldCurrent, before))
                OnShieldChanged?.Invoke(ShieldCurrent, shieldMax);

            if (ShieldCurrent <= 0f)
                OnShieldBroken?.Invoke();

            if (amount <= 0f) return; // tutto assorbito dallo scudo
        }

        // 2) Il resto va agli HP
        float old = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (!Mathf.Approximately(CurrentHealth, old))
        {
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            Debug.Log($"HP Player: {CurrentHealth}");
        }

        if (CurrentHealth <= 0f)
            Die();
    }

    // ====== Cura HP (non influenza lo scudo) ======
    public void Heal(float amount)
    {
        if (_isDead) return;
        if (amount <= 0f) return;

        float old = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        if (!Mathf.Approximately(CurrentHealth, old))
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    // ====== Gestione Scudo ======
    /// <summary>
    /// Prova a dare lo scudo (slot unico). Ritorna true se applicato, false se già presente.
    /// </summary>
    public bool TryGiveShield()
    {
        if (HasShield) return false; // slot già occupato (anche se parzialmente)
        ShieldCurrent = shieldMax;
        OnShieldChanged?.Invoke(ShieldCurrent, shieldMax);
        OnShieldGained?.Invoke();
        return true;
    }

    /// <summary>Rimuove lo scudo (es. alla morte o per altri effetti).</summary>
    public void ClearShield()
    {
        if (!HasShield) return;
        ShieldCurrent = 0f;
        OnShieldChanged?.Invoke(ShieldCurrent, shieldMax);
        OnShieldBroken?.Invoke();
    }

    // ====== Cambio Max HP ======
    public void SetMaxHealth(float newMax, bool keepRatio = true)
    {
        if (newMax <= 0f) return;

        float oldMax = maxHealth;
        float oldCurrent = CurrentHealth;

        if (keepRatio && oldMax > 0f)
        {
            float ratio = Mathf.Clamp01(CurrentHealth / oldMax);
            maxHealth = newMax;
            CurrentHealth = Mathf.Clamp(newMax * ratio, 0f, newMax);
        }
        else
        {
            maxHealth = newMax;
            CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        }

        OnMaxHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = true;
        CurrentHealth = 0f;

        // Notifica UI della discesa a 0 prima del death event
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        // (opzionale) ripulisci lo scudo alla morte
        if (HasShield) ClearShield();

        OnDeath?.Invoke();
    }
}
