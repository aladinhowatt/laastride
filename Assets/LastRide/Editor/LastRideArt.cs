using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates every sprite of the prototype as code-drawn 8-bit pixel art into
/// Assets/LastRide/Resources/LastRide/*.png (loaded at runtime with Resources.Load).
/// Change the drawing here and re-run Tools/Last Ride/Regenerate Art instead of editing the PNGs.
/// </summary>
public static class LastRideArt
{
    public const string Dir = "Assets/LastRide/Resources/LastRide";
    public const float PPU = 16f;

    // ---- palette --------------------------------------------------------------------------
    static Color32 H(string hex, byte a = 255) { return PixelCanvas.Hex(hex, a); }
    static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    static readonly Color32 Ink = H("1b1034");
    static readonly Color32 Teal = H("2fb8a8"), TealD = H("1f857b"), TealL = H("7fe6d6");
    static readonly Color32 Pink = H("f06a8a"), PinkD = H("b8456a"), PinkL = H("ffa0b8");
    static readonly Color32 Yel = H("f2c14e"), YelD = H("c48a2a"), YelL = H("ffe39a");
    static readonly Color32 Cream = H("fff3d6"), Orange = H("ff9a1f"), Red = H("e0455a");
    static readonly Color32 Tire = H("14121f"), Rim = H("9aa0b8"), Light = H("fff3a0");
    static readonly Color32 CabinDark = H("2a1d4a"), Glass = H("5a6aa8"), GlassL = H("8fa0d8");

    struct Pending { public string path; public Vector2 pivot; public Vector4 border; public bool repeat; }
    static readonly List<Pending> pending = new List<Pending>();

    // ---- entry ------------------------------------------------------------------------------
    public static void GenerateAll()
    {
        Directory.CreateDirectory(Dir);
        pending.Clear();

        var BC = new Vector2(0.5f, 0f);   // bottom-centre
        var BL = new Vector2(0f, 0f);     // bottom-left
        var C = new Vector2(0.5f, 0.5f);  // centre

        // sky + sky objects
        Save("sky_night", Sky(false), C);
        Save("sky_dawn", Sky(true), C);
        Save("stars_a", Stars(11, 120), BC);
        Save("stars_b", Stars(77, 90), BC);
        Save("moon", Moon(), C);
        Save("moon_glow", MoonGlow(), C);

        // parallax / ground tiles
        Save("mountains", Mountains(H("1c2352"), H("2c3676"), 34, 16, 5, 72), BL);
        Save("hills", Hills(), BL);
        Save("fields", Fields(), BL);
        Save("road", Road(), BL);

        // vehicles
        Save("tuk_0", Tuk(0), BC);
        Save("tuk_1", Tuk(1), BC);
        Save("ghost_sri", Ghost(0), BC);
        Save("ghost_ton", Ghost(1), BC);
        Save("beam", Beam(), new Vector2(0f, 0.5f));
        Save("car_pickup", CarPickup(H("e0663a"), H("a8402a")), BC);
        Save("car_van", CarVan(H("f0e6c8"), H("2fb8a8")), BC);
        Save("car_lorry", CarLorry(H("3f8f5a"), H("8a5a3a")), BC);
        Save("car_pickup2", CarPickup(H("4a78c8"), H("2f4f8a")), BC);
        Save("car_van2", CarVan(H("f2c14e"), H("e0455a")), BC);
        Save("brake", Rect1(5, 3, H("ff5a5a")), C);
        Save("buffalo", Buffalo(), BC);
        Save("bike_scooter", Bike(0), BC);
        Save("bike_taxi", Bike(1), BC);
        Save("bike_delivery", Bike(2), BC);
        Save("sig_pole", SigPole(), BC);
        Save("sig_lamp", SigLamp(), C);

        // roadside props
        Save("sala", Sala(), BC);
        Save("shrine", Shrine(), BC);
        Save("banyan", Banyan(), BC);
        Save("palm", Palm(), BC);
        Save("pole", Pole(), BC);
        Save("bush", Bush(), BC);
        Save("shop", Shop(), BC);
        Save("gas", Gas(), BC);
        Save("temple", Temple(), BC);
        Save("marker", Marker(), BC);
        Save("cart", Cart(), BC);

        // UI
        Save("ui_panel", Panel(), C, new Vector4(8, 8, 8, 8));
        Save("ui_arrow", Arrow(), C);
        Save("icon_fuel", IconFuel(), C);
        Save("icon_calm", IconCalm(), C);
        Save("icon_clock", IconMoon(), C);
        Save("portrait_lung", Portrait(H("e0a878"), H("3a3a4a"), Teal, Yel, 0, false), C);
        Save("portrait_sri", Portrait(H("d9ecff"), H("d8d8e8"), Cream, Pink, 1, true), C);
        Save("portrait_ton", Portrait(H("d9ecff"), H("3a3a5a"), Orange, Red, 2, true), C);
        Save("portrait_gas", Portrait(H("c89060"), H("2a2a3a"), H("4a78c8"), Yel, 3, false), C);
        Save("portrait_vendor", Portrait(H("d8a070"), H("5a3a3a"), H("e08aa8"), Cream, 4, false), C);

        // cut-scene backdrops (240x135)
        Save("cut_road", CutBg("road"), C);
        Save("cut_sala", CutBg("sala"), C);
        Save("cut_shrine", CutBg("shrine"), C);
        Save("cut_banyan", CutBg("banyan"), C);
        Save("cut_gas", CutBg("gas"), C);
        Save("cut_shop", CutBg("shop"), C);
        Save("cut_temple", CutBg("temple"), C);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var p in pending) Configure(p);
        AssetDatabase.SaveAssets();
        pending.Clear();
        Debug.Log("[LastRide] Pixel art generated in " + Dir);
    }

    // ---- saving ---------------------------------------------------------------------------
    static void Save(string name, PixelCanvas c, Vector2 pivot, Vector4 border = default(Vector4))
    {
        string path = Dir + "/" + name + ".png";
        var tex = c.ToTexture();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        pending.Add(new Pending { path = path, pivot = pivot, border = border });
    }

    static void Configure(Pending p)
    {
        var imp = AssetImporter.GetAtPath(p.path) as TextureImporter;
        if (imp == null) { Debug.LogWarning("[LastRide] no importer for " + p.path); return; }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = PPU;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.maxTextureSize = 2048;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = p.pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.spriteBorder = p.border;
        imp.SaveAndReimport();
    }

    // ---- helpers --------------------------------------------------------------------------
    static PixelCanvas Rect1(int w, int h, Color32 c) { var p = new PixelCanvas(w, h); p.Fill(c); return p; }

    static Color32 StopColor(Color32[] s, float t)
    {
        float f = Mathf.Clamp01(t) * (s.Length - 1);
        int i = Mathf.Min(s.Length - 2, (int)f);
        return PixelCanvas.Lerp(s[i], s[i + 1], f - i);
    }

    // ---- sky ------------------------------------------------------------------------------
    static PixelCanvas Sky(bool dawn)
    {
        var night = new[] { H("070a26"), H("0f1240"), H("1d1a55"), H("3a2870"), H("6a3a80") };
        var morn = new[] { H("2a3170"), H("5a4a98"), H("b2609a"), H("f0906e"), H("ffd088") };
        var stops = dawn ? morn : night;
        const int bands = 14;
        var c = new PixelCanvas(16, 135);
        for (int y = 0; y < 135; y++)
        {
            float t = (y + 0.5f) / 135f;
            float bf = t * bands;
            int bi = Mathf.Min(bands - 1, (int)bf);
            float frac = bf - bi;
            for (int x = 0; x < 16; x++)
            {
                float tt = (bi + 0.5f) / bands;
                if (((x + y) & 1) == 0)
                {
                    if (frac > 0.78f && bi < bands - 1) tt = (bi + 1.5f) / bands;
                    else if (frac < 0.22f && bi > 0) tt = (bi - 0.5f) / bands;
                }
                c.Set(x, y, StopColor(stops, tt));
            }
        }
        return c;
    }

    static PixelCanvas Stars(int seed, int count)
    {
        var c = new PixelCanvas(480, 135);
        var r = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            int x = r.Next(2, 478);
            int y = (int)(Math.Pow(r.NextDouble(), 1.5) * 118);
            var col = r.Next(4) == 0 ? H("ffe9a8") : H("e8f0ff");
            c.Set(x, y, col);
            if (r.Next(8) == 0)
            {
                var d = new Color32(col.r, col.g, col.b, 140);
                c.Set(x - 1, y, d); c.Set(x + 1, y, d); c.Set(x, y - 1, d); c.Set(x, y + 1, d);
            }
        }
        return c;
    }

    static PixelCanvas Moon()
    {
        var c = new PixelCanvas(24, 24);
        c.Disc(11.5f, 11.5f, 10.5f, H("f6efc8"));
        c.Disc(14, 13, 8.5f, H("f6efc8"));
        var shade = H("d9cf98");
        c.Disc(7, 8, 2.2f, shade); c.Disc(14, 15, 2f, shade); c.Disc(15, 6, 1.4f, shade); c.Disc(8, 16, 1.2f, shade);
        return c;
    }

    static PixelCanvas MoonGlow()
    {
        var c = new PixelCanvas(96, 96);
        for (int y = 0; y < 96; y++)
            for (int x = 0; x < 96; x++)
            {
                float d = Mathf.Sqrt((x - 47.5f) * (x - 47.5f) + (y - 47.5f) * (y - 47.5f)) / 47.5f;
                if (d >= 1) continue;
                float a = Mathf.Pow(1 - d, 2f);
                a = Mathf.Floor(a * 10f) / 10f;
                if (a <= 0) continue;
                c.Set(x, y, new Color32(250, 240, 190, (byte)(a * 38)));
            }
        return c;
    }

    // ---- tileable background layers ------------------------------------------------------
    static PixelCanvas Mountains(Color32 fill, Color32 rim, float baseH, float amp, int seed, int h)
    {
        var r = new System.Random(seed);
        double p1 = r.NextDouble() * 6.28, p2 = r.NextDouble() * 6.28, p3 = r.NextDouble() * 6.28;
        var c = new PixelCanvas(256, h);
        for (int x = 0; x < 256; x++)
        {
            double a = 2 * Math.PI * x / 256.0;
            double ht = baseH + amp * (0.55 * Math.Sin(2 * a + p1) + 0.3 * Math.Sin(5 * a + p2) + 0.15 * Math.Sin(11 * a + p3));
            int top = h - (int)ht;
            for (int y = top; y < h; y++)
            {
                var col = y == top ? rim : fill;
                if (y > top && y < top + 3 && ((x + y) & 1) == 0) col = PixelCanvas.Lerp(fill, rim, 0.35f);
                c.Set(x, y, col);
            }
        }
        return c;
    }

    static PixelCanvas Hills()
    {
        const int h = 56;
        var fill = H("14213f"); var rim = H("22345e");
        var c = new PixelCanvas(256, h);
        var r = new System.Random(21);
        var tops = new int[256];
        for (int x = 0; x < 256; x++)
        {
            double a = 2 * Math.PI * x / 256.0;
            double ht = 20 + 7 * Math.Sin(3 * a + 0.7) + 3 * Math.Sin(8 * a + 2.0);
            tops[x] = h - (int)ht;
            for (int y = tops[x]; y < h; y++) c.Set(x, y, y == tops[x] ? rim : fill);
        }
        // tree crowns (kept away from the seam)
        var tree = H("102038"); var treeL = H("1b3050");
        for (int i = 0; i < 16; i++)
        {
            int x = 8 + i * 15 + r.Next(5);
            if (x > 248) continue;
            int rad = 4 + r.Next(3);
            int cy = tops[x] - rad + 2;
            c.Disc(x, cy, rad, tree);
            c.Disc(x - 1, cy - 1, rad - 2, treeL);
        }
        // little houses with warm windows
        foreach (int hx in new[] { 40, 128, 205 })
        {
            int hy = tops[hx] - 7;
            c.Rect(hx - 6, hy, 12, 7, H("101a30"));
            for (int k = 0; k < 4; k++) c.Rect(hx - 7 + k, hy - k, 14 - 2 * k + 0, 1, H("0c1426"));
            c.Rect(hx - 3, hy + 2, 2, 2, H("ffd36a"));
            c.Rect(hx + 1, hy + 2, 2, 2, H("ffb84a"));
        }
        return c;
    }

    static PixelCanvas Fields()
    {
        const int h = 40;
        var c = new PixelCanvas(256, h);
        var r = new System.Random(31);
        var water = H("1d3f4b"); var waterL = H("2b5d6b"); var ridge = H("14303a");
        for (int x = 0; x < 256; x++)
        {
            int top = 8 + (int)(2.5 * Math.Sin(2 * Math.PI * 3 * x / 256.0) + 1.5 * Math.Sin(2 * Math.PI * 9 * x / 256.0));
            for (int y = top; y < h; y++)
            {
                var col = water;
                if (y == top) col = H("2c6a58");
                else if ((y - top) % 5 == 0) col = waterL;
                c.Set(x, y, col);
            }
        }
        for (int i = 0; i < 26; i++)
        {
            int x = r.Next(0, 256);
            c.Rect(x, 12, 1, h - 12, ridge);
        }
        for (int i = 0; i < 40; i++)
            c.Set(r.Next(256), 14 + r.Next(24), H("a8d8e8", 200));
        // palms
        foreach (int px in new[] { 30, 150, 215 })
        {
            c.Line(px, h - 1, px + 2, 10, H("0c1c26"));
            c.Line(px + 2, 10, px - 5, 13, H("0c1c26")); c.Line(px + 2, 10, px + 9, 13, H("0c1c26"));
            c.Line(px + 2, 10, px - 3, 7, H("0c1c26")); c.Line(px + 2, 10, px + 7, 7, H("0c1c26"));
        }
        // hut with a lantern
        int hx = 80;
        c.Rect(hx, 8, 12, 8, H("101c28")); c.Rect(hx - 2, 6, 16, 2, H("0c1620"));
        c.Rect(hx + 3, 10, 3, 3, H("ffd36a"));
        return c;
    }

    static PixelCanvas Road()
    {
        const int w = 64, h = 72;
        var c = new PixelCanvas(w, h);
        var r = new System.Random(5);
        var grass = H("2c5a4a"); var grassL = H("3f7a5a"); var grassD = H("234a3c");
        var dirt = H("5a4a45"); var asphalt = H("3b3b52"); var asphaltN = H("41415a");
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 col;
                if (y < 6) col = grass;
                else if (y < 10) col = dirt;
                else if (y == 10) col = H("7a7a92");
                else col = ((x * 7 + y * 13) % 11 == 0) ? asphaltN : asphalt;
                c.Set(x, y, col);
            }
        // grass tufts
        for (int i = 0; i < 14; i++)
        {
            int x = r.Next(w), y = r.Next(0, 5);
            c.Set(x, y, grassL); c.Set(x, y + 1, grassD);
        }
        for (int i = 0; i < 8; i++) c.Set(r.Next(w), 6 + r.Next(4), H("7a665c"));
        // lane divider (dashed) and edge
        for (int x = 0; x < w; x++)
        {
            if ((x % 32) < 14) { c.Set(x, 38, H("e9d27a")); c.Set(x, 39, H("c8b060")); }
            c.Set(x, 68, H("8a8aa0"));
        }
        // thin far edge
        for (int x = 0; x < w; x++) c.Set(x, 13, H("55556e"));
        return c;
    }

    // ---- vehicles -------------------------------------------------------------------------
    static void Wheel(PixelCanvas c, int cx, int cy, int r, int frame)
    {
        c.Disc(cx, cy, r, Tire);
        c.Disc(cx, cy, r - 2, Rim);
        c.Disc(cx, cy, 1, Cream);
        var spoke = H("5a607a");
        if (frame == 0)
        {
            c.Line(cx, cy - r + 2, cx, cy + r - 2, spoke);
            c.Line(cx - r + 2, cy, cx + r - 2, cy, spoke);
        }
        else
        {
            c.Line(cx - r + 3, cy - r + 3, cx + r - 3, cy + r - 3, spoke);
            c.Line(cx - r + 3, cy + r - 3, cx + r - 3, cy - r + 3, spoke);
        }
    }

    static PixelCanvas Tuk(int frame)
    {
        // matches the mood-board tuk-tuk: navy canopy, blue body with a gold stripe, yellow nose, driver at the handlebar
        var c = new PixelCanvas(56, 34);
        var navy = H("1e2b57"); var navyL = H("33447f"); var navyD = H("141c3c");
        var blue = H("2a4a9a"); var blueD = H("1b3170"); var blueL = H("4a6cc0");
        var gold = H("f2b632"); var goldD = H("b9821c"); var goldL = H("ffd869");
        var skin = H("c98a5c"); var shirt = H("dfe6ee"); var shirtD = H("a7b3c6"); var hair = H("20161a");
        var seat = H("6a3e2a");
        // body
        c.Rect(5, 21, 38, 6, blue);
        c.Rect(5, 26, 38, 1, blueD);
        c.Rect(6, 22, 36, 1, blueL);
        c.Rect(6, 24, 36, 1, gold);                       // gold stripe along the side
        // flag sticker on the door
        c.Rect(31, 22, 4, 1, Red); c.Rect(31, 23, 4, 1, H("f4f4f4")); c.Rect(31, 24, 4, 2, H("2a3f9a")); c.Rect(31, 26, 4, 1, H("f4f4f4"));
        // cabin (open sides, dark inside) + rear seat + rail
        c.Rect(5, 10, 27, 11, CabinDark);
        c.Rect(7, 17, 20, 4, seat);
        c.Rect(7, 12, 2, 6, H("4a2a1e"));
        c.Rect(6, 14, 9, 1, H("c9ced8"));
        c.Set(6, 15, H("c9ced8")); c.Set(14, 15, H("c9ced8"));
        // pillars
        c.Rect(4, 9, 1, 13, H("c9ced8"));
        c.Rect(30, 9, 1, 13, H("c9ced8"));
        // driver
        c.Rect(32, 15, 6, 6, shirt); c.Rect(32, 18, 6, 3, shirtD);
        c.Disc(35f, 12.5f, 2.6f, skin);
        c.Rect(32, 10, 6, 2, hair);                         // hair
        c.Rect(37, 13, 1, 1, Ink);                       // eye
        c.Rect(37, 15, 8, 2, skin);                        // arm to the handlebar
        // canopy
        c.Rect(2, 5, 39, 4, navy);
        c.Rect(4, 4, 34, 1, navyL);
        c.Rect(2, 9, 39, 1, navyD);
        c.Rect(6, 3, 26, 1, navyL);
        c.Set(34, 3, gold); c.Set(35, 3, gold); c.Set(34, 2, goldL);     // roof sign
        // marigold garland + amulet hanging at the front of the canopy
        for (int i = 0; i < 6; i++) { c.Set(38, 10 + i, i % 2 == 0 ? Orange : Yel); c.Set(39, 10 + i, i % 2 == 0 ? Yel : Orange); }
        c.Rect(41, 10, 2, 4, H("efe6d0"));
        // windshield pillar + front nose (yellow upper, blue lower)
        c.Rect(38, 8, 2, 9, gold);
        c.Rect(38, 17, 13, 5, gold);
        c.Rect(38, 17, 13, 1, goldL);
        c.Rect(38, 22, 13, 5, blue);
        c.Rect(38, 26, 13, 1, blueD);
        c.Set(38, 17, Clear); c.Set(38, 18, Clear);
        // handlebar
        c.Rect(43, 13, 5, 2, Rim);
        c.Rect(45, 14, 2, 4, Tire);
        // lights
        c.Rect(50, 19, 3, 4, Light);
        c.Rect(3, 21, 2, 3, Red);
        // exhaust
        c.Rect(0, 28, 4, 2, H("5a607a"));
        // wheels (animated)
        Wheel(c, 13, 28, 5, frame);
        Wheel(c, 46, 28, 5, frame);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Ghost(int style)
    {
        var c = new PixelCanvas(14, 20);
        var body = H("d8eeff", 215); var shade = H("a6c8f0", 215); var hair = H("e8e8f8", 230);
        c.Disc(6.5f, 7, 5.5f, body);
        c.Rect(1, 7, 11, 10, body);
        for (int x = 1; x < 12; x++)
        {
            int bottom = 16 + ((x % 4 < 2) ? 1 : 3);
            for (int y = 17; y <= bottom; y++) c.Set(x, y, body);
        }
        c.Rect(1, 12, 2, 4, shade);
        c.Rect(10, 12, 2, 4, shade);
        if (style == 0) { c.Disc(6.5f, 2f, 2.4f, hair); c.Rect(3, 3, 8, 2, hair); }       // bun (Sri)
        else { c.Rect(2, 1, 10, 3, H("e0455a", 235)); c.Rect(2, 4, 11, 1, H("b02a40", 235)); }  // cap (Ton)
        c.Rect(4, 7, 2, 2, Ink); c.Rect(8, 7, 2, 2, Ink);
        c.Rect(6, 11, 2, 1, Ink);
        return c;
    }

    static PixelCanvas Beam()
    {
        var c = new PixelCanvas(40, 14);
        for (int x = 0; x < 40; x++)
        {
            float t = x / 40f;
            int half = 1 + (int)(t * 6);
            byte a = (byte)(110 * (1 - t));
            for (int y = 7 - half; y <= 6 + half; y++) c.Set(x, y, new Color32(255, 243, 160, a));
        }
        return c;
    }

    static PixelCanvas CarPickup(Color32 body, Color32 dark)
    {
        var c = new PixelCanvas(60, 28);
        c.Rect(2, 12, 56, 9, body);
        c.Rect(2, 19, 56, 2, dark);
        c.Rect(30, 4, 22, 9, body);
        c.Rect(31, 3, 20, 1, body);
        c.Rect(33, 6, 8, 5, Glass); c.Rect(43, 6, 7, 5, Glass);
        c.Set(34, 6, GlassL); c.Set(44, 6, GlassL);
        c.Rect(3, 9, 26, 4, dark);
        c.Rect(3, 9, 26, 1, body);
        c.Rect(8, 6, 9, 4, H("c8a870"));     // sacks in the bed
        c.Rect(19, 7, 7, 3, H("b89860"));
        c.Rect(2, 13, 2, 3, Red);
        c.Rect(57, 14, 2, 3, Light);
        c.Rect(1, 19, 4, 2, Rim); c.Rect(56, 19, 3, 2, Rim);
        c.Set(58, 12, Clear);
        Wheel(c, 14, 23, 4, 0); Wheel(c, 47, 23, 4, 0);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas CarVan(Color32 body, Color32 accent)
    {
        var c = new PixelCanvas(56, 30);
        c.Rect(2, 5, 40, 19, body);
        c.Rect(42, 11, 12, 13, body);
        c.Set(42, 11, Clear); c.Set(43, 11, Clear);
        c.Rect(43, 12, 6, 6, Glass); c.Set(44, 12, GlassL);
        c.Rect(18, 8, 9, 6, Glass); c.Rect(30, 8, 9, 6, Glass);
        c.Set(19, 8, GlassL); c.Set(31, 8, GlassL);
        c.Rect(2, 18, 52, 2, accent);
        c.Rect(2, 22, 52, 2, H("8a8aa0"));
        c.Rect(2, 9, 2, 4, Red);
        c.Rect(53, 17, 2, 3, Light);
        c.Rect(4, 4, 36, 1, Rim);
        Wheel(c, 12, 25, 4, 0); Wheel(c, 44, 25, 4, 0);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas CarLorry(Color32 cab, Color32 tarp)
    {
        var c = new PixelCanvas(92, 44);
        var tarpD = PixelCanvas.Lerp(tarp, Ink, 0.35f);
        c.Rect(2, 30, 88, 5, H("3a3a4a"));
        c.Rect(2, 4, 60, 27, tarp);
        for (int y = 8; y < 30; y += 4) c.Rect(2, y, 60, 1, tarpD);
        for (int x = 8; x < 60; x += 14) c.Rect(x, 4, 1, 27, tarpD);
        c.Rect(2, 3, 60, 1, PixelCanvas.Lerp(tarp, Cream, 0.3f));
        c.Rect(64, 12, 26, 20, cab);
        c.Set(89, 12, Clear); c.Set(88, 12, Clear);
        c.Rect(78, 15, 10, 9, Glass); c.Set(79, 15, GlassL);
        c.Rect(68, 15, 8, 9, Glass); c.Set(69, 15, GlassL);
        c.Rect(66, 3, 2, 10, Rim);
        c.Rect(2, 24, 2, 4, Red);
        c.Rect(89, 26, 2, 4, Light);
        Wheel(c, 14, 37, 5, 0); Wheel(c, 27, 37, 5, 0); Wheel(c, 74, 37, 5, 0);
        c.Outline(Ink);
        return c;
    }

    /// <summary>Motorcycles that zip past as scenery: 0 = scooter, 1 = motorcycle taxi (orange vest), 2 = delivery bike (box on the back). Faces right.</summary>
    static PixelCanvas Bike(int variant)
    {
        var c = new PixelCanvas(40, 28);
        Color32[] body = { H("4a78c8"), H("e0455a"), H("3fa070") };
        Color32 paint = body[variant];
        Color32 paintD = PixelCanvas.Lerp(paint, Ink, 0.35f);
        Color32 shirt = variant == 1 ? H("ff8a1f") : (variant == 2 ? H("3fb86a") : H("f2c14e"));
        Color32 helmet = variant == 1 ? H("f0e6d0") : (variant == 2 ? H("e0455a") : H("fff3d6"));
        Color32 skin = H("d9a070");
        // delivery box on the back
        if (variant == 2) { c.Rect(1, 6, 12, 10, H("e8b84a")); c.Rect(1, 6, 12, 2, H("c48a2a")); c.Rect(3, 10, 8, 2, H("e0455a")); }
        // body and floorboard
        c.Rect(10, 17, 18, 4, paint);
        c.Rect(10, 20, 18, 1, paintD);
        c.Rect(12, 14, 11, 3, H("2a2a3a"));                 // seat
        c.Rect(26, 9, 3, 11, paint);                         // front shield
        c.Rect(26, 9, 3, 1, paintD);
        c.Rect(25, 7, 7, 2, H("2a2a3a"));                    // handlebar
        c.Rect(30, 14, 5, 2, Rim);                           // front fork
        // rider
        c.Rect(14, 4, 6, 10, shirt);
        c.Rect(14, 4, 6, 2, PixelCanvas.Lerp(shirt, Cream, 0.4f));
        if (variant == 1) { c.Rect(14, 8, 6, 1, Cream); c.Rect(14, 10, 6, 1, Cream); }
        c.Disc(18f, 2.5f, 3.2f, helmet);                    // helmet
        c.Rect(19, 2, 3, 2, skin);
        c.Rect(19, 1, 3, 1, H("2a2a3a"));                    // visor
        c.Rect(19, 8, 8, 2, shirt);                          // arm to the handlebar
        c.Rect(24, 8, 2, 2, skin);
        c.Rect(17, 13, 7, 3, H("3a3a6a"));                   // legs
        // lights
        c.Rect(36, 12, 3, 3, Light);
        c.Rect(0, 16, 2, 3, Red);
        Wheel(c, 9, 23, 4, 0); Wheel(c, 32, 23, 4, 1);
        c.Outline(Ink);
        return c;
    }

    /// <summary>Traffic light pole; the three lamps are separate sprites so they can change colour.</summary>
    static PixelCanvas SigPole()
    {
        var c = new PixelCanvas(16, 68);
        c.Rect(7, 22, 2, 46, H("6a6a82"));
        c.Rect(7, 22, 1, 46, H("8a8aa2"));
        c.Rect(5, 66, 6, 2, H("4a4a62"));
        c.Rect(2, 0, 12, 24, H("26263a"));
        c.Rect(2, 0, 12, 1, H("4a4a66"));
        foreach (int y in new[] { 5, 12, 19 })
        {
            c.Disc(8f, y, 3.6f, H("0e0e1e"));
            c.Rect(4, y - 5, 8, 1, H("4a4a66"));              // little hood
        }
        c.Rect(13, 6, 1, 14, H("4a4a66"));
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas SigLamp()
    {
        var c = new PixelCanvas(7, 7);
        c.Disc(3f, 3f, 3f, Cream);
        c.Set(2, 2, Cream);
        return c;
    }

    static PixelCanvas Buffalo()
    {
        var c = new PixelCanvas(60, 36);
        var body = H("4a5878"); var dark = H("36425e"); var light = H("66789c");
        // legs
        foreach (int lx in new[] { 10, 18, 34, 42 }) c.Rect(lx, 25, 5, 10, dark);
        foreach (int lx in new[] { 10, 18, 34, 42 }) c.Rect(lx, 33, 5, 2, H("8a7a68"));
        // body
        c.Ellipse(27, 20, 22, 10, body);
        c.Ellipse(36, 13, 9, 6, body);              // shoulder hump
        c.Ellipse(24, 14, 14, 4, light);
        // head
        c.Ellipse(51, 23, 7, 5, body);
        c.Ellipse(55, 26, 4, 3, H("7a8aac"));        // muzzle
        c.Rect(56, 25, 2, 1, Ink);
        c.Rect(49, 21, 2, 2, Cream); c.Set(50, 21, Ink);
        // horns sweeping back
        var horn = H("e8e0c8");
        c.Line(48, 19, 43, 13, horn); c.Line(43, 13, 38, 12, horn); c.Line(38, 12, 36, 14, horn);
        c.Line(49, 19, 44, 14, H("c8c0a8"));
        // tail
        c.Line(5, 15, 2, 24, dark); c.Rect(1, 24, 3, 4, dark);
        // mud belly
        c.Rect(8, 27, 38, 2, H("5a4a40"));
        c.Outline(Ink);
        return c;
    }

    // ---- roadside props -------------------------------------------------------------------
    static PixelCanvas Sala()
    {
        var c = new PixelCanvas(48, 38);
        var roof = H("d1503c"); var roofL = H("ee7a5a"); var wood = H("7a4a30"); var woodD = H("5a3422");
        // roof (gable seen from the side, with up-curved eaves)
        for (int i = 0; i < 10; i++) c.Rect(2 + i * 2, 12 - i, 44 - i * 4, 1, i % 2 == 0 ? roof : roofL);
        c.Rect(0, 9, 3, 2, roof); c.Rect(45, 9, 3, 2, roof);
        c.Rect(4, 13, 40, 1, H("2f8f6a"));
        // posts
        c.Rect(6, 14, 3, 21, wood); c.Rect(39, 14, 3, 21, wood);
        // floor
        c.Rect(3, 33, 42, 4, woodD); c.Rect(3, 33, 42, 1, wood);
        // bench
        c.Rect(10, 26, 28, 2, wood); c.Rect(12, 28, 2, 5, woodD); c.Rect(34, 28, 2, 5, woodD);
        // hanging lantern
        c.Rect(23, 14, 1, 4, Ink); c.Disc(23.5f, 20, 2.5f, H("ffd36a")); c.Disc(23.5f, 20, 1f, Cream);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Shrine()
    {
        var c = new PixelCanvas(28, 44);
        var gold = H("e8b84a"); var goldD = H("b88a2a"); var white = H("f0e6d0");
        c.Rect(12, 22, 4, 22, white);                // pole
        c.Rect(12, 22, 1, 22, H("c8c0a8"));
        c.Rect(4, 20, 20, 3, goldD); c.Rect(4, 20, 20, 1, gold);        // platform
        c.Rect(7, 11, 14, 9, white);                  // house
        c.Rect(7, 11, 14, 1, gold);
        c.Rect(11, 14, 6, 6, H("3a2a4a"));            // door
        for (int i = 0; i < 5; i++) c.Rect(5 + i * 2 - 0, 10 - i, 18 - i * 4, 1, i % 2 == 0 ? Red : H("b02a40"));
        c.Rect(3, 9, 2, 1, Red); c.Rect(23, 9, 2, 1, Red);     // chofa tips
        c.Rect(13, 4, 2, 2, gold);
        // offerings
        c.Rect(5, 16, 2, 4, Red);                     // red soda bottle
        c.Rect(20, 17, 3, 3, PinkL); c.Rect(21, 16, 1, 1, Yel);   // flowers
        c.Rect(8, 18, 3, 2, Cream);                   // rice bowl
        // incense
        c.Rect(18, 13, 1, 7, H("8a4a2a")); c.Set(18, 12, Orange);
        c.Rect(16, 14, 1, 6, H("8a4a2a")); c.Set(16, 13, Orange);
        // garland
        for (int x = 8; x < 20; x++) c.Set(x, 10 + ((x % 2 == 0) ? 0 : 1) + 7, x % 3 == 0 ? Orange : Yel);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Banyan()
    {
        var c = new PixelCanvas(72, 92);
        var bark = H("5a3f34"); var barkL = H("7a5a48");
        var leaf = H("1e5a46"); var leafL = H("2b7a58"); var leafD = H("163f34");
        // trunk with flared base and aerial roots
        c.Rect(30, 44, 12, 46, bark);
        c.Rect(30, 44, 3, 46, barkL);
        for (int i = 0; i < 10; i++) { c.Line(30 - i / 2, 80 + i, 24 - i, 91, bark); c.Line(41 + i / 2, 80 + i, 48 + i, 91, bark); }
        c.Rect(22, 86, 28, 5, bark);
        foreach (int rx in new[] { 17, 22, 52, 56 }) c.Line(rx, 36, rx - 1, 70 + (rx % 5), barkL);
        // canopy
        c.Disc(36, 28, 22, leafD);
        c.Disc(22, 32, 14, leafD); c.Disc(50, 32, 14, leafD);
        c.Disc(36, 24, 20, leaf);
        c.Disc(20, 30, 12, leaf); c.Disc(52, 30, 12, leaf);
        c.Disc(30, 18, 11, leafL); c.Disc(46, 22, 9, leafL); c.Disc(18, 26, 6, leafL);
        // sacred ribbons wrapped on the trunk
        var cols = new[] { Red, Pink, H("4a78c8"), H("3fb86a"), Yel, Orange };
        for (int i = 0; i < 6; i++)
        {
            int x = 29 + i * 2;
            int len = 10 + (i * 7) % 9;
            for (int y = 0; y < len; y++) c.Set(x + (y > 5 ? 1 : 0), 50 + y, cols[i]);
        }
        c.Rect(30, 56, 12, 2, Red); c.Rect(30, 59, 12, 1, Cream); c.Rect(30, 61, 12, 2, H("3fb86a"));
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Palm()
    {
        var c = new PixelCanvas(34, 72);
        var trunk = H("6a4a38"); var trunkL = H("8a6a50"); var leaf = H("2b7a58"); var leafD = H("1e5a46");
        for (int y = 0; y < 60; y++)
        {
            int x = 15 + (int)(Math.Sin(y * 0.09) * 2.5);
            c.Rect(x, 71 - y, 3, 1, (y % 6 < 2) ? trunkL : trunk);
        }
        int tx = 17, ty = 12;
        c.Line(tx, ty, 3, ty + 4, leaf); c.Line(tx, ty, 32, ty + 4, leaf);
        c.Line(tx, ty, 6, ty - 5, leaf); c.Line(tx, ty, 29, ty - 5, leaf);
        c.Line(tx, ty, 14, ty - 8, leaf); c.Line(tx, ty, 21, ty - 8, leaf);
        c.Line(tx, ty + 1, 7, ty + 9, leafD); c.Line(tx, ty + 1, 27, ty + 9, leafD);
        c.Disc(tx - 1, ty + 3, 2, H("8a5a2a")); c.Disc(tx + 3, ty + 3, 2, H("8a5a2a"));
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Pole()
    {
        var c = new PixelCanvas(14, 76);
        var wood = H("6a4a38");
        c.Rect(6, 2, 2, 74, wood);
        c.Rect(6, 2, 1, 74, H("8a6a50"));
        c.Rect(0, 8, 14, 2, wood);
        c.Rect(1, 6, 2, 2, H("e8e0c8")); c.Rect(11, 6, 2, 2, H("e8e0c8"));
        c.Rect(3, 18, 8, 2, wood);
        c.Rect(4, 34, 6, 8, H("3a3a52")); c.Rect(5, 35, 4, 2, H("8a8aa8"));    // transformer box
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Bush()
    {
        var c = new PixelCanvas(24, 14);
        var leaf = H("2b7a58"); var leafL = H("3fa070"); var leafD = H("1e5a46");
        c.Disc(7, 9, 5, leafD); c.Disc(16, 9, 5.5f, leafD); c.Disc(11.5f, 6, 6, leaf);
        c.Disc(9, 5, 3, leafL); c.Set(18, 5, PinkL); c.Set(5, 8, Yel); c.Set(14, 3, PinkL);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Shop()
    {
        var c = new PixelCanvas(64, 44);
        var wall = H("d8c8a0"); var wallD = H("b8a880"); var roofC = H("6a7a8a");
        c.Rect(4, 14, 56, 28, wall);
        c.Rect(4, 38, 56, 4, wallD);
        // corrugated roof
        c.Rect(0, 8, 64, 6, roofC);
        for (int x = 0; x < 64; x += 3) c.Rect(x, 8, 1, 6, H("8a9aaa"));
        // striped awning
        for (int x = 4; x < 60; x += 4)
        {
            c.Rect(x, 14, 2, 5, Red); c.Rect(x + 2, 14, 2, 5, Cream);
        }
        // sign board
        c.Rect(18, 3, 28, 7, H("3fa070")); c.Rect(19, 4, 26, 5, H("4fb888"));
        for (int x = 22; x < 42; x += 4) c.Rect(x, 5, 2, 3, Cream);
        // window / counter, lit
        c.Rect(10, 22, 22, 12, H("ffd36a")); c.Rect(10, 22, 22, 1, H("c88a2a")); c.Rect(10, 34, 22, 2, wallD);
        c.Rect(12, 26, 3, 5, Red); c.Rect(17, 25, 3, 6, H("4a78c8")); c.Rect(22, 27, 3, 4, Yel);   // goods
        // door
        c.Rect(38, 22, 14, 20, H("3a2a4a")); c.Rect(40, 24, 10, 16, H("5a4a7a"));
        c.Rect(36, 30, 2, 12, wallD);
        // hanging snack packets
        for (int x = 8; x < 34; x += 5) c.Rect(x, 19, 2, 3, x % 2 == 0 ? Orange : PinkL);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Gas()
    {
        var c = new PixelCanvas(84, 56);
        var orange = H("e8803a");
        // canopy
        c.Rect(2, 8, 80, 7, orange);
        for (int x = 2; x < 82; x += 8) c.Rect(x, 8, 4, 7, Cream);
        c.Rect(2, 15, 80, 2, H("8a4a2a"));
        // pillars
        c.Rect(10, 17, 4, 37, H("d0d0e0")); c.Rect(70, 17, 4, 37, H("d0d0e0"));
        // lights under canopy
        for (int x = 20; x < 66; x += 12) c.Rect(x, 17, 4, 2, Light);
        // pump
        c.Rect(32, 28, 14, 26, Red); c.Rect(33, 30, 12, 7, H("2a2a4a")); c.Rect(34, 31, 6, 2, H("7aff9a"));
        c.Rect(34, 40, 10, 2, H("8a1a2a"));
        c.Rect(46, 34, 2, 14, Ink); c.Rect(46, 46, 4, 2, Ink);          // hose
        // second pump
        c.Rect(52, 28, 14, 26, H("4a78c8")); c.Rect(53, 30, 12, 7, H("2a2a4a")); c.Rect(54, 31, 6, 2, H("7aff9a"));
        // ground
        c.Rect(0, 52, 84, 4, H("5a5a72"));
        // price sign on a pole
        c.Rect(77, 20, 2, 32, H("8a8aa8"));
        c.Disc(78, 18, 6, H("ffd36a")); c.Disc(78, 18, 3, orange);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Temple()
    {
        var c = new PixelCanvas(120, 130);
        var wall = H("f2e6c4"); var roofRed = H("d1503c"); var roofL = H("ee7a5a"); var green = H("2f8f6a");
        var gold = H("f2c14e"); var goldD = H("c48a2a"); var hill = H("234a3c"); var hillL = H("2f6a50");
        // glowing hill
        c.Ellipse(60, 128, 60, 20, hill);
        c.Ellipse(60, 126, 54, 12, hillL);
        // chedi (right)
        c.Rect(78, 104, 34, 14, goldD);
        c.Rect(78, 104, 34, 2, gold);
        for (int i = 0; i < 30; i++)
        {
            int half = (int)(14 - i * 0.35f);
            c.Rect(95 - half, 74 + i, half * 2 + 1, 1, (i % 5 == 0) ? goldD : gold);
        }
        for (int i = 0; i < 14; i++) c.Rect(94 - (14 - i) / 4, 60 + i, ((14 - i) / 4) * 2 + 2, 1, gold);
        c.Rect(94, 36, 2, 26, gold);
        c.Rect(93, 32, 4, 5, goldD);
        c.Rect(95, 24, 1, 10, gold);
        // hall (left)
        c.Rect(10, 88, 56, 32, wall);
        c.Rect(10, 88, 56, 2, H("c8b890"));
        for (int x = 18; x < 60; x += 14) { c.Rect(x, 94, 8, 14, H("c88a2a")); c.Rect(x + 1, 95, 6, 12, H("ffd36a")); }
        // two tiered roofs
        for (int i = 0; i < 12; i++) c.Rect(6 + i, 76 + i, 64 - i * 2, 1, i % 3 == 0 ? roofL : roofRed);
        c.Rect(4, 74, 5, 3, roofRed); c.Rect(67, 74, 5, 3, roofRed);
        c.Rect(8, 88, 60, 2, green);
        for (int i = 0; i < 10; i++) c.Rect(16 + i, 62 + i, 44 - i * 2, 1, i % 3 == 0 ? roofL : roofRed);
        c.Rect(14, 60, 5, 3, roofRed); c.Rect(57, 60, 5, 3, roofRed);
        c.Rect(18, 72, 40, 2, green);
        c.Rect(36, 52, 4, 10, gold);
        // stairs and naga rails
        for (int i = 0; i < 6; i++) c.Rect(34 - i * 2, 120 + i, 20 + i * 4, 1, i % 2 == 0 ? H("d8c8a8") : H("b8a880"));
        c.Line(26, 120, 18, 126, green); c.Line(62, 120, 70, 126, green);
        // lanterns
        foreach (int lx in new[] { 8, 70, 74 }) { c.Rect(lx, 100, 1, 4, Ink); c.Disc(lx + 0.5f, 106, 2.4f, H("ff8a3a")); }
        c.Outline(Ink);
        return c;
    }

    /// <summary>Closed rice-curry street cart with a striped umbrella, a red lantern and a plastic stool.</summary>
    static PixelCanvas Cart()
    {
        var c = new PixelCanvas(48, 34);
        var wood = H("8a5a3a"); var woodD = H("5a3a24");
        // umbrella
        for (int x = 0; x < 40; x++) { c.Set(x, 5, x % 6 < 3 ? Red : Cream); c.Set(x, 6, x % 6 < 3 ? Red : Cream); }
        c.Rect(2, 3, 36, 2, Red); c.Rect(6, 1, 28, 2, Red);
        c.Rect(19, 7, 2, 16, H("c8c0a8"));
        // glass display
        c.Rect(4, 12, 28, 9, H("6a8ab8")); c.Rect(5, 13, 26, 7, H("a8c8e8"));
        c.Rect(7, 16, 5, 3, Orange); c.Rect(14, 16, 5, 3, Yel); c.Rect(21, 16, 5, 3, PinkL);
        // cart body
        c.Rect(3, 21, 30, 8, wood); c.Rect(3, 27, 30, 2, woodD);
        c.Rect(36, 21, 6, 2, woodD);                                        // push handle
        // wheels
        Wheel(c, 9, 30, 3, 0); Wheel(c, 27, 30, 3, 0);
        // lantern and stool
        c.Rect(36, 7, 1, 4, Ink); c.Disc(36.5f, 13, 3f, Red); c.Disc(36.5f, 13, 1f, Light);
        c.Rect(38, 25, 7, 2, H("e0455a")); c.Rect(39, 27, 1, 5, H("8a8aa0")); c.Rect(43, 27, 1, 5, H("8a8aa0"));
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Marker()
    {
        var c = new PixelCanvas(18, 22);
        c.Rect(1, 1, 16, 15, Cream);
        c.Set(1, 1, Clear); c.Set(16, 1, Clear); c.Set(1, 15, Clear); c.Set(16, 15, Clear);
        // tail
        c.Rect(7, 16, 4, 2, Cream); c.Rect(8, 18, 2, 2, Cream);
        // exclamation mark
        c.Rect(8, 3, 2, 7, Red);
        c.Rect(8, 12, 2, 2, Red);
        c.Outline(Ink);
        return c;
    }

    // ---- UI --------------------------------------------------------------------------------
    static PixelCanvas Panel()
    {
        var c = new PixelCanvas(24, 24);
        var fill = H("151236", 238); var border = H("f2e6c4"); var inner = H("6a5aa8");
        c.Fill(fill);
        for (int i = 0; i < 24; i++)
        {
            c.Set(i, 0, border); c.Set(i, 1, border); c.Set(i, 22, border); c.Set(i, 23, border);
            c.Set(0, i, border); c.Set(1, i, border); c.Set(22, i, border); c.Set(23, i, border);
            c.Set(i, 2, inner); c.Set(i, 21, inner); c.Set(2, i, inner); c.Set(21, i, inner);
        }
        // notched corners
        foreach (var p in new[] { new[] { 0, 0 }, new[] { 23, 0 }, new[] { 0, 23 }, new[] { 23, 23 } })
            c.Set(p[0], p[1], Clear);
        return c;
    }

    static PixelCanvas Arrow()
    {
        var c = new PixelCanvas(8, 9);
        for (int y = 0; y < 9; y++)
        {
            int len = 4 - Mathf.Abs(y - 4);
            for (int x = 0; x < len + 3; x++) c.Set(x, y, H("ffe39a"));
        }
        return c;
    }

    static PixelCanvas IconFuel()
    {
        var c = new PixelCanvas(11, 11);
        c.Rect(1, 3, 7, 8, Red); c.Rect(2, 4, 5, 2, H("ff9a9a"));
        c.Rect(2, 1, 3, 2, Rim); c.Rect(5, 1, 3, 2, Rim);
        c.Rect(8, 5, 2, 1, Rim); c.Rect(9, 5, 1, 4, Rim);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas IconCalm()
    {
        var c = new PixelCanvas(11, 11);
        c.Ellipse(5, 6, 3.5f, 4.5f, H("9ad8ff"));
        c.Ellipse(5, 6, 1.5f, 2.5f, H("f0fbff"));
        c.Rect(4, 9, 3, 1, H("9ad8ff"));
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas IconMoon()
    {
        var c = new PixelCanvas(11, 11);
        c.Disc(5, 5, 4.5f, H("f6efc8"));
        c.Disc(7, 4, 3.6f, Clear);
        c.Outline(Ink);
        return c;
    }

    static PixelCanvas Portrait(Color32 skin, Color32 hair, Color32 shirt, Color32 accent, int style, bool ghost)
    {
        var c = new PixelCanvas(48, 48);
        var skinD = PixelCanvas.Lerp(skin, Ink, 0.18f);
        // shoulders and shirt
        c.Ellipse(24, 52, 22, 16, shirt);
        c.Rect(20, 36, 8, 6, skinD);
        c.Rect(20, 40, 8, 1, PixelCanvas.Lerp(skinD, Ink, 0.3f));
        // head
        c.Ellipse(24, 22, 12, 14, skin);
        c.Disc(11.5f, 23, 2.5f, skin); c.Disc(36.5f, 23, 2.5f, skin);
        // hair styles
        for (int y = 0; y < 48; y++)
            for (int x = 0; x < 48; x++)
            {
                float dx = (x - 24) / 12.5f, dy = (y - 22) / 14.5f;
                bool inHead = dx * dx + dy * dy <= 1.02f;
                if (!inHead) continue;
                int cap = style == 2 || style == 3 ? 15 : 14;
                if (y < cap) c.Set(x, y, style == 2 ? H("c83a4a") : (style == 3 ? H("3a68b8") : hair));
                if ((style == 0 || style == 4) && y < 21 && (x < 14 || x > 34)) c.Set(x, y, hair);
            }
        if (style == 1) { c.Disc(24, 5.5f, 5.5f, hair); c.Rect(17, 8, 14, 3, hair); }
        if (style == 2 || style == 3)
        {
            var capC = style == 2 ? H("c83a4a") : H("3a68b8");
            c.Rect(10, 14, 28, 3, capC);
            c.Rect(8, 16, 16, 2, PixelCanvas.Lerp(capC, Ink, 0.3f));
            c.Rect(18, 8, 12, 2, accent);
        }
        if (style == 4) { c.Disc(24, 4, 4, hair); c.Rect(14, 14, 20, 2, accent); }
        // face
        c.Rect(17, 23, 4, 4, Ink); c.Rect(27, 23, 4, 4, Ink);
        c.Rect(18, 23, 1, 1, Cream); c.Rect(28, 23, 1, 1, Cream);
        c.Rect(16, 20, 6, 1, PixelCanvas.Lerp(hair, Ink, 0.4f)); c.Rect(26, 20, 6, 1, PixelCanvas.Lerp(hair, Ink, 0.4f));
        c.Rect(21, 33, 6, 1, H("a8483a"));
        c.Set(20, 32, H("a8483a")); c.Set(27, 32, H("a8483a"));
        c.Rect(14, 28, 3, 2, PixelCanvas.Lerp(skin, Red, 0.35f)); c.Rect(31, 28, 3, 2, PixelCanvas.Lerp(skin, Red, 0.35f));
        if (style == 3 || style == 2) c.Rect(19, 30, 10, 2, PixelCanvas.Lerp(hair, Ink, 0.2f));   // moustache
        if (style == 2) { for (int i = 0; i < 9; i++) c.Set(17 + i * 2, 35 + (i % 2), H("7a7a98")); }   // stubble
        // clothing details
        if (style == 2)
        {
            c.Rect(14, 44, 3, 4, accent); c.Rect(31, 44, 3, 4, accent);
            c.Rect(14, 44, 3, 1, Cream); c.Rect(31, 44, 3, 1, Cream);
        }
        if (style == 0) c.Rect(18, 42, 12, 2, accent);
        if (style == 4) c.Rect(14, 44, 20, 2, accent);
        if (ghost)
        {
            // ghost: fade the bottom into wisps and lower the alpha
            for (int y = 0; y < 48; y++)
                for (int x = 0; x < 48; x++)
                {
                    var p = c.Get(x, y);
                    if (p.a == 0) continue;
                    double tail = 38 + 4 * Math.Sin(x * 0.55) + (x % 3);
                    if (y > tail) { c.Set(x, y, Clear); continue; }
                    float a = y > 30 ? Mathf.Lerp(1f, 0.35f, (y - 30) / 10f) : 1f;
                    c.Set(x, y, new Color32(p.r, p.g, p.b, (byte)(p.a * a * 0.92f)));
                }
        }
        c.Outline(Ink);
        return c;
    }

    // ---- cut-scene backdrops ----------------------------------------------------------------
    static PixelCanvas CutBg(string kind)
    {
        const int W = 240, Hh = 135;
        var c = new PixelCanvas(W, Hh);
        bool dawn = kind == "temple";
        var sky = Sky(dawn);
        for (int y = 0; y < Hh; y++)
            for (int x = 0; x < W; x++) c.Set(x, y, sky.Get(x % 16, y));
        if (!dawn)
        {
            var st = Stars(11, 120);
            for (int y = 0; y < Hh; y++)
                for (int x = 0; x < W; x++) { var p = st.Get(x, y); if (p.a > 0) c.Over(x, y, p); }
            c.Blit(Moon(), 186, 14);
        }
        c.Blit(Mountains(H("1c2352"), H("2c3676"), 34, 16, 5, 72), -10, 2);
        c.Blit(Hills(), -4, 20);
        // ground: kept high on the canvas so the dialogue box (bottom ~34%) never hides the action
        c.Rect(0, 72, W, 63, H("3b3b52"));
        c.Rect(0, 64, W, 6, H("2c5a4a"));
        c.Rect(0, 70, W, 2, H("5a4a45"));
        c.Rect(0, 72, W, 1, H("7a7a92"));
        for (int x = 0; x < W; x++) if ((x % 32) < 14) c.Set(x, 99, H("e9d27a"));

        PixelCanvas prop = null; int px = 130; bool half = false;
        switch (kind)
        {
            case "sala": prop = Sala(); px = 120; break;
            case "shrine": prop = Shrine(); px = 130; break;
            case "banyan": prop = Banyan(); px = 135; half = true; break;
            case "gas": prop = Gas(); px = 120; break;
            case "shop": prop = Shop(); px = 125; break;
            case "temple": prop = Temple(); px = 120; half = true; break;
        }
        if (prop != null)
        {
            if (half) c.BlitHalf(prop, px - prop.w / 4, 69 - prop.h / 2);
            else c.Blit(prop, px - prop.w / 2, 69 - prop.h);
        }
        // our tuk-tuk, parked on the left
        c.Blit(Tuk(0), 16, 86 - 34);
        return c;
    }
}
