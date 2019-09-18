using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IxMilia.Erlang.Tokens
{
    public enum ErlangOperatorKind
    {
        Asterisk,
        Bang,
        Hash,
        Colon,
        Plus,
        PlusPlus,
        Minus,
        MinusMinus,
        Slash,
        SlashEquals,
        Greater,
        GreaterEquals,
        Less,
        Equals,
        EqualsEquals,
        EqualsLess,
        EqualsColonEquals,
        EqualsSlashEquals,
        BNot,
        Not,
        Div,
        Rem,
        BAnd,
        And,
        BOr,
        Bxor,
        Bsl,
        Bsr,
        Or,
        Xor,
        AndAlso,
        OrElse,
        Catch
    }

    public abstract class ErlangOperatorToken : ErlangToken
    {
        public ErlangOperatorKind OperatorKind { get; protected set; }

        public int Precedence { get; protected set; }

        public bool IsLeftAssociative { get; protected set; }

        public ErlangOperatorToken(string text, ErlangOperatorKind opKind, int precedence, bool isLeftAssoc = true)
        {
            Text = text;
            Kind = ErlangTokenKind.Operator;
            OperatorKind = opKind;
            Precedence = precedence;
            IsLeftAssociative = isLeftAssoc;
        }

        internal static ErlangOperatorToken GetKeywordOperator(string text)
        {
            switch (text)
            {
                case "bnot":
                    return new ErlangBNotToken();
                case "not":
                    return new ErlangNotToken();
                case "div":
                    return new ErlangDivToken();
                case "rem":
                    return new ErlangRemToken();
                case "band":
                    return new ErlangBAndToken();
                case "and":
                    return new ErlangAndToken();
                case "bor":
                    return new ErlangBOrToken();
                case "bxor":
                    return new ErlangBXorToken();
                case "bsl":
                    return new ErlangBslToken();
                case "bsr":
                    return new ErlangBsrToken();
                case "or":
                    return new ErlangOrToken();
                case "xor":
                    return new ErlangXorToken();
                case "andalso":
                    return new ErlangAndAlsoToken();
                case "orelse":
                    return new ErlangOrElseToken();
                case "catch":
                    return new ErlangCatchToken();
                default:
                    return null;
            }
        }

        internal static bool IsKeywordOperator(string text)
        {
            for (int i = 0; i < KeywordOperators.Length; i++)
            {
                if (KeywordOperators[i] == text)
                {
                    return true;
                }
            }

            return false;
        }

        private static readonly string[] KeywordOperators = new string[]
        {
            "bnot",
            "not",
            "div",
            "rem",
            "band",
            "and",
            "bor",
            "bxor",
            "bsl",
            "bsr",
            "or",
            "xor",
            "andalso",
            "orelse",
            "catch"
        };
    }

    public class ErlangAsteriskToken : ErlangOperatorToken
    {
        public ErlangAsteriskToken()
            : base("*", ErlangOperatorKind.Asterisk, 8)
        {
        }
    }

    public class ErlangBangToken : ErlangOperatorToken
    {
        public ErlangBangToken()
            : base("!", ErlangOperatorKind.Bang, 2, false)
        {
        }
    }

    public class ErlangHashToken : ErlangOperatorToken
    {
        public ErlangHashToken()
            : base("#", ErlangOperatorKind.Hash, 10)
        {
        }
    }

    public class ErlangColonToken : ErlangOperatorToken
    {
        public ErlangColonToken()
            : base(":", ErlangOperatorKind.Colon, 11)
        {
        }
    }

    public class ErlangPlusToken : ErlangOperatorToken
    {
        public ErlangPlusToken()
            : base("+", ErlangOperatorKind.Plus, 7)
        {
        }
    }

    public class ErlangPlusPlusToken : ErlangOperatorToken
    {
        public ErlangPlusPlusToken()
            : base("++", ErlangOperatorKind.PlusPlus, 6, false)
        {
        }
    }

    public class ErlangMinusToken : ErlangOperatorToken
    {
        public ErlangMinusToken()
            : base("-", ErlangOperatorKind.Minus, 7)
        {
        }
    }

    public class ErlangMinusMinusToken : ErlangOperatorToken
    {
        public ErlangMinusMinusToken()
            : base("--", ErlangOperatorKind.MinusMinus, 6, false)
        {
        }
    }

    public class ErlangSlashToken : ErlangOperatorToken
    {
        public ErlangSlashToken()
            : base("/", ErlangOperatorKind.Slash, 8)
        {
        }
    }

    public class ErlangSlashEqualsToken : ErlangOperatorToken
    {
        public ErlangSlashEqualsToken()
            : base("/=", ErlangOperatorKind.SlashEquals, 5)
        {
        }
    }

    public class ErlangGreaterToken : ErlangOperatorToken
    {
        public ErlangGreaterToken()
            : base(">", ErlangOperatorKind.Greater, 5)
        {
        }
    }

    public class ErlangGreaterEqualsToken : ErlangOperatorToken
    {
        public ErlangGreaterEqualsToken()
            : base(">=", ErlangOperatorKind.GreaterEquals, 5)
        {
        }
    }

    public class ErlangLessToken : ErlangOperatorToken
    {
        public ErlangLessToken()
            : base("<", ErlangOperatorKind.Less, 5)
        {
        }
    }

    public class ErlangEqualsToken : ErlangOperatorToken
    {
        public ErlangEqualsToken()
            : base("=", ErlangOperatorKind.Equals, 2, false)
        {
        }
    }

    public class ErlangEqualsEqualsToken : ErlangOperatorToken
    {
        public ErlangEqualsEqualsToken()
            : base("==", ErlangOperatorKind.EqualsEquals, 5)
        {
        }
    }

    public class ErlangEqualsLessToken : ErlangOperatorToken
    {
        public ErlangEqualsLessToken()
            : base("=<", ErlangOperatorKind.EqualsLess, 5)
        {
        }
    }

    public class ErlangEqualsColonEqualsToken : ErlangOperatorToken
    {
        public ErlangEqualsColonEqualsToken()
            : base("=:=", ErlangOperatorKind.EqualsColonEquals, 5)
        {
        }
    }

    public class ErlangEqualsSlashEqualsToken : ErlangOperatorToken
    {
        public ErlangEqualsSlashEqualsToken()
            : base("=/=", ErlangOperatorKind.EqualsSlashEquals, 5)
        {
        }
    }

    public class ErlangBNotToken : ErlangOperatorToken
    {
        public ErlangBNotToken()
            : base("bnot", ErlangOperatorKind.BNot, 9)
        {
        }
    }

    public class ErlangNotToken : ErlangOperatorToken
    {
        public ErlangNotToken()
            : base("not", ErlangOperatorKind.Not, 9)
        {
        }
    }

    public class ErlangDivToken : ErlangOperatorToken
    {
        public ErlangDivToken()
            : base("div", ErlangOperatorKind.Div, 8)
        {
        }
    }

    public class ErlangRemToken : ErlangOperatorToken
    {
        public ErlangRemToken()
            : base("rem", ErlangOperatorKind.Rem, 8)
        {
        }
    }

    public class ErlangBAndToken : ErlangOperatorToken
    {
        public ErlangBAndToken()
            : base("band", ErlangOperatorKind.BAnd, 8)
        {
        }
    }

    public class ErlangAndToken : ErlangOperatorToken
    {
        public ErlangAndToken()
            : base("and", ErlangOperatorKind.And, 8)
        {
        }
    }

    public class ErlangBOrToken : ErlangOperatorToken
    {
        public ErlangBOrToken()
            : base("bor", ErlangOperatorKind.BOr, 7)
        {
        }
    }

    public class ErlangBXorToken : ErlangOperatorToken
    {
        public ErlangBXorToken()
            : base("bxor", ErlangOperatorKind.Bxor, 7)
        {
        }
    }

    public class ErlangBslToken : ErlangOperatorToken
    {
        public ErlangBslToken()
            : base("bsl", ErlangOperatorKind.Bsl, 7)
        {
        }
    }

    public class ErlangBsrToken : ErlangOperatorToken
    {
        public ErlangBsrToken()
            : base("bsr", ErlangOperatorKind.Bsr, 7)
        {
        }
    }

    public class ErlangOrToken : ErlangOperatorToken
    {
        public ErlangOrToken()
            : base("or", ErlangOperatorKind.Or, 7)
        {
        }
    }

    public class ErlangXorToken : ErlangOperatorToken
    {
        public ErlangXorToken()
            : base("xor", ErlangOperatorKind.Xor, 7)
        {
        }
    }

    public class ErlangAndAlsoToken : ErlangOperatorToken
    {
        public ErlangAndAlsoToken()
            : base("andalso", ErlangOperatorKind.AndAlso, 4)
        {
        }
    }

    public class ErlangOrElseToken : ErlangOperatorToken
    {
        public ErlangOrElseToken()
            : base("orelse", ErlangOperatorKind.OrElse, 3)
        {
        }
    }

    public class ErlangCatchToken : ErlangOperatorToken
    {
        public ErlangCatchToken()
            : base("catch", ErlangOperatorKind.Catch, 1)
        {
        }
    }
}
