using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DummyEnemy : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer enemyRenderer;
    public Tilemap terrainTilemap; // da assegnare nell'Inspector

    [Header("Colore corrente")]
    public Color currentColor;

    [Header("Assorbimento per-canale")]
    [Tooltip("Ampiezza finestra per canale in 8 bit (0..255). Esempio: 30 → ±30 attorno al valore del tile.")]
    [Range(0, 255)] public int channelWindow8bit = 30;

    [Tooltip("Se true, assorbi il nemico subito quando entra in una cella il cui colore è compatibile.")]
    public bool autoAbsorbOnTileEnter = true;

    // --- Compatibilità con Boss/HUD ---
    // Continuiamo a fornire un valore [0..1] per l'HUD delle barre.
    // Qui definiamo il "match" come la minima vicinanza normalizzata tra i tre canali.
    [Range(0f, 1f)] public float matchToCountAsReached = 1.0f; // 1.0 → richiede che tutti e 3 i canali siano dentro finestra
    private IAbsorptionReceiver absorptionReceiver;

    // --- Caching per performance ---
    // Molte celle condividono lo stesso sprite → cache per sprite
    private readonly Dictionary<Sprite, Color> spriteAvgCache = new Dictionary<Sprite, Color>();
    // In caso tu applichi tint per-cella, puoi anche cache-per-cella
    private readonly Dictionary<Vector3Int, Color> cellAvgCache = new Dictionary<Vector3Int, Color>();

    // Tracciamento cella corrente (per "a ogni passo")
    private Vector3Int _lastCellPos;

    // NEW: ricordiamo l'ultimo tint visto per quella cella
    private readonly Dictionary<Vector3Int, Color> cellTintCache = new Dictionary<Vector3Int, Color>();


   private void Awake()
    {
        absorptionReceiver = GetComponent<IAbsorptionReceiver>();

        // >>> NEW: Bridge automatico verso la tilemap del Boss, se non è stata assegnata
        if (!terrainTilemap)
        {
            var boss = GetComponentInParent<BossController>();
            if (boss && boss.terrainTilemap)
                terrainTilemap = boss.terrainTilemap;
            else
            {
                // fallback intelligente: prova a trovare una tilemap che abbia una tile sotto i piedi
                terrainTilemap = FindTilemapAtPosition(transform.position);
            }
        }
    }

// >>> NEW: helper fallback (puoi metterlo più sotto nella classe)
private Tilemap FindTilemapAtPosition(Vector3 worldPos)
{
    Tilemap best = null;
    int bestScore = int.MinValue;

    foreach (var tm in FindObjectsOfType<Tilemap>())
    {
        Vector3Int cell = tm.WorldToCell(worldPos);
        if (!tm.HasTile(cell)) continue;

        int score = 0;
        var r = tm.GetComponent<TilemapRenderer>();
        if (r) score += r.sortingOrder;                          // preferisci tilemap "sopra"
        var n = tm.name.ToLowerInvariant();
        if (n.Contains("arena") || n.Contains("floor") || n.Contains("ground")) score += 50;

        // se è già colorata (onda passata) preferiscila
        Color tint = tm.GetColor(cell);
        if (!Approx(tint, Color.white)) score += 100;

        if (score > bestScore) { bestScore = score; best = tm; }
    }

    return best;
}

// >>> NEW: approx utility (se non ce l’hai già in questa classe)
static bool Approx(Color a, Color b, float eps = 0.002f)
{
    return Mathf.Abs(a.r - b.r) <= eps &&
           Mathf.Abs(a.g - b.g) <= eps &&
           Mathf.Abs(a.b - b.b) <= eps;
}

    private void Start()
    {
        if (!enemyRenderer) enemyRenderer = GetComponent<SpriteRenderer>();
        currentColor = enemyRenderer ? enemyRenderer.color : Color.white;

        if (!terrainTilemap)
            Debug.LogWarning("[DummyEnemy] Tilemap non assegnata!");

        _lastCellPos = GetCurrentCell();
        // Primo aggiornamento
        CheckAbsorptionOnCell(_lastCellPos);
    }

    private void Update()
    {
        // “A ogni passo”: quando cambia cella
        var cell = GetCurrentCell();
        if (cell != _lastCellPos)
        {
            _lastCellPos = cell;
            CheckAbsorptionOnCell(cell);
        }
    }

    public void SetColor(Color addedColor, float fraction)
    {
        currentColor.r = Mathf.Clamp01(currentColor.r + addedColor.r * fraction);
        currentColor.g = Mathf.Clamp01(currentColor.g + addedColor.g * fraction);
        currentColor.b = Mathf.Clamp01(currentColor.b + addedColor.b * fraction);

        if (enemyRenderer) enemyRenderer.color = currentColor;

        // Dopo una variazione cromatica controlliamo l’assorbimento sulla cella attuale
        CheckAbsorptionOnCell(_lastCellPos);
    }

    public void RemoveColorComponent(Color subtractColor, float amount)
    {
        currentColor.r = Mathf.Clamp01(currentColor.r - subtractColor.r * amount);
        currentColor.g = Mathf.Clamp01(currentColor.g - subtractColor.g * amount);
        currentColor.b = Mathf.Clamp01(currentColor.b - subtractColor.b * amount);

        if (enemyRenderer) enemyRenderer.color = currentColor;

        CheckAbsorptionOnCell(_lastCellPos);
    }

    public void ForceColor(Color newColor)
    {
        currentColor = newColor;
        if (enemyRenderer) enemyRenderer.color = newColor;
        CheckAbsorptionOnCell(_lastCellPos);
    }

    // ------------------ CORE LOGIC ------------------

    private Vector3Int GetCurrentCell()
    {
        if (!terrainTilemap) return Vector3Int.zero;
        return terrainTilemap.WorldToCell(transform.position);
    }

    private void CheckAbsorptionOnCell(Vector3Int cellPos)
    {
        if (!terrainTilemap) return;

        // Ottieni il colore medio del tile sotto i piedi
        if (!TryGetTileAverageColor(cellPos, out var tileColor))
            return; // cella vuota o sprite nullo

        // Calcola il match per l’HUD (0..1) e verifica finestra per-canale
        float match = ComputeWindowMatch01(currentColor, tileColor, channelWindow8bit);
        var boss = GetComponentInParent<BossController>();
        if (boss != null) boss.UpdateColorMatch(match);

        bool insideWindow = IsWithinWindow(currentColor, tileColor, channelWindow8bit);

        if (!insideWindow) return;

        if (absorptionReceiver != null)
        {
            absorptionReceiver.OnAbsorptionThresholdReached(match);
            return;
        }

        if (autoAbsorbOnTileEnter)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Calcola un "match" [0..1] basato sulla finestra per-canale.
    /// 1.0 significa che |enemy - tile| <= window su TUTTI e 3 i canali.
    /// Se un canale è oltre la finestra, il contributo scende < 1.
    /// Usiamo la *min* dei tre contributi così l'HUD rispecchia la barra più indietro.
    /// </summary>
    private static float ComputeWindowMatch01(Color enemy01, Color tile01, int window8bit)
    {
        float w = Mathf.Clamp(window8bit, 0, 255) / 255f + 1e-6f; // evit. div/0
        float mr = 1f - Mathf.Clamp01(Mathf.Abs(enemy01.r - tile01.r) / w);
        float mg = 1f - Mathf.Clamp01(Mathf.Abs(enemy01.g - tile01.g) / w);
        float mb = 1f - Mathf.Clamp01(Mathf.Abs(enemy01.b - tile01.b) / w);
        return Mathf.Min(mr, Mathf.Min(mg, mb));
    }

    // ------------------ TILE COLOR ------------------

    private bool TryGetTileAverageColor(Vector3Int cellPos, out Color avg)
{
    avg = Color.black;
    if (!terrainTilemap) return false;

    TileBase tile = terrainTilemap.GetTile(cellPos);
    if (!(tile is Tile t) || t.sprite == null) return false;

    // Tint attuale della cella (può cambiare con l'onda)
    Color tintNow = terrainTilemap.GetColor(cellPos);

    // Se abbiamo in cache e il tint non è cambiato in modo significativo → usa cache
    if (cellAvgCache.TryGetValue(cellPos, out var cachedAvg)
        && cellTintCache.TryGetValue(cellPos, out var cachedTint)
        && Approx(tintNow, cachedTint))
    {
        avg = cachedAvg;
        return true;
    }

    // Cache per sprite (costosa solo la prima volta per sprite)
    if (!spriteAvgCache.TryGetValue(t.sprite, out var baseAvg))
    {
        baseAvg = GetAverageColorFromSprite(t.sprite); // texture Readable!
        spriteAvgCache[t.sprite] = baseAvg;
    }

    // Applica il tint della *cella*
    avg = new Color(baseAvg.r * tintNow.r,
                    baseAvg.g * tintNow.g,
                    baseAvg.b * tintNow.b,
                    1f);

    // Aggiorna le cache
    cellAvgCache[cellPos] = avg;
    cellTintCache[cellPos] = tintNow;
    return true;
}

    private static Color GetAverageColorFromSprite(Sprite sprite)
    {
        // ATTENZIONE: la texture deve essere "Readable".
        Texture2D texture = sprite.texture;
        Rect rect = sprite.textureRect;

        // Evita allocazioni inutili in runtime pesante? In alternativa: downsample o sampling sparso.
        Color[] pixels = texture.GetPixels(
            Mathf.RoundToInt(rect.x),
            Mathf.RoundToInt(rect.y),
            Mathf.RoundToInt(rect.width),
            Mathf.RoundToInt(rect.height)
        );

        if (pixels == null || pixels.Length == 0) return Color.black;

        float r = 0f, g = 0f, b = 0f;
        for (int i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            r += p.r; g += p.g; b += p.b;
        }
        float inv = 1f / pixels.Length;
        return new Color(r * inv, g * inv, b * inv);
    }
    private static bool IsWithinWindow(Color enemy01, Color tile01, int window8bit)
    {
        float w = Mathf.Clamp(window8bit, 0, 255) / 255f;
        // se vuoi includere esattamente il bordo, usa <=
        return Mathf.Abs(enemy01.r - tile01.r) <= w
            && Mathf.Abs(enemy01.g - tile01.g) <= w
            && Mathf.Abs(enemy01.b - tile01.b) <= w;
    }
}
