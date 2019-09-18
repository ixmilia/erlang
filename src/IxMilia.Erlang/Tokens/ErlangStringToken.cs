using System.Diagnostics;
using System.Text;

namespace IxMilia.Erlang.Tokens
{
    public class ErlangStringToken : ErlangToken
    {
        public string Value { get; private set; }

        public ErlangStringToken(string value)
        {
            Value = value;
            Text = value;
            Kind = ErlangTokenKind.String;
        }

        public static bool IsStringStart(char c)
        {
            return c == '"';
        }

        public static ErlangStringToken Lex(TextBuffer buffer)
        {
            var sb = new StringBuilder();
            var c = buffer.Peek();
            Debug.Assert(IsStringStart(c));
            buffer.Advance();
            bool isEscape = false;
            string error = null;
            while (buffer.TextRemains())
            {
                c = buffer.Peek();
                if (isEscape)
                {
                    buffer.Advance();
                    isEscape = false;
                    sb.Append(c);
                }
                else if (IsStringStart(c)) // string start and end are the same thing
                {
                    buffer.Advance(); // swallow and move on
                    break;
                }
                else if (c == '\n' || c == '\r')
                {
                    error = "Expected string terminator";
                    break;
                }
                else
                {
                    buffer.Advance();
                    if (c == '\\')
                    {
                        isEscape = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            return new ErlangStringToken(sb.ToString()) { Error = error };
        }
    }
}
