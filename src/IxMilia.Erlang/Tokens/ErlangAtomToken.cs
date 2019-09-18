using System.Text;

namespace IxMilia.Erlang.Tokens
{
    public class ErlangAtomToken : ErlangToken
    {
        public ErlangAtomToken(string text)
        {
            Text = text;
            Kind = ErlangTokenKind.Atom;
        }

        internal static bool IsAtomStart(char c)
        {
            return IsLower(c) || IsSingleQuote(c);
        }

        private static bool IsAtomContinue(char c, bool complex)
        {
            if (complex)
            {
                return !IsSingleQuote(c) && !IsNewline(c);
            }
            else
            {
                return IsLower(c) || IsUpper(c) || IsDigit(c) || IsAtSign(c) || IsUnderscore(c);
            }
        }

        internal static ErlangToken Lex(TextBuffer buffer)
        {
            var sb = new StringBuilder();
            var first = buffer.Peek();
            var isComplex = IsSingleQuote(first);
            sb.Append(first);
            buffer.Advance();
            while (buffer.TextRemains())
            {
                var c = buffer.Peek();
                if (IsAtomContinue(c, isComplex))
                {
                    sb.Append(c);
                    buffer.Advance();
                }
                else if (isComplex && IsSingleQuote(c))
                {
                    // end of token
                    sb.Append(c);
                    buffer.Advance();
                    break;
                }
                else
                {
                    break;
                }
            }

            var text = sb.ToString();
            if (ErlangKeywordToken.IsKeyword(text))
            {
                return new ErlangKeywordToken(text);
            }
            else
            {
                return (ErlangOperatorToken.GetKeywordOperator(text) as ErlangToken) ?? new ErlangAtomToken(text);
            }
        }
    }
}
