using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Cut-scene conversation: pixel backdrop, speaker portrait, typewriter text, up/down choices, Space to continue.</summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    GameObject root;
    CanvasGroup group;
    Image backdrop, portrait, portraitFrame, nameTag, leftFrame, leftPortrait;
    string lastOther;
    TextMeshProUGUI nameText, bodyText, indicator;
    RectTransform choiceBox;
    Image choicePanel, arrow;
    readonly List<TextMeshProUGUI> choiceLines = new List<TextMeshProUGUI>();

    Sprite[] backdrops;
    Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();
    Sprite panelSprite, arrowSprite;
    AudioSource sfx;

    DScript script;
    DNode node;
    List<DChoice> visible = new List<DChoice>();
    int selected;
    bool typing;
    float typeT;
    int shown;
    int openFrame;
    float fade;

    const float CharsPerSecond = 48f;

    public static DialogueUI Build(Transform canvas, AudioSource sfx, Sprite panel, Sprite arrow,
                                   Dictionary<string, Sprite> backdrops, Dictionary<string, Sprite> portraits)
    {
        var go = new GameObject("DialogueUI", typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        var ui = go.AddComponent<DialogueUI>();
        ui.sfx = sfx;
        ui.panelSprite = panel;
        ui.arrowSprite = arrow;
        ui.portraits = portraits;
        ui.bd = backdrops;
        ui.BuildUI();
        Instance = ui;
        return ui;
    }

    Dictionary<string, Sprite> bd;

    void BuildUI()
    {
        var r = GetComponent<RectTransform>();
        if (r == null) r = gameObject.AddComponent<RectTransform>();
        UIKit.Stretch(r);
        root = gameObject;
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;

        backdrop = UIKit.Img("backdrop", transform, null, Color.white);
        UIKit.Stretch(backdrop.rectTransform);

        // portrait of whoever we talk to (right side)
        portraitFrame = UIKit.Img("portraitFrame", transform, panelSprite, new Color(1, 1, 1, 0.95f), true);
        UIKit.Place(portraitFrame.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-48, 200), new Vector2(230, 230));
        portrait = UIKit.Img("portrait", portraitFrame.transform, null, Color.white);
        UIKit.Place(portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(192, 192));

        // protagonist portrait (left side)
        leftFrame = UIKit.Img("leftFrame", transform, panelSprite, new Color(1, 1, 1, 0.95f), true);
        UIKit.Place(leftFrame.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 200), new Vector2(230, 230));
        leftPortrait = UIKit.Img("leftPortrait", leftFrame.transform, null, Color.white);
        UIKit.Place(leftPortrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(192, 192));
        leftFrame.gameObject.SetActive(false);

        // name tag
        nameTag = UIKit.Img("nameTag", transform, panelSprite, Color.white, true);
        UIKit.Place(nameTag.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 186), new Vector2(230, 46));
        nameText = UIKit.Txt("name", nameTag.transform, "", 24, UIKit.Gold, TextAlignmentOptions.Center);
        UIKit.Stretch(nameText.rectTransform, 6, 2, 6, 2);

        // text box
        var box = UIKit.Img("textBox", transform, panelSprite, Color.white, true);
        UIKit.Place(box.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(880, 160));
        bodyText = UIKit.Txt("body", box.transform, "", 25, UIKit.Cream, TextAlignmentOptions.TopLeft);
        UIKit.Stretch(bodyText.rectTransform, 28, 18, 28, 16);
        bodyText.lineSpacing = 6;
        indicator = UIKit.Txt("more", box.transform, "▼", 20, UIKit.Gold, TextAlignmentOptions.Center);
        UIKit.Place(indicator.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 12), new Vector2(32, 28));

        // choices
        choicePanel = UIKit.Img("choices", transform, panelSprite, Color.white, true);
        choiceBox = choicePanel.rectTransform;
        UIKit.Place(choiceBox, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 196), new Vector2(460, 120));
        arrow = UIKit.Img("arrow", choicePanel.transform, arrowSprite, Color.white);
        UIKit.Place(arrow.rectTransform, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(22, -30), new Vector2(20, 22));
        choicePanel.gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    public bool IsOpen { get { return group != null && group.alpha > 0.01f && script != null; } }

    public void Play(DScript s)
    {
        var g = RideGame.I;
        script = s;
        lastOther = null;
        g.SetState(RideState.Dialogue);
        Sprite bg;
        if (bd.TryGetValue(s.backdrop, out bg)) backdrop.sprite = bg;
        group.alpha = 0.001f;
        fade = 0f;
        openFrame = Time.frameCount;
        Show(s.start);
    }

    void Show(string key)
    {
        node = script.nodes[key];
        if (node.onEnter != null) node.onEnter();

        // speaker: the driver (lung) stands on the left, the person he talks to on the right
        bool hasName = !string.IsNullOrEmpty(node.speaker);
        nameTag.gameObject.SetActive(hasName);
        nameText.text = ThaiText.Fix(node.speaker);
        bool lungSpeaks = node.portrait == "lung";
        var nr = nameTag.rectTransform;
        if (lungSpeaks) UIKit.Place(nr, new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 186), new Vector2(230, 46));
        else UIKit.Place(nr, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-48, 186), new Vector2(230, 46));

        Sprite p;
        bool hasPortrait = !string.IsNullOrEmpty(node.portrait) && portraits.TryGetValue(node.portrait, out p);
        if (hasPortrait && !lungSpeaks) lastOther = node.portrait;
        var dim = new Color(0.5f, 0.5f, 0.55f, 0.95f);
        var lit = new Color(1, 1, 1, 0.95f);

        bool showRight = hasPortrait && lastOther != null;       // narration shows no portraits
        portraitFrame.gameObject.SetActive(showRight);
        if (showRight)
        {
            portrait.sprite = portraits[lastOther];
            portraitFrame.color = lungSpeaks ? dim : lit; portrait.color = lungSpeaks ? dim : Color.white;
        }
        Sprite lp;
        bool showLeft = hasPortrait && portraits.TryGetValue("lung", out lp) && (lungSpeaks || lastOther != null);
        leftFrame.gameObject.SetActive(showLeft);
        if (showLeft)
        {
            leftPortrait.sprite = portraits["lung"];
            leftFrame.color = lungSpeaks ? lit : dim; leftPortrait.color = lungSpeaks ? Color.white : dim;
        }

        bodyText.text = ThaiText.Fix(node.text);
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        typing = true; typeT = 0; shown = 0;
        indicator.enabled = false;

        // choices become visible once the text is complete
        visible.Clear();
        if (node.choices != null)
            foreach (var c in node.choices) if (c.when == null || c.when()) visible.Add(c);
        choicePanel.gameObject.SetActive(false);
        selected = 0;
    }

    void FinishTyping()
    {
        typing = false;
        bodyText.maxVisibleCharacters = 99999;
        if (visible.Count > 0) ShowChoices(); else indicator.enabled = true;
    }

    void ShowChoices()
    {
        choicePanel.gameObject.SetActive(true);
        foreach (var l in choiceLines) l.gameObject.SetActive(false);
        float h = 28 + visible.Count * 40;
        choiceBox.sizeDelta = new Vector2(460, h);
        for (int i = 0; i < visible.Count; i++)
        {
            if (i >= choiceLines.Count)
            {
                var t = UIKit.Txt("c" + i, choicePanel.transform, "", 23, UIKit.Cream, TextAlignmentOptions.Left);
                choiceLines.Add(t);
            }
            var line = choiceLines[i];
            line.gameObject.SetActive(true);
            line.text = ThaiText.Fix(visible[i].text);
            UIKit.Place(line.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -14 - i * 40), new Vector2(396, 36));
        }
        RefreshSelection();
    }

    void RefreshSelection()
    {
        for (int i = 0; i < visible.Count; i++)
            choiceLines[i].color = i == selected ? UIKit.Gold : UIKit.Cream;
        UIKit.Place(arrow.rectTransform, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(24, -32 - selected * 40), new Vector2(20, 22));
    }

    void Update()
    {
        if (script == null) return;

        // fade in
        if (fade < 1f) { fade = Mathf.Min(1f, fade + Time.deltaTime / 0.25f); group.alpha = fade; }

        // typewriter
        if (typing)
        {
            typeT += Time.deltaTime * CharsPerSecond;
            int n = Mathf.Min((int)typeT, bodyText.textInfo.characterCount);
            if (n != shown)
            {
                if (sfx != null && n % 2 == 0 && n > 0) { sfx.pitch = string.IsNullOrEmpty(node.speaker) ? 0.8f : 1f + (node.speaker.Length % 4) * 0.08f; sfx.PlayOneShot(ChipAudio.Blip(1f), 0.35f); }
                shown = n;
                bodyText.maxVisibleCharacters = n;
            }
            if (n >= bodyText.textInfo.characterCount) FinishTyping();
        }
        else if (indicator.enabled)
        {
            indicator.alpha = Mathf.Sin(Time.time * 7f) > 0 ? 1f : 0.2f;
        }

        if (Time.frameCount == openFrame) return;

        var kb = Keyboard.current;
        var gp = Gamepad.current;
        var ms = Mouse.current;
        bool confirm = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame))
                    || (gp != null && gp.buttonSouth.wasPressedThisFrame)
                    || (ms != null && ms.leftButton.wasPressedThisFrame);
        int move = 0;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) move = -1;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) move = 1;
        }
        if (gp != null)
        {
            if (gp.dpad.up.wasPressedThisFrame) move = -1;
            if (gp.dpad.down.wasPressedThisFrame) move = 1;
        }

        if (!typing && visible.Count > 0 && move != 0)
        {
            selected = (selected + move + visible.Count) % visible.Count;
            RefreshSelection();
            if (sfx != null) { sfx.pitch = 1f; sfx.PlayOneShot(ChipAudio.Select(), 0.5f); }
        }

        if (confirm)
        {
            if (typing) { FinishTyping(); return; }
            Advance();
        }
    }

    void Advance()
    {
        if (sfx != null) { sfx.pitch = 1f; sfx.PlayOneShot(ChipAudio.Confirm(), 0.4f); }
        string next = node.next;
        if (visible.Count > 0)
        {
            var c = visible[selected];
            if (c.act != null) c.act();
            next = c.next;
        }
        if (!string.IsNullOrEmpty(next) && script.nodes.ContainsKey(next)) { Show(next); return; }
        End();
    }

    // ---- test hooks (ShotBot) ----------------------------------------------------------------
    public void DebugFinishTyping() { if (script != null && typing) FinishTyping(); }
    public void DebugChoose(int idx) { if (visible.Count > 0) selected = Mathf.Clamp(idx, 0, visible.Count - 1); }

    public System.Collections.IEnumerator DebugSkipAll()
    {
        int guard = 0;
        while (script != null && guard++ < 200)
        {
            if (typing) FinishTyping(); else Advance();
            yield return null;
        }
    }

    public System.Collections.IEnumerator DebugAdvanceUntilChoices()
    {
        int guard = 0;
        while (script != null && guard++ < 200)
        {
            if (typing) { FinishTyping(); yield return null; continue; }
            if (visible.Count > 0) yield break;
            Advance();
            yield return null;
        }
    }

    void End()
    {
        if (node.onEnd != null) node.onEnd();
        script = null;
        group.alpha = 0;
        var g = RideGame.I;
        if (g.state != RideState.Ended) g.SetState(RideState.Driving);
    }
}
