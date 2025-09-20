using UnityEngine;
using UnityEngine.Tilemaps;

public static class TileThresholdsProvider
{
    // Ritorna i range per canale e il colore effettivo del pavimento (spriteAvg * cellTint).
    // halfWindow è [0..1] e deriva da channelWindow8bit/255f del nemico.
    public static (Range01 r, Range01 g, Range01 b, Color effective)
        GetRanges(Tilemap tm, Vector3 worldPos, float halfWindow)
    {
        Color eff;
        if (!TryGetEffectiveTileColor(tm, worldPos, out eff))
        {
            // fallback neutro
            eff = Color.black;
        }

        float w = Mathf.Clamp01(halfWindow);
        Range01 rr = new Range01(Mathf.Max(0f, eff.r - w), Mathf.Min(1f, eff.r + w));
        Range01 rg = new Range01(Mathf.Max(0f, eff.g - w), Mathf.Min(1f, eff.g + w));
        Range01 rb = new Range01(Mathf.Max(0f, eff.b - w), Mathf.Min(1f, eff.b + w));

        return (rr, rg, rb, eff);
    }

    // Trova una tilemap plausibile sotto worldPos (utile per il boss se non è assegnata)
    public static Tilemap FindTilemapAtPosition(Vector3 worldPos)
    {
        Tilemap best = null;
        int bestScore = int.MinValue;

        foreach (var tm in Object.FindObjectsOfType<Tilemap>())
        {
            Vector3Int cell = tm.WorldToCell(worldPos);
            if (!tm.HasTile(cell)) continue;

            int score = 0;

            var r = tm.GetComponent<TilemapRenderer>();
            if (r) score += r.sortingOrder;

            string n = tm.name.ToLowerInvariant();
            if (n.Contains("arena") || n.Contains("floor") || n.Contains("ground")) score += 50;

            Color tint = tm.GetColor(cell);
            if (!Approx(tint, Color.white)) score += 100; // preferisci già tinta (onda)

            if (score > bestScore) { bestScore = score; best = tm; }
        }
        return best;
    }

    // -------- helpers --------

    static bool TryGetEffectiveTileColor(Tilemap tm, Vector3 worldPos, out Color outColor)
    {
        outColor = Color.black;
        if (!tm) return false;

        Vector3Int cell = tm.WorldToCell(worldPos);
        if (!tm.HasTile(cell)) return false;

        Tile t = tm.GetTile<Tile>(cell);
        if (t == null || t.sprite == null) return false;

        // colore medio dallo sprite (texture deve essere Readable)
        Color baseAvg = GetAverageColorFromSprite(t.sprite);

        // tint per-cella impostato dall’onda
        Color tint = tm.GetColor(cell);

        outColor = new Color(baseAvg.r * tint.r,
                             baseAvg.g * tint.g,
                             baseAvg.b * tint.b, 1f);
        return true;
    }

    static Color GetAverageColorFromSprite(Sprite sprite)
    {
        Texture2D tex = sprite.texture;
        Rect r = sprite.textureRect;
        int x = Mathf.RoundToInt(r.x), y = Mathf.RoundToInt(r.y);
        int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);

        var px = tex.GetPixels(x, y, w, h);
        if (px == null || px.Length == 0) return Color.black;

        float rr = 0f, gg = 0f, bb = 0f;
        for (int i = 0; i < px.Length; i++) { rr += px[i].r; gg += px[i].g; bb += px[i].b; }
        float inv = 1f / px.Length;
        return new Color(rr * inv, gg * inv, bb * inv, 1f);
    }

    static bool Approx(Color a, Color b, float eps = 0.002f)
    {
        return Mathf.Abs(a.r - b.r) <= eps &&
               Mathf.Abs(a.g - b.g) <= eps &&
               Mathf.Abs(a.b - b.b) <= eps;
    }

    // Costruisce un Range01 senza dipendere da costruttori specifici
}
