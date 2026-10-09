using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The car ahead: keeps a steady, slightly varying pace (never stops for no reason).
/// It only stops where real traffic would: at a red light, or behind something blocking the road.
/// </summary>
public class LeadCar : RoadBlocker
{
    public SpriteRenderer brakeLight;
    public Vector3 brakeLocal;

    float modeT, target;
    bool leaving;

    protected override void Awake()
    {
        base.Awake();
        PickPace();
    }

    public void Leave() { leaving = true; target = 13f; }

    void PickPace()
    {
        bool slow = Random.value < 0.35f;
        target = slow ? Random.Range(3.6f, 4.6f) : Random.Range(4.8f, 7.2f);
        modeT = Random.Range(5f, 11f);
    }

    void Update()
    {
        var g = RideGame.I;
        if (g == null) return;
        float dt = Time.deltaTime;

        if (!leaving)
        {
            modeT -= dt;
            if (modeT <= 0f) PickPace();
        }

        float prev = speed;
        float eff = target;
        if (!leaving)
        {
            float front = worldX + length * 0.5f;

            // traffic light
            var sig = TrafficSignal.NextAhead(front);
            if (sig != null && sig.ShouldStop(front, speed))
            {
                float dStop = Mathf.Max(0f, sig.stopX - 0.3f - front);
                float limit = dStop <= 0.02f ? 0f : Mathf.Sqrt(2f * 4.5f * dStop);     // brake so as to stop exactly at the line
                eff = Mathf.Min(eff, limit);
            }

            // something blocking the road ahead (the buffalo): queue behind it
            for (int i = 0; i < All.Count; i++)
            {
                var o = All[i];
                if (o == this || !o.blocking) continue;
                float d = o.RearX - front;
                if (d < 0f || d > 14f) continue;
                eff = Mathf.Min(eff, Mathf.Max(0f, o.speed) + Mathf.Max(0f, d - 1.5f) * 1.2f);
            }
        }
        speed = Mathf.MoveTowards(speed, eff, (eff < speed ? 7f : 3.2f) * dt);
        worldX += speed * dt;

        if (brakeLight != null)
        {
            bool braking = speed < prev - 0.001f || speed < 0.05f;
            brakeLight.enabled = braking && (worldX - g.distance) < cull;
        }

        float rel = worldX - g.distance;
        if ((leaving && rel > 40f) || rel < -30f || rel > 90f) Destroy(gameObject);
    }
}

/// <summary>Cars on the other lane: pure atmosphere, no collision.</summary>
public class OncomingCar : WorldAnchor
{
    public float speed = -11f;

    void Update()
    {
        var g = RideGame.I;
        if (g == null) return;
        worldX += speed * Time.deltaTime;
        if ((speed < 0f && worldX < g.distance - 40f) || (speed > 0f && worldX > g.distance + 46f)) Destroy(gameObject);
    }
}

/// <summary>A water buffalo that stands in the road until you wait (or honk). Cosy rural blocker.</summary>
public class Buffalo : RoadBlocker
{
    float waitT;
    bool leaving;
    SpriteRenderer sr;
    float startY;

    protected override void Awake()
    {
        base.Awake();
        startY = baseY;
    }

    public override void OnHonk()
    {
        var g = RideGame.I;
        if (g != null && !leaving && worldX - g.distance < 14f) waitT += 1.6f;
    }

    void Update()
    {
        var g = RideGame.I;
        if (g == null) return;
        if (!leaving)
        {
            float d = worldX - g.distance;
            if (d < 8f && g.speed < 0.6f) waitT += Time.deltaTime;
            if (waitT > 3f)
            {
                leaving = true;
                blocking = false;
                g.Toast("ควายเดินหลบให้แล้ว");
            }
        }
        else
        {
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
            if (sr == null) { Destroy(gameObject); return; }
            baseY += Time.deltaTime * 1.1f;
            worldX += Time.deltaTime * 0.6f;
            if (sr != null)
            {
                var c = sr.color;
                c.a = Mathf.Max(0f, c.a - Time.deltaTime * 0.6f);
                sr.color = c;
                if (c.a <= 0f) Destroy(gameObject);
                sr.sortingOrder = 1;
            }
        }
    }
}

/// <summary>Spawns and removes lead cars (zones along the route) and decorative oncoming traffic.</summary>
public class TrafficDirector : MonoBehaviour
{
    public Material mat;
    public Sprite[] carSprites;       // pickup, van, lorry, pickup2, van2
    public Sprite brakeSprite;
    public Sprite[] bikeSprites;        // scooter, motorcycle taxi, delivery bike (scenery only)
    public float nearLaneY, farLaneY;
    public Vector2[] zones;

    LeadCar current;
    float spawnCd, oncomingCd = 3f, bikeCd = 2.5f;
    RideGame g;

    void Start() { g = RideGame.I; }

    static readonly float[] CarLen = { 3.7f, 3.4f, 5.7f, 3.7f, 3.4f };

    void Update()
    {
        if (g == null) return;
        float dt = Time.deltaTime;

        // --- lead car -------------------------------------------------------------------
        bool inZone = false;
        float zoneEnd = 0f;
        foreach (var z in zones)
            if (g.distance >= z.x && g.distance <= z.y) { inZone = true; zoneEnd = z.y; }

        if (current == null)
        {
            spawnCd -= dt;
            if (inZone && spawnCd <= 0f && g.state != RideState.Ended && g.distance < zoneEnd - 60f)
            {
                SpawnLead();
                spawnCd = 4f;
            }
        }
        else if (!inZone) current.Leave();

        // --- motorcycles that zip past (scenery only) ------------------------------
        bikeCd -= dt;
        if (bikeCd <= 0f && g.state != RideState.Ended && bikeSprites != null && bikeSprites.Length > 0)
        {
            SpawnBike();
            bikeCd = Random.Range(3f, 7.5f);
        }

        // --- oncoming ---------------------------------------------------------------
        oncomingCd -= dt;
        if (oncomingCd <= 0f && g.state != RideState.Ended)
        {
            SpawnOncoming();
            oncomingCd = Random.Range(5f, 11f);
        }
    }

    void SpawnLead()
    {
        int k = Random.Range(0, carSprites.Length);
        var go = new GameObject("LeadCar");
        var car = go.AddComponent<LeadCar>();
        car.length = CarLen[k];
        car.worldX = g.distance + 26f;
        car.baseY = nearLaneY;
        car.speed = 3f;
        var sr = Make(go.transform, "body", carSprites[k], 5, Color.white);
        var br = Make(go.transform, "brake", brakeSprite, 6, Color.white);
        br.transform.localPosition = new Vector3(-CarLen[k] * 0.5f + 0.25f, 0.95f, 0);
        br.enabled = false;
        car.brakeLight = br;
        car.CacheRenderers();
        current = car;
    }

    /// <summary>A motorcycle in the oncoming (lower) lane: either coming towards us or overtaking us (it keeps going when we stop).</summary>
    void SpawnBike()
    {
        var s = bikeSprites[Random.Range(0, bikeSprites.Length)];
        if (s == null) return;
        bool oncoming = Random.value < 0.55f;
        var go = new GameObject("Bike");
        var bike = go.AddComponent<OncomingCar>();
        bike.baseY = farLaneY + Random.Range(-0.1f, 0.5f);
        if (oncoming) { bike.worldX = g.distance + 36f; bike.speed = -Random.Range(10f, 15f); }
        else { bike.worldX = g.distance - 30f; bike.speed = Random.Range(11f, 15f); }
        var sr = Make(go.transform, "body", s, 12, Color.white);
        sr.flipX = oncoming;                      // sprites face right
        bike.CacheRenderers();
    }

    void SpawnOncoming()
    {
        int k = Random.Range(0, carSprites.Length);
        var go = new GameObject("Oncoming");
        var car = go.AddComponent<OncomingCar>();
        car.worldX = g.distance + 34f;
        car.baseY = farLaneY;
        car.speed = -Random.Range(9f, 14f);
        var sr = Make(go.transform, "body", carSprites[k], 12, new Color(0.7f, 0.7f, 0.8f, 1f));
        sr.flipX = true;
        car.CacheRenderers();
    }

    SpriteRenderer Make(Transform parent, string n, Sprite s, int order, Color c)
    {
        var o = new GameObject(n);
        o.transform.SetParent(parent, false);
        var sr = o.AddComponent<SpriteRenderer>();
        SpriteMats.Apply(sr, s);
        sr.sortingOrder = order;
        sr.color = c;
        return sr;
    }
}
