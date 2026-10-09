using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A working traffic light with a stop line. The car in front obeys it (LeadCar brakes for red / late yellow),
/// so the player ends up queueing behind it. Running a red light yourself startles the passenger.
/// </summary>
public class TrafficSignal : WorldAnchor
{
    public enum Phase { Green, Yellow, Red }

    public static readonly List<TrafficSignal> All = new List<TrafficSignal>();

    public Phase phase = Phase.Green;
    public float greenTime = 16f, yellowTime = 2.5f, redTime = 7f;
    public float stopX;                    // route position of the stop line
    public SpriteRenderer lampRed, lampYellow, lampGreen, glow;
    public bool penalized;

    float t;

    protected virtual void OnEnable() { All.Add(this); }
    protected virtual void OnDisable() { All.Remove(this); }

    public bool IsRed { get { return phase == Phase.Red; } }
    public float TimeLeft
    {
        get { return (phase == Phase.Green ? greenTime : phase == Phase.Yellow ? yellowTime : redTime) - t; }
    }

    public void StartAt(Phase p, float secondsLeft)
    {
        phase = p;
        float dur = p == Phase.Green ? greenTime : p == Phase.Yellow ? yellowTime : redTime;
        t = Mathf.Clamp(dur - secondsLeft, 0f, dur - 0.1f);
        penalized = false;
    }

    /// <summary>First signal whose stop line is still ahead of this front bumper.</summary>
    public static TrafficSignal NextAhead(float frontX)
    {
        TrafficSignal best = null;
        float bestD = float.MaxValue;
        for (int i = 0; i < All.Count; i++)
        {
            float d = All[i].stopX - frontX;
            if (d < -0.3f) continue;
            if (d < bestD) { bestD = d; best = All[i]; }
        }
        return best;
    }

    /// <summary>Should a vehicle whose front is at frontX (moving at speed) stop at this light?</summary>
    public bool ShouldStop(float frontX, float speed)
    {
        float d = stopX - frontX;
        if (phase == Phase.Red) return true;
        if (phase == Phase.Yellow) return d > speed * speed / (2f * 4.5f) + 0.5f;
        return false;
    }

    void Update()
    {
        var g = RideGame.I;
        if (g == null) return;
        if (g.state == RideState.Driving)
        {
            t += Time.deltaTime;
            float dur = phase == Phase.Green ? greenTime : phase == Phase.Yellow ? yellowTime : redTime;
            if (t >= dur)
            {
                t = 0f;
                phase = phase == Phase.Green ? Phase.Yellow : phase == Phase.Yellow ? Phase.Red : Phase.Green;
                if (phase == Phase.Green) penalized = false;
            }
        }

        var off = new Color(0.22f, 0.05f, 0.05f, 1f);
        var offY = new Color(0.25f, 0.2f, 0.04f, 1f);
        var offG = new Color(0.04f, 0.2f, 0.07f, 1f);
        bool flash = phase == Phase.Yellow && Mathf.Sin(Time.time * 10f) > 0f;
        if (lampRed != null) lampRed.color = phase == Phase.Red ? new Color(1f, 0.18f, 0.15f, 1f) : off;
        if (lampYellow != null) lampYellow.color = (phase == Phase.Yellow && flash) || (phase == Phase.Yellow) ? new Color(1f, 0.82f, 0.2f, 1f) : offY;
        if (lampGreen != null) lampGreen.color = phase == Phase.Green ? new Color(0.25f, 1f, 0.45f, 1f) : offG;
        if (glow != null)
        {
            var c = phase == Phase.Red ? new Color(1f, 0.15f, 0.1f, 0.45f)
                  : phase == Phase.Yellow ? new Color(1f, 0.8f, 0.2f, 0.4f) : new Color(0.2f, 1f, 0.4f, 0.35f);
            glow.color = c;
            glow.transform.localPosition = new Vector3(0f, phase == Phase.Red ? 3.94f : phase == Phase.Yellow ? 3.5f : 3.06f, 0f);
        }
    }
}
