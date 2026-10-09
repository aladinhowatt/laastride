using UnityEngine;

/// <summary>Endless horizontal strip made of repeated copies of one bottom-left-pivot sprite.</summary>
public class ParallaxLayer : MonoBehaviour
{
    public float factor = 1f;      // 1 = same speed as the road
    public float drift = 0f;       // constant extra scroll (units / s)
    float tileW;
    SpriteRenderer[] tiles;
    float driftPos;

    public void Build(Sprite sprite, float factor, int sortingOrder, Material mat, Color tint)
    {
        this.factor = factor;
        tileW = sprite.rect.width / RideGameConst.PPU;
        int n = Mathf.CeilToInt(32f / tileW) + 2;
        tiles = new SpriteRenderer[n];
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("tile" + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            SpriteMats.Apply(sr, sprite);
            sr.sortingOrder = sortingOrder;
            sr.color = tint;
            tiles[i] = sr;
        }
    }

    public void SetTint(Color c) { if (tiles != null) for (int i = 0; i < tiles.Length; i++) tiles[i].color = c; }

    void LateUpdate()
    {
        var g = RideGame.I;
        if (g == null || tiles == null) return;
        driftPos += drift * Time.deltaTime;
        float off = Mathf.Repeat(-(g.distance * factor + driftPos), tileW);
        float baseX = -15f - tileW + off;
        for (int i = 0; i < tiles.Length; i++)
        {
            float x = WorldAnchor.Snap(baseX + i * tileW);
            tiles[i].transform.localPosition = new Vector3(x, 0, 0);
        }
    }
}
