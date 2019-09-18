namespace IxMilia.Erlang.Tokens
{
    public enum ErlangPunctuationKind
    {
        LeftParen,
        RightParen,
        LeftBracket,
        RightBracket,
        LeftBrace,
        RightBrace,
        MinusGreater,
        GreaterGreater,
        LessLess,
        LessMinus,
        Comma,
        Dot,
        DotDot,
        DotDotDot,
        Semicolon,
        ColonColon,
        Pipe,
        PipePipe
    }

    public abstract class ErlangPunctuationToken : ErlangToken
    {
        public ErlangPunctuationKind PunctuationKind { get; protected set; }

        public ErlangPunctuationToken(string text, ErlangPunctuationKind kind)
        {
            Text = text;
            Kind = ErlangTokenKind.Punctuation;
            PunctuationKind = kind;
        }
    }

    public class ErlangLeftParenToken : ErlangPunctuationToken
    {
        public ErlangLeftParenToken()
            : base("(", ErlangPunctuationKind.LeftParen)
        {
        }
    }

    public class ErlangRightParenToken : ErlangPunctuationToken
    {
        public ErlangRightParenToken()
            : base(")", ErlangPunctuationKind.RightParen)
        {
        }
    }

    public class ErlangLeftBracketToken : ErlangPunctuationToken
    {
        public ErlangLeftBracketToken()
            : base("[", ErlangPunctuationKind.LeftBracket)
        {
        }
    }

    public class ErlangRightBracketToken : ErlangPunctuationToken
    {
        public ErlangRightBracketToken()
            : base("]", ErlangPunctuationKind.RightBracket)
        {
        }
    }

    public class ErlangLeftBraceToken : ErlangPunctuationToken
    {
        public ErlangLeftBraceToken()
            : base("{", ErlangPunctuationKind.LeftBrace)
        {
        }
    }

    public class ErlangRightBraceToken : ErlangPunctuationToken
    {
        public ErlangRightBraceToken()
            : base("}", ErlangPunctuationKind.RightBrace)
        {
        }
    }

    public class ErlangMinusGreaterToken : ErlangPunctuationToken
    {
        public ErlangMinusGreaterToken()
            : base("->", ErlangPunctuationKind.MinusGreater)
        {
        }
    }

    public class ErlangGreaterGreaterToken : ErlangPunctuationToken
    {
        public ErlangGreaterGreaterToken()
            : base(">>", ErlangPunctuationKind.GreaterGreater)
        {
        }
    }

    public class ErlangLessLessToken : ErlangPunctuationToken
    {
        public ErlangLessLessToken()
            : base("<<", ErlangPunctuationKind.LessLess)
        {
        }
    }

    public class ErlangLessMinusToken : ErlangPunctuationToken
    {
        public ErlangLessMinusToken()
            : base("<-", ErlangPunctuationKind.LessMinus)
        {
        }
    }

    public class ErlangPipeToken : ErlangPunctuationToken
    {
        public ErlangPipeToken()
            : base("|", ErlangPunctuationKind.Pipe)
        {
        }
    }

    public class ErlangPipePipeToken : ErlangPunctuationToken
    {
        public ErlangPipePipeToken()
            : base("||", ErlangPunctuationKind.PipePipe)
        {
        }
    }

    public class ErlangCommaToken : ErlangPunctuationToken
    {
        public ErlangCommaToken()
            : base(",", ErlangPunctuationKind.Comma)
        {
        }
    }

    public class ErlangDotToken : ErlangPunctuationToken
    {
        public ErlangDotToken()
            : base(".", ErlangPunctuationKind.Dot)
        {
        }
    }

    public class ErlangDotDotToken : ErlangPunctuationToken
    {
        public ErlangDotDotToken()
            : base("..", ErlangPunctuationKind.DotDot)
        {
        }
    }

    public class ErlangDotDotDotToken : ErlangPunctuationToken
    {
        public ErlangDotDotDotToken()
            : base("...", ErlangPunctuationKind.DotDotDot)
        {
        }
    }

    public class ErlangSemicolonToken : ErlangPunctuationToken
    {
        public ErlangSemicolonToken()
            : base(";", ErlangPunctuationKind.Semicolon)
        {
        }
    }

    public class ErlangColonColonToken : ErlangPunctuationToken
    {
        public ErlangColonColonToken()
            : base("::", ErlangPunctuationKind.ColonColon)
        {
        }
    }
}
