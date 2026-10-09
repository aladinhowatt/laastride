using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Tiny helpers to build the uGUI hierarchy from code (reference canvas 960x540 = 4x the 240x135 art).</summary>
public static class UIKit
{
    public static readonly Color Cream = new Color(0.98f, 0.93f, 0.78f, 1f);
    public static readonly Color Gold = new Color(1f, 0.82f, 0.38f, 1f);

    public static RectTransform Rt(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = anchor; r.anchorMax = anchor; r.pivot = pivot;
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    public static void Stretch(RectTransform r, float l = 0, float b = 0, float rr = 0, float t = 0)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -t);
    }

    public static Image Img(string name, Transform parent, Sprite s, Color c, bool sliced = false)
    {
        var r = Rt(name, parent);
        var im = r.gameObject.AddComponent<Image>();
        im.sprite = s;
        im.color = c;
        im.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        im.raycastTarget = false;
        if (s == null) im.sprite = null;
        return im;
    }

    public static TextMeshProUGUI Txt(string name, Transform parent, string text, float size, Color c,
                                      TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        var r = Rt(name, parent);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = ThaiFont.Get();
        t.text = ThaiText.Fix(text);
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        t.richText = true;
        return t;
    }

    public static Sprite WhiteSprite()
    {
        if (white == null)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels32(new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) });
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }
        return white;
    }
    static Sprite white;
}
