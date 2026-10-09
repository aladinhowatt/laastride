using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Throttle / brake only: the road is a single lane. The player must keep a calm pace behind the
/// car in front. Bumping is forgiving (no game over): it only startles the passenger.
/// </summary>
public class TukTukController : MonoBehaviour
{
    public SpriteRenderer body, ghost, beam;
    public Sprite[] frames;
    public Sprite ghostSri, ghostTon;
    public AudioSource engineSrc, sfxSrc;
    public float baseY;

    RideGame g;
    float animT, bumpCd, honkCd, bob, shake;
    int frame;

    void Start() { g = RideGame.I; }

    void Update()
    {
        if (g == null) return;
        float dt = Time.deltaTime;
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        bool gas = false, brk = false, honk = false;
        if (kb != null)
        {
            gas = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
            brk = kb.aKey.isPressed || kb.leftArrowKey.isPressed || kb.sKey.isPressed || kb.downArrowKey.isPressed;
            honk = kb.hKey.wasPressedThisFrame;
        }
        if (gp != null)
        {
            gas |= gp.rightTrigger.isPressed || gp.leftStick.x.ReadValue() > 0.4f || gp.buttonSouth.isPressed;
            brk |= gp.leftTrigger.isPressed || gp.leftStick.x.ReadValue() < -0.4f || gp.buttonEast.isPressed;
            honk |= gp.buttonWest.wasPressedThisFrame;
        }

        bumpCd -= dt;
        honkCd -= dt;

        if (g.state == RideState.Driving)
        {
            float cap = g.fuel > 0f ? g.maxSpeed : 1.5f;      // out of fuel: push along slowly
            if (gas) g.speed += g.accel * dt; else g.speed -= g.drag * dt;
            if (brk) g.speed -= g.brake * dt;
            g.speed = Mathf.Clamp(g.speed, 0f, cap);

            // keep behind whatever is in front; flooring it into a car crashes
            float front = g.distance + RideGame.TukLength * 0.5f;
            float gap;
            var b = RoadBlocker.NearestAhead(front, out gap);
            crashCd -= dt;
            if (b != null && gap < 6f)
            {
                float closing = g.speed - b.speed;
                if (gas && b is LeadCar && gap < 0.4f && closing > 1f && crashCd <= 0f) { Crash(b); return; }
                if (!gas || !(b is LeadCar))
                {
                    float allowed = Mathf.Max(0f, b.speed) + Mathf.Max(0f, gap - 0.25f) * 2.2f;
                    if (g.speed > allowed) g.speed = Mathf.MoveTowards(g.speed, allowed, 16f * dt);
                }
            }

            if (honk && honkCd <= 0f) Honk();

            float frontBefore = g.distance + RideGame.TukLength * 0.5f;
            float step = g.speed * dt;
            g.distance += step;
            CheckRedLight(frontBefore, g.distance + RideGame.TukLength * 0.5f);
            float endX = g.routeLength + 8f;       // the road ends at the temple
            if (g.distance > endX) { g.distance = endX; g.speed = 0f; }
            if (g.fuel > 0f) g.fuel = Mathf.Max(0f, g.fuel - step / g.fuelRange);
        }
        else
        {
            g.speed = Mathf.MoveTowards(g.speed, 0f, g.brake * dt);
        }

        Animate(dt);
    }

    void CheckRedLight(float before, float after)
    {
        foreach (var s in TrafficSignal.All)
        {
            if (!s.IsRed || s.penalized) continue;
            if (before < s.stopX && after >= s.stopX)
            {
                s.penalized = true;
                if (sfxSrc != null) sfxSrc.PlayOneShot(ChipAudio.Honk(), 0.6f);
                g.AddCalm(-5f);
                g.Toast("ฝ่าไฟแดง! ตำรวจเรียก");
                if (DialogueUI.Instance != null) DialogueUI.Instance.Play(Story.Police());
            }
        }
    }

    void Honk()
    {
        honkCd = 0.6f;
        if (sfxSrc != null) sfxSrc.PlayOneShot(ChipAudio.Honk(), 0.8f);
        for (int i = 0; i < RoadBlocker.All.Count; i++) RoadBlocker.All[i].OnHonk();
        if (g.passenger != PassengerId.None) g.AddCalm(-1f);
    }

    float crashCd = 8f;

    /// <summary>Rear-ended the car in front: stop, get out and sort it out (costs time).</summary>
    void Crash(RoadBlocker b)
    {
        crashCd = 30f;
        shake = 0.4f;
        if (sfxSrc != null) sfxSrc.PlayOneShot(ChipAudio.Honk(), 0.9f);
        g.speed = 0f;
        g.distance = b.RearX - RideGame.TukLength * 0.5f - 1.2f;       // step back out of the other car
        g.AddCalm(-8f);
        if (DialogueUI.Instance != null) DialogueUI.Instance.Play(Story.Crash());
    }

    void Bump()
    {
        bumpCd = 1.5f;
        shake = 0.25f;
        if (sfxSrc != null) sfxSrc.PlayOneShot(ChipAudio.Honk(), 0.5f);
        g.AddCalm(-4f);
        g.Toast("ชนเบา ๆ — ชะลอก่อนถึงรถคันหน้านะ");
    }

    void Animate(float dt)
    {
        float s = g.speed;
        // wheel frames
        if (s > 0.15f)
        {
            animT += dt * (4f + s * 2.2f);
            frame = ((int)animT) & 1;
            bob += dt * (6f + s);
        }
        if (body.sprite != frames[frame]) SpriteMats.Apply(body, frames[frame]);

        float yb = (s > 0.2f ? (Mathf.Sin(bob * 3f) > 0.4f ? 1f / 16f : 0f) : 0f);
        if (shake > 0f) { shake -= dt; yb += (Random.value > 0.5f ? 1 : -1) * (1f / 16f); }
        transform.position = new Vector3(RideGame.TukScreenX, baseY + yb, 0);

        // passenger
        bool has = g.passenger != PassengerId.None;
        ghost.enabled = has;
        if (has)
        {
            var gd = Ghosts.Get(g.passenger);
            var gs = gd != null && gd.female ? ghostSri : ghostTon;
            if (ghost.sprite != gs) SpriteMats.Apply(ghost, gs);
            float f = 0.82f + 0.12f * Mathf.Sin(Time.time * 2.2f);
            ghost.color = new Color(1f, 1f, 1f, f * Mathf.Lerp(0.55f, 1f, g.calm / 100f));
            ghost.transform.localPosition = new Vector3(-0.75f, 0.5f + Mathf.Sin(Time.time * 2f) * 0.0625f, 0);
        }

        beam.color = new Color(1, 1, 1, 0.55f + 0.1f * Mathf.Sin(Time.time * 9f));

        // sound
        if (engineSrc != null)
        {
            bool on = g.state != RideState.Ended;
            engineSrc.volume = on ? Mathf.Lerp(0.12f, 0.35f, s / g.maxSpeed) : 0f;
            engineSrc.pitch = 0.75f + s / g.maxSpeed * 0.9f;
        }
    }
}
