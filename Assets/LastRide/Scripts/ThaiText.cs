using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// TextMesh Pro does no OpenType substitution, so a tone mark that sits on an upper vowel
/// (หนึ่ง, ที่, ปั้น, กิ๊ก ...) is drawn exactly on top of the vowel and seems to vanish.
/// This lifts those tone marks with a rich-text voffset. Every string shown through TMP goes through Fix().
/// </summary>
public static class ThaiText
{
    public static bool ToneFix = true;
    public static float Lift = 0.16f;       // em; tuned on Leelawadee UI

    // upper vowels / sara am-ring (ั ิ ี ึ ื ํ) followed by a tone mark (่ ้ ๊ ๋)
    static readonly Regex Stacked = new Regex("([\u0E31\u0E34-\u0E37\u0E4D])([\u0E48-\u0E4B])", RegexOptions.Compiled);

    public static string Fix(string s)
    {
        if (!ToneFix || string.IsNullOrEmpty(s)) return s;
        string tag = "<voffset=" + Lift.ToString("0.###", CultureInfo.InvariantCulture) + "em>";
        return Stacked.Replace(s, "$1" + tag + "$2</voffset>");
    }
}
