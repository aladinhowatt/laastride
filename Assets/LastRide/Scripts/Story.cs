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
        s.Add("p2", "ตำรวจ", "", "คุณลุงครับ ไฟแดงนะครับ ทำไมไม่หยุดครับ", null, null, null,
            Ch("ขอโทษครับ ผมเหม่อไปหน่อย", "sorry"),
            Ch("ผมรีบไปส่งคนก่อนฟ้าสางครับ", "rush", null, () => G.passenger != PassengerId.None),
            Ch("ผมไม่เห็นไฟครับ", "deny"));
        s.Add("sorry", "ตำรวจ", "", "ครั้งหน้าอย่าลืมนะครับ ถนนดึก ๆ ก็ต้องระวัง วันนี้ตักเตือนก่อน", "end", () => { G.clock += 6f; });
        s.Add("rush", "ตำรวจ", "", "ผมเข้าใจครับ... แต่ยิ่งรีบยิ่งต้องปลอดภัย ผมเขียนเตือนไว้ก่อน ไปเถอะครับ", "end", () => { G.clock += 10f; G.AddCalm(-4f); });
        s.Add("deny", "ตำรวจ", "", "ไฟใหญ่ขนาดนั้นเลยนะครับ... ขอตรวจใบขับขี่ก่อน", "end", () => { G.clock += 14f; G.AddCalm(-6f); G.merit = Math.Max(0, G.merit - 1); });
        s.Add("end", "", "", "ลุงแอดค่อย ๆ ออกรถอีกครั้ง คราวนี้จอดรอไฟแดงให้ครบทุกดวง", null);
        return s;
    }

    // ------------------------------------------------------------------------------------------
    // Passenger 1 — ป้าศรี, rice-curry seller, waiting in a roadside sala
    // ------------------------------------------------------------------------------------------
    public static DScript PassengerSri()
    {
        var s = new DScript { backdrop = "yaowarat" };
        s.Add("a1", "", "", "ตีหนึ่งกว่าแล้ว ริมถนนเยาวราชที่เงียบลง มีหญิงสูงวัยนั่งอยู่ข้างรถเข็นข้าวแกงที่เก็บของแล้ว มือกอดถุงใส่กล่องข้าวใบเก่า ตุ๊กตุ๊กค่อย ๆ จอดเข้าข้างทาง", "a2");
        s.Add("a2", "ป้าศรี", "sri", "โอ๊ย ขอบใจนะลูกที่จอดให้ ป้านั่งรอรถมาตั้งแต่หัวค่ำ ไม่มีใครชะลอเลยสักคัน", "a3");
        s.Add("a3", LUNG, "lung", "ดึกป่านนี้ ป้าจะไปไหนครับ", "a4");
        s.Add("a4", "ป้าศรี", "sri", "ไปวัดบนดอยสุเทพที่เชียงใหม่น่ะลูก มีของต้องไปส่ง แล้วก็มีคนที่ป้าอยากไปบอกก่อนฟ้าสาง", "a5");
        s.Add("a5", "", "", "เชียงใหม่เลยเหรอ... ไกลกว่าที่รถคันไหนจะไปทันก่อนฟ้าสาง ถุงในมือป้าไม่มีน้ำหนักเลยสักนิด และตัวป้า... เย็นกว่าที่ควรจะเป็น", "pick");
        s.Add("pick", "", "", "จะให้ป้าขึ้นรถไหม?", null, null, null,
            Ch("ขึ้นมาเลยครับป้า", "yes", () => { G.passenger = PassengerId.Sri; G.calm = 70f; G.Flag("sri"); }),
            Ch("ขอถามชื่อป้าก่อนครับ", "ask", null, () => !G.Has("sri_asked")),
            Ch("ขอโทษครับ ผมไปอีกทาง", "no"));
        s.Add("ask", "ป้าศรี", "sri", "ป้าศรีจ้ะ ขายข้าวแกงหน้าตลาดมาสามสิบปีแล้ว ไม่ต้องกลัวนะลูก ป้าไม่ทำอะไรใครหรอก", "pick", () => G.Flag("sri_asked"));
        s.Add("yes", "ป้าศรี", "sri", "ขอบใจนะลูก ป้าจะนั่งเงียบ ๆ ไม่กวนเลย ขับไปทางเหนือเรื่อย ๆ นะ", "yes2");
        s.Add("yes2", "", "", "ป้าศรีขึ้นไปนั่งเบาะหลัง เบาจนรถแทบไม่ยวบ ไฟหน้ากะพริบหนึ่งครั้ง แล้วสว่างขึ้นกว่าเดิม", null, () => G.Toast("ป้าศรีขึ้นรถแล้ว — พาไปวัดก่อนฟ้าสาง"));
        s.Add("no", LUNG, "lung", "ขอโทษครับป้า ผมต้องไปอีกทาง", "no2");
        s.Add("no2", "ป้าศรี", "sri", "ไม่เป็นไรจ้ะลูก ป้านั่งรอคันต่อไปก็ได้", "no3", () => G.Flag("sri_declined"));
        s.Add("no3", "", "", "ป้าศรียิ้มบาง ๆ ในกระจกมองหลัง ข้างรถเข็นข้าวแกงว่างเปล่าไปแล้ว");
        return s;
    }

    // ------------------------------------------------------------------------------------------
    // Passenger 2 — พี่ต้น, truck driver, under the banyan at the bend
    // ------------------------------------------------------------------------------------------
    public static DScript PassengerTon()
    {
        var s = new DScript { backdrop = "ayutthaya" };
        if (G.passenger != PassengerId.None)
        {
            s.Add("t1", "", "", "ในอยุธยา ใต้ต้นไม้ใหญ่ข้างวัดเก่าที่รากพันก้อนอิฐ มีชายใส่เสื้อคนขับรถบรรทุกยืนอยู่ เขามองมาที่ตุ๊กตุ๊ก แล้วมองที่เบาะหลังที่มีคนนั่งอยู่แล้ว ก่อนจะยกมือไหว้", "t2");
            s.Add("t2", "พี่ต้น", "ton", "ไม่เป็นไรครับ พี่รอคืนอื่นก็ได้ ขอให้ถึงที่หมายนะ", null, () => G.Flag("ton_met"));
            return s;
        }
        s.Add("b1", "", "", "อยุธยายามดึก ใต้ต้นไม้ใหญ่ข้างวัดเก่าที่รากพันก้อนอิฐ มีชายใส่เสื้อคนขับรถบรรทุกยืนโบกมืออยู่ ข้างหลังเขาไม่มีรถจอดอยู่เลยสักคัน", "b2");
        s.Add("b2", "พี่ต้น", "ton", "น้องครับ ขอติดรถไปด้วยได้ไหม พี่ขับรถมาทั้งชีวิต ไม่ได้นั่งเบาะหลังมานานแล้ว", "b3");
        s.Add("b3", "พี่ต้น", "ton", "พี่จะไปดอยสุเทพ เชียงใหม่ ลูกสาวพี่ไปทำบุญอยู่ที่นั่น พี่อยากเจอเขาก่อนฟ้าสาง", "pick");
        s.Add("pick", "", "", "จะให้พี่ต้นขึ้นรถไหม?", null, null, null,
            Ch("เชิญครับพี่", "yes", () => { G.passenger = PassengerId.Ton; G.calm = 70f; G.Flag("ton"); }),
            Ch("ถามพี่ว่าเกิดอะไรขึ้น", "ask", null, () => !G.Has("ton_asked")),
            Ch("ขอโทษครับพี่ ผมไม่สะดวก", "no"));
        s.Add("ask", "พี่ต้น", "ton", "เบรกไม่ทันตรงโค้งหน้าวัดนี้เมื่อสามปีก่อน แต่พี่ไม่รู้ตัวเลย ว่าพี่ไม่ได้ขับออกไปไหนต่อ... ไม่ต้องกลัวนะน้อง", "pick", () => G.Flag("ton_asked"));
        s.Add("yes", "พี่ต้น", "ton", "ขอบคุณมากน้อง เบรกเบา ๆ นะ พี่ตกใจง่าย", "yes2");
        s.Add("yes2", "", "", "พี่ต้นขึ้นนั่งเบาะหลังอย่างเกรงใจ ไอเย็น ๆ ลอยเข้ามาในรถ และเสียงปี่จากวิทยุก็ค่อย ๆ เบาลง", null, () => G.Toast("พี่ต้นขึ้นรถแล้ว — พาไปวัดก่อนฟ้าสาง"));
        s.Add("no", LUNG, "lung", "ขอโทษนะพี่ ผมไม่สะดวกจริง ๆ", "no2");
        s.Add("no2", "พี่ต้น", "ton", "ไม่เป็นไรน้อง พี่ยืนรอตรงนี้ก็ชินแล้ว", null, () => G.Flag("ton_declined"));
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
            Ch("ข้าวสวยร้อน ๆ กับน้ำพริกปลาทู", "o_rice", () => Offer(PassengerId.Sri)),
            Ch("น้ำแดงขวดเล็ก", "o_soda", () => Offer(PassengerId.Ton)),
            Ch("พวงมาลัยดอกมะลิ", "o_flower", () => Offer(PassengerId.None)),
            Ch("ไหว้เฉย ๆ", "o_none"));
        s.Add("o_rice", "", "", "ลุงแอดวางข้าวสวยร้อน ๆ ไว้หน้าศาล ควันข้าวลอยตรงขึ้นไป", "reaction");
        s.Add("o_soda", "", "", "ลุงแอดเสียบหลอดลงขวดน้ำแดง แล้ววางไว้ข้างแจกันดอกไม้", "reaction");
        s.Add("o_flower", "", "", "พวงมาลัยดอกมะลิกลิ่นหอมถูกแขวนไว้ที่เสาศาล", "reaction");
        s.Add("o_none", "", "", "ลุงแอดยกมือไหว้เงียบ ๆ แล้วถอนใจเบา ๆ", "reaction");
        s.Add("reaction", "", "", ShrineReaction(), null);
        return s;
    }

    static void Offer(PassengerId liked)
    {
        var g = G;
        g.AddMerit(1);
        if (g.passenger == PassengerId.None) return;
        g.AddCalm(g.passenger == liked ? 14f : 4f);
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
        var p = G.passenger;
        s.Add("w1", "", "", "ถนนช่วงสุดท้ายพาขึ้นดอยสุเทพ บันไดพญานาคทอดขึ้นไปหาพระธาตุสีทอง ฟ้าทางตะวันออกเริ่มจางเป็นสีส้มอ่อน ๆ", p == PassengerId.None ? "w_alone" : "w2");

        // nobody on board
        s.Add("w_alone", "", "", "เบาะหลังว่างเปล่าคืนนี้ ลุงแอดจอดรถ ยกมือไหว้พระธาตุบนดอย นั่งฟังเสียงระฆังเช้าอยู่คนเดียวจนฟ้าสว่าง", null, null,
            () => G.EndRide("คืนที่เงียบ",
                "คืนนี้ไม่มีใครขึ้นรถ ลุงแอดขับจากเยาวราชมาถึงดอยสุเทพคนเดียว แต่ไฟข้างทางทุกดวงยังเปิดรอไว้ให้ ใครสักคนที่อาจมาในคืนหน้า"));

        string name = G.PassengerName;
        s.Add("w2", name, p == PassengerId.Sri ? "sri" : "ton", p == PassengerId.Sri
            ? "ถึงแล้วสินะลูก... ลูกชายป้ามาทำบุญที่ดอยนี้ทุกปี ป้าไม่ได้อยากเห็นหน้าเขาหรอก ป้าแค่อยากให้เขารู้ว่าป้าไม่โกรธ"
            : "ถึงแล้ว... ลูกสาวพี่ยืนอยู่ตรงนั้น แกมาทำบุญให้พี่ทุกปี พี่แค่อยากให้แกรู้ว่าพี่ไม่เป็นอะไรแล้ว", "w3");
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
                if (good)
                    g.EndRide("ส่งใจถึงฝั่ง",
                        name + " ยิ้มบาง ๆ แล้วค่อย ๆ จางไปกับแสงแรกของวัน ที่นั่งหลังตุ๊กตุ๊กว่างเปล่าอีกครั้ง แต่ลุงแอดรู้สึกเบาขึ้นอย่างบอกไม่ถูก");
                else
                    g.EndRide("ยังมีเรื่องค้างใจ",
                        name + " ไปถึงวัดทันเวลา แต่ยังมีบางเรื่องที่พูดไม่หมด ลุงแอดสัญญากับตัวเองว่า คืนหน้าจะฟังให้มากกว่านี้");
            });
        return s;
    }

    // ------------------------------------------------------------------------------------------
    public static string TimeUpTitle(RideGame g) { return "ฟ้าสางเสียก่อน"; }

    public static string TimeUpBody(RideGame g)
    {
        if (g.passenger == PassengerId.None)
            return "ฟ้าสว่างแล้ว ลุงแอดยังขับอยู่กลางทางสายเหนือ คืนนี้ไม่มีใครขึ้นรถ และไม่มีใครต้องไปส่ง";
        return g.PassengerName + " เงยหน้ามองฟ้า ยิ้มบาง ๆ แล้วจางหายไปกับแสงแรก ก่อนที่ตุ๊กตุ๊กจะถึงดอยสุเทพ เรื่องบางเรื่องคงต้องรอไว้คืนหน้า";
    }
}
