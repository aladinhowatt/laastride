using System;
using System.Collections.Generic;

public class DChoice
{
    public string text;
    public string next;
    public Action act;
    public Func<bool> when;
}

public class DNode
{
    public string speaker = "";      // empty = narration
    public string portrait = "";     // lung / sri / ton / gas / vendor / ""
    public string text = "";
    public string next;              // null = end of conversation
    public DChoice[] choices;
    public Action onEnter;
    public Action onEnd;             // runs when the conversation finishes from this node
}

public class DScript
{
    public string backdrop = "road";
    public string start;
    public readonly Dictionary<string, DNode> nodes = new Dictionary<string, DNode>();

    public DScript Add(string key, string speaker, string portrait, string text, string next = null,
                       Action onEnter = null, Action onEnd = null, params DChoice[] choices)
    {
        if (start == null) start = key;
        nodes[key] = new DNode
        {
            speaker = speaker, portrait = portrait, text = text, next = next,
            choices = choices != null && choices.Length > 0 ? choices : null,
            onEnter = onEnter, onEnd = onEnd
        };
        return this;
    }
}

/// <summary>All dialogue of the prototype. Text lives here so it is easy to rewrite.</summary>
public static class Story
{
    const string LUNG = "ลุงแอด";
    static RideGame G { get { return RideGame.I; } }

    static DChoice Ch(string text, string next, Action act = null, Func<bool> when = null)
    {
        return new DChoice { text = text, next = next, act = act, when = when };
    }

    public static DScript Intro()
    {
        var s = new DScript { backdrop = "road" };
        s.Add("i1", "", "", "ตีหนึ่ง เยาวราชเงียบลงแล้ว โคมแดงสุดซอยยังแกว่งไปมาเบา ๆ ลุงแอดสตาร์ทตุ๊กตุ๊กคันเก่า ผ้ามาลัยดอกดาวเรืองที่ห้อยหน้ารถแกว่งตาม", "i2");
        s.Add("i2", LUNG, "lung", "คืนนี้ก็เหมือนทุกคืน ขับช้า ๆ ไปเรื่อย ๆ ใครโบกมือก็จอดดูก่อน ตุ๊กตุ๊กคันนี้วิ่งข้ามคืนได้ไกลกว่ารถธรรมดา ถ้าขับให้ถูกจังหวะ", "i3");
        s.Add("i3", "", "", "มองหาเครื่องหมาย (!) ข้างทาง ชะลอรถและจอดข้างจุดนั้น แล้วกด Space เพื่อพูดคุย", null);
        return s;
    }

    // ------------------------------------------------------------------------------------------
    // Ran a red light: the police wave the tuk-tuk over
    // ------------------------------------------------------------------------------------------
    public static DScript Police()
    {
        var s = new DScript { backdrop = "road" };
        s.Add("p1", "", "", "ไฟสีแดงสว่างอยู่ข้างหลัง เสียงนกหวีดดังขึ้น ตำรวจจราจรในเสื้อสะท้อนแสงเดินออกมายกมือให้จอดข้างทาง", "p2");
        s.Add("p2", "ตำรวจ", "police", "คุณลุงครับ ไฟแดงนะครับ ทำไมไม่หยุดครับ", null, null, null,
            Ch("ขอโทษครับ ผมเหม่อไปหน่อย", "sorry"),
            Ch("ผมรีบไปส่งคนก่อนฟ้าสางครับ", "rush", null, () => G.passenger != PassengerId.None),
            Ch("ผมไม่เห็นไฟครับ", "deny"));
        s.Add("sorry", "ตำรวจ", "police", "ครั้งหน้าอย่าลืมนะครับ ถนนดึก ๆ ก็ต้องระวัง วันนี้ตักเตือนก่อน", "end", () => { G.clock += 6f; });
        s.Add("rush", "ตำรวจ", "police", "ผมเข้าใจครับ... แต่ยิ่งรีบยิ่งต้องปลอดภัย ผมเขียนเตือนไว้ก่อน ไปเถอะครับ", "end", () => { G.clock += 10f; G.AddCalm(-4f); });
        s.Add("deny", "ตำรวจ", "police", "ไฟใหญ่ขนาดนั้นเลยนะครับ... ขอตรวจใบขับขี่ก่อน", "end", () => { G.clock += 14f; G.AddCalm(-6f); G.merit = Math.Max(0, G.merit - 1); });
        s.Add("end", "", "", "ลุงแอดค่อย ๆ ออกรถอีกครั้ง คราวนี้จอดรอไฟแดงให้ครบทุกดวง", null);
        return s;
    }

    // ------------------------------------------------------------------------------------------
    // The ghost passengers: every stage has two waiting, the driver takes one. Text lives in Ghosts.cs
    // ------------------------------------------------------------------------------------------
    public static DScript Passenger(PassengerId id)
    {
        var d = Ghosts.Get(id);
        var s = new DScript { backdrop = d.backdrop };
        bool hasNote = !string.IsNullOrEmpty(d.note);
        s.Add("a1", "", "", d.intro, "a2");
        s.Add("a2", d.name, d.key, d.greet, "a3");
        s.Add("a3", LUNG, "lung", "ดึกป่านนี้แล้ว จะไปไหนครับ", "a4");
        s.Add("a4", d.name, d.key, d.why, hasNote ? "a5" : "pick");
        if (hasNote) s.Add("a5", "", "", d.note, "pick");
        s.Add("pick", "", "", "จะให้" + d.name + "ขึ้นรถไหม?", null, null, null,
            Ch("ขึ้นมาเลยครับ", "yes", () => G.Board(id)),
            Ch("ขอถามเรื่องของ" + d.name + "ก่อน", "ask", null, () => !G.Has(d.key + "_asked")),
            Ch("ขอโทษครับ ผมไปอีกทาง", "no"));
        s.Add("ask", d.name, d.key, d.ask, "pick", () => G.Flag(d.key + "_asked"));
        s.Add("yes", d.name, d.key, d.yes, "yes2");
        s.Add("yes2", "", "", d.board, null, () => G.Toast(d.name + "ขึ้นรถแล้ว — พาไปวัดก่อนฟ้าสาง"));
        s.Add("no", LUNG, "lung", "ขอโทษนะ ผมไม่สะดวกจริง ๆ", "no2");
        s.Add("no2", d.name, d.key, d.decline, "no3", () => G.Flag(d.key + "_declined"));
        s.Add("no3", "", "", d.declineNote, null);
        return s;
    }

    // ------------------------------------------------------------------------------------------
    // Rear-ended the car in front: get out, talk it through, lose time
    // ------------------------------------------------------------------------------------------
    public static DScript Crash()
    {
        var s = new DScript { backdrop = "road" };
        s.Add("c1", "", "", "เสียงโครมดังขึ้นท้ายรถคันหน้า ลุงแอดเหยียบเบรกสุดแรง ตุ๊กตุ๊กสะดุ้งไปทั้งคัน คนขับรถคันหน้าจอดชิดข้างทางแล้วเดินลงมาดูท้ายรถ", "c2");
        s.Add("c2", "คนขับรถคันหน้า", "driver", "เฮ้ย! ขับรถยังไงครับลุง ท้ายรถผมบุบหมดแล้ว", null, null, null,
            Ch("ขอโทษครับ ผมผิดเอง ขอชดใช้ค่าซ่อม", "pay", () => { G.clock += 16f; G.fuel = Math.Max(0f, G.fuel - 0.06f); }),
            Ch("ใจเย็น ๆ ครับ ขอคุยกันก่อน", "talk", () => { G.clock += 26f; G.AddMerit(1); }),
            Ch("ผมไม่ได้ตั้งใจ แต่คุณก็ขับช้าเกินไป", "argue", () => { G.clock += 38f; G.AddCalm(-8f); }));
        s.Add("pay", "คนขับรถคันหน้า", "driver", "โห ลุงรับผิดชอบดีนะ งั้นเอาค่าทำสีนิดหน่อยก็พอ ไม่เป็นไรครับ", "end");
        s.Add("talk", "คนขับรถคันหน้า", "driver", "...ก็จริงครับ ไม่มีใครเป็นอะไร ผมแค่ตกใจ ขับตามกันมาเกือบทั้งคืนแล้วนะลุง", "end");
        s.Add("argue", "คนขับรถคันหน้า", "driver", "ผมขับตามกฎทุกอย่างนะครับ! ไม่เถียงดีกว่า แต่ลุงต้องรอผมถ่ายรูปก่อน", "end");
        s.Add("end", "", "", "ลุงแอดกลับขึ้นรถอีกครั้ง เวลาผ่านไปเร็วกว่าที่คิด ฟ้าทางตะวันออกยังมืดอยู่ แต่ดูเหมือนจะเริ่มจางลงนิดหนึ่งแล้ว", null);
        return s;
    }

    // ------------------------------------------------------------------------------------------
    // Spirit house (ศาลพระภูมิ) — the right offering calms the passenger
    // ------------------------------------------------------------------------------------------
    public static DScript Shrine()
    {
        var s = new DScript { backdrop = "shrine" };
        s.Add("s1", "", "", "ศาลพระภูมิหลังเล็กหน้าตึกแถว ธูปสามดอกยังมีควันอยู่ ลุงแอดชะลอรถ ลงมายกมือไหว้ แล้วเปิดกล่องของเซ่นที่เตรียมไว้", "s2");
        s.Add("s2", "", "", "วางอะไรถวายดี?", null, null, null,
            Ch("ข้าวสวยร้อน ๆ กับน้ำพริกปลาทู", "o_rice", () => Offer(0)),
            Ch("น้ำแดงขวดเล็ก", "o_soda", () => Offer(1)),
            Ch("พวงมาลัยดอกมะลิ", "o_flower", () => Offer(2)),
            Ch("ไหว้เฉย ๆ", "o_none"));
        s.Add("o_rice", "", "", "ลุงแอดวางข้าวสวยร้อน ๆ ไว้หน้าศาล ควันข้าวลอยตรงขึ้นไป", "reaction");
        s.Add("o_soda", "", "", "ลุงแอดเสียบหลอดลงขวดน้ำแดง แล้ววางไว้ข้างแจกันดอกไม้", "reaction");
        s.Add("o_flower", "", "", "พวงมาลัยดอกมะลิกลิ่นหอมถูกแขวนไว้ที่เสาศาล", "reaction");
        s.Add("o_none", "", "", "ลุงแอดยกมือไหว้เงียบ ๆ แล้วถอนใจเบา ๆ", "reaction");
        s.Add("reaction", "", "", ShrineReaction(), null);
        return s;
    }

    /// <summary>0 rice, 1 red soda, 2 jasmine garland: every ghost aboard who likes it calms down a lot.</summary>
    static void Offer(int offering)
    {
        var g = G;
        g.AddMerit(1);
        if (g.riders.Count == 0) return;
        bool liked = false;
        foreach (var r in g.riders) { var d = Ghosts.Get(r); if (d != null && d.likes == offering) liked = true; }
        g.AddCalm(liked ? 14f : 4f);
    }

    static string ShrineReaction()
    {
        // evaluated when the script is built: reaction text is neutral so it is safe for every case
        return "ธูปลุกวาบขึ้นหนึ่งครั้ง แล้วควันก็ลอยตรงไปเหมือนมีใครรับไว้ ลุงแอดยกมือไหว้อีกครั้งก่อนกลับขึ้นรถ";
    }

    // ------------------------------------------------------------------------------------------
    // Gas station
    // ------------------------------------------------------------------------------------------
    public static DScript GasStation()
    {
        var s = new DScript { backdrop = "gas" };
        bool has = G.passenger != PassengerId.None;
        s.Add("g1", "", "", has
            ? "ลุงเจ้าของปั๊มมองมาที่เบาะหลังแล้วยกมือไหว้เงียบ ๆ ก่อนจะหันไปหยิบหัวจ่ายน้ำมัน"
            : "ปั๊มเล็ก ๆ ข้างถนนสายเหนือยังเปิดไฟอยู่ ลุงเจ้าของปั๊มนั่งเฝ้าหน้าตู้ด้วยวิทยุเก่า ๆ", "g2");
        s.Add("g2", "ลุงเจ้าของปั๊ม", "gas", "ทางขึ้นดอยช่วงสุดท้ายชันหน่อยนะ เติมให้เต็มดีกว่า คืนนี้รถขึ้นดอยสุเทพเยอะ ไม่รู้ทำไม", "g3");
        s.Add("g3", "", "", "จะเติมเท่าไหร่?", null, null, null,
            Ch("เต็มถัง", "full", () => { G.fuel = 1f; }),
            Ch("ร้อยเดียวพอ", "half", () => { G.fuel = Math.Min(1f, G.fuel + 0.5f); }),
            Ch("ขอคุยกับลุงก่อน", "chat", null, () => !G.Has("gas_chat")));
        s.Add("chat", "ลุงเจ้าของปั๊ม", "gas", "ตั้งแต่ลุงเฝ้าปั๊มนี่มานะ คืนที่มีรถขับผ่านคนเดียวนี่แหละ คืนที่มีคนต้องไปส่ง ลุงเลยเปิดไฟทิ้งไว้ให้ทุกคืน", "g3", () => { G.Flag("gas_chat"); G.AddMerit(1); });
        s.Add("full", "", "", "ถังเต็มแล้ว ลุงยื่นแก้วน้ำเย็นให้อีกแก้ว \"เอาไปให้คนข้างหลังด้วยนะ\"", null, () => { if (G.passenger != PassengerId.None) G.AddCalm(5f); });
        s.Add("half", "", "", "ลุงเจ้าของปั๊มพยักหน้า เติมให้อย่างเงียบ ๆ พลางมองฟ้าทางตะวันออก", null, () => { if (G.passenger != PassengerId.None) G.AddCalm(3f); });
        return s;
    }

    // ------------------------------------------------------------------------------------------
    // Roadside drink stall — a cosy optional stop
    // ------------------------------------------------------------------------------------------
    public static DScript DrinkStall()
    {
        var s = new DScript { backdrop = "shop" };
        s.Add("d1", "", "", "ร้านชำริมถนนบนเขายังไม่ปิด ป้าเจ้าของร้านกำลังนั่งเรียงขวดน้ำเก๊กฮวยใต้หลอดไฟสีส้ม ไอหมอกลอยมาจากหุบเขา", "d2");
        s.Add("d2", "ป้าเจ้าของร้าน", "vendor", "ดึกแล้วนะลูก ขับรถช้า ๆ น้ำเก๊กฮวยเย็น ๆ แก้วนึงไหม ป้าแถมให้", "d3");
        s.Add("d3", "", "", "จะรับไหม?", null, null, null,
            Ch("รับครับ ขอบคุณป้า", "take", () => { G.AddMerit(1); if (G.passenger != PassengerId.None) G.AddCalm(6f); }),
            Ch("ไม่เป็นไรครับ", "skip"));
        s.Add("take", "", "", "ลุงแอดดื่มน้ำเย็น ๆ ให้ใจชื้นขึ้น และวางขวดอีกใบไว้ที่เบาะหลัง เผื่อ \"ใครสักคน\" ", null);
        s.Add("skip", "ป้าเจ้าของร้าน", "vendor", "งั้นเดินทางปลอดภัยนะลูก", null);
        return s;
    }

    // ------------------------------------------------------------------------------------------
    // Temple at the end of the road
    // ------------------------------------------------------------------------------------------
    public static DScript Temple()
    {
        var s = new DScript { backdrop = "suthep" };
        var riders = G.riders;
        string first = riders.Count == 0 ? "w_alone" : "r0";
        s.Add("w1", "", "", "ถนนช่วงสุดท้ายพาขึ้นดอยสุเทพ บันไดพญานาคทอดขึ้นไปหาพระธาตุสีทอง ฟ้าทางตะวันออกเริ่มจางเป็นสีส้มอ่อน ๆ", first);

        // nobody on board
        s.Add("w_alone", "", "", "เบาะหลังว่างเปล่าคืนนี้ ลุงแอดจอดรถ ยกมือไหว้พระธาตุบนดอย นั่งฟังเสียงระฆังเช้าอยู่คนเดียวจนฟ้าสว่าง", null, null,
            () => G.EndRide("คืนที่เงียบ",
                "คืนนี้ไม่มีใครขึ้นรถ ลุงแอดขับจากเยาวราชมาถึงดอยสุเทพคนเดียว แต่ไฟข้างทางทุกดวงยังเปิดรอไว้ให้ ใครสักคนที่อาจมาในคืนหน้า"));

        // each ghost says their piece, one after another
        for (int i = 0; i < riders.Count; i++)
        {
            var d = Ghosts.Get(riders[i]);
            string next = i + 1 < riders.Count ? "r" + (i + 1) : "w3";
            s.Add("r" + i, d.name, d.key, d.temple, next);
        }
        string names = G.RiderNames;
        int count = riders.Count;
        s.Add("w3", "", "", "ก่อนฟ้าสางจะมาถึง มีสิ่งที่ลุงแอดทำได้อีกอย่าง", null, null, null,
            Ch("กรวดน้ำอุทิศส่วนกุศลให้", "w_water", () => { G.AddMerit(2); G.AddCalm(10f); }),
            Ch("นั่งฟังเรื่องที่ค้างใจอยู่ก่อน", "w_listen", () => { G.AddCalm(6f); }),
            Ch("ยกมือไหว้ส่ง แล้วนั่งเงียบ ๆ", "w_quiet"));
        s.Add("w_water", "", "", "ลุงแอดรินน้ำช้า ๆ จากแก้วลงบนพื้นดินใต้ต้นไม้ ตามเสียงสวดที่ได้ยินมาจากโบสถ์ น้ำหยดสุดท้ายหยุดลงพร้อมกับระฆังแรกของเช้า", "w_end");
        s.Add("w_listen", "", "", "ลุงแอดนั่งฟังจนจบทุกประโยค แม้บางประโยคจะซ้ำ ๆ แต่ไม่มีใครรีบลุกไปไหน", "w_end");
        s.Add("w_quiet", "", "", "ลุงแอดไม่พูดอะไร แค่ยกมือไหว้ และปล่อยให้ความเงียบทำหน้าที่ของมัน", "w_end");

        s.Add("w_end", "", "", "ท้องฟ้าด้านตะวันออกสว่างขึ้นแล้ว", null, null,
            () =>
            {
                var g = G;
                bool good = g.calm >= 55f;
                if (good && count >= 3)
                    g.EndRide("ตุ๊กตุ๊กเต็มคัน",
                        names + " ค่อย ๆ จางไปกับแสงแรกของวัน ทีละคน ทีละคน ที่นั่งหลังตุ๊กตุ๊กว่างเปล่า แต่ลุงแอดรู้สึกเหมือนมีเสียงหัวเราะเบา ๆ ตามลมมาอีกนาน");
                else if (good)
                    g.EndRide("ส่งใจถึงฝั่ง",
                        names + " ยิ้มบาง ๆ แล้วค่อย ๆ จางไปกับแสงแรกของวัน ที่นั่งหลังตุ๊กตุ๊กว่างเปล่าอีกครั้ง แต่ลุงแอดรู้สึกเบาขึ้นอย่างบอกไม่ถูก");
                else
                    g.EndRide("ยังมีเรื่องค้างใจ",
                        names + " ไปถึงวัดทันเวลา แต่ยังมีบางเรื่องที่พูดไม่หมด ลุงแอดสัญญากับตัวเองว่า คืนหน้าจะฟังให้มากกว่านี้");
            });
        return s;
    }

    // ------------------------------------------------------------------------------------------
    public static string TimeUpTitle(RideGame g) { return "ฟ้าสางเสียก่อน"; }

    public static string TimeUpBody(RideGame g)
    {
        if (g.passenger == PassengerId.None)
            return "ฟ้าสว่างแล้ว ลุงแอดยังขับอยู่กลางทางสายเหนือ คืนนี้ไม่มีใครขึ้นรถ และไม่มีใครต้องไปส่ง";
        return g.RiderNames + " เงยหน้ามองฟ้า ยิ้มบาง ๆ แล้วจางหายไปกับแสงแรก ก่อนที่ตุ๊กตุ๊กจะถึงดอยสุเทพ เรื่องบางเรื่องคงต้องรอไว้คืนหน้า";
    }
}
