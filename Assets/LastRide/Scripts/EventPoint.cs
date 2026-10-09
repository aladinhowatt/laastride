using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A roadside spot: shows a (!) bubble from afar. Slow down and stop beside it, press Space, and the
/// game cuts to a conversation.
/// </summary>
public class EventPoint : WorldAnchor
{
    public static readonly List<EventPoint> All = new List<EventPoint>();
    public static EventPoint Current;      // the point the player may use right now (for the HUD prompt)

    public string id;
    public int stage = -1;               // route stage this event belongs to (ghost spots: only one ghost per stage)
    public SpriteRenderer figure;        // the waiting ghost
    public string prompt = "พูดคุย";
    public Func<DScript> script;
    public SpriteRenderer marker;
    public bool done;
    public bool repeatable;
    public float zoneBack = 3.2f, zoneFront = 3.0f;
    public float stopSpeed = 0.7f;

    float bobT;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); if (Current == this) Current = null; }

    /// <summary>A ghost boarded: the other ghost of that stage waits for another night.</summary>
    public static void CloseStage(int stage, string exceptKey)
    {
        foreach (var e in All)
            if (e.stage == stage && e.id != exceptKey && !e.done) { e.done = true; }
    }

    public bool InZone(RideGame g) { return g.distance > worldX - zoneBack && g.distance < worldX + zoneFront; }

    void Update()
    {
        var g = RideGame.I;
        if (g == null) return;

        float rel = worldX - g.distance;
        bool showMarker = !done && rel < 30f && rel > -8f && g.state != RideState.Ended;
        if (marker != null)
        {
            marker.enabled = showMarker;
            bobT += Time.deltaTime;
            marker.transform.localPosition = new Vector3(0, markerHeight + (Mathf.Sin(bobT * 4f) > 0 ? 1f / 16f : 0f), 0);
        }

        if (figure != null)
        {
            figure.enabled = !done && rel < 26f && rel > -10f;
            figure.color = new Color(1f, 1f, 1f, 0.7f + 0.15f * Mathf.Sin(bobT * 2f));
            figure.transform.localPosition = new Vector3(1.6f, 0.2f + Mathf.Sin(bobT * 2f) * 0.0625f, 0);
        }

        bool usable = !done && g.state == RideState.Driving && InZone(g) && g.speed < stopSpeed;
        if (usable) Current = this;
        else if (Current == this) Current = null;

        if (usable)
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool press = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                      || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            if (press) Trigger(g);
        }
    }

    public float markerHeight = 4.5f;

    public void TriggerNow() { Trigger(RideGame.I); }

    void Trigger(RideGame g)
    {
        var s = script != null ? script() : null;
        if (s == null) return;
        if (!repeatable) done = true;
        DialogueUI.Instance.Play(s);
    }
}
