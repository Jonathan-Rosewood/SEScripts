//@ split
public class ZACustomData
{
    private Dictionary<string, string> Data = new Dictionary<string, string>();

    public void Parse(IMyTerminalBlock block)
    {
        Data.Clear();

        var lines = System.Text.RegularExpressions.Regex.Split(block.CustomData, "\r\n|\r|\n");
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            var tokens = ZASplit.Split(trimmed);
            if (tokens.Count != 2)
            {
                throw new Exception(string.Format("Invalid CustomData: {0}", trimmed));
            }
            Data[tokens[0].ToLower()] = tokens[1];
        }
    }

    public string GetValue(string key, string def)
    {
        string value;
        if (!Data.TryGetValue(key.ToLower(), out value))
        {
            value = def;
        }
        return value;
    }

    public string GetValue(string key)
    {
        return GetValue(key, null);
    }
}
