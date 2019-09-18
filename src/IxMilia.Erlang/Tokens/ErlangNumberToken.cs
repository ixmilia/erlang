using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace IxMilia.Erlang.Tokens
{
    public class ErlangNumberToken : ErlangToken
    {
        private double doubleValue;
        private BigInteger bigValue;

        public bool IsIntegral { get; private set; }

        public double DoubleValue
        {
            get
            {
                if (IsIntegral)
                    throw new InvalidOperationException();
                return doubleValue;
            }
        }

        public BigInteger IntegerValue
        {
            get
            {
                if (!IsIntegral)
                    throw new InvalidOperationException();
                return bigValue;
            }
        }

        private ErlangNumberToken(string text)
        {
            Text = text;
            Kind = ErlangTokenKind.Number;
        }

        public ErlangNumberToken(string text, double value)
            : this(text)
        {
            doubleValue = value;
            IsIntegral = false;
        }

        public ErlangNumberToken(string text, int value)
            : this(text, (BigInteger)value)
        {
        }

        public ErlangNumberToken(string text, BigInteger value)
            : this(text)
        {
            bigValue = value;
            IsIntegral = true;
        }

        public static bool IsNumberStart(char c)
        {
            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsNumberContinue(char c)
        {
            switch (char.ToUpperInvariant(c))
            {
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                    return true;
                default:
                    return IsNumberStart(c);
            }
        }

        public static ErlangNumberToken Lex(TextBuffer buffer)
        {
            var sb = new StringBuilder();
            sb.Append(buffer.Peek());
            buffer.Advance();
            bool seenHash = false;
            bool seenDecimal = false;
            var last = default(char);
            while (buffer.TextRemains())
            {
                var c = buffer.Peek();
                if (c == '#')
                {
                    if (!seenHash && !seenDecimal)
                    {
                        seenHash = true;
                        buffer.Advance();
                        sb.Append(c);
                    }
                    else
                    {
                        // premature end of number
                        break;
                    }
                }
                else if (c == '.')
                {
                    if (!seenDecimal && !seenHash)
                    {
                        seenDecimal = true;
                        buffer.Advance();
                        sb.Append(c);
                    }
                    else
                    {
                        // premature end of number
                        break;
                    }
                }
                else if (IsNumberContinue(c))
                {
                    buffer.Advance();
                    sb.Append(c);
                }
                else
                {
                    break;
                }

                last = c;
            }

            Debug.Assert(!(seenDecimal && seenHash)); // should not have seen both
            if (last == '.')
            {
                // numbers can't end in a decimal point
                buffer.Retreat();
                sb.Remove(sb.Length - 1, 1);
                seenDecimal = false;
            }

            var text = sb.ToString();
            double doubleValue = default(double);
            BigInteger bigValue = default(BigInteger);
            string error = null;
            if (!seenHash)
            {
                // simple parsing
                if (seenDecimal)
                    doubleValue = Convert.ToDouble(text);
                else
                    bigValue = BigInteger.Parse(text);
            }
            else
            {
                // complex hash parsing
                var parts = text.Split("#".ToCharArray(), 2);
                int @base = Convert.ToInt32(parts[0]);
                if (@base >= 2 && @base <= 36)
                {
                    int val = 0;
                    for (int i = 0; i < parts[1].Length; i++)
                    {
                        int digitValue = 0;
                        var l = parts[1][i];
                        if (l >= '0' && l <= '9')
                            digitValue = l - '0';
                        else
                            digitValue = char.ToUpperInvariant(l) - 'A' + 10;

                        if (digitValue < 0 || digitValue >= @base)
                        {
                            error = string.Format("Digit '{0}' not valid for base '{1}'", l, @base);
                            return new ErlangNumberToken(text) { Error = error };
                        }

                        val = (val * @base) + digitValue;
                    }

                    bigValue = val;
                }
                else
                {
                    error = "Base must be between 2 and 36 inclusive.";
                }
            }

            if (seenDecimal)
                return new ErlangNumberToken(text, doubleValue) { Error = error };
            else
                return new ErlangNumberToken(text, bigValue) { Error = error };
        }
    }
}
