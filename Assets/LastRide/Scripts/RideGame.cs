using System;
using System.Collections.Generic;
using UnityEngine;

public enum RideState { Driving, Dialogue, Ended }
public enum PassengerId { None, Sri, Heng, Ton, Mai, Bua, Nok, Khum, Tong }

/// <summary>Shared state of one night's ride. Everything else reads from here.</summary>
public class RideGame : MonoBehaviour
{
    public static RideGame I { get; private set; }

    // The tuk-tuk stays at this x on screen; the world scrolls past it.
    public const float TukScreenX = -8f;
    public const float TukLength = 3.4f;

    [Header("Route")]
    public float routeLength = 1300f;

    [Header("Driving")]
    public float maxSpeed = 9f;
    public float accel = 3.5f;
    public float brake = 8f;
    public float drag = 1.2f;
    public float fuelRange = 850f;     // distance one full tank lasts

    [Header("Night clock (seconds of real time before dawn)")]
    public float clockTotal = 330f;
    public float parkedClockRate = 0.5f;

    public RideState state = RideState.Driving;
    public float distance;
    public float speed;
    public float fuel = 1f;
    public float calm = 70f;
    public int merit;
    public float clock;
    public readonly List<PassengerId> riders = new List<PassengerId>();
    /// <summary>The most recent ghost to get in (None = empty back seat).</summary>
    public PassengerId passenger { get { return riders.Count > 0 ? riders[riders.Count - 1] : PassengerId.None; } }
    public readonly HashSet<string> flags = new HashSet<string>();

    public string endTitle = "", endBody = "";
    public event Action<string> OnToast;

    void Awake() { I = this; }
    void OnDestroy() { if (I == this) I = null; }

    public bool Has(string f) { return flags.Contains(f); }
    public void Flag(string f) { flags.Add(f); }
    public float NightProgress { get { return Mathf.Clamp01(clock / clockTotal); } }
    public float DistanceLeft { get { return Mathf.Max(0, routeLength - distance); } }

    public string PassengerName
    {
        get { var d = Ghosts.Get(passenger); return d != null ? d.name : ""; }
    }

    /// <summary>Everyone on the back seat, e.g. "ป้าศรี · พี่ต้น".</summary>
    public string RiderNames
    {
        get
        {
            var l = new List<string>();
            foreach (var r in riders) { var d = Ghosts.Get(r); if (d != null) l.Add(d.name); }
            return string.Join(" · ", l);
        }
    }

    public void Board(PassengerId id)
    {
        if (riders.Contains(id)) return;
        riders.Add(id);
        if (riders.Count == 1) calm = 70f;
        var d = Ghosts.Get(id);
        if (d != null) { Flag(d.key); EventPoint.CloseStage(d.stage, d.key); }
    }

    /// <summary>22:00 -> 05:30 mapped onto the night clock.</summary>
    public string ClockText
    {
        get
        {
            float m = 22 * 60 + NightProgress * 450f;
            int mm = Mathf.FloorToInt(m) % (24 * 60);
            return string.Format("{0:00}:{1:00}", mm / 60, mm % 60);
        }
    }

    void Update()
    {
        if (state != RideState.Driving) return;
        clock += Time.deltaTime * (speed > 0.3f ? 1f : parkedClockRate);
        if (clock >= clockTotal) EndRide(Story.TimeUpTitle(this), Story.TimeUpBody(this));
    }

    public void SetState(RideState s) { state = s; }

    public void Toast(string msg) { if (OnToast != null) OnToast(msg); }

    public void AddCalm(float d)
    {
        if (passenger == PassengerId.None) return;
        float before = calm;
        calm = Mathf.Clamp(calm + d, 0f, 100f);
        int diff = Mathf.RoundToInt(calm - before);
        if (diff != 0) Toast((riders.Count > 1 ? "ผู้โดยสาร" : PassengerName) + (diff > 0 ? " สงบขึ้น +" : " หวั่นใจ ") + diff);
    }

    public void AddMerit(int n)
    {
        merit += n;
        Toast("บุญ +" + n);
    }

    public void EndRide(string title, string body)
    {
        if (state == RideState.Ended) return;
        endTitle = title;
        endBody = body;
        speed = 0;
        state = RideState.Ended;
    }
}
