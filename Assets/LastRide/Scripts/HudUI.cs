using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Bars, clock, event prompt, toast messages and the ending card.</summary>
public class HudUI : MonoBehaviour
{
    RideGame g;
    Image fuelFill, calmFill, calmIcon, progressFill, markerDot;
    Image[] speedSegs;
    TextMeshProUGUI clockText, promptText, toastText, calmLabel, hintText, lcdPlace, lcdInfo;
    CanvasGroup toastGroup;
    CanvasGroup bannerGroup;
    TextMeshProUGUI bannerTitle, bannerSub;
    int lastRegion = -1;
    float bannerT;
    float toastT;

    // ending
    CanvasGroup endGroup;
    TextMeshProUGUI endTitle, endBody, endStats, endHint;
    float endFade;

    Sprite panel;

    public static HudUI Build(Transform canvas, Sprite white, Sprite panel, Sprite iconFuel, Sprite iconCalm, Sprite iconClock)
    {
        var go = new GameObject("HUD", typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        var h = go.AddComponent<HudUI>();
        UIKit.Stretch((RectTransform)go.transform);
        h.panel = panel;
        h.BuildUI(white, panel, iconFuel, iconCalm, iconClock);
        return h;
    }

    void BuildUI(Sprite white, Sprite panelS, Sprite iconFuel, Sprite iconCalm, Sprite iconClock)
    {
        g = RideGame.I;
        g.OnToast += Toast;

        BuildDashboard(white, panelS, iconFuel, iconCalm, iconClock);

        // --- prompt, toast, hint -------------------------------------------------------
        var pbox = UIKit.Img("promptBox", transform, panelS, Color.white, true);
        UIKit.Place(pbox.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(440, 56));
        promptText = UIKit.Txt("prompt", pbox.transform, "", 21, UIKit.Gold, TextAlignmentOptions.Center);
        UIKit.Stretch(promptText.rectTransform, 8, 4, 8, 4);
        promptText.transform.parent.gameObject.name = "promptBox";

        toastGroup = new GameObject("toast", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
        toastGroup.transform.SetParent(transform, false);
        UIKit.Place((RectTransform)toastGroup.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(520, 44));
        var tbg = UIKit.Img("toastBg", toastGroup.transform, panelS, Color.white, true);
        UIKit.Stretch(tbg.rectTransform);
        toastText = UIKit.Txt("toastText", toastGroup.transform, "", 22, UIKit.Cream, TextAlignmentOptions.Center);
        UIKit.Stretch(toastText.rectTransform, 10, 2, 10, 2);
        toastGroup.alpha = 0;

        hintText = UIKit.Txt("hint", transform, "→ / D เร่ง    ← / A เบรก    H แตร    Space คุย", 18, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.BottomLeft);
        UIKit.Place(hintText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, RideGameConst.DashPx + 6), new Vector2(700, 30));

        // --- place banner ---
        var bg = new GameObject("banner", typeof(RectTransform), typeof(CanvasGroup));
        bg.transform.SetParent(transform, false);
        bannerGroup = bg.GetComponent<CanvasGroup>();
        UIKit.Place((RectTransform)bg.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(800, 150));
        var bpan = UIKit.Img("bannerPanel", bg.transform, panelS, new Color(1, 1, 1, 0.92f), true);
        UIKit.Stretch(bpan.rectTransform);
        bannerTitle = UIKit.Txt("bannerTitle", bg.transform, "", 48, UIKit.Gold, TextAlignmentOptions.Center);
        UIKit.Place(bannerTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(760, 70));
        bannerSub = UIKit.Txt("bannerSub", bg.transform, "", 26, UIKit.Cream, TextAlignmentOptions.Center);
        UIKit.Place(bannerSub.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(760, 40));
        bannerGroup.alpha = 0;

        // --- ending -------------------------------------------------------------------------
        var eg = new GameObject("ending", typeof(RectTransform), typeof(CanvasGroup));
        eg.transform.SetParent(transform, false);
        UIKit.Stretch((RectTransform)eg.transform);
        endGroup = eg.GetComponent<CanvasGroup>();
        var dim = UIKit.Img("dim", eg.transform, white, new Color(0.03f, 0.02f, 0.1f, 0.88f));
        UIKit.Stretch(dim.rectTransform);
        var ep = UIKit.Img("endPanel", eg.transform, panelS, Color.white, true);
        UIKit.Place(ep.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 380));
        endTitle = UIKit.Txt("endTitle", ep.transform, "", 46, UIKit.Gold, TextAlignmentOptions.Center);
        UIKit.Place(endTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(720, 70));
        endBody = UIKit.Txt("endBody", ep.transform, "", 26, UIKit.Cream, TextAlignmentOptions.Top);
        UIKit.Place(endBody.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -108), new Vector2(690, 170));
        endStats = UIKit.Txt("endStats", ep.transform, "", 22, new Color(0.7f, 0.85f, 1f), TextAlignmentOptions.Center);
        UIKit.Place(endStats.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 62), new Vector2(720, 34));
        endHint = UIKit.Txt("endHint", ep.transform, "กด R เพื่อขับอีกคืน", 22, UIKit.Cream, TextAlignmentOptions.Center);
        UIKit.Place(endHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(720, 34));
        endGroup.alpha = 0; endGroup.blocksRaycasts = false;
    }

    Image Box(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color fill, Color border, float bw, Sprite white)
    {
        var o = UIKit.Img(name + "_b", parent, white, border);
        UIKit.Place(o.rectTransform, anchor, pivot, pos, size);
        var i = UIKit.Img(name, o.transform, white, fill);
        UIKit.Stretch(i.rectTransform, bw, bw, bw, bw);
        return i;
    }

    /// <summary>The tuk-tuk dashboard along the bottom of the screen (the camera is lifted so the road sits above it).</summary>
    void BuildDashboard(Sprite white, Sprite panelS, Sprite iconFuel, Sprite iconCalm, Sprite iconClock)
    {
        var navy = new Color(0.07f, 0.06f, 0.17f, 1f);
        var inset = new Color(0.04f, 0.035f, 0.11f, 1f);
        var edge = new Color(0.36f, 0.32f, 0.7f, 1f);
        var bottom = new Vector2(0.5f, 0f);
        float H = RideGameConst.DashPx;

        var dash = UIKit.Img("dashboard", transform, white, navy);
        dash.rectTransform.anchorMin = new Vector2(0, 0); dash.rectTransform.anchorMax = new Vector2(1, 0);
        dash.rectTransform.pivot = new Vector2(0.5f, 0);
        dash.rectTransform.anchoredPosition = Vector2.zero; dash.rectTransform.sizeDelta = new Vector2(0, H);
        var trim1 = UIKit.Img("trimCream", dash.transform, white, UIKit.Cream);
        trim1.rectTransform.anchorMin = new Vector2(0, 1); trim1.rectTransform.anchorMax = new Vector2(1, 1);
        trim1.rectTransform.pivot = new Vector2(0.5f, 1);
        trim1.rectTransform.anchoredPosition = Vector2.zero; trim1.rectTransform.sizeDelta = new Vector2(0, 2);
        var trim2 = UIKit.Img("trimPurple", dash.transform, white, edge);
        trim2.rectTransform.anchorMin = new Vector2(0, 1); trim2.rectTransform.anchorMax = new Vector2(1, 1);
        trim2.rectTransform.pivot = new Vector2(0.5f, 1);
        trim2.rectTransform.anchoredPosition = new Vector2(0, -2); trim2.rectTransform.sizeDelta = new Vector2(0, 3);

        // left: fuel + calm gauges
        var left = Box("left", dash.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 34), new Vector2(262, 50), inset, edge, 2, white);
        Image fi, ci2;
        Row(left.transform, white, iconFuel, "น้ำมัน", 28, new Color(0.95f, 0.45f, 0.35f), out fuelFill, out fi, out var fuelLbl);
        Row(left.transform, white, iconCalm, "ความสงบ", 6, new Color(0.6f, 0.85f, 1f), out calmFill, out ci2, out calmLabel);
        calmIcon = ci2;

        // centre: LCD
        var lcd = Box("lcd", dash.transform, bottom, bottom, new Vector2(0, 34), new Vector2(300, 50), new Color(0.6f, 0.76f, 0.48f, 1f), new Color(0.2f, 0.22f, 0.3f, 1f), 3, white);
        var dark = new Color(0.09f, 0.2f, 0.12f, 1f);
        lcdPlace = UIKit.Txt("lcdPlace", lcd.transform, "", 15, dark, TextAlignmentOptions.Left);
        UIKit.Place(lcdPlace.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -4), new Vector2(280, 22));
        lcdInfo = UIKit.Txt("lcdInfo", lcd.transform, "", 14, dark, TextAlignmentOptions.Left);
        UIKit.Place(lcdInfo.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 4), new Vector2(280, 22));

        // right: clock + speed
        var right = Box("right", dash.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 34), new Vector2(262, 50), inset, edge, 2, white);
        var ci = UIKit.Img("clockIcon", right.transform, iconClock, Color.white);
        UIKit.Place(ci.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(26, 26));
        clockText = UIKit.Txt("clock", right.transform, "22:00", 22, UIKit.Gold, TextAlignmentOptions.Left);
        UIKit.Place(clockText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(90, 34));
        speedSegs = new Image[10];
        for (int i = 0; i < speedSegs.Length; i++)
        {
            speedSegs[i] = UIKit.Img("seg" + i, right.transform, white, Color.gray);
            UIKit.Place(speedSegs[i].rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(136 + i * 12, 22), new Vector2(9, 8 + i));
        }
        var spl = UIKit.Txt("speedLbl", right.transform, "ความเร็ว", 11, new Color(UIKit.Cream.r, UIKit.Cream.g, UIKit.Cream.b, 0.7f), TextAlignmentOptions.Left);
        UIKit.Place(spl.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(136, 2), new Vector2(120, 18));

        // bottom strip: route progress with the places on the way
        var strip = UIKit.Rt("route", dash.transform);
        strip.anchorMin = new Vector2(0, 0); strip.anchorMax = new Vector2(1, 0);
        strip.offsetMin = new Vector2(46, 2); strip.offsetMax = new Vector2(-46, 32);
        var track = UIKit.Img("track", strip, white, new Color(0.22f, 0.2f, 0.45f, 1f));
        track.rectTransform.anchorMin = new Vector2(0, 0); track.rectTransform.anchorMax = new Vector2(1, 0);
        track.rectTransform.offsetMin = new Vector2(0, 22); track.rectTransform.offsetMax = new Vector2(0, 27);
        progressFill = UIKit.Img("progressFill", track.transform, white, new Color(1f, 0.82f, 0.38f, 1f));
        UIKit.Stretch(progressFill.rectTransform);
        progressFill.type = Image.Type.Filled; progressFill.fillMethod = Image.FillMethod.Horizontal;
        float rl = RideGame.I.routeLength;
        for (int i = 0; i < RouteRegions.All.Length; i++)
        {
            float f = Mathf.Clamp01(RouteRegions.All[i].start / rl);
            var tk = UIKit.Img("tick" + i, track.transform, white, UIKit.Cream);
            UIKit.Place(tk.rectTransform, new Vector2(f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4, 11));
            var lb = UIKit.Txt("tickLbl" + i, track.transform, RouteRegions.All[i].title, 11, UIKit.Cream, TextAlignmentOptions.Top);
            UIKit.Place(lb.rectTransform, new Vector2(f, 0), new Vector2(0.5f, 1), new Vector2(0, -3), new Vector2(110, 16));
        }
        markerDot = UIKit.Img("dot", track.transform, white, Color.white);
        UIKit.Place(markerDot.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 15));
    }

    void Row(Transform parent, Sprite white, Sprite icon, string label, float y, Color barColor, out Image fill, out Image ic, out TextMeshProUGUI lbl)
    {
        ic = UIKit.Img("icon", parent, icon, Color.white);
        UIKit.Place(ic.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(7, y - 2), new Vector2(18, 18));
        lbl = UIKit.Txt("label", parent, label, 12, UIKit.Cream, TextAlignmentOptions.Left);
        UIKit.Place(lbl.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, y - 3), new Vector2(110, 20));
        var bg = UIKit.Img("barBg", parent, white, new Color(0.14f, 0.13f, 0.3f, 1f));
        UIKit.Place(bg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(112, y + 1), new Vector2(142, 11));
        fill = UIKit.Img("fill", bg.transform, white, barColor);
        UIKit.Stretch(fill.rectTransform, 2, 2, 2, 2);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
    }

    void OnDestroy() { if (g != null) g.OnToast -= Toast; }

    public void Toast(string msg)
    {
        toastText.text = ThaiText.Fix(msg);
        toastT = 3f;
    }

    void Update()
    {
        if (g == null) return;

        fuelFill.fillAmount = g.fuel;
        bool low = g.fuel < 0.18f;
        fuelFill.color = low ? (Mathf.Sin(Time.time * 8f) > 0 ? new Color(1f, 0.25f, 0.25f) : new Color(0.6f, 0.15f, 0.15f))
                             : new Color(0.95f, 0.45f, 0.35f);

        bool has = g.passenger != PassengerId.None;
        calmFill.fillAmount = has ? g.calm / 100f : 0f;
        calmFill.color = g.calm < 30f ? new Color(1f, 0.6f, 0.4f) : new Color(0.6f, 0.85f, 1f);
        calmIcon.color = has ? Color.white : new Color(1, 1, 1, 0.3f);
        calmLabel.text = ThaiText.Fix(has ? "ความสงบ" : "ไม่มีผู้โดยสาร");
        calmLabel.color = has ? UIKit.Cream : new Color(UIKit.Cream.r, UIKit.Cream.g, UIKit.Cream.b, 0.45f);

        lcdPlace.text = ThaiText.Fix(RouteRegions.All[RouteRegions.IndexAt(g.distance)].title);
        lcdInfo.text = ThaiText.Fix((has ? g.RiderNames : "รถว่าง") + "   บุญ " + g.merit);

        float sp = Mathf.Clamp01(g.speed / g.maxSpeed);
        for (int i = 0; i < speedSegs.Length; i++)
        {
            bool on = sp * speedSegs.Length > i + 0.2f;
            speedSegs[i].color = !on ? new Color(0.16f, 0.15f, 0.32f) : i >= 8 ? new Color(1f, 0.45f, 0.35f) : i >= 5 ? UIKit.Gold : new Color(0.55f, 0.9f, 0.6f);
        }

        clockText.text = g.ClockText;
        float prog = Mathf.Clamp01(g.distance / g.routeLength);
        progressFill.fillAmount = prog;
        var mr = markerDot.rectTransform;
        mr.anchorMin = mr.anchorMax = new Vector2(prog, 0.5f);

        // prompt
        string prompt = "";
        if (g.state == RideState.Driving)
        {
            if (EventPoint.Current != null) prompt = "[Space]  " + EventPoint.Current.prompt;
            else
            {
                EventPoint near = null; float best = 14f;
                foreach (var e in EventPoint.All)
                {
                    float d = e.worldX - g.distance;
                    if (!e.done && d > -2f && d < best) { best = d; near = e; }
                }
                if (near != null) prompt = "ชะลอและจอดข้างจุด (!) แล้วกด Space";
                else
                {
                    float front = g.distance + RideGame.TukLength * 0.5f;
                    var sig = TrafficSignal.NextAhead(front);
                    if (sig != null && sig.stopX - front < 16f && sig.phase != TrafficSignal.Phase.Green)
                        prompt = sig.phase == TrafficSignal.Phase.Red ? "ไฟแดง — หยุดรอก่อน" : "ไฟเหลือง — ชะลอ";
                }
            }
        }
        promptText.text = ThaiText.Fix(prompt);
        promptText.transform.parent.gameObject.SetActive(prompt.Length > 0);

        // place banner
        if (g.state == RideState.Driving)
        {
            int ri = RouteRegions.IndexAt(g.distance);
            if (ri != lastRegion)
            {
                lastRegion = ri;
                var info = RouteRegions.All[ri];
                bannerTitle.text = ThaiText.Fix(info.title);
                bannerSub.text = ThaiText.Fix(info.subtitle);
                bannerT = 4.5f;
            }
        }
        if (bannerT > 0f)
        {
            bannerT -= Time.deltaTime;
            float a = Mathf.Clamp01(Mathf.Min((4.5f - bannerT) / 0.5f, bannerT / 0.8f));
            bannerGroup.alpha = a;
        }
        else bannerGroup.alpha = 0f;

        // toast
        if (toastT > 0f) { toastT -= Time.deltaTime; toastGroup.alpha = Mathf.Clamp01(toastT / 0.5f); }
        else toastGroup.alpha = 0;

        // controls hint fades after a while
        float ha = Mathf.Clamp01(1f - (Time.timeSinceLevelLoad - 22f) / 4f);
        hintText.color = new Color(1, 1, 1, 0.85f * ha);

        // ending
        if (g.state == RideState.Ended)
        {
            if (endFade <= 0f)
            {
                endTitle.text = ThaiText.Fix(g.endTitle);
                endBody.text = ThaiText.Fix(g.endBody);
                endStats.text = ThaiText.Fix(string.Format("บุญที่สะสม {0}      ความสงบ {1}      เวลา {2}", g.merit, g.passenger == PassengerId.None ? 0 : Mathf.RoundToInt(g.calm), g.ClockText));
            }
            endFade += Time.deltaTime;
            endGroup.alpha = Mathf.Clamp01((endFade - 0.8f) / 0.8f);
            var kb = Keyboard.current;
            if (endFade > 1.6f && kb != null && kb.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
