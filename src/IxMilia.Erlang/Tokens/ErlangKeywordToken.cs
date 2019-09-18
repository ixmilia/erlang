namespace IxMilia.Erlang.Tokens
{
    public class ErlangKeywordToken : ErlangToken
    {
        public ErlangKeywordToken(string text)
        {
            Text = text;
            Kind = ErlangTokenKind.Keyword;
        }

        internal static bool IsKeyword(string text)
        {
            for (int i = 0; i < Keywords.Length; i++)
            {
                if (Keywords[i] == text)
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] Keywords = new string[]
        {
            "after",
            "begin",
            "case",
            "cond",
            "end",
            "fun",
            "if",
            "let",
            "of",
            "query",
            "receive",
            "try",
            "when"
        };
    }
}
