using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anything that lives at a fixed point along the road. The tuk-tuk never moves on screen;
/// objects are placed at (TukScreenX + worldX - distance) and snapped to the 16-px grid.
/// </summary>
public class WorldAnchor : MonoBehaviour
{
    public float worldX;
    public float parallax = 1f;      // < 1: a far layer, scrolls slower than the road (worldX is then in that layer's own space)
    public float baseY;
    public float yOffset;
    public float cull = 24f;

    SpriteRenderer[] rends;
    bool visible = true;

    public static float Snap(float v) { return Mathf.Round(v * RideGameConst.PPU) / RideGameConst.PPU; }

    protected virtual void Awake() { CacheRenderers(); }

    public void CacheRenderers() { rends = GetComponentsInChildren<SpriteRenderer>(true); }

    protected virtual void LateUpdate()
    {
        var g = RideGame.I;
        if (g == null) return;
        float x = RideGame.TukScreenX + (worldX - g.distance * parallax);
        var p = transform.position;
        transform.position = new Vector3(Snap(x), Snap(baseY + yOffset), p.z);

        bool vis = x > -cull && x < cull;
        if (vis != visible && rends != null)
        {
            visible = vis;
            for (int i = 0; i < rends.Length; i++) if (rends[i] != null) rends[i].enabled = vis;
        }
    }
}

public static class RideGameConst
{
    public const float PPU = 16f;
    public const float DashPx = 92f;                       // dashboard height in 960x540 reference px
    public const float DashUnits = DashPx / 32f;           // world lift of the camera (32 ref px per world unit)
}

/// <summary>A vehicle / animal on the road that the tuk-tuk must not run into.</summary>
public class RoadBlocker : WorldAnchor
{
    public static readonly List<RoadBlocker> All = new List<RoadBlocker>();

    public float length = 4f;
    public float speed;
    public bool blocking = true;

    protected virtual void OnEnable() { All.Add(this); }
    protected virtual void OnDisable() { All.Remove(this); }

    public float RearX { get { return worldX - length * 0.5f; } }

    /// <summary>Nearest blocking object whose rear is in front of x. gap = distance between the two bumpers.</summary>
    public static RoadBlocker NearestAhead(float frontX, out float gap)
    {
        RoadBlocker best = null;
        gap = float.MaxValue;
        for (int i = 0; i < All.Count; i++)
        {
            var b = All[i];
            if (!b.blocking) continue;
            float d = b.RearX - frontX;
            if (d < -0.5f) continue;     // already behind us
            if (d < gap) { gap = d; best = b; }
        }
        return best;
    }

    public virtual void OnHonk() { }
}
