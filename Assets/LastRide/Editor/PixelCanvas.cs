using System;
using UnityEngine;

/// <summary>Tiny CPU pixel painter used by the art generator. Origin is TOP-LEFT (y grows downward).</summary>
public class PixelCanvas
{
    public readonly int w, h;
    public readonly Color32[] px;

    public PixelCanvas(int w, int h)
    {
        this.w = w; this.h = h;
        px = new Color32[w * h];
    }

    public static Color32 Hex(string hex, byte a = 255)
    {
        hex = hex.TrimStart('#');
        return new Color32(Convert.ToByte(hex.Substring(0, 2), 16),
                           Convert.ToByte(hex.Substring(2, 2), 16),
                           Convert.ToByte(hex.Substring(4, 2), 16), a);
    }

    public static Color32 Lerp(Color32 a, Color32 b, float t)
    {
        return new Color32((byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                           (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                           (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                           (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
    }

    public bool In(int x, int y) { return x >= 0 && y >= 0 && x < w && y < h; }

    public void Set(int x, int y, Color32 c) { if (In(x, y)) px[y * w + x] = c; }

    public Color32 Get(int x, int y) { return In(x, y) ? px[y * w + x] : default(Color32); }

    /// <summary>Alpha-composite a pixel over what is already there.</summary>
    public void Over(int x, int y, Color32 c)
    {
        if (!In(x, y) || c.a == 0) return;
        if (c.a == 255) { px[y * w + x] = c; return; }
        var d = px[y * w + x];
        float a = c.a / 255f;
        float da = d.a / 255f;
        float oa = a + da * (1 - a);
        if (oa <= 0) return;
        byte r = (byte)Mathf.RoundToInt((c.r * a + d.r * da * (1 - a)) / oa);
        byte g = (byte)Mathf.RoundToInt((c.g * a + d.g * da * (1 - a)) / oa);
        byte b = (byte)Mathf.RoundToInt((c.b * a + d.b * da * (1 - a)) / oa);
        px[y * w + x] = new Color32(r, g, b, (byte)Mathf.RoundToInt(oa * 255));
    }

    public void Fill(Color32 c) { for (int i = 0; i < px.Length; i++) px[i] = c; }

    public void Rect(int x, int y, int rw, int rh, Color32 c)
    {
        for (int j = 0; j < rh; j++)
            for (int i = 0; i < rw; i++)
                Set(x + i, y + j, c);
    }

    public void Line(int x0, int y0, int x1, int y1, Color32 c)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            Set(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public void Disc(float cx, float cy, float r, Color32 c)
    {
        int x0 = Mathf.FloorToInt(cx - r - 1), x1 = Mathf.CeilToInt(cx + r + 1);
        int y0 = Mathf.FloorToInt(cy - r - 1), y1 = Mathf.CeilToInt(cy + r + 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= r * r + 0.25f) Set(x, y, c);
            }
    }

    public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
    {
        int x0 = Mathf.FloorToInt(cx - rx - 1), x1 = Mathf.CeilToInt(cx + rx + 1);
        int y0 = Mathf.FloorToInt(cy - ry - 1), y1 = Mathf.CeilToInt(cy + ry + 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1.02f) Set(x, y, c);
            }
    }

    /// <summary>Draw a 1px outline around every opaque pixel (4-neighbourhood) on transparent pixels.</summary>
    public void Outline(Color32 c)
    {
        var snap = (Color32[])px.Clone();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (snap[y * w + x].a != 0) continue;
                bool n = (x > 0 && snap[y * w + x - 1].a > 0) || (x < w - 1 && snap[y * w + x + 1].a > 0) ||
                         (y > 0 && snap[(y - 1) * w + x].a > 0) || (y < h - 1 && snap[(y + 1) * w + x].a > 0);
                if (n) px[y * w + x] = c;
            }
    }

    public void Blit(PixelCanvas s, int ox, int oy, bool flipX = false)
    {
        for (int y = 0; y < s.h; y++)
            for (int x = 0; x < s.w; x++)
            {
                var p = s.px[y * s.w + (flipX ? s.w - 1 - x : x)];
                if (p.a == 0) continue;
                Over(ox + x, oy + y, p);
            }
    }

    /// <summary>Blit at half size (nearest: first opaque pixel of each 2x2 block).</summary>
    public void BlitHalf(PixelCanvas s, int ox, int oy)
    {
        for (int y = 0; y < s.h / 2; y++)
            for (int x = 0; x < s.w / 2; x++)
            {
                Color32 p = default(Color32);
                for (int k = 0; k < 4 && p.a == 0; k++)
                    p = s.px[(y * 2 + (k >> 1)) * s.w + x * 2 + (k & 1)];
                if (p.a == 0) continue;
                Over(ox + x, oy + y, p);
            }
    }

    public Texture2D ToTexture()
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var flipped = new Color32[px.Length];
        for (int y = 0; y < h; y++)
            Array.Copy(px, y * w, flipped, (h - 1 - y) * w, w);
        t.SetPixels32(flipped);
        t.Apply();
        return t;
    }
}
