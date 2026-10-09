using UnityEngine;

/// <summary>Slowly turns night into dawn as the night clock runs out; moves the moon, twinkles the stars.</summary>
public class SkyController : MonoBehaviour
{
    public SpriteRenderer dawn, starsA, starsB, moon, moonGlow;
    public ParallaxLayer[] layers;
    RideGame g;

    void Start() { g = RideGame.I; }

    void LateUpdate()
    {
        if (g == null) return;
        float t = g.NightProgress;
        float d = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, t));
        dawn.color = new Color(1, 1, 1, d);

        float starFade = 1f - Mathf.InverseLerp(0.55f, 0.95f, t);
        starsA.color = new Color(1, 1, 1, starFade * (0.55f + 0.45f * Mathf.Sin(Time.time * 1.3f)));
        starsB.color = new Color(1, 1, 1, starFade * (0.55f + 0.45f * Mathf.Sin(Time.time * 1.3f + 2.4f)));

        float mx = Mathf.Lerp(10f, -8f, t);
        float my = 4.2f + 2.4f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.05f)) - t * 2.2f;
        moon.transform.position = new Vector3(WorldAnchor.Snap(mx), WorldAnchor.Snap(my), 0);
        moonGlow.transform.position = moon.transform.position;
        float ma = 1f - Mathf.InverseLerp(0.8f, 1f, t) * 0.8f;
        moon.color = new Color(1, 1, 1, ma);
        moonGlow.color = new Color(1, 1, 1, ma * (1f - d * 0.6f));

        // warm tint on the far scenery as the sun gets close
        Color tint = Color.Lerp(Color.white, new Color(1.0f, 0.82f, 0.78f, 1f), d);
        if (layers != null) foreach (var l in layers) if (l != null) l.SetTint(tint);
    }
}
