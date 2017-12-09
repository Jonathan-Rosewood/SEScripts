public static class ZASplit
{
    enum States { Normal, Escaped, Quoted, QuotedEscaped };
    
    public static List<string> Split(string input, bool complete = true)
    {
        var result = new List<string>();

        var state = States.Normal;
        var current = new StringBuilder();

        foreach (var c in input)
        {
            switch (state)
            {
                case States.Normal:
                    if (c == '\\') state = States.Escaped;
                    else if (c == '"') state = States.Quoted;
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
                case States.Escaped:
                case States.QuotedEscaped:
                    if (c == '\\') current.Append('\\');
                    else if (c == '"') current.Append('"');
                    else
                    {
                        // Not a valid escape
                        current.Append('\\');
                        current.Append(c);
                    }
                    state = state == States.Escaped ? States.Normal : States.Quoted;
                    break;
                case States.Quoted:
                    if (c == '\\') state = States.QuotedEscaped;
                    else if (c == '"') state = States.Normal;
                    else current.Append(c);
                    break;
            }
        }

        // Throw if quote isn't terminated. Note we don't really care about unfinished escape sequences.
        if (complete && (state == States.Quoted || state == States.QuotedEscaped))
        {
            throw new Exception("Unterminated quote");
        }

        if (state == States.Escaped) current.Append('\\');

        // Check final token
        if (current.Length > 0) result.Add(current.ToString());

        return result;
    }
}
