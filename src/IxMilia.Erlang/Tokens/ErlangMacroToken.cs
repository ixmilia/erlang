using System.Diagnostics;
using System.Text;

namespace IxMilia.Erlang.Tokens
{
    public class ErlangMacroToken : ErlangToken
    {
        public ErlangMacroToken(string text)
        {
            Text = text;
            Kind = ErlangTokenKind.Macro;
        }

        public static bool IsMacroStart(char c)
        {
            return c == '?';
        }

        private static bool IsMacroContinue(char c)
        {
            return IsUpper(c) || IsLower(c) || IsUnderscore(c) || IsDigit(c);
        }

        internal static ErlangMacroToken Lex(TextBuffer buffer)
        {
            var sb = new StringBuilder();
            var first = buffer.Peek();
            Debug.Assert(IsMacroStart(first));
            buffer.Advance();
            sb.Append(first);
            while (buffer.TextRemains())
            {
                var c = buffer.Peek();
                if (IsMacroContinue(c))
                {
                    buffer.Advance();
                    sb.Append(c);
                }
                else
                {
                    break;
                }
            }

            return new ErlangMacroToken(sb.ToString());
        }
    }
}
