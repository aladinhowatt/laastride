using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Test helper: when the editor/player is started with "-lastride-shots &lt;dir&gt;" it plays a short scripted
/// session and writes PNG screenshots, then quits. Used to check the look without a human at the keyboard.
/// </summary>
public class ShotBot : MonoBehaviour
{
    string dir;
    RideGame g;
    Camera cam;

    public static string DirFromArgs()
    {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-lastride-shots") return a[i + 1];
        return null;
    }

    void Start()
    {
        dir = DirFromArgs();
        if (dir == null) { Destroy(this); return; }
        Directory.CreateDirectory(dir);
        g = RideGame.I;
        cam = Camera.main;
        StartCoroutine(Run());
    }

    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    void Shot(string name)
    {
        var rt = new RenderTexture(1920, 1080, 24);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = prev;
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        Destroy(tex); rt.Release(); Destroy(rt);
        Debug.Log("[ShotBot] " + name);
    }

    IEnumerator Run()
    {
        yield return Frames(20);
        yield return new WaitForSeconds(2.5f);
        yield return null;
        Shot("01_intro_cutscene");

        // close the intro
        var ui = DialogueUI.Instance;
        yield return ui.DebugSkipAll();
        yield return Frames(5);

        // driving shots
        g.distance = 8f; g.speed = 6f;
        for (int i = 0; i < 40; i++) { g.speed = 6f; yield return null; }
        yield return null;
        Shot("02_driving_night");

        // follow the lead car (first traffic zone starts at 30)
        g.distance = 32f; g.speed = 8f;
        float t0 = Time.time;
        while (Time.time - t0 < 4f) { g.speed = Mathf.Max(g.speed, 8f); yield return null; }
        LeadCar lead = null;
        foreach (var b in RoadBlocker.All) { var lc = b as LeadCar; if (lc != null) lead = lc; }
        if (lead != null) { g.distance = lead.worldX - 11f; Debug.Log("[ShotBot] lead car found, speed " + lead.speed); }
        else Debug.Log("[ShotBot] NO lead car found");
        yield return new WaitForSeconds(0.25f);
        Shot("02b_following_lead_car");
        float t1 = Time.time;
        while (Time.time - t1 < 5f) { g.speed = Mathf.Max(g.speed, 8f); yield return null; }
        Shot("02c_following_lead_car_later");

        // sala: park beside it
        g.distance = 60f; g.speed = 0f;
        for (int i = 0; i < 10; i++) { g.speed = 0f; yield return null; }
        yield return null;
        Shot("03_prompt_at_sala");

        if (EventPoint.Current != null) EventPoint.Current.TriggerNow();
        yield return new WaitForSeconds(1.2f);
        yield return null;
        Shot("04_cutscene_typing");
        ui.DebugFinishTyping();
        yield return Frames(3);
        // jump to the choice node
        yield return ui.DebugAdvanceUntilChoices();
        yield return new WaitForSeconds(0.5f);
        Shot("05_choices");
        ui.DebugChoose(0);          // accept
        yield return ui.DebugSkipAll();

        g.distance = 100f; g.speed = 6f;
        for (int i = 0; i < 40; i++) { g.speed = 6f; yield return null; }
        yield return null;
        Shot("06_driving_with_passenger");

        // shrine
        g.distance = 170f; g.speed = 0f;
        for (int i = 0; i < 10; i++) { g.speed = 0f; yield return null; }
        if (EventPoint.Current != null) EventPoint.Current.TriggerNow();
        yield return ui.DebugAdvanceUntilChoices();
        yield return new WaitForSeconds(0.5f);
        Shot("07_shrine_choices");
        ui.DebugChoose(0);
        yield return ui.DebugSkipAll();

        // buffalo + lead car
        g.distance = 505f; g.speed = 5f;
        for (int i = 0; i < 60; i++) { g.speed = 5f; yield return null; }
        yield return null;
        Shot("08_buffalo_and_traffic");

        // dawn
        g.clock = g.clockTotal * 0.93f;
        g.distance = 700f; g.speed = 0f;
        for (int i = 0; i < 10; i++) { g.speed = 0f; yield return null; }
        if (EventPoint.Current != null) EventPoint.Current.TriggerNow();
        yield return ui.DebugAdvanceUntilChoices();
        yield return new WaitForSeconds(0.5f);
        Shot("09_gas_station_dawnish");
        ui.DebugChoose(0);
        yield return ui.DebugSkipAll();

        // temple
        g.distance = g.routeLength; g.speed = 0f;
        for (int i = 0; i < 10; i++) { g.speed = 0f; yield return null; }
        yield return null;
        Shot("10_temple_arrival");
        if (EventPoint.Current != null) EventPoint.Current.TriggerNow();
        yield return ui.DebugAdvanceUntilChoices();
        yield return new WaitForSeconds(0.5f);
        Shot("11_temple_cutscene");
        ui.DebugChoose(0);
        yield return ui.DebugSkipAll();
        yield return new WaitForSeconds(3f);
        yield return null;
        Shot("12_ending");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(0);
#else
        Application.Quit();
#endif
    }
}
