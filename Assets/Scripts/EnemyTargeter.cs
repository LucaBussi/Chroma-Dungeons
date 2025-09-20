using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

[System.Serializable] public class EnemyEvent : UnityEvent<DummyEnemy> {}

public class EnemyTargeter : MonoBehaviour
{
    // ================= Inspector =================
    [Header("Targeting")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField, Range(0.1f, 3f)] private float pickRadius = 0.8f;

    [Header("Aggiornamento colore continuo")]
    [Tooltip("Aggiorna le info colore anche senza cambiare bersaglio (utile per l'onda).")]
    [SerializeField] private bool continuousColorUpdate = true;
    [SerializeField, Range(1f, 60f)] private float colorUpdateHz = 8f;

    [Header("Output")]
    public EnemyEvent OnTargetChanged = new EnemyEvent();   // invariato

    // ================= API pubblica (nessun wiring richiesto) =================
    public static EnemyTargeter Instance { get; private set; }
    public DummyEnemy Current { get; private set; }
    public float PickRadius => pickRadius;

    // Info colore "effettivo" del pavimento sotto il bersaglio
    public bool HasColorInfo { get; private set; }
    public Color EffectiveTileColor { get; private set; }   // spriteAvg * tintPerCella
    public Color WindowMin { get; private set; }            // [r,g,b] min (inclusivo)
    public Color WindowMax { get; private set; }            // [r,g,b] max (inclusivo)
    public int Window8bit { get; private set; }             // dal nemico bersaglio

    // ================= Interni =================
    float _colorTimer;
    DummyEnemy _lastReportedEnemy;
    Color _lastEffective;

    void Awake()
    {
        if (Instance && Instance != this) { Instance = this; }
        else Instance = this;
    }

    private void Reset(){ cam = Camera.main; }

    private void Update()
    {
        if (!cam) cam = Camera.main;
        Vector3 m = Input.mousePosition;
        Vector3 world = cam ? cam.ScreenToWorldPoint(m) : m;
        world.z = 0f;

        var hits = Physics2D.OverlapCircleAll(world, pickRadius, enemyMask);
        float best = float.MaxValue;
        DummyEnemy bestEnemy = null;

        foreach (var h in hits)
        {
            var de = h.GetComponentInParent<DummyEnemy>();
            if (!de) continue;
            float d = (h.transform.position - world).sqrMagnitude;
            if (d < best) { best = d; bestEnemy = de; }
        }

        if (bestEnemy != Current)
        {
            Current = bestEnemy;
            if (Current) Debug.Log($"[EnemyTargeter] TARGET = {Current.name}");
            else Debug.Log("[EnemyTargeter] TARGET = <none>");

            OnTargetChanged.Invoke(Current);

            // forza refresh immediato delle info colore
            _lastReportedEnemy = null;
            _colorTimer = 0f;
            RefreshColorInfo(); // update subito
        }

        if (continuousColorUpdate)
        {
            _colorTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, colorUpdateHz);
            if (_colorTimer >= interval)
            {
                _colorTimer = 0f;
                RefreshColorInfo();
            }
        }
    }

    // ====== Colore effettivo del pavimento + finestra per-canale ======
    void RefreshColorInfo()
{
    if (!Current)
    {
        HasColorInfo = false;
        return;
    }

    // 1) Trova la tilemap giusta: prima quella del nemico, poi quella del BossController,
    //    infine una tilemap che abbia davvero una tile sotto i piedi (fallback).
    Tilemap tm = Current.terrainTilemap;
    if (!tm)
    {
        var bc = Current.GetComponentInParent<BossController>();
        if (bc && bc.terrainTilemap) tm = bc.terrainTilemap;
    }
    if (!tm)
    {
        tm = FindTilemapAtPosition(Current.transform.position);
    }
    if (!tm) { HasColorInfo = false; return; }

    // 2) Cella/tile sotto il bersaglio
    Vector3Int cell = tm.WorldToCell(Current.transform.position);
    if (!tm.HasTile(cell)) { HasColorInfo = false; return; }

    Tile t = tm.GetTile<Tile>(cell);
    if (t == null || t.sprite == null) { HasColorInfo = false; return; }

    // 3) Colore base del tile (dallo sprite)  +  4) Tint per-cella impostato dall’onda
    Color baseAvg = GetAverageColorFromSprite(t.sprite);  // texture "Readable"
    Color tint    = tm.GetColor(cell);

    // 5) Colore effettivo percepito del pavimento
    Color effective = new Color(baseAvg.r * tint.r,
                                baseAvg.g * tint.g,
                                baseAvg.b * tint.b, 1f);

    // 6) Evita spam se il colore effettivo non è cambiato
    if (ReferenceEquals(Current, _lastReportedEnemy) && Approx(effective, _lastEffective))
        return;

    _lastReportedEnemy = Current;
    _lastEffective = effective;

    // 7) Finestra per-canale (usiamo la window del nemico target)
    Window8bit = Mathf.Clamp(Current.channelWindow8bit, 0, 255);
    float w = Window8bit / 255f;

    WindowMin = new Color(
        Mathf.Max(0f, effective.r - w),
        Mathf.Max(0f, effective.g - w),
        Mathf.Max(0f, effective.b - w),
        1f
    );
    WindowMax = new Color(
        Mathf.Min(1f, effective.r + w),
        Mathf.Min(1f, effective.g + w),
        Mathf.Min(1f, effective.b + w),
        1f
    );

    EffectiveTileColor = effective;
    HasColorInfo = true;
}


    // ================= Helpers =================
    static bool Approx(Color a, Color b, float eps = 0.002f)
    {
        return Mathf.Abs(a.r - b.r) <= eps &&
               Mathf.Abs(a.g - b.g) <= eps &&
               Mathf.Abs(a.b - b.b) <= eps;
    }

    static string ColorToStr(Color c)
    {
        return $"({(int)(c.r*255)},{(int)(c.g*255)},{(int)(c.b*255)})";
    }

    static Color GetAverageColorFromSprite(Sprite sprite)
    {
        Texture2D tex = sprite.texture;
        Rect r = sprite.textureRect;
        int x = Mathf.RoundToInt(r.x), y = Mathf.RoundToInt(r.y);
        int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);

        // ATTENZIONE: la texture deve essere Readable
        Color[] px = tex.GetPixels(x, y, w, h);
        if (px == null || px.Length == 0) return Color.black;

        float rr = 0f, gg = 0f, bb = 0f;
        for (int i = 0; i < px.Length; i++) { rr += px[i].r; gg += px[i].g; bb += px[i].b; }
        float inv = 1f / px.Length;
        return new Color(rr * inv, gg * inv, bb * inv, 1f);
    }

    private void OnDrawGizmos()
    {
        if (!cam) cam = Camera.main;
        Vector3 m = Application.isPlaying ? Input.mousePosition : new Vector3(Screen.width*0.5f, Screen.height*0.5f, 0);
        Vector3 world = cam ? cam.ScreenToWorldPoint(m) : m;
        world.z = 0f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(world, pickRadius);
    }

    private Tilemap FindTilemapAtPosition(Vector3 worldPos)
{
    Tilemap best = null;
    int bestScore = int.MinValue;

    foreach (var tm in FindObjectsOfType<Tilemap>())
    {
        Vector3Int cell = tm.WorldToCell(worldPos);
        if (!tm.HasTile(cell)) continue;

        int score = 0;

        // Preferisci quella "sopra" (se hai più tilemap sovrapposte)
        var r = tm.GetComponent<TilemapRenderer>();
        if (r) score += r.sortingOrder;

        // Preferisci nomi plausibili
        string n = tm.name.ToLowerInvariant();
        if (n.Contains("arena") || n.Contains("floor") || n.Contains("ground")) score += 50;

        // Preferisci celle già tinte (onda passata)
        Color tint = tm.GetColor(cell);
        if (!Approx(tint, Color.white)) score += 100;

        if (score > bestScore) { bestScore = score; best = tm; }
    }

    return best;
}

}
