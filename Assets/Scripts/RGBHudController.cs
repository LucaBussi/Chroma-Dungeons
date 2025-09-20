using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Tilemaps;

public class RGBHudController : MonoBehaviour
{
    [System.Serializable]
    public class ChannelRow
    {
        public TMP_Text label;                 // opzionale: "R"/"G"/"B"
        public CustomBarController bar;        // renderer (fill + glow + chevron)
        public RectTransform rangeOverlay;     // fascia finestra (come già usi tu)
        public Color tint = Color.white;       // tema (R/G/B)
    }

    [Header("Refs")]
    [SerializeField] private EnemyTargeter targeter;

    [Header("Rows")]
    public ChannelRow rowR;
    public ChannelRow rowG;
    public ChannelRow rowB;

    [Header("Tuning")]
    [Range(0.02f, 0.5f)] public float halfWindow = 0.15f;
    [Range(4f, 30f)] public float uiHz = 12f;

    [SerializeField] private CanvasGroup canvasGroup;

    private float _nextUiTick;

    private void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void SetVisible(bool on)
    {
        canvasGroup.alpha = on ? 1f : 0f;
        canvasGroup.interactable = on;
        canvasGroup.blocksRaycasts = on;
    }

    private void Update()
{
    var t = targeter ? targeter.Current : null;
    if (!t) { SetVisible(false); return; }

    if (Time.time < _nextUiTick) return;     // throttling UI
    _nextUiTick = Time.time + 1f / uiHz;

    // --- Tilemap corretta (anche per il boss) ---
    Tilemap tm = t.terrainTilemap;
    if (!tm)
    {
        var bc = t.GetComponentInParent<BossController>();
        if (bc && bc.terrainTilemap) tm = bc.terrainTilemap;
    }
    if (!tm) tm = TileThresholdsProvider.FindTilemapAtPosition(t.transform.position); // fallback furbo

    // Se ancora niente, non possiamo calcolare i range
    if (!tm) { SetVisible(false); return; }

    // --- Colore effettivo del pavimento sotto il target + range per canale ---
    // Usa la STESSA finestra dell’assorbimento: channelWindow8bit/255f
    float hw = Mathf.Clamp01(t.channelWindow8bit / 255f);

    var (rr, rg, rb, effective) = TileThresholdsProvider.GetRanges(tm, t.transform.position, hw);

    SetVisible(true);

    // colore corrente del nemico
    var curr = t.currentColor;

    // aggiorna righe
    UpdateRow(rowR, curr.r, rr);
    UpdateRow(rowG, curr.g, rg);
    UpdateRow(rowB, curr.b, rb);

    // (opzionale) puoi usare 'effective' per mostrare un chip colore del pavimento reale
}

    private void UpdateRow(ChannelRow row, float v01, Range01 target)
    {
        if (row.bar == null) return;

        // tema coerente
        row.bar.SetThemeColor(row.tint);

        // fill della barra
        row.bar.SetValue(v01);

        // overlay finestra (ancoraggi X come già facevi)
        if (row.rangeOverlay)
        {
            var rt = row.rangeOverlay;
            float ax0 = Mathf.Clamp01(target.min);
            float ax1 = Mathf.Clamp01(target.max);

            var aMin = rt.anchorMin; aMin.x = ax0; aMin.y = 0f;
            var aMax = rt.anchorMax; aMax.x = ax1; aMax.y = 1f;
            rt.anchorMin = aMin; rt.anchorMax = aMax;

            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }

        // logica di feedback
        bool below   = v01 < target.min;
        bool above   = v01 > target.max;
        bool inRange = !below && !above;

        row.bar.SetGlowEnabled(inRange);
        row.bar.SetChevron(inRange ? ChevronMode.None : (below ? ChevronMode.Up : ChevronMode.Down));
    }
}
