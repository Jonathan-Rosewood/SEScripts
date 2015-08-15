public class DumbSerializer
{
    private const char KEY_DELIM = '\\';
    private const char PAIR_DELIM = '$';
    private readonly string PAIR_DELIM_STR = new string(PAIR_DELIM, 1);

    public readonly Dictionary<string, string> Data = new Dictionary<string, string>();

    public string Encode()
    {
        var encoded = new List<string>();

        for (var e = Data.GetEnumerator(); e.MoveNext();)
        {
            var kv = e.Current;
            ValidityCheck(kv.Key);
            ValidityCheck(kv.Value);
            var pair = new StringBuilder();
            pair.Append(kv.Key);
            pair.Append(KEY_DELIM);
            pair.Append(kv.Value);
            encoded.Add(pair.ToString());
        }

        return string.Join(PAIR_DELIM_STR, encoded);
    }

    public void Decode(string data)
    {
        Data.Clear();

        var pairs = data.Split(new Char[] { PAIR_DELIM });
        for (int i = 0; i < pairs.Length; i++)
        {
            var parts = pairs[i].Split(new Char[] { KEY_DELIM }, 2);
            if (parts.Length == 2)
            {
                Data[parts[0]] = parts[1];
            }
        }
    }

    private void ValidityCheck(string value)
    {
        if (value.IndexOf(KEY_DELIM) >= 0 ||
            value.IndexOf(PAIR_DELIM) >= 0)
        {
            throw new Exception(string.Format("String '{0}' cannot be used by DumbSerializer!", value));
        }
    }
}
