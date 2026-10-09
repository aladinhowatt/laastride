using UnityEngine;

public struct RegionInfo
{
    public string key, title, subtitle;
    public float start;
}

/// <summary>The route: Yaowarat -> Ayutthaya -> Sukhothai -> the northern road -> Doi Suthep.</summary>
public static class RouteRegions
{
    public static readonly RegionInfo[] All =
    {
        new RegionInfo { key = "yaowarat",  title = "เยาวราช",         subtitle = "กรุงเทพมหานคร",    start = 0f },
        new RegionInfo { key = "ayutthaya", title = "พระนครศรีอยุธยา",  subtitle = "เมืองเก่าริมน้ำ",    start = 290f },
        new RegionInfo { key = "sukhothai", title = "สุโขทัย",         subtitle = "เมืองแห่งรุ่งอรุณ",  start = 620f },
        new RegionInfo { key = "north",     title = "ถนนสู่ภาคเหนือ",   subtitle = "ขุนเขาและสายหมอก",  start = 930f },
        new RegionInfo { key = "suthep",    title = "ดอยสุเทพ",        subtitle = "เชียงใหม่",          start = 1170f },
    };

    public static int IndexAt(float distance)
    {
        int cur = 0;
        for (int i = 0; i < All.Length; i++) if (distance >= All[i].start) cur = i;
        return cur;
    }

    public static float EndOf(int index, float routeLength)
    {
        return index < All.Length - 1 ? All[index + 1].start : routeLength + 8f;
    }
}
