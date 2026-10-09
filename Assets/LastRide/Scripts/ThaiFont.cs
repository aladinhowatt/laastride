using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Thai-capable TextMeshPro font. Preference order:
///  1. Resources/LastRide/ThaiFontAsset.asset  (made by the builder from a .ttf in Assets/LastRide/Fonts)
///  2. a dynamic font loaded straight from an installed Windows font file (Leelawadee UI / Tahoma)
///  3. TMP's default font (no Thai glyphs, but the game still runs)
/// </summary>
public static class ThaiFont
{
    static TMP_FontAsset cached;

    static readonly string[] WindowsCandidates =
    {
        @"C:\Windows\Fonts\LeelawUI.ttf",
        @"C:\Windows\Fonts\tahoma.ttf",
        @"C:\Windows\Fonts\segoeui.ttf",
        @"C:\Windows\Fonts\cordia.ttf",
    };

    public static TMP_FontAsset Get()
    {
        if (cached != null) return cached;
        cached = Resources.Load<TMP_FontAsset>("LastRide/ThaiFontAsset");
        if (cached == null)
        {
            foreach (var path in WindowsCandidates)
            {
                if (!File.Exists(path)) continue;
                var fa = TMP_FontAsset.CreateFontAsset(path, 0, 72, 6, GlyphRenderMode.SDFAA, 1024, 1024);
                if (fa != null) { fa.name = "ThaiFontRuntime"; cached = fa; break; }
            }
        }
        if (cached == null)
        {
            Debug.LogWarning("[LastRide] No Thai font found. Drop a Thai .ttf into Assets/LastRide/Fonts and run Tools/Last Ride/Build Scene.");
            cached = TMP_Settings.defaultFontAsset;
        }
        return cached;
    }
}
