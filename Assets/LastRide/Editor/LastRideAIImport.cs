using UnityEditor;
using UnityEngine;

/// <summary>
/// Images generated with Higgsfield (Assets/LastRide/Resources/LastRide/AI/*.png) are full-colour illustrations,
/// not 16-px-grid sprites: import them as smooth Sprites, uncompressed, centred pivot.
/// </summary>
public class LastRideAIImport : AssetPostprocessor
{
    const string Folder = "/LastRide/Resources/LastRide/AI/";

    void OnPreprocessTexture()
    {
        if (!assetPath.Contains(Folder)) return;
        var imp = (TextureImporter)assetImporter;
        if (assetPath.Contains("/AI/Elements/")) { ImportElement(imp); return; }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.filterMode = FilterMode.Bilinear;
        imp.mipmapEnabled = false;
        // wide region backdrops (bg_*) are big: compress them; everything else stays uncompressed
        bool wide = System.IO.Path.GetFileName(assetPath).StartsWith("bg_");
        imp.textureCompression = wide ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = false;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.maxTextureSize = wide ? 4096 : 2048;
        imp.wrapMode = TextureWrapMode.Clamp;
    }

    /// <summary>Scenery pieces: trimmed transparent sprites, pivot at the bottom centre, sized by ElementCatalog.</summary>
    void ImportElement(TextureImporter imp)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        int w, h;
        imp.GetSourceTextureWidthAndHeight(out w, out h);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = Mathf.Max(1f, h / ElementCatalog.Height(name));
        imp.filterMode = FilterMode.Bilinear;
        imp.mipmapEnabled = true;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.CompressedHQ;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.maxTextureSize = 2048;
        imp.wrapMode = TextureWrapMode.Clamp;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
    }
}
