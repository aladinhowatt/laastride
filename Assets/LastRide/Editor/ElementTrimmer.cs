using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scenery pieces come from the image generator as big transparent PNGs with margins and soft glow halos.
/// This crops each one to its visible pixels (bottom edge exactly at the ground) and drops the faint halo,
/// so the sprite rect is the piece itself. Safe to run repeatedly.
/// </summary>
public static class ElementTrimmer
{
    const string Dir = "Assets/LastRide/Resources/LastRide/AI/Elements";
    const byte Cut = 72;

    [MenuItem("Tools/Last Ride/Trim Scenery Elements")]
    public static void TrimAll()
    {
        if (!Directory.Exists(Dir)) return;
        int changed = 0;
        foreach (var f in Directory.GetFiles(Dir, "*.png"))
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(f));
            int w = t.width, h = t.height;
            var px = t.GetPixels32();
            int cleared = 0, minX = w, maxX = -1, minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (px[i].a < Cut) { if (px[i].a != 0) { px[i] = default(Color32); cleared++; } continue; }
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            Object.DestroyImmediate(t);
            if (maxX < 0) { Debug.LogWarning("[LastRide] empty element " + f); continue; }

            int x0 = Mathf.Max(0, minX - 1), x1 = Mathf.Min(w - 1, maxX + 1);
            int y0 = minY, y1 = Mathf.Min(h - 1, maxY + 1);                 // bottom edge exactly on the ground
            int cw = x1 - x0 + 1, ch = y1 - y0 + 1;
            if (cw == w && ch == h && cleared == 0) continue;

            var o = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
            var outPx = new Color32[cw * ch];
            for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                    outPx[y * cw + x] = px[(y + y0) * w + (x + x0)];
            o.SetPixels32(outPx);
            File.WriteAllBytes(f, o.EncodeToPNG());
            Object.DestroyImmediate(o);
            changed++;
            Debug.Log("[LastRide] trimmed " + Path.GetFileName(f) + " " + w + "x" + h + " -> " + cw + "x" + ch + " (halo px cleared: " + cleared + ")");
        }
        if (changed > 0) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }
}
