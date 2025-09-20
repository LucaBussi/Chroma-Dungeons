using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomSpawnPoint : MonoBehaviour
{
    [Tooltip("Pool di prefab nemici che questo punto può generare.")]
    public List<GameObject> enemyPrefabs = new();
    [Tooltip("Facoltativo: contenitore in gerarchia dove parentare i nemici istanziati.")]
    public Transform instancesParent;

    // --- Nuove opzioni colore ---
    public bool randomizeSpawnColor = true;

    public enum DistanceMetric { Euclidean, MaxChannel }
    [Tooltip("Metrica per misurare la 'lontananza' dal colore del tile.")]
    public DistanceMetric metric = DistanceMetric.Euclidean;

    [Range(0f, 1f)]
    [Tooltip("Distanza minima richiesta dal colore del tile (0..1). 0.7 è un buon punto di partenza.")]
    public float minDistance = 0.7f;

    [Tooltip("Numero massimo di tentativi per trovare un colore abbastanza lontano.")]
    public int maxTries = 20;

    public RoomEnemyAdapter Spawn(RoomController room, Tilemap groundTilemap)
    {
        var prefab = PickRandomEnemy();
        if (!prefab) return null;

        var go = Instantiate(prefab, transform.position, Quaternion.identity,
                             instancesParent ? instancesParent : null);

        var bossCtrl = go.GetComponent<BossController>();
        if (bossCtrl && groundTilemap)
            bossCtrl.terrainTilemap = groundTilemap;

        // Adapter per la stanza
        var adapter = go.GetComponent<RoomEnemyAdapter>();
        if (!adapter) adapter = go.AddComponent<RoomEnemyAdapter>();
        adapter.BindRoom(room);

        // Passa la tilemap al DummyEnemy (senza toccarne la logica)
        var dummy = go.GetComponent<DummyEnemy>();
        if (dummy && groundTilemap) dummy.terrainTilemap = groundTilemap;

        // --- Colore random: applicalo al frame successivo ---
        if (dummy)
        {
            bool isBoss = go.GetComponent<BossController>() != null;
            if (isBoss)
                StartCoroutine(ApplyColorNextFrame(dummy, Color.white)); // componenti maxate
            else if (randomizeSpawnColor)
                StartCoroutine(ApplyRandomColorNextFrame(dummy));
        }

        return adapter;
    }


    GameObject PickRandomEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return null;
        int idx = Random.Range(0, enemyPrefabs.Count);
        return enemyPrefabs[idx];
    }

    // ---------- Utility Colore ----------

    static Color PickRandomFarColor(Color baseColor, float minDist, DistanceMetric metric, int maxTries)
    {
        for (int i = 0; i < Mathf.Max(1, maxTries); i++)
        {
            var c = new Color(Random.value, Random.value, Random.value, 1f);
            if (Distance(baseColor, c, metric) >= minDist) return c;
        }
        // Se non troviamo entro i tentativi, prendi il più distante tra pochi campioni
        Color best = Color.black; float bestD = -1f;
        for (int i = 0; i < 8; i++)
        {
            var c = new Color(Random.value, Random.value, Random.value, 1f);
            float d = Distance(baseColor, c, metric);
            if (d > bestD) { bestD = d; best = c; }
        }
        return best;
    }

    static float Distance(Color a, Color b, DistanceMetric metric)
    {
        if (metric == DistanceMetric.MaxChannel)
        {
            float dr = Mathf.Abs(a.r - b.r);
            float dg = Mathf.Abs(a.g - b.g);
            float db = Mathf.Abs(a.b - b.b);
            return Mathf.Max(dr, Mathf.Max(dg, db));
        }
        else // Euclidean
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db) / 1.7320508f; // normalizza in [0..1]
        }
    }

    static bool TryGetTileAverageColor(Tilemap tilemap, Vector3 worldPos, out Color avg)
    {
        avg = Color.black;

        var cell = tilemap.WorldToCell(worldPos);
        var tileBase = tilemap.GetTile(cell);
        if (!(tileBase is Tile t) || t.sprite == null) return false;

        // Tint per-cella (richiede flag colore corretti sulla tilemap)
        Color tint = tilemap.GetColor(cell);
        var baseAvg = GetAverageColorFromSprite(t.sprite);
        avg = new Color(baseAvg.r * tint.r, baseAvg.g * tint.g, baseAvg.b * tint.b, 1f);
        return true;
    }

    static Color GetAverageColorFromSprite(Sprite sprite)
    {
        // Richiede texture Readable (come già nel tuo DummyEnemy)
        Texture2D tex = sprite.texture;
        Rect r = sprite.textureRect;

        // Nota: per performance potresti downsamplare; qui usiamo GetPixels completo
        Color[] px = tex.GetPixels(
            Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.y),
            Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height)
        );
        if (px == null || px.Length == 0) return Color.black;

        float rSum = 0, gSum = 0, bSum = 0;
        for (int i = 0; i < px.Length; i++) { var p = px[i]; rSum += p.r; gSum += p.g; bSum += p.b; }
        float inv = 1f / px.Length;
        return new Color(rSum * inv, gSum * inv, bSum * inv, 1f);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.25f);
    }
#endif
    private System.Collections.IEnumerator ApplyRandomColorNextFrame(DummyEnemy d)
    {
        // Aspetta un frame: DummyEnemy.Start() avrà già collegato enemyRenderer
        yield return null;
        d.ForceColor(PickRandomVividColor());
    }

    private Color PickRandomVividColor()
    {
        // Random piacevole e non troppo scuro/smuted
        // Hue 0..1, Saturation 0.6..1, Value 0.6..1
        return Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f, 1f, 1f);
    }
    private System.Collections.IEnumerator ApplyColorNextFrame(DummyEnemy d, Color c)
    {
        yield return null; // aspetta che DummyEnemy.Start() colleghi i renderer
        d.ForceColor(c);
    }


}


