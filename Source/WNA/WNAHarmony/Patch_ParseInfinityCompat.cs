using System.Globalization;
using HarmonyLib;
using Verse;

namespace WNA.WNAHarmony
{
    internal class Patch_ParseInfinityCompat
    {
        // The game writes floats with ToString("G9") using the current culture (zh-CN -> "∞"),
        // but parses them with InvariantCulture ("Infinity"), so any infinite float in a save
        // throws on load and silently falls back to 0. Accept the culture-specific symbols too.
        [HarmonyPatch(typeof(ParseHelper), nameof(ParseHelper.ParseFloat))]
        internal static class Patch_ParseHelper_ParseFloat
        {
            internal static bool Prefix(string str, ref float __result)
            {
                if (!TryParseSpecial(str, out float value))
                    return true;
                __result = value;
                return false;
            }
        }

        [HarmonyPatch(typeof(ParseHelper), nameof(ParseHelper.ParseDouble))]
        internal static class Patch_ParseHelper_ParseDouble
        {
            internal static bool Prefix(string str, ref double __result)
            {
                if (!TryParseSpecial(str, out float value))
                    return true;
                __result = value;
                return false;
            }
        }

        private static bool TryParseSpecial(string str, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(str))
                return false;
            NumberFormatInfo invariant = NumberFormatInfo.InvariantInfo;
            NumberFormatInfo current = CultureInfo.CurrentCulture.NumberFormat;
            if (str == invariant.PositiveInfinitySymbol
                || str == current.PositiveInfinitySymbol
                || str == "∞")
            {
                value = float.PositiveInfinity;
                return true;
            }
            if (str == invariant.NegativeInfinitySymbol
                || str == current.NegativeInfinitySymbol
                || str == "-∞")
            {
                value = float.NegativeInfinity;
                return true;
            }
            if (str == invariant.NaNSymbol || str == current.NaNSymbol)
            {
                value = float.NaN;
                return true;
            }
            return false;
        }
    }
}
