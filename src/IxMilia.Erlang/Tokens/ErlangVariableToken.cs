using System.Text;

namespace IxMilia.Erlang.Tokens
{
    public class ErlangVariableToken : ErlangToken
    {
        public ErlangVariableToken(string text)
        {
            Text = text;
            Kind = ErlangTokenKind.Variable;
        }

        internal static bool IsVariableStart(char c)
        {
            return IsUpper(c) || c == '_';
        }

        private static bool IsVariableContinue(char c)
        {
            return IsUpper(c) || IsLower(c) || IsDigit(c) || IsUnderscore(c) || IsAtSign(c);
        }

        internal static ErlangVariableToken Lex(TextBuffer buffer)
        {
            var sb = new StringBuilder();
            sb.Append(buffer.Peek());
            buffer.Advance();
            while (buffer.TextRemains())
            {
                var c = buffer.Peek();
                if (IsVariableContinue(c))
                {
                    sb.Append(c);
                    buffer.Advance();
                }
                else
                {
                    break;
                }
            }

            return new ErlangVariableToken(sb.ToString());
        }
    }
}
