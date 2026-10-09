using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the whole world at runtime from the generated sprites in Resources/LastRide.
/// The scene only needs a camera and this component (see Tools/Last Ride/Build Scene).
/// </summary>
public class RideBootstrap : MonoBehaviour
{
    // y positions (world units). Camera: orthographic, size 135/16, centred on 0 => 30 x 16.875 units = 480 x 270 px.
    const float RoadBottom = -8.4375f;
    const float RoadTop = -3.9375f;
    const float PropBase = -4.2f;
    const float NearLaneY = -5.5625f;      // wheels of our lane: the upper (far) one, so we keep LEFT like Thai traffic
    const float FarLaneY = -7.5625f;       // wheels of the oncoming lane: the lower one, drawn in front of us

    Material mat;
    readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    RideGame g;
    Camera cam;
    TrafficDirector traffic;

    Sprite S(string n)
    {
        Sprite s;
        if (cache.TryGetValue(n, out s)) return s;
        s = Resources.Load<Sprite>("LastRide/" + n);
        if (s == null) Debug.LogError("[LastRide] missing sprite 'LastRide/" + n + "' — run Tools/Last Ride/Regenerate Art");
        cache[n] = s;
        return s;
    }

    /// <summary>Prefer the Higgsfield illustration (Resources/LastRide/AI), fall back to the code-drawn sprite.</summary>
    Sprite Pick(string aiName, string fallbackName)
    {
        var s = Resources.Load<Sprite>("LastRide/AI/" + aiName);
        return s != null ? s : S(fallbackName);
    }

    SpriteRenderer Sr(Transform parent, string name, Sprite s, int order, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        SpriteMats.Apply(sr, s);
        sr.sortingOrder = order;
        return sr;
    }

    void Awake()
    {
        RoadBlocker.All.Clear();
        EventPoint.All.Clear();
        EventPoint.Current = null;

        SpriteMats.Clear();

        SetupCamera();
        g = new GameObject("RideGame").AddComponent<RideGame>();

        var skyRoot = BuildSky();
        bool regions = BuildScenery();
        var layers = BuildLayers(regions);
        BuildDecor(regions);
        BuildEvents();
        BuildSignals();
        BuildTraffic();
        BuildTuk();
        BuildAudio();
        BuildUI();

        skySc.layers = layers;
    }

    void Start()
    {
        DialogueUI.Instance.Play(Story.Intro());
        if (ShotBot.DirFromArgs() != null) gameObject.AddComponent<ShotBot>();
    }

    // ---- camera ------------------------------------------------------------------------------
    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 135f / 16f;
        cam.transform.position = new Vector3(0, -RideGameConst.DashUnits, -10);   // world is lifted above the dashboard
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;
        if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
    }

    // ---- sky ------------------------------------------------------------------------------------
    GameObject BuildSky()
    {
        var root = new GameObject("Sky");
        var night = Sr(root.transform, "night", S("sky_night"), -100, Vector3.zero);
        night.transform.localScale = new Vector3(32f, 2f, 1f);
        var dawn = Sr(root.transform, "dawn", S("sky_dawn"), -99, Vector3.zero);
        dawn.transform.localScale = new Vector3(32f, 2f, 1f);
        dawn.color = new Color(1, 1, 1, 0);
        var sa = Sr(root.transform, "starsA", S("stars_a"), -98, new Vector3(0, 0, 0));
        var sb = Sr(root.transform, "starsB", S("stars_b"), -97, new Vector3(0, 0, 0));
        var glow = Sr(root.transform, "moonGlow", S("moon_glow"), -96, new Vector3(9, 5, 0));
        glow.transform.localScale = new Vector3(2f, 2f, 1f);
        var moon = Sr(root.transform, "moon", S("moon"), -95, new Vector3(9, 5, 0));

        var sc = root.AddComponent<SkyController>();
        sc.dawn = dawn; sc.starsA = sa; sc.starsB = sb; sc.moon = moon; sc.moonGlow = glow;
        skySc = sc;
        return root;
    }
    SkyController skySc;

    /// <summary>Roadside pieces per place along the route; false (old code-drawn hills are used) if the AI pieces are missing.</summary>
    bool BuildScenery()
    {
        System.Func<string, Sprite> load = n => Resources.Load<Sprite>(ElementCatalog.Folder + n);
        if (!SceneryField.Available(load)) return false;
        new GameObject("Scenery").AddComponent<SceneryField>().Build(load, EventBlocks, g.routeLength);
        return true;
    }

    /// <summary>True if a scenery piece centred at this route position would cover an event spot.</summary>
    bool EventBlocks(float centre, float halfWidth)
    {
        foreach (var e in EventXs) if (Mathf.Abs(e - centre) < halfWidth + 6f) return true;
        return false;
    }

    ParallaxLayer[] BuildLayers(bool regions)
    {
        var list = new List<ParallaxLayer>();
        ParallaxLayer L(string name, string sprite, float y, float factor, int order, bool addToTint)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(0, y, 0);
            var pl = go.AddComponent<ParallaxLayer>();
            pl.Build(S(sprite), factor, order, mat, Color.white);
            if (addToTint) list.Add(pl);
            return pl;
        }
        if (!regions)
        {
            L("mountains", "mountains", -2.4f, 0.05f, -80, true);
            L("hills", "hills", -3.5f, 0.12f, -70, true);
            L("fields", "fields", -4.35f, 0.3f, -60, true);
        }
        L("road", "road", RoadBottom, 1f, -10, false);
        return list.ToArray();
    }

    // ---- decoration along the road ------------------------------------------------------------
    static readonly float[] EventXs = { 60, 170, 225, 330, 450, 520, 665, 800, 880, 975, 1070, 1125, 1300 };

    bool NearEvent(float x)
    {
        foreach (var e in EventXs) if (Mathf.Abs(e - x) < 9f) return true;
        return false;
    }

    WorldAnchor Prop(string name, string sprite, float x, int order, float dy = 0f, bool flip = false)
    {
        var go = new GameObject(name);
        var wa = go.AddComponent<WorldAnchor>();
        wa.worldX = x; wa.baseY = PropBase + dy;
        var sr = Sr(go.transform, "s", S(sprite), order, Vector3.zero);
        sr.flipX = flip;
        wa.CacheRenderers();
        return wa;
    }

    void BuildDecor(bool sceneryPieces)
    {
        if (sceneryPieces) return;          // the roadside is built from scenery pieces instead
        var rnd = new System.Random(7);
        var root = new GameObject("Decor").transform;
        float end = g.routeLength + 40f;
        // utility poles
        for (float x = 20f; x < end; x += 34f + (float)rnd.NextDouble() * 6f)
            if (!NearEvent(x)) Prop("pole", "pole", x, 1).transform.SetParent(root, true);
        // palms and bushes
        for (float x = 8f; x < end; x += 14f + (float)rnd.NextDouble() * 26f)
            if (!NearEvent(x) && x > 285f) Prop("palm", "palm", x, 0, 0.1f, rnd.Next(2) == 0).transform.SetParent(root, true);
        for (float x = 4f; x < end; x += 6f + (float)rnd.NextDouble() * 16f)
            if (!NearEvent(x) && x > 285f) Prop("bush", "bush", x, 2, -0.05f, rnd.Next(2) == 0).transform.SetParent(root, true);
        // a few hamlet shops
    }

    // ---- events ---------------------------------------------------------------------------------
    /// <summary>AI-made event prop if present, else the code-drawn one.</summary>
    Sprite Ev(string aiName, string fallback)
    {
        var s = Resources.Load<Sprite>(ElementCatalog.Folder + aiName);
        return s != null ? s : S(fallback);
    }

    EventPoint MakeEvent(string id, float x, Sprite prop, string prompt, Func<DScript> script, int order = 1)
    {
        var go = new GameObject("Event_" + id);
        var ev = go.AddComponent<EventPoint>();
        ev.id = id; ev.prompt = prompt; ev.script = script;
        ev.worldX = x; ev.baseY = PropBase;
        float h = 2.6f;                       // no prop sprite: only the (!) bubble (the scenery itself is the place)
        if (prop != null)
        {
            Sr(go.transform, "prop", prop, order, Vector3.zero);
            h = prop.rect.height / prop.pixelsPerUnit;
        }
        var mk = Sr(go.transform, "marker", S("marker"), 20, new Vector3(0, h + 0.3f, 0));
        ev.marker = mk;
        ev.markerHeight = h + 0.3f;
        ev.CacheRenderers();
        return ev;
    }

    /// <summary>A waiting ghost: only one ghost per stage can get in, so the player chooses.</summary>
    EventPoint MakeGhost(PassengerId id, float x, Sprite prop)
    {
        var d = Ghosts.Get(id);
        var ev = MakeEvent(d.key, x, prop, "คุยกับ" + d.name, () => Story.Passenger(id));
        ev.stage = d.stage;
        var fig = Sr(ev.transform, "figure", S(d.female ? "ghost_sri" : "ghost_ton"), 3, new Vector3(1.6f, 0.2f, 0f));
        fig.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        ev.figure = fig;
        ev.CacheRenderers();
        return ev;
    }

    void BuildEvents()
    {
        // stage 1 Yaowarat: ป้าศรี or อาแปะเฮง
        MakeGhost(PassengerId.Sri, 60, Ev("yao_stall", "cart"));
        MakeEvent("shrine", 170, Ev("spirit_house", "shrine"), "ไหว้ศาลพระภูมิ", Story.Shrine);
        MakeGhost(PassengerId.Heng, 225, null);
        // stage 2 Ayutthaya: พี่ต้น or น้องมายด์
        MakeGhost(PassengerId.Ton, 330, Ev("ayu_banyan_head", "banyan"));
        MakeGhost(PassengerId.Mai, 450, null);
        // stage 3 Sukhothai: ครูบัว or พี่นก
        MakeGhost(PassengerId.Bua, 665, Ev("suk_columns", "banyan"));
        MakeGhost(PassengerId.Nok, 800, Ev("suk_lotus", "banyan"));
        MakeEvent("gas", 880, Ev("gas_station", "gas"), "แวะปั๊ม", Story.GasStation);
        // stage 4 the northern road: ยายคำ or ต๋อง
        MakeGhost(PassengerId.Khum, 975, Ev("north_house", "shop"));
        MakeGhost(PassengerId.Tong, 1070, null);
        MakeEvent("stall", 1125, Ev("roadside_shop", "shop"), "แวะร้านชำ", Story.DrinkStall);
        var t = MakeEvent("temple", g.routeLength, null, "ขึ้นดอยสุเทพ", Story.Temple, 0);
        t.zoneFront = 9f; t.zoneBack = 5f;

        // buffalo
        var go = new GameObject("Buffalo");
        var b = go.AddComponent<Buffalo>();
        b.length = 3.6f; b.worldX = 520; b.baseY = NearLaneY;
        Sr(go.transform, "s", S("buffalo"), 4, Vector3.zero);
        b.CacheRenderers();
    }

    // ---- traffic lights ---------------------------------------------------------------------------
    // (stop-line route position, phase at the start of the night, seconds left in that phase)
    void BuildSignals()
    {
        var defs = new[]
        {
            new { x = 112f, p = TrafficSignal.Phase.Red, left = 7f },
            new { x = 252f, p = TrafficSignal.Phase.Green, left = 11f },
            new { x = 368f, p = TrafficSignal.Phase.Red, left = 9f },
            new { x = 762f, p = TrafficSignal.Phase.Green, left = 6f },
        };
        foreach (var d in defs) MakeSignal(d.x, d.p, d.left);
    }

    void MakeSignal(float stopX, TrafficSignal.Phase phase, float left)
    {
        var go = new GameObject("Signal_" + (int)stopX);
        var sig = go.AddComponent<TrafficSignal>();
        sig.stopX = stopX;
        sig.worldX = stopX + 1.2f;                         // the pole stands just past the stop line
        sig.baseY = PropBase - 0.1f;
        Sr(go.transform, "pole", S("sig_pole"), 4, Vector3.zero);
        sig.glow = Sr(go.transform, "glow", S("moon_glow"), 3, new Vector3(0f, 3.94f, 0f));
        sig.glow.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
        sig.lampRed = Sr(go.transform, "lampR", S("sig_lamp"), 5, new Vector3(0f, 3.94f, 0f));
        sig.lampYellow = Sr(go.transform, "lampY", S("sig_lamp"), 5, new Vector3(0f, 3.5f, 0f));
        sig.lampGreen = Sr(go.transform, "lampG", S("sig_lamp"), 5, new Vector3(0f, 3.06f, 0f));
        sig.CacheRenderers();
        sig.StartAt(phase, left);

        // stop line across the road
        var line = new GameObject("StopLine_" + (int)stopX);
        var la = line.AddComponent<WorldAnchor>();
        la.worldX = stopX; la.baseY = -8.1f;
        var lsr = Sr(line.transform, "s", UIKit.WhiteSprite(), -9, new Vector3(0f, 1.7f, 0f));
        lsr.transform.localScale = new Vector3(0.3f, 3.4f, 1f);
        lsr.color = new Color(0.9f, 0.9f, 0.85f, 0.9f);
        la.CacheRenderers();
    }

    void BuildTraffic()
    {
        var go = new GameObject("Traffic");
        traffic = go.AddComponent<TrafficDirector>();
        traffic.mat = mat;
        traffic.carSprites = new[] { S("car_pickup"), S("car_van"), S("car_lorry"), S("car_pickup2"), S("car_van2") };
        traffic.brakeSprite = S("brake");
        traffic.bikeSprites = new[] { S("bike_scooter"), S("bike_taxi"), S("bike_delivery") };
        traffic.nearLaneY = NearLaneY;
        traffic.farLaneY = FarLaneY;
        traffic.zones = new[] { new Vector2(30, 420), new Vector2(545, 900), new Vector2(975, 1240) };
    }

    // ---- our tuk-tuk -------------------------------------------------------------------------------
    void BuildTuk()
    {
        var go = new GameObject("TukTuk");
        var t = go.AddComponent<TukTukController>();
        t.baseY = NearLaneY;
        t.frames = new[] { S("tuk_0"), S("tuk_1") };
        t.ghostSri = S("ghost_sri");
        t.ghostTon = S("ghost_ton");
        t.beam = Sr(go.transform, "beam", S("beam"), 9, new Vector3(1.6f, 0.75f, 0));
        t.body = Sr(go.transform, "body", S("tuk_0"), 10, Vector3.zero);
        t.ghost = Sr(go.transform, "ghost", S("ghost_sri"), 11, new Vector3(-0.75f, 0.5f, 0));
        t.ghost.enabled = false;
        go.transform.position = new Vector3(RideGame.TukScreenX, NearLaneY, 0);
        tuk = t;
    }
    TukTukController tuk;

    // ---- audio -----------------------------------------------------------------------------------
    void BuildAudio()
    {
        var go = new GameObject("Audio");
        var eng = go.AddComponent<AudioSource>();
        eng.clip = ChipAudio.Engine(); eng.loop = true; eng.volume = 0.15f; eng.Play();
        var music = go.AddComponent<AudioSource>();
        music.clip = ChipAudio.Music(); music.loop = true; music.volume = 0.32f; music.Play();
        sfx = go.AddComponent<AudioSource>();
        tuk.engineSrc = eng; tuk.sfxSrc = sfx;
    }
    AudioSource sfx;

    // ---- UI -----------------------------------------------------------------------------------------
    void BuildUI()
    {
        var cgo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler));
        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5f;
        canvas.sortingOrder = 100;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(960, 540);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;

        var white = UIKit.WhiteSprite();
        HudUI.Build(cgo.transform, white, S("ui_panel"), S("icon_fuel"), S("icon_calm"), S("icon_clock"));

        var bds = new Dictionary<string, Sprite>();
        foreach (var k in new[] { "road", "sala", "shrine", "banyan", "gas", "shop", "temple" }) bds[k] = Pick("cut_" + k, "cut_" + k);
        bds["yaowarat"] = Pick("cut_yaowarat", "cut_road");
        bds["ayutthaya"] = Pick("cut_ayutthaya", "cut_banyan");
        bds["suthep"] = Pick("cut_suthep", "cut_temple");
        var pts = new Dictionary<string, Sprite>();
        foreach (var k in new[] { "lung", "sri", "ton", "gas", "vendor", "police", "heng", "mai", "bua", "nok", "khum", "tong", "driver" }) pts[k] = Pick("portrait_" + k, "portrait_" + k);
        DialogueUI.Build(cgo.transform, sfx, S("ui_panel"), S("ui_arrow"), bds, pts);
    }
}
