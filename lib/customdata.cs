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

    public string GetString(string key, string def = "")
    {
        string value;
        if (!Data.TryGetValue(key.ToLower(), out value))
        {
            value = def;
        }
        return value;
    }

    public int GetInt(string key, int def = 0)
    {
        int value = def;
        string str;
        if (Data.TryGetValue(key.ToLower(), out str))
        {
            if (!int.TryParse(str, out value)) throw new Exception(string.Format("Invalid CustomData int: {0}", str));
        }
        return value;
    }

    public double GetDouble(string key, double def = 0.0)
    {
        double value = def;
        string str;
        if (Data.TryGetValue(key.ToLower(), out str))
        {
            if (!double.TryParse(str, out value)) throw new Exception(string.Format("Invalid CustomData double: {0}", str));
        }
        return value;
    }

    public bool GetBool(string key, bool def = false)
    {
        bool value = def;
        string str;
        if (Data.TryGetValue(key.ToLower(), out str))
        {
            switch (str.ToLower())
            {
                case "t":
                case "true":
                case "y":
                case "yes":
                    {
                        value = true;
                        break;
                    }
                case "f":
                case "false":
                case "n":
                case "no":
                    {
                        value = false;
                        break;
                    }
                default:
                    throw new Exception(string.Format("Invalid CustomData bool: {0}", str));
            }
        }
        return value;
    }

    public Base6Directions.Direction GetDirection(string key, Base6Directions.Direction def = Base6Directions.Direction.Forward)
    {
        Base6Directions.Direction value = def;
        string str;
        if (Data.TryGetValue(key.ToLower(), out str))
        {
            switch (str.ToLower())
            {
                case "forward":
                case "forwards":
                    {
                        value = Base6Directions.Direction.Forward;
                        break;
                    }
                case "backward":
                case "backwards":
                    {
                        value = Base6Directions.Direction.Backward;
                        break;
                    }
                case "left":
                    {
                        value = Base6Directions.Direction.Left;
                        break;
                    }
                case "right":
                    {
                        value = Base6Directions.Direction.Right;
                        break;
                    }
                case "up":
                    {
                        value = Base6Directions.Direction.Up;
                        break;
                    }
                case "down":
                    {
                        value = Base6Directions.Direction.Down;
                        break;
                    }
                default:
                    throw new Exception(string.Format("Invalid CustomData direction: {0}", str));
            }
        }
        return value;
    }
}
