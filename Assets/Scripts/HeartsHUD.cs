using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // LayoutElement

public class HeartsHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerHealth playerHealth;      // se vuoto, lo trova
    [SerializeField] private RectTransform heartsParent;     // se vuoto, usa questo transform
    [SerializeField] private HeartUI heartPrefab;

    [Header("Settings")]
    [SerializeField] private int   minHearts  = 5;
    [SerializeField] private float hpPerHeart = 20;

    [Header("Shield (opzionale)")]
    [Tooltip("PUOI assegnare qui il prefab di ShieldUI oppure lasciare vuoto se lo hai già in scena come figlio del panel.")]
    [SerializeField] private ShieldUI shieldUIPrefab;   // <- può essere prefab o null
    private ShieldUI shieldInstance;                    // <- istanza in scena usata dalla HUD

    [Tooltip("Se true, la cornice dello scudo si vede solo quando lo scudo > 0.")]
    [SerializeField] private bool hideShieldSlotWhenEmpty = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private readonly List<HeartUI> hearts = new();
    private float cur, max;

    // cache per fallback sync scudo
    private float lastShieldCur = float.NaN, lastShieldMax = float.NaN;

    void Awake()
    {
        if (!heartsParent) heartsParent = (RectTransform)transform;
        if (!playerHealth) playerHealth = FindObjectOfType<PlayerHealth>(true);

        if (!playerHealth)
        {
            Debug.LogError("[HeartsHUD] PlayerHealth non trovato in scena.");
            enabled = false; return;
        }
    }

    void Start()
    {
        // Trova o crea l'istanza di ShieldUI sotto il panel
        EnsureShieldInstance();

        cur = playerHealth.CurrentHealth;
        max = playerHealth.MaxHealth;

        BuildHearts(CalcHeartsCount(max));
        Refresh();

        playerHealth.OnHealthChanged    += HandleHealthChanged;
        playerHealth.OnMaxHealthChanged += HandleHealthChanged;
        playerHealth.OnShieldChanged    += HandleShieldChanged;

        // sync iniziale scudo (anche se 0)
        HandleShieldChanged(playerHealth.ShieldCurrent, playerHealth.ShieldMax);
        lastShieldCur = playerHealth.ShieldCurrent;
        lastShieldMax = playerHealth.ShieldMax;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged    -= HandleHealthChanged;
            playerHealth.OnMaxHealthChanged -= HandleHealthChanged;
            playerHealth.OnShieldChanged    -= HandleShieldChanged;
        }
    }

    int CalcHeartsCount(float maxHp)
    {
        int need = Mathf.CeilToInt(maxHp / Mathf.Max(1f, hpPerHeart));
        return Mathf.Max(minHearts, need);
    }

    void BuildHearts(int count)
    {
        // elimina SOLO i cuori
        for (int i = heartsParent.childCount - 1; i >= 0; i--)
        {
            var child = heartsParent.GetChild(i);
            if (child.GetComponent<HeartUI>())
                Destroy(child.gameObject);
        }
        hearts.Clear();

        if (!heartPrefab)
        {
            Debug.LogError("[HeartsHUD] heartPrefab non assegnato.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var h = Instantiate(heartPrefab, heartsParent);
            hearts.Add(h);
        }

        // assicurati che lo shield resti ultimo
        EnsureShieldIsLast();
    }

    void HandleHealthChanged(float current, float maximum)
    {
        cur = current;
        max = maximum;

        int desired = CalcHeartsCount(max);
        if (desired != hearts.Count)
            BuildHearts(desired);

        Refresh();

        if (logDebug) Debug.Log($"[HeartsHUD] HP Update: cur={cur}, max={max}");
    }

    void Refresh()
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            float heartMin = i * hpPerHeart;
            float hpInThis = Mathf.Clamp(cur - heartMin, 0f, hpPerHeart);
            float fill = hpPerHeart > 0f ? (hpInThis / hpPerHeart) : 0f;
            hearts[i].SetFill(fill);
        }
    }

    // ===== SHIELD =====

    // Crea/trova l'istanza in scena e mettila per ultima
    void EnsureShieldInstance()
    {
        if (!heartsParent) return;

        // 1) c'è già un ShieldUI come figlio del panel?
        shieldInstance = heartsParent.GetComponentInChildren<ShieldUI>(true);

        // 2) se non c'è, ma ho un prefab assegnato, lo istanzio
        if (!shieldInstance && shieldUIPrefab)
        {
            shieldInstance = Instantiate(shieldUIPrefab, heartsParent);
        }

        // 3) se ora ho l'istanza, la posiziono e ne regolo le dimensioni
        if (shieldInstance)
        {
            EnsureShieldIsLast();
        }
        else
        {
            if (logDebug) Debug.LogWarning("[HeartsHUD] Nessuno ShieldUI trovato o prefab non assegnato.");
        }
    }

    void HandleShieldChanged(float current, float maximum)
    {
        if (!shieldInstance) return;

        float t = (maximum > 0f) ? (current / maximum) : 0f;

        shieldInstance.SetFill(t);

        if (hideShieldSlotWhenEmpty)
            shieldInstance.SetSlotVisible(t > 0f);

        lastShieldCur = current;
        lastShieldMax = maximum;

        if (logDebug) Debug.Log($"[HeartsHUD] Shield Update: cur={current}, max={maximum}, t={t:0.00}");
    }

    // Mantieni lo scudo come ultimo figlio e con la stessa dimensione di un cuore
    void EnsureShieldIsLast()
    {
        if (!shieldInstance || !heartsParent) return;

        var shieldRT = shieldInstance.transform as RectTransform;

        // mettilo a destra (ultimo)
        shieldRT.SetAsLastSibling();

        // opzionale: stessa dimensione del cuore prefab (se noto)
        var heartRT = heartPrefab ? heartPrefab.GetComponent<RectTransform>() : null;
        if (heartRT)
        {
            var le = shieldRT.GetComponent<LayoutElement>();
            if (!le) le = shieldRT.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth  = heartRT.sizeDelta.x;
            le.preferredHeight = heartRT.sizeDelta.y;
        }
    }

    void OnTransformChildrenChanged() => EnsureShieldIsLast();

    // Fallback: se PlayerHealth non emette l'evento, sincronizza qui
    void LateUpdate()
    {
        if (!playerHealth) return;

        if (!Mathf.Approximately(lastShieldCur, playerHealth.ShieldCurrent) ||
            !Mathf.Approximately(lastShieldMax, playerHealth.ShieldMax))
        {
            HandleShieldChanged(playerHealth.ShieldCurrent, playerHealth.ShieldMax);
        }
    }
}
