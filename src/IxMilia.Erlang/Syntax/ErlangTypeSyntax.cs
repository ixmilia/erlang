using System.Collections.Generic;
using IxMilia.Erlang.Tokens;

namespace IxMilia.Erlang.Syntax
{
    public abstract class ErlangTypeSyntax : ErlangSyntaxNode
    {
    }

    public class ErlangTypeWithGuardSyntax : ErlangTypeSyntax
    {
        public ErlangTypeSyntax Type { get; }
        public ErlangTypeGuardSyntax TypeGuard { get; }

        public ErlangTypeWithGuardSyntax(ErlangTypeSyntax type, ErlangTypeGuardSyntax typeGuard)
        {
            Type = type;
            TypeGuard = typeGuard;
        }

        public override string ToString()
        {
            return string.Concat(Type, TypeGuard);
        }
    }

    public class ErlangParenthesizedTypeSyntax : ErlangTypeSyntax
    {
        public ErlangLeftParenToken LeftParen { get; }
        public ErlangTypeSyntax InnerType { get; }
        public ErlangRightParenToken RightParen { get; }

        public ErlangParenthesizedTypeSyntax(ErlangLeftParenToken left, ErlangTypeSyntax innerType, ErlangRightParenToken right)
        {
            LeftParen = left;
            InnerType = innerType;
            RightParen = right;
        }

        public override string ToString()
        {
            return string.Concat(LeftParen, InnerType, RightParen);
        }
    }

    public class ErlangAtomTypeSyntax : ErlangTypeSyntax
    {
        public ErlangAtomToken Atom { get; }
        public ErlangLeftParenToken LeftParen { get; }
        public IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> TypeArguments { get; }
        public ErlangRightParenToken RightParen { get; }

        public ErlangAtomTypeSyntax(ErlangAtomToken atom, ErlangLeftParenToken left, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> typeArguments, ErlangRightParenToken right)
        {
            Atom = atom;
            LeftParen = left;
            TypeArguments = typeArguments ?? new ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>[0];
            RightParen = right;
        }

        public override string ToString()
        {
            return string.Concat(Atom, LeftParen, string.Join(string.Empty, TypeArguments), RightParen);
        }
    }

    public class ErlangQualifiedAtomTypeSyntax : ErlangAtomTypeSyntax
    {
        public ErlangAtomToken Module { get; }
        public ErlangColonToken Colon { get; }

        public ErlangQualifiedAtomTypeSyntax(ErlangAtomToken module, ErlangColonToken colon, ErlangAtomToken atom, ErlangLeftParenToken left, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> typeArguments, ErlangRightParenToken right)
            : base(atom, left, typeArguments, right)
        {
            Module = module;
            Colon = colon;
        }

        public override string ToString()
        {
            return string.Concat(Module, Colon, base.ToString());
        }
    }

    public class ErlangVariableTypeSyntax : ErlangTypeSyntax
    {
        public ErlangVariableToken Variable { get; }

        public ErlangVariableTypeSyntax(ErlangVariableToken variable)
        {
            Variable = variable;
        }

        public override string ToString()
        {
            return Variable.ToString();
        }
    }

    public class ErlangIntegerTypeSyntax : ErlangTypeSyntax
    {
        public ErlangNumberToken Integer { get; }

        public ErlangIntegerTypeSyntax(ErlangNumberToken integer)
        {
            Integer = integer;
        }

        public override string ToString()
        {
            return Integer.ToString();
        }
    }

    public class ErlangIntegerRangeTypeSyntax : ErlangTypeSyntax
    {
        public ErlangNumberToken LowerBound { get; }
        public ErlangDotDotToken DotDot { get; }
        public ErlangNumberToken UpperBound { get; }

        public ErlangIntegerRangeTypeSyntax(ErlangNumberToken lower, ErlangDotDotToken dotDot, ErlangNumberToken upper)
        {
            LowerBound = lower;
            DotDot = dotDot;
            UpperBound = upper;
        }

        public override string ToString()
        {
            return string.Concat(LowerBound, DotDot, UpperBound);
        }
    }

    public class ErlangTupleTypeSyntax : ErlangTypeSyntax
    {
        public ErlangLeftBraceToken LeftBrace { get; }
        public IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> TypeArguments { get; }
        public ErlangRightBraceToken RightBrace { get; }

        public ErlangTupleTypeSyntax(ErlangLeftBraceToken left, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> typeArguments, ErlangRightBraceToken right)
        {
            LeftBrace = left;
            TypeArguments = typeArguments;
            RightBrace = right;
        }

        public override string ToString()
        {
            return string.Concat(LeftBrace, string.Join(string.Empty, TypeArguments), RightBrace);
        }
    }

    public abstract class ErlangListTypeSyntax : ErlangTypeSyntax
    {
        public ErlangLeftBracketToken LeftBracket { get; }
        public ErlangRightBracketToken RightBracket { get; }

        public ErlangListTypeSyntax(ErlangLeftBracketToken left, ErlangRightBracketToken right)
        {
            LeftBracket = left;
            RightBracket = right;
        }
    }

    public class ErlangEmptyListTypeSyntax : ErlangListTypeSyntax
    {
        public ErlangEmptyListTypeSyntax(ErlangLeftBracketToken left, ErlangRightBracketToken right)
            : base(left, right)
        {
        }

        public override string ToString()
        {
            return string.Concat(LeftBracket, RightBracket);
        }
    }

    public class ErlangListOfTypeSyntax : ErlangListTypeSyntax
    {
        public ErlangTypeSyntax Type { get; }

        public ErlangListOfTypeSyntax(ErlangLeftBracketToken left, ErlangTypeSyntax type, ErlangRightBracketToken right)
            : base(left, right)
        {
            Type = type;
        }

        public override string ToString()
        {
            return string.Concat(LeftBracket, Type, RightBracket);
        }
    }

    public class ErlangNonEmptyListOfTypeSyntax : ErlangListOfTypeSyntax
    {
        public ErlangCommaToken Comma { get; }
        public ErlangDotDotDotToken DotDotDot { get; }

        public ErlangNonEmptyListOfTypeSyntax(ErlangLeftBracketToken left, ErlangTypeSyntax type, ErlangCommaToken comma, ErlangDotDotDotToken dotDotDot, ErlangRightBracketToken right)
            : base(left, type, right)
        {
            Comma = comma;
            DotDotDot = dotDotDot;
        }

        public override string ToString()
        {
            return string.Concat(LeftBracket, Type, Comma, DotDotDot, RightBracket);
        }
    }
}
