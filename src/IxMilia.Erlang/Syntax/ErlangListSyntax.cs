using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Erlang.Tokens;

namespace IxMilia.Erlang.Syntax
{
    public abstract class ErlangListSyntax : ErlangExpressionSyntax
    {
        public ErlangLeftBracketToken LeftBracket { get; private set; }
        public ErlangRightBracketToken RightBracket { get; private set; }

        public ErlangListSyntax(ErlangLeftBracketToken leftBracket, ErlangRightBracketToken rightBracket)
        {
            LeftBracket = leftBracket;
            RightBracket = rightBracket;
        }

        internal static ErlangListSyntax ParseList(TokenBuffer buffer, ParseStyle style)
        {
            var left = buffer.Peek();
            if (ErlangToken.IsString(left))
            {
                buffer.Advance();
                return new ErlangListStringSyntax((ErlangStringToken)left);
            }
            else if (ErlangToken.IsLeftBracket(left))
            {
                buffer.Advance();
                var items = new List<ErlangListItemSyntax>();
                ErlangExpressionSyntax tail = null;
                while (buffer.TokensRemain())
                {
                    var item = ParseListItem(buffer, style);
                    if (item == null)
                        break;
                    items.Add(item);
                    if (!ErlangToken.IsComma(item.Separator))
                        break;
                }

                if (items.Count == 1 && ErlangToken.IsDoublePipe(buffer.Peek()))
                {
                    // list comprehension, parse generator expression and filters
                    // result of generator expressions get filtered, e.g.
                    //   [X || {X, some_atom} <- some_list()].
                    var listItem = items.Single();
                    var doublePipe = (ErlangPipePipeToken)buffer.Peek();
                    buffer.Advance();
                    return ParseListComprehension((ErlangLeftBracketToken)left, listItem.Item, doublePipe, buffer, style);
                }
                else if (items.Count > 0)
                {
                    var lastSep = items.Last().Separator;
                    if (ErlangToken.IsPipe(lastSep))
                    {
                        // list tail, only parse one more expression
                        tail = ParseExpression(buffer, style);
                    }
                    else if (lastSep != null)
                    {
                        Debug.Assert(false, "unexpected list separator");
                    }
                }

                ErlangRightBracketToken rightBracket = null;
                var next = buffer.Peek();
                if (ErlangToken.IsRightBracket(next))
                {
                    buffer.Advance();
                    rightBracket = (ErlangRightBracketToken)next;
                }
                else
                {
                    Debug.Assert(false, "Missing closing bracket");
                }

                return new ErlangListRegularSyntax((ErlangLeftBracketToken)left, items, tail, rightBracket);
            }

            return null;
        }

        private static ErlangListComprehensionSyntax ParseListComprehension(ErlangLeftBracketToken leftBracket, ErlangExpressionSyntax expression, ErlangPipePipeToken doublePipe, TokenBuffer buffer, ParseStyle style)
        {
            var generators = ParseSyntaxListWithComma(buffer, style, ErlangListComprehensionGeneratorSyntax.Parse);
            var filters = ParseSyntaxListWithComma(buffer, style, ErlangListComprehensionFilterSyntax.Parse);

            ErlangRightBracketToken rightBracket = null;
            var right = buffer.Peek();
            if (ErlangToken.IsRightBracket(right))
            {
                buffer.Advance();
                rightBracket = (ErlangRightBracketToken)right;
            }

            return new ErlangListComprehensionSyntax(leftBracket, expression, doublePipe, generators, filters, rightBracket);
        }

        private static ErlangListItemSyntax ParseListItem(TokenBuffer buffer, ParseStyle style)
        {
            var expression = ParseExpression(buffer, style);
            if (expression != null)
            {
                ErlangPunctuationToken separator = null;
                var sep = buffer.Peek();
                if (ErlangToken.IsComma(sep) || ErlangToken.IsPipe(sep))
                {
                    buffer.Advance();
                    separator = (ErlangPunctuationToken)sep;
                }

                return new ErlangListItemSyntax(expression, separator);
            }

            return null;
        }
    }

    public class ErlangListRegularSyntax : ErlangListSyntax
    {
        public SyntaxList<ErlangListItemSyntax> Items { get; private set; }

        public ErlangExpressionSyntax Tail { get; private set; }

        public ErlangListRegularSyntax(ErlangLeftBracketToken leftBracket, IEnumerable<ErlangListItemSyntax> items, ErlangExpressionSyntax tail, ErlangRightBracketToken rightBracket)
            : base(leftBracket, rightBracket)
        {
            Items = new SyntaxList<ErlangListItemSyntax>(items);
            Tail = tail;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}{3}", LeftBracket, Items, Tail, RightBracket);
        }
    }

    public class ErlangListStringSyntax : ErlangListSyntax
    {
        public ErlangStringToken String { get; private set; }

        public ErlangListStringSyntax(ErlangStringToken str)
            : base(null, null)
        {
            String = str;
        }

        public override string ToString()
        {
            return string.Format("\"{0}\"", String);
        }
    }

    public class ErlangListComprehensionSyntax : ErlangListSyntax
    {
        public ErlangExpressionSyntax Expression { get; private set; }
        public ErlangPipePipeToken DoublePipe { get; private set; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangListComprehensionGeneratorSyntax>> Generators { get; private set; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangListComprehensionFilterSyntax>> Filters { get; private set; }

        public ErlangListComprehensionSyntax(
            ErlangLeftBracketToken leftBracket,
            ErlangExpressionSyntax expression,
            ErlangPipePipeToken doublePipe,
            IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangListComprehensionGeneratorSyntax>> generators,
            IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangListComprehensionFilterSyntax>> filters,
            ErlangRightBracketToken rightBracket)
            : base(leftBracket, rightBracket)
        {
            Expression = expression;
            DoublePipe = doublePipe;
            Generators = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangListComprehensionGeneratorSyntax>>(generators);
            Filters = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangListComprehensionFilterSyntax>>(filters);
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}{3}{4}{5}", LeftBracket, Expression, DoublePipe, Generators, Filters, RightBracket);
        }
    }

    public class ErlangListComprehensionGeneratorSyntax : ErlangSyntaxNode
    {
        public ErlangExpressionSyntax Expression { get; private set; }
        public ErlangLessMinusToken Arrow { get; private set; }
        public ErlangExpressionSyntax Function { get; private set; }
        public ErlangCommaToken Comma { get; private set; }

        public ErlangListComprehensionGeneratorSyntax(ErlangExpressionSyntax expression, ErlangLessMinusToken arrow, ErlangExpressionSyntax function, ErlangCommaToken comma)
        {
            Expression = expression;
            Arrow = arrow;
            Function = function;
            Comma = comma;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}{3}", Expression, Arrow, Function, Comma);
        }

        internal static ErlangListComprehensionGeneratorSyntax Parse(TokenBuffer buffer, ParseStyle style)
        {
            var start = buffer.Offset;
            var expression = ParseExpression(buffer, style);
            if (expression != null)
            {
                var next = buffer.Peek();
                if (ErlangToken.IsLeftArrow(next))
                {
                    buffer.Advance();
                    var arrow = (ErlangLessMinusToken)next;
                    var function = ParseExpression(buffer, style);
                    if (function != null)
                    {
                        ErlangCommaToken comma = null;
                        next = buffer.Peek();
                        if (ErlangToken.IsComma(next))
                        {
                            buffer.Advance();
                            comma = (ErlangCommaToken)next;
                        }

                        return new ErlangListComprehensionGeneratorSyntax(expression, arrow, function, comma);
                    }
                }
            }

            buffer.SetOffset(start);
            return null;
        }
    }

    public class ErlangListComprehensionFilterSyntax : ErlangSyntaxNode
    {
        public ErlangExpressionSyntax Expression { get; private set; }
        public ErlangCommaToken Comma { get; private set; }

        public ErlangListComprehensionFilterSyntax(ErlangExpressionSyntax expression, ErlangCommaToken comma)
        {
            Expression = expression;
            Comma = comma;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}", Expression, Comma);
        }

        internal static ErlangListComprehensionFilterSyntax Parse(TokenBuffer buffer, ParseStyle style)
        {
            var expression = ParseExpression(buffer, style);
            if (expression != null)
            {
                ErlangCommaToken comma = null;
                var next = buffer.Peek();
                if (ErlangToken.IsComma(next))
                {
                    buffer.Advance();
                    comma = (ErlangCommaToken)next;
                }

                return new ErlangListComprehensionFilterSyntax(expression, comma);
            }

            return null;
        }
    }

    public class ErlangListItemSyntax : ErlangSyntaxNode
    {
        public ErlangExpressionSyntax Item { get; private set; }
        public ErlangPunctuationToken Separator { get; private set; }

        public ErlangListItemSyntax(ErlangExpressionSyntax item, ErlangPunctuationToken separator)
        {
            Item = item;
            Separator = separator;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}", Item, Separator);
        }
    }
}
