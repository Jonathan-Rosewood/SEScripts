public static class ZASplit
{
    // Do enums work in SE?
    private const int NORMAL = 0;
    private const int ESCAPED = 1;
    private const int QUOTED = 2;
    private const int QUOTED_ESCAPED = 3;
    
    public static List<string> Split(string input, bool complete = true)
    {
        var result = new List<string>();

        var state = NORMAL;
        var current = new StringBuilder();

        foreach (var c in input)
        {
            switch (state)
            {
                case NORMAL:
                    if (c == '\\') state = ESCAPED;
                    else if (c == '"') state = QUOTED;
                    else
                    {
                        if (current.Length == 0)
                        {
                            // Current token empty, skip leading whitespace
                            if (!Char.IsWhiteSpace(c)) current.Append(c);
                        }
                        else if (Char.IsWhiteSpace(c))
                        {
                            // End of token
                            result.Add(current.ToString());
                            current = new StringBuilder();
                        }
                        else current.Append(c);
                    }
                    break;
                case ESCAPED:
                case QUOTED_ESCAPED:
                    if (c == '\\') current.Append('\\');
                    else if (c == '"') current.Append('"');
                    else
                    {
                        // Not a valid escape
                        current.Append('\\');
                        current.Append(c);
                    }
                    state = state == ESCAPED ? NORMAL : QUOTED;
                    break;
                case QUOTED:
                    if (c == '\\') state = QUOTED_ESCAPED;
                    else if (c == '"') state = NORMAL;
                    else current.Append(c);
                    break;
            }
        }

        // Throw if quote isn't terminated. Note we don't really care about unfinished escape sequences.
        if (complete && (state == QUOTED || state == QUOTED_ESCAPED))
        {
            throw new Exception("Unterminated quote");
        }

        if (state == ESCAPED) current.Append('\\');

        // Check final token
        if (current.Length > 0) result.Add(current.ToString());

        return result;
    }
}
