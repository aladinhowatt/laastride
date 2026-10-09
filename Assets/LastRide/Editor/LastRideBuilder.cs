using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools/Last Ride/Build Scene regenerates the pixel art, the Thai font asset (if you dropped a .ttf into
/// Assets/LastRide/Fonts) and the one-object scene. The world itself is assembled at runtime by RideBootstrap.
/// </summary>
public static class LastRideBuilder
{
    public const string ScenePath = "Assets/LastRide/Scenes/LastRide.unity";
    const string FontDir = "Assets/LastRide/Fonts";
    const string FontAssetPath = "Assets/LastRide/Resources/LastRide/ThaiFontAsset.asset";

    [MenuItem("Tools/Last Ride/Regenerate Art")]
    public static void RegenerateArt()
    {
        LastRideArt.GenerateAll();
    }

    static bool tmpExit;

    static bool TmpReady() { return File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"); }

    [MenuItem("Tools/Last Ride/Build Scene")]
    public static void Build()
    {
        // TextMesh Pro needs its "essential resources" once; importing is asynchronous, so continue afterwards.
        if (!TmpReady())
        {
            Debug.Log("[LastRide] Importing TextMesh Pro essential resources first...");
            AssetDatabase.importPackageCompleted += OnTmpImported;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            return;
        }
        BuildCore();
    }

    static void OnTmpImported(string packageName)
    {
        AssetDatabase.importPackageCompleted -= OnTmpImported;
        EditorApplication.delayCall += () =>
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildCore();
            if (tmpExit) EditorApplication.Exit(0);
        };
    }

    static void BuildCore()
    {
        ElementTrimmer.TrimAll();
        LastRideArt.GenerateAll();
        BuildThaiFontAsset();

        Directory.CreateDirectory("Assets/LastRide/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.orthographicSize = 135f / 16f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.transform.position = new Vector3(0, 0, -10);

        new GameObject("RideBootstrap").AddComponent<RideBootstrap>();

        EditorSceneManager.SaveScene(scene, ScenePath);

        // scene first in Build Settings (the restart key reloads by build index)
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == ScenePath);
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("[LastRide] Scene built: " + ScenePath + "  — press Play.");
    }

    /// <summary>If a .ttf/.otf with Thai glyphs is in Assets/LastRide/Fonts, bake a TMP font asset from it.</summary>
    static void BuildThaiFontAsset()
    {
        if (!Directory.Exists(FontDir)) return;
        string[] fonts = Directory.GetFiles(FontDir, "*.ttf");
        if (fonts.Length == 0) fonts = Directory.GetFiles(FontDir, "*.otf");
        if (fonts.Length == 0) return;
        var font = AssetDatabase.LoadAssetAtPath<Font>(fonts[0].Replace('\\', '/'));
        if (font == null) return;
        var fa = TMP_FontAsset.CreateFontAsset(font, 72, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        Directory.CreateDirectory(Path.GetDirectoryName(FontAssetPath));
        AssetDatabase.DeleteAsset(FontAssetPath);
        AssetDatabase.CreateAsset(fa, FontAssetPath);
        if (fa.atlasTexture != null) { fa.atlasTexture.name = "ThaiFontAtlas"; AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa); }
        if (fa.material != null) { fa.material.name = "ThaiFontMaterial"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        Debug.Log("[LastRide] Thai TMP font asset created from " + fonts[0]);
    }

    // ---- batch-mode helpers -------------------------------------------------------------------
    /// <summary>unity -batchmode -executeMethod LastRideBuilder.BuildBatch  (no -quit: it exits itself)</summary>
    public static void BuildBatch()
    {
        tmpExit = true;
        Build();
        if (TmpReady()) EditorApplication.Exit(0);
    }

    /// <summary>Opens the scene and enters play mode (used with -lastride-shots &lt;dir&gt; for headless screenshots).</summary>
    public static void PlayForShots()
    {
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }
}
