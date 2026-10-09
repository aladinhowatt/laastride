using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One Sprites/Default material per texture. Sharing a single runtime Material across hundreds of
/// SpriteRenderers made some of them sample the wrong texture in the Game view (everything drew
/// the sky gradient except sprites re-assigned every frame), so every sprite now binds its own texture.
/// </summary>
public static class SpriteMats
{
    static Shader shader;
    static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

    static Shader Sh()
    {
        if (shader == null) shader = Shader.Find("Sprites/Default");
        return shader;
    }

    public static Material For(Sprite s)
    {
        if (s == null || Sh() == null) return null;
        var tex = s.texture;
        int id = tex.GetInstanceID();
        Material m;
        if (cache.TryGetValue(id, out m) && m != null) return m;
        m = new Material(Sh()) { name = "SpriteMat_" + tex.name };
        m.mainTexture = tex;
        cache[id] = m;
        return m;
    }

    /// <summary>Set the material first, then the sprite.</summary>
    public static void Apply(SpriteRenderer sr, Sprite s)
    {
        var m = For(s);
        if (m != null) sr.sharedMaterial = m;
        sr.sprite = s;
    }

    public static void Clear() { cache.Clear(); }
}
