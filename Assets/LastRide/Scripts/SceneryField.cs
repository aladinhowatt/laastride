using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the roadside from separate pieces (shophouses, traffic lights, ruins, wooden houses, trees ...) in three depth rows
/// that scroll at different speeds, so the tuk-tuk really drives past things instead of in front of one big picture.
/// What stands where is decided per place by <see cref="ElementCatalog"/>.
/// </summary>
public class SceneryField : MonoBehaviour
{
    public static bool Active;

    /// <summary>How tightly a row is packed: gap between neighbours (units, negative = overlap) and chance of an open stretch.</summary>
    struct Dense { public float min, max, bigProb; public Dense(float a, float b, float p) { min = a; max = b; bigProb = p; } }

    static Dense DensityOf(string key)
    {
        switch (key)
        {
            case "yaowarat": return new Dense(-0.9f, 0.3f, 0f);       // a solid street front
            case "ayutthaya": return new Dense(-0.8f, 1.0f, 0.04f);
            case "sukhothai": return new Dense(-0.8f, 1.0f, 0.04f);
            case "north": return new Dense(-0.6f, 1.0f, 0.06f);
            default: return new Dense(-0.4f, 1.2f, 0.05f);
        }
    }

    struct Layer
    {
        public float factor, scale, baseY;
        public int order;
        public Color night, dawn;
    }

    const float NearBase = -4.2f;

    static readonly Layer Near = new Layer { factor = 1f, scale = 1f, baseY = NearBase, order = 0,
        night = Color.white, dawn = new Color(1f, 0.88f, 0.78f, 1f) };
    static readonly Layer Mid = new Layer { factor = 0.8f, scale = 0.86f, baseY = -3.85f, order = -20,
        night = new Color(0.72f, 0.76f, 0.92f, 1f), dawn = new Color(0.97f, 0.84f, 0.82f, 1f) };
    static readonly Layer Far = new Layer { factor = 0.6f, scale = 0.72f, baseY = -3.55f, order = -50,
        night = new Color(0.52f, 0.58f, 0.84f, 1f), dawn = new Color(0.95f, 0.78f, 0.80f, 1f) };
    static readonly Layer VeryFar = new Layer { factor = 0.25f, scale = 1f, baseY = -2.95f, order = -70,
        night = new Color(0.36f, 0.42f, 0.70f, 1f), dawn = new Color(0.82f, 0.66f, 0.76f, 1f) };

    class Piece { public SpriteRenderer sr; public Layer layer; }
    readonly List<Piece> pieces = new List<Piece>();
    SpriteRenderer bandNear, bandFar, bandVeryFar;
    float lastDawn = -1f;
    RideGame g;
    int count;

    public static bool Available(Func<string, Sprite> load) { return load("yao_shophouse_a") != null; }

    public void Build(Func<string, Sprite> load, Func<float, float, bool> blocked, float routeLength)
    {
        Active = true;
        g = RideGame.I;
        var rnd = new System.Random(11);

        bandVeryFar = Band("bandVeryFar", -3.55f, -2.95f, -75);
        bandFar = Band("bandFar", -4.15f, -3.55f, -60);

        for (int r = 0; r < RouteRegions.All.Length; r++)
        {
            string key = RouteRegions.All[r].key;
            float start = RouteRegions.All[r].start;
            float end = RouteRegions.EndOf(r, routeLength);
            Def[] set;
            if (!TryNear(key, out set)) continue;

            var dens = DensityOf(key);
            FillRow(load, set, Near, start, end, rnd, blocked, dens);

            var tall = new List<Def>();
            foreach (var d in set) if (ElementCatalog.Height(d.name) >= 4f) tall.Add(d);
            if (tall.Count > 0)
            {
                FillRow(load, tall.ToArray(), Mid, start, end, rnd, null, new Dense(dens.min - 0.4f, dens.max, dens.bigProb * 0.5f));
                FillRow(load, tall.ToArray(), Far, start, end, rnd, null, new Dense(dens.min - 0.4f, dens.max + 0.5f, dens.bigProb * 0.5f));
            }

            string vf;
            if (ElementCatalog.VeryFar.TryGetValue(key, out vf))
                FillRow(load, new[] { new Def(vf, 1f) }, VeryFar, start, end, rnd, null, new Dense(0f, 0f, 0f), true);
        }

        foreach (var lm in ElementCatalog.Landmarks)
        {
            var s = load(lm.Key);
            if (s == null) continue;
            float w = s.rect.width / s.pixelsPerUnit;
            Place(s, Near, lm.Value - w * 0.5f, false);
        }
        ApplyColors(0f);
    }

    struct Def { public string name; public float weight; public Def(string n, float w) { name = n; weight = w; } }

    static bool TryNear(string key, out Def[] set)
    {
        ElementCatalog.Def[] src;
        set = null;
        if (!ElementCatalog.Near.TryGetValue(key, out src)) return false;
        set = new Def[src.Length];
        for (int i = 0; i < src.Length; i++) set[i] = new Def(src[i].name, src[i].weight);
        return true;
    }

    SpriteRenderer Band(string name, float bottom, float top, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        SpriteMats.Apply(sr, UIKit.WhiteSprite());
        sr.sortingOrder = order;
        go.transform.position = new Vector3(0f, (top + bottom) * 0.5f, 0f);
        go.transform.localScale = new Vector3(40f, top - bottom, 1f);       // white sprite is 1 unit square
        return sr;
    }

    void FillRow(Func<string, Sprite> load, Def[] set, Layer layer, float start, float end, System.Random rnd,
                 Func<float, float, bool> blocked, Dense dens, bool contiguous = false)
    {
        string last = null;
        float f = layer.factor;
        float q = start * f - (layer.factor < 1f ? 6f : 0f);
        float qEnd = end * f + (layer.factor < 1f ? 6f : 0f);
        float total = 0f; foreach (var d in set) total += d.weight;
        int guard = 0;
        while (q < qEnd && guard++ < 400)
        {
            Def chosen = set[0];
            for (int tries = 0; tries < 5; tries++)          // do not put the same piece twice in a row
            {
                float pick = (float)rnd.NextDouble() * total, acc = 0f;
                foreach (var d in set) { acc += d.weight; if (pick <= acc) { chosen = d; break; } }
                if (set.Length < 2 || chosen.name != last) break;
            }
            last = chosen.name;
            var s = load(chosen.name);
            if (s == null) { q += 2f; continue; }
            float w = s.rect.width / s.pixelsPerUnit * layer.scale;
            float centreRoute = (q + w * 0.5f) / f;                       // where in the route this piece stands
            bool skip = blocked != null && blocked(centreRoute, w * 0.5f);
            if (!skip) Place(s, layer, q, rnd.NextDouble() < 0.5 && chosen.name != "yao_gate");
            if (contiguous) { q += w * 0.96f; continue; }          // distant silhouettes form one unbroken skyline
            float gap = Mathf.Lerp(dens.min, dens.max, (float)rnd.NextDouble());
            if (rnd.NextDouble() < dens.bigProb) gap += 2.5f + (float)rnd.NextDouble() * 3f;
            q += w + gap;
        }
    }

    void Place(Sprite s, Layer layer, float left, bool flip)
    {
        float w = s.rect.width / s.pixelsPerUnit * layer.scale;
        var go = new GameObject("piece_" + s.name);
        go.transform.SetParent(transform, false);
        var wa = go.AddComponent<WorldAnchor>();
        wa.parallax = layer.factor;
        wa.worldX = left + w * 0.5f;
        wa.baseY = layer.baseY;
        wa.cull = 22f + w * 0.5f;
        var child = new GameObject("s");
        child.transform.SetParent(go.transform, false);
        child.transform.localScale = new Vector3(layer.scale, layer.scale, 1f);
        var sr = child.AddComponent<SpriteRenderer>();
        SpriteMats.Apply(sr, s);
        sr.sortingOrder = layer.order + (count % 4);          // neighbours that overlap do not fight over the same order
        sr.flipX = flip;
        wa.CacheRenderers();
        pieces.Add(new Piece { sr = sr, layer = layer });
        count++;
    }

    void ApplyColors(float dawn)
    {
        foreach (var p in pieces) p.sr.color = Color.Lerp(p.layer.night, p.layer.dawn, dawn);
        bandFar.color = Color.Lerp(new Color(0.06f, 0.08f, 0.18f), new Color(0.32f, 0.22f, 0.32f), dawn);
        bandVeryFar.color = Color.Lerp(new Color(0.09f, 0.11f, 0.26f), new Color(0.42f, 0.30f, 0.40f), dawn);
        lastDawn = dawn;
    }

    void LateUpdate()
    {
        if (g == null) g = RideGame.I;
        if (g == null) return;
        float d = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, g.NightProgress));
        if (Mathf.Abs(d - lastDawn) > 0.01f) ApplyColors(d);
    }
}
