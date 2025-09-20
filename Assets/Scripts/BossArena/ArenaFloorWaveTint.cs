using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public class ArenaFloorWaveTint : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Tilemap arenaTilemap;

    [Header("Wave")]
    [Tooltip("Celle al secondo che l'onda percorre radialmente.")]
    [SerializeField] private float waveSpeed = 20f;

    [Header("Tint")]
    [Range(0f, 1f)] public float tintStrength = 0.65f; // usalo nel blend per assorbimento
    public Color CurrentTint { get; private set; } = Color.white;

    // Cache celle presenti nell'arena
    private List<Vector3Int> _cells;
    private bool _cellsCached;

    void Reset() { arenaTilemap = GetComponent<Tilemap>(); }

    void Awake()
    {
        if (!arenaTilemap) arenaTilemap = GetComponent<Tilemap>();
        CacheCellsIfNeeded();
    }

    void CacheCellsIfNeeded()
    {
        if (_cellsCached || arenaTilemap == null) return;

        _cells = new List<Vector3Int>(512);
        var b = arenaTilemap.cellBounds;

        for (int x = b.min.x; x < b.max.x; x++)
        for (int y = b.min.y; y < b.max.y; y++)
        {
            var c = new Vector3Int(x, y, 0);
            if (!arenaTilemap.HasTile(c)) continue;

            // sblocca i tile flags per permettere SetColor
            arenaTilemap.SetTileFlags(c, TileFlags.None);
            _cells.Add(c);
        }

        _cellsCached = true;
    }

    /// <summary>
    /// Avvia l'onda di tint partendo da worldCenter.
    /// Evita di far partire coroutine se l'oggetto/componente è inattivo
    /// o se siamo in transizione scena (opzionale: GameState.IsChangingScene).
    /// </summary>
    public void SlamWave(Vector3 worldCenter)
    {
        // opzionale: se hai un flag globale per i cambi scena
        // if (GameState.IsChangingScene) return;

        // SEGNAPOSTO-CHIAVE: evita StartCoroutine su componente inattivo
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (!arenaTilemap) return;

        CacheCellsIfNeeded();
        if (_cells == null || _cells.Count == 0) return;

        CurrentTint = RandomHSV(0.55f, 0.9f, 0.35f, 0.85f);

        // OK chiamare StopAllCoroutines qui (il componente è attivo ed abilitato)
        StopAllCoroutines();
        StartCoroutine(WaveRoutine(worldCenter, CurrentTint));
    }

    private IEnumerator WaveRoutine(Vector3 worldCenter, Color tint)
    {
        if (!arenaTilemap || _cells == null || _cells.Count == 0)
            yield break;

        float start = Time.time;

        // Colorazione progressiva
        while (true)
        {
            // se durante l’esecuzione il GO venisse disattivato, fermiamo pulito
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
                yield break;

            float elapsed = Time.time - start;
            float radius = elapsed * waveSpeed;

            bool anyPending = false;

            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                Vector3 center = arenaTilemap.GetCellCenterWorld(cell);
                float d = Vector2.Distance((Vector2)center, (Vector2)worldCenter);

                if (arenaTilemap.GetColor(cell) != tint)
                {
                    if (d <= radius)
                    {
                        arenaTilemap.SetColor(cell, tint);
                    }
                    else
                    {
                        anyPending = true;
                    }
                }
            }

            if (!anyPending) break;
            yield return null;
        }
    }

    public void ResetAllTo(Color c)
    {
        // opzionale: non fare lavoro se stiamo spegnendo
        if (!arenaTilemap || _cells == null) return;
        for (int i = 0; i < _cells.Count; i++)
            arenaTilemap.SetColor(_cells[i], c);
    }

    public Color GetCellTint(Vector3 worldPos)
    {
        if (!arenaTilemap) return Color.white;
        var cell = arenaTilemap.WorldToCell(worldPos);
        return arenaTilemap.GetColor(cell);
    }

    static Color RandomHSV(float sMin, float sMax, float vMin, float vMax)
    {
        float h = Random.value;
        float s = Random.Range(sMin, sMax);
        float v = Random.Range(vMin, vMax);
        return Color.HSVToRGB(h, s, v);
    }
}
