using System.Collections.Generic;

/// <summary>
/// The drive-by scenery pieces: AI-generated, transparent, trimmed sprites in Resources/LastRide/AI/Elements.
/// Heights are the size of each piece in world units (16 px = 1 unit art grid; the screen is 30 x 16.9 units).
/// The editor importer sizes every sprite from this table, and SceneryField picks pieces per place from it.
/// </summary>
public static class ElementCatalog
{
    public const string Folder = "LastRide/AI/Elements/";

    public struct Def
    {
        public string name;
        public float weight;
        public Def(string n, float w) { name = n; weight = w; }
    }

    static readonly Dictionary<string, float> heights = new Dictionary<string, float>
    {
        // Yaowarat
        { "yao_shophouse_a", 8.5f }, { "yao_shophouse_b", 7.2f }, { "yao_shophouse_c", 6.2f }, { "yao_gate", 7.5f },
        { "yao_stall", 3.2f }, { "yao_lantern_pole", 5.5f }, { "yao_neon_sign", 6.5f }, { "yao_trafficlight", 4.8f },
        { "far_highrise", 7.0f },
        // Ayutthaya
        { "ayu_prang", 9f }, { "ayu_chedi", 6.5f }, { "ayu_buddha_row", 2.6f }, { "ayu_banyan_head", 5.2f },
        { "ayu_ruin_wall", 2.6f }, { "ayu_tree", 7f }, { "ayu_buddha_big", 4.2f }, { "ayu_lotus", 1.1f },
        // Sukhothai
        { "suk_chedi", 8.5f }, { "suk_columns", 4.5f }, { "suk_walking_buddha", 5f }, { "suk_palm", 8.5f },
        { "suk_lotus", 1.1f }, { "suk_buddha", 4.4f }, { "suk_wall", 2.4f },
        // North
        { "north_house", 5f }, { "north_pine", 8f }, { "north_teak", 7.5f }, { "north_terrace", 2.4f },
        { "north_mountain", 6f }, { "north_bamboo", 6f },
        // Doi Suthep
        { "suthep_stairs", 9f }, { "suthep_chedi", 8f }, { "suthep_hall", 5.5f }, { "suthep_trees", 7.5f }, { "suthep_lanterns", 3f },
        // event props
        { "spirit_house", 3.2f }, { "gas_station", 5f }, { "roadside_shop", 4.2f },
        // generic far band
        { "far_treeline", 3.5f },
    };

    public static bool Has(string name) { return heights.ContainsKey(name); }

    public static float Height(string name)
    {
        float h;
        return heights.TryGetValue(name, out h) ? h : 4f;
    }

    // ---- what grows along each stretch of road ------------------------------------------------
    public static readonly Dictionary<string, Def[]> Near = new Dictionary<string, Def[]>
    {
        { "yaowarat", new[] { new Def("yao_shophouse_a", 2f), new Def("yao_shophouse_b", 2f), new Def("yao_shophouse_c", 2f),
                              new Def("yao_stall", 0.8f), new Def("yao_lantern_pole", 0.8f), new Def("yao_neon_sign", 0.8f) } },
        { "ayutthaya", new[] { new Def("ayu_prang", 1.2f), new Def("ayu_chedi", 1.2f), new Def("ayu_buddha_row", 1f),
                               new Def("ayu_ruin_wall", 1.2f), new Def("ayu_tree", 1.5f), new Def("ayu_buddha_big", 0.8f),
                               new Def("ayu_lotus", 1.2f) } },
        { "sukhothai", new[] { new Def("suk_chedi", 1.2f), new Def("suk_columns", 1.2f), new Def("suk_walking_buddha", 0.8f),
                               new Def("suk_palm", 1.5f), new Def("suk_lotus", 1.2f), new Def("suk_buddha", 1f), new Def("suk_wall", 1f) } },
        { "north", new[] { new Def("north_house", 1.5f), new Def("north_pine", 2f), new Def("north_teak", 1.5f),
                           new Def("north_terrace", 1f), new Def("north_bamboo", 1f) } },
        { "suthep", new[] { new Def("suthep_trees", 2f), new Def("suthep_hall", 0.6f), new Def("suthep_lanterns", 1f), new Def("north_pine", 1f) } },
    };

    /// <summary>Distant silhouettes (parallax 0.25) per place.</summary>
    public static readonly Dictionary<string, string> VeryFar = new Dictionary<string, string>
    {
        { "yaowarat", "far_highrise" }, { "ayutthaya", "far_treeline" }, { "sukhothai", "far_treeline" },
        { "north", "north_mountain" }, { "suthep", "north_mountain" },
    };

    /// <summary>Landmarks placed once: (element, route position of its centre).</summary>
    public static readonly KeyValuePair<string, float>[] Landmarks =
    {
        new KeyValuePair<string, float>("yao_gate", 26f),
        new KeyValuePair<string, float>("suthep_chedi", 1262f),
        new KeyValuePair<string, float>("suthep_stairs", 1296f),
    };
}
