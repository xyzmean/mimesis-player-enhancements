namespace MimesisPlayerEnhancement.Util
{
    internal static class ScalingMath
    {
        internal const int VanillaPlayerBaseline = 4;

        internal static float GetPlayerScale(int playerCount, bool autoScaleEnabled)
        {
            return !autoScaleEnabled || playerCount <= VanillaPlayerBaseline ? 1f : playerCount / (float)VanillaPlayerBaseline;
        }

        internal static int ScaleCount(int vanilla, float multiplier)
        {
            return vanilla == 0 ? 0 : multiplier <= 0f ? 0 : System.Math.Max(1, (int)System.Math.Round(vanilla * multiplier));
        }

        internal static int ScaleCountWithImplicitBase(int vanilla, float multiplier, int implicitWhenZero)
        {
            int baseCount = vanilla > 0 ? vanilla : implicitWhenZero;
            return ScaleCount(baseCount, multiplier);
        }
    }
}
