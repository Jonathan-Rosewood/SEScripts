public class SolarHack
{
    public static float? GetSolarPanelMaxOutput(IMySolarPanel panel)
    {
        var lines = panel.DetailedInfo.Split(new char[] { '\n' });
        if (lines.Length == 3)
        {
            // Second line
            var parts = lines[1].Split(new char[] { ':' });
            if (parts.Length == 2)
            {
                // Right half
                var maxOutputText = parts[1].Trim();
                var match = System.Text.RegularExpressions.Regex.Match(maxOutputText, "([0-9]+(\\.[0-9]+)?) *([kM]?W)");
                if (match.Success)
                {
                    var power = float.Parse(match.Groups[1].Value);
                    var units = match.Groups[3].Value;
                    switch (units)
                    {
                        case "W":
                            power /= 1000000.0f;
                            break;
                        case "kW":
                            power /= 1000.0f;
                            break;
                        case "MW":
                            break;
                        default:
                            throw new Exception("Unknown power units: " + units);
                    }
                    return power;
                }
                else throw new Exception("Regex match fail: " + maxOutputText);
            }
        }
        return null;
    }
}
