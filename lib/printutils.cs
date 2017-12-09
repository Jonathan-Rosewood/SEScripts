public static class PrintUtils
{
    public static string FormatPower(float value)
    {
        if (value >= 1.0f)
        {
            return string.Format("{0:F2} MW", value);
        }
        else if (value >= 0.001)
        {
            return string.Format("{0:F2} kW", value * 1000f);
        }
        else
        {
            return string.Format("{0:F2} W", value * 1000000f);
        }
    }
}
