using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Erlang.Tokens;

namespace IxMilia.Erlang.Syntax
{
    internal enum ParseStyle
    {
        Attributes,
        Body
    }

    public abstract class ErlangSyntaxNode
    {
        public static ErlangModuleSyntax Parse(string text)
        {
            var tokens = ErlangToken.Tokenize(new TextBuffer(text));
            return Parse(new TokenBuffer(tokens));
        }

        public static ErlangModuleSyntax Parse(TokenBuffer buffer)
        {
            var elements = new List<ErlangAttributeOrFunctionGroupSyntax>();
            while (buffer.TokensRemain())
            {
                if (ParseAttribute(buffer, ParseStyle.Attributes) is ErlangAttributeSyntax attribute)
                {
                    elements.Add(attribute);
                    continue;
                }
                else if (ParseFunctionGroup(buffer, ParseStyle.Body) is ErlangFunctionGroupSyntax function)
                {
                    elements.Add(function);
                    continue;
                }

                // don't loop forever
                break;
            }

            Debug.Assert(!buffer.TokensRemain());
            return new ErlangModuleSyntax(elements);
        }

        internal static ErlangAttributeSyntax ParseAttribute(TokenBuffer buffer, ParseStyle style)
        {
            if (buffer.Peek() is ErlangMinusToken minus)
            {
                buffer.Advance();
                if (buffer.Peek() is ErlangAtomToken atom)
                {
                    buffer.Advance();
                    ErlangLeftParenToken leftParen = null;
                    ErlangRightParenToken rightParen = null;
                    IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> parameters = new ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>[0];
                    ErlangDotToken dot = null;
                    leftParen = buffer.Peek() as ErlangLeftParenToken;
                    if (leftParen != null)
                    {
                        buffer.Advance();
                    }

                    parameters = ParseSyntaxListWithComma(buffer, style, ParseExpression);

                    if (leftParen != null)
                    {
                        rightParen = buffer.Peek() as ErlangRightParenToken;
                        if (rightParen != null)
                        {
                            buffer.Advance();
                        }
                    }

                    dot = buffer.Peek() as ErlangDotToken;
                    if (dot != null)
                    {
                        buffer.Advance();
                    }

                    return new ErlangAttributeSyntax(minus, atom, leftParen, parameters, rightParen, dot);
                }
            }

            return null;
        }

        private static ErlangTypeSyntax ParseType(TokenBuffer buffer, ParseStyle style)
        {
            // See http://learnyousomeerlang.com/dialyzer#typing-about-types-of-types
            //    Variable            % a-
            //    atom                % b-
            //    atom()              % c-
            //    atom(?)             % d-
            //    atom:atom()         % e-
            //    atom:atom(?)        % f-
            //    []                  % g-
            //    [?]                 % h-
            //    [?,...]             % i-
            //    {}                  % j-
            //    {?}                 % k-
            //    #atom{}             % l
            //    #atom{?}            % m
            //    binary_type         % n
            //    integer             % o-
            //    integer..integer    % p-
            //    fun()               % q
            //    fun(?)              % r
            //    fun(...) -> ?       % s
            var offset = buffer.Offset;
            var first = buffer.Peek();
            if (first is ErlangVariableToken variable)
            {
                buffer.Advance();
                // (a)
                return new ErlangVariableTypeSyntax(variable);
            }
            else if (first is ErlangAtomToken)
            {
                buffer.Advance();
                ErlangLeftParenToken leftParen = null;
                ErlangRightParenToken rightParen = null;
                IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> typeArguments = null;
                if (buffer.Peek() is ErlangLeftParenToken)
                {
                    leftParen = (ErlangLeftParenToken)buffer.Peek();
                    buffer.Advance();
                    typeArguments = ParseSyntaxListWithComma(buffer, style, ParseType);
                    if (buffer.Peek() is ErlangRightParenToken)
                    {
                        rightParen = (ErlangRightParenToken)buffer.Peek();
                        buffer.Advance();
                    }
                }
                else if (buffer.Peek() is ErlangColonToken colon)
                {
                    buffer.Advance();
                    var secondAtom = buffer.Peek() as ErlangAtomToken;
                    if (secondAtom != null)
                    {
                        buffer.Advance();
                        leftParen = buffer.Peek() as ErlangLeftParenToken;
                        if (leftParen != null)
                        {
                            buffer.Advance();
                            typeArguments = ParseSyntaxListWithComma(buffer, style, ParseType);
                            rightParen = buffer.Peek() as ErlangRightParenToken;
                            if (rightParen != null)
                            {
                                buffer.Advance();
                                // (e), (f)
                                return new ErlangQualifiedAtomTypeSyntax((ErlangAtomToken)first, colon, secondAtom, leftParen, typeArguments, rightParen);
                            }
                        }
                    }
                }

                // (b), (c), (d)
                return new ErlangAtomTypeSyntax((ErlangAtomToken)first, leftParen, typeArguments, rightParen);
            }
            else if (first is ErlangNumberToken)
            {
                buffer.Advance();
                if (buffer.Peek() is ErlangDotDotToken dotDot)
                {
                    buffer.Advance();
                    if (buffer.Peek() is ErlangNumberToken upper)
                    {
                        buffer.Advance();
                        // (p)
                        return new ErlangIntegerRangeTypeSyntax((ErlangNumberToken)first, dotDot, upper);
                    }
                }
                else
                {
                    // (o)
                    return new ErlangIntegerTypeSyntax((ErlangNumberToken)first);
                }
            }
            else if (first is ErlangLeftBracketToken)
            {
                ErlangCommaToken comma = null;
                ErlangDotDotDotToken dotDotDot = null;
                buffer.Advance();
                var innerType = ParseType(buffer, style);
                if (innerType != null)
                {
                    comma = buffer.Peek() as ErlangCommaToken;
                    if (comma != null)
                    {
                        buffer.Advance();
                        dotDotDot = buffer.Peek() as ErlangDotDotDotToken;
                        if (dotDotDot != null)
                        {
                            buffer.Advance();
                        }
                    }
                }

                var right = buffer.Peek() as ErlangRightBracketToken;
                if (right != null)
                {
                    buffer.Advance();
                }

                if (innerType == null)
                {
                    // (g)
                    return new ErlangEmptyListTypeSyntax((ErlangLeftBracketToken)first, right);
                }
                else if (comma == null || dotDotDot == null)
                {
                    // (h)
                    return new ErlangListOfTypeSyntax((ErlangLeftBracketToken)first, innerType, right);
                }
                else
                {
                    // (i)
                    return new ErlangNonEmptyListOfTypeSyntax((ErlangLeftBracketToken)first, innerType, comma, dotDotDot, right);
                }
            }
            else if (first is ErlangLeftBraceToken)
            {
                buffer.Advance();
                var typeArguments = ParseSyntaxListWithComma(buffer, style, ParseTypeWithPossibleGuard);
                if (buffer.Peek() is ErlangRightBraceToken right)
                {
                    buffer.Advance();
                    // (j), (k)
                    return new ErlangTupleTypeSyntax((ErlangLeftBraceToken)first, typeArguments, right);
                }
            }
            else if (first is ErlangLeftParenToken)
            {
                buffer.Advance();
                var innerType = ParseTypeWithPossibleGuard(buffer, style);
                if (innerType != null)
                {
                    if (buffer.Peek() is ErlangRightParenToken right)
                    {
                        buffer.Advance();
                        return new ErlangParenthesizedTypeSyntax((ErlangLeftParenToken)first, innerType, right);
                    }
                }
            }

            buffer.SetOffset(offset);
            return null;
        }

        private static ErlangTypeSyntax ParseTypeWithPossibleGuard(TokenBuffer buffer, ParseStyle style)
        {
            // type() :: type() [ | type() | ...].
            var offset = buffer.Offset;
            var type = ParseType(buffer, style);
            if (type != null)
            {
                var typeGuard = ParseTypeGuard(buffer, style);
                if (typeGuard != null)
                {
                    return new ErlangTypeWithGuardSyntax(type, typeGuard);
                }
                else
                {
                    return type;
                }
            }

            buffer.SetOffset(offset);
            return null;
        }

        private static ErlangTypeGuardSyntax ParseTypeGuard(TokenBuffer buffer, ParseStyle style)
        {
            var offset = buffer.Offset;
            if (buffer.Peek() is ErlangColonColonToken doubleColon)
            {
                buffer.Advance();
                var restrictions = ParseSyntaxListWithPipe(buffer, style, ParseType);
                return new ErlangTypeGuardSyntax(doubleColon, restrictions);
            }

            return null;
        }

        internal static List<ErlangSeparatedSyntaxNodeSyntax<T>> ParseSyntaxList<T, TSeparator>(TokenBuffer buffer, ParseStyle style, Func<TokenBuffer, ParseStyle, T> parser)
            where T : ErlangSyntaxNode
            where TSeparator : ErlangPunctuationToken
        {
            var items = new List<ErlangSeparatedSyntaxNodeSyntax<T>>();
            while (buffer.TokensRemain())
            {
                var item = parser(buffer, style);
                if (item == null)
                    break;
                var separator = buffer.Peek() as TSeparator;
                if (separator != null)
                {
                    buffer.Advance();
                }

                items.Add(new ErlangSeparatedSyntaxNodeSyntax<T>(item, separator));
            }

            return items;
        }

        internal static List<ErlangSeparatedSyntaxNodeSyntax<TSyntaxNode>> ParseSyntaxListWithComma<TSyntaxNode>(TokenBuffer buffer, ParseStyle style, Func<TokenBuffer, ParseStyle, TSyntaxNode> parser)
            where TSyntaxNode : ErlangSyntaxNode
        {
            return ParseSyntaxList<TSyntaxNode, ErlangCommaToken>(buffer, style, parser);
        }

        internal static List<ErlangSeparatedSyntaxNodeSyntax<TSyntaxNode>> ParseSyntaxListWithPipe<TSyntaxNode>(TokenBuffer buffer, ParseStyle style, Func<TokenBuffer, ParseStyle, TSyntaxNode> parser)
            where TSyntaxNode : ErlangSyntaxNode
        {
            return ParseSyntaxList<TSyntaxNode, ErlangPipeToken>(buffer, style, parser);
        }

        private static bool ParseParameters(TokenBuffer buffer, ParseStyle style, out List<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> parameters, out ErlangLeftParenToken leftParen, out ErlangRightParenToken rightParen)
        {
            parameters = null;
            leftParen = null;
            rightParen = null;
            var left = buffer.Peek();
            if (ErlangToken.IsLeftParen(left))
            {
                buffer.Advance();
                leftParen = (ErlangLeftParenToken)left;
                parameters = ParseSyntaxListWithComma(buffer, style, ParseExpression);
                var right = buffer.Peek();
                if (ErlangToken.IsRightParen(right))
                {
                    buffer.Advance();
                    rightParen = (ErlangRightParenToken)right;
                }

                return true;
            }

            return false;
        }

        // this function will be the entry point for the REPL
        public static ErlangExpressionSyntax ParseExpression(TokenBuffer buffer)
        {
            return ParseExpression(buffer, ParseStyle.Body);
        }

        internal static ErlangExpressionSyntax ParseExpression(TokenBuffer buffer, ParseStyle style)
        {
            // TODO: create dummy OperatorSyntax class to hold operator token so we don't have to use 'object'
            // shunting yard
            var getExpression = true;
            var output = new List<object>();
            var opStack = new Stack<object>();
            while (buffer.TokensRemain())
            {
                // get the next item.  should be either an expression or an operator token
                object item = null;
                if (getExpression)
                {
                    // try to get a unary operator
                    ErlangOperatorToken op = null;
                    var unary = buffer.Peek();
                    if (ErlangToken.IsPlus(unary) || ErlangToken.IsMinus(unary) || ErlangToken.IsNot(unary) || ErlangToken.IsBNot(unary))
                    {
                        buffer.Advance();
                        op = (ErlangOperatorToken)unary;
                    }

                    var expression = ParseSimpleExpression(buffer, style);
                    if (op == null)
                        item = expression;
                    else
                        item = new ErlangUnaryOperationSyntax(op, expression);
                }
                else
                {
                    // parse an operator
                    var op = buffer.Peek();
                    if (op != null && op.Kind == ErlangTokenKind.Operator)
                    {
                        buffer.Advance();
                        item = op;
                    }
                }

                if (item == null)
                    break;
                getExpression = !getExpression;

                if (item is ErlangOperatorToken)
                {
                    var op = (ErlangOperatorToken)item;
                    while (opStack.Count > 0 && opStack.Peek() is ErlangOperatorToken &&
                        ((op.IsLeftAssociative && op.Precedence <= ((ErlangOperatorToken)opStack.Peek()).Precedence)
                        || op.Precedence < ((ErlangOperatorToken)opStack.Peek()).Precedence))
                    {
                        output.Add(opStack.Pop());
                    }
                    opStack.Push(item);
                }
                else
                {
                    output.Add(item);
                }
            }

            //
            while (opStack.Count > 0)
            {
                output.Add(opStack.Pop());
            }

            if (output.Count == 0)
                return null;

            // now do like a normal stack calculator
            // TODO: cleanup and verify stack counts
            var resultStack = new Stack<object>();
            foreach (var item in output)
            {
                if (item is ErlangOperatorToken)
                {
                    var b = (ErlangExpressionSyntax)resultStack.Pop();
                    var a = (ErlangExpressionSyntax)resultStack.Pop();
                    resultStack.Push(new ErlangBinaryOperationSyntax(a, (ErlangOperatorToken)item, b));
                }
                else
                {
                    resultStack.Push(item);
                }
            }

            if (resultStack.Count == 1)
                return (ErlangExpressionSyntax)resultStack.Pop();
            else
                return null;
        }

        private static ErlangExpressionSyntax ParseSimpleExpression(TokenBuffer buffer, ParseStyle style)
        {
            return (ParseFunctionReference(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseTypeGuard(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseCase(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseFunctionSpecification(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseFunctionInvocation(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseAtom(buffer) as ErlangExpressionSyntax)
                ?? (ParseVariable(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseConstant(buffer) as ErlangExpressionSyntax)
                ?? (ErlangListSyntax.ParseList(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseTuple(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseParentheticalExpression(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseMacroExpression(buffer, style) as ErlangExpressionSyntax);
        }

        private static ErlangCaseSyntax ParseCase(TokenBuffer buffer, ParseStyle style)
        {
            var caseKeyword = buffer.Peek();
            if (ErlangToken.IsCaseKeyword(caseKeyword))
            {
                buffer.Advance();
                var expression = ParseExpression(buffer, style); // TODO: how to disallow case/if here?
                if (expression != null)
                {
                    var ofKeyword = buffer.Peek();
                    if (ErlangToken.IsOfKeyword(ofKeyword))
                    {
                        buffer.Advance();
                        var branches = ParseSyntaxListWithComma(buffer, style, ParseCaseBranch);
                        var endKeyword = buffer.Peek();
                        if (ErlangToken.IsEndKeyword(endKeyword))
                        {
                            buffer.Advance();
                            return new ErlangCaseSyntax((ErlangKeywordToken)caseKeyword, expression, (ErlangKeywordToken)ofKeyword, branches, (ErlangKeywordToken)endKeyword);
                        }
                    }
                }
            }

            return null;
        }

        private static ErlangCaseBranchSyntax ParseCaseBranch(TokenBuffer buffer, ParseStyle style)
        {
            // pattern is an atom, variable, constant, list, or tuple
            var pattern = (ParseAtom(buffer) as ErlangExpressionSyntax)
                ?? (ParseVariable(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseConstant(buffer) as ErlangExpressionSyntax)
                ?? (ErlangListSyntax.ParseList(buffer, style) as ErlangExpressionSyntax)
                ?? (ParseTuple(buffer, style) as ErlangExpressionSyntax);
            if (pattern != null)
            {
                var guard = ParseGuard(buffer, style);
                var arrow = buffer.Peek();
                if (ErlangToken.IsRightArrow(arrow))
                {
                    buffer.Advance();
                    // TODO: this block was copied from ParseFunctionDefintion.  generalize it
                    var expression = new List<ErlangTermintaedExpressionSyntax>();
                    while (buffer.TokensRemain())
                    {
                        var expr = ParseTerminatedExpression(buffer, style);
                        if (expr == null)
                            break;
                        expression.Add(expr);
                        if (expr.Terminator == null || expr.Terminator.PunctuationKind == ErlangPunctuationKind.Semicolon || expr.Terminator.PunctuationKind == ErlangPunctuationKind.Dot)
                            break;
                    }

                    return new ErlangCaseBranchSyntax(pattern, guard, (ErlangMinusGreaterToken)arrow, expression);
                }
            }

            return null;
        }

        private static ErlangParentheticalExpressionSyntax ParseParentheticalExpression(TokenBuffer buffer, ParseStyle style)
        {
            var left = buffer.Peek();
            if (ErlangToken.IsLeftParen(left))
            {
                buffer.Advance();
                var expression = ParseExpression(buffer, style);
                if (expression != null)
                {
                    var right = buffer.Peek();
                    if (ErlangToken.IsRightParen(right))
                    {
                        buffer.Advance();
                        return new ErlangParentheticalExpressionSyntax((ErlangLeftParenToken)left, expression, (ErlangRightParenToken)right);
                    }
                }

                buffer.Retreat();
            }

            return null;
        }

        private static ErlangMacroExpressionSyntax ParseMacroExpression(TokenBuffer buffer, ParseStyle style)
        {
            if (buffer.Peek() is ErlangMacroToken macro)
            {
                buffer.Advance();
                return new ErlangMacroExpressionSyntax(macro);
            }

            return null;
        }

        private static bool IsGroupingOperator(ErlangOperatorKind op)
        {
            switch (op)
            {
                case ErlangOperatorKind.Asterisk:
                case ErlangOperatorKind.Equals:
                case ErlangOperatorKind.EqualsColonEquals:
                case ErlangOperatorKind.EqualsEquals:
                case ErlangOperatorKind.EqualsLess:
                case ErlangOperatorKind.EqualsSlashEquals:
                case ErlangOperatorKind.Greater:
                case ErlangOperatorKind.GreaterEquals:
                case ErlangOperatorKind.Less:
                case ErlangOperatorKind.Minus:
                case ErlangOperatorKind.MinusMinus:
                case ErlangOperatorKind.Plus:
                case ErlangOperatorKind.PlusPlus:
                case ErlangOperatorKind.Slash:
                case ErlangOperatorKind.SlashEquals:
                    return true;
                default:
                    return false;
            }
        }

        private static ErlangTupleSyntax ParseTuple(TokenBuffer buffer, ParseStyle style)
        {
            var left = buffer.Peek();
            if (ErlangToken.IsLeftBrace(left))
            {
                buffer.Advance();
                var items = new List<ErlangTupleItemSyntax>();
                while (buffer.TokensRemain())
                {
                    var item = ParseTupleItem(buffer, style);
                    if (item == null)
                        break;
                    items.Add(item);
                }

                ErlangRightBraceToken rightBrace = null;
                var right = buffer.Peek();
                if (ErlangToken.IsRightBrace(right))
                {
                    buffer.Advance();
                    rightBrace = (ErlangRightBraceToken)right;
                }

                return new ErlangTupleSyntax((ErlangLeftBraceToken)left, items, rightBrace);
            }

            return null;
        }

        private static ErlangTupleItemSyntax ParseTupleItem(TokenBuffer buffer, ParseStyle style)
        {
            var expression = ParseExpression(buffer, style);
            if (expression != null)
            {
                ErlangCommaToken commaToken = null;
                var comma = buffer.Peek();
                if (ErlangToken.IsComma(comma))
                {
                    buffer.Advance();
                    commaToken = (ErlangCommaToken)comma;
                }

                return new ErlangTupleItemSyntax(expression, commaToken);
            }

            return null;
        }

        private static ErlangAtomSyntax ParseAtom(TokenBuffer buffer)
        {
            var atom = buffer.Peek();
            if (atom != null && atom.Kind == ErlangTokenKind.Atom)
            {
                buffer.Advance();
                return new ErlangAtomSyntax((ErlangAtomToken)atom);
            }
            else
                return null;
        }

        private static ErlangVariableSyntax ParseVariable(TokenBuffer buffer, ParseStyle style)
        {
            var variable = buffer.Peek();
            if (variable != null && variable.Kind == ErlangTokenKind.Variable)
            {
                buffer.Advance();
                return new ErlangVariableSyntax((ErlangVariableToken)variable);
            }
            else
                return null;
        }

        private static ErlangConstantSyntax ParseConstant(TokenBuffer buffer)
        {
            var token = buffer.Peek();
            if (token != null)
            {
                if (token.Kind == ErlangTokenKind.Number)
                {
                    buffer.Advance();
                    return new ErlangConstantSyntax((ErlangNumberToken)token);
                }
            }

            return null;
        }

        private static ErlangFunctionReferenceSyntax ParseFunctionReference(TokenBuffer buffer, ParseStyle style)
        {
            int offset = buffer.Offset;
            ErlangKeywordToken fun = null;
            if (style == ParseStyle.Body)
            {
                var first = buffer.Peek();
                if (first == null || first.Kind != ErlangTokenKind.Keyword || ((ErlangKeywordToken)first).Text != "fun")
                    return null;
                fun = (ErlangKeywordToken)first;
                buffer.Advance();
            }

            ErlangToken module = null;
            ErlangColonToken colon = null;
            ErlangToken function = null;
            ErlangSlashToken slash = null;
            ErlangNumberToken airity = null;
            var mod = buffer.Peek();
            if (mod != null && (mod.Kind == ErlangTokenKind.Atom || mod.Kind == ErlangTokenKind.Variable))
            {
                buffer.Advance();
                var tok = buffer.Peek();
                if (ErlangToken.IsColon(tok))
                {
                    buffer.Advance();
                    module = mod;
                    colon = (ErlangColonToken)tok;
                }
                else if (ErlangToken.IsSlash(tok))
                {
                    buffer.Advance();
                    function = mod;
                    slash = (ErlangSlashToken)tok;
                }
                else
                {
                    goto funrefend;
                }

                // if parsing a fully qualified reference, next token is the function
                // if parsing a local reference, next token is the airity
                tok = buffer.Peek();
                if (module != null && colon != null)
                {
                    // parsing a fully qualified reference, token is the function
                    if (tok != null && (tok.Kind == ErlangTokenKind.Atom || tok.Kind == ErlangTokenKind.Variable))
                    {
                        buffer.Advance();
                        function = tok;
                        tok = buffer.Peek();
                        if (ErlangToken.IsSlash(tok))
                        {
                            buffer.Advance();
                            slash = (ErlangSlashToken)tok;
                        }
                    }
                    else
                    {
                        goto funrefend;
                    }
                }

                // parse the airity
                tok = buffer.Peek();
                if (ErlangToken.IsNumber(tok))
                {
                    buffer.Advance();
                    airity = (ErlangNumberToken)tok;
                    Debug.Assert(function != null);
                    Debug.Assert(slash != null);
                    return new ErlangFunctionReferenceSyntax(fun, module, colon, function, slash, airity);
                }
            }

        funrefend:
            buffer.SetOffset(offset);
            return null;
        }

        private static ErlangFunctionSpecificationSyntax ParseFunctionSpecification(TokenBuffer buffer, ParseStyle style)
        {
            var offset = buffer.Offset;
            var name = buffer.Peek() as ErlangAtomToken;
            if (name != null)
            {
                buffer.Advance();
                var left = buffer.Peek() as ErlangLeftParenToken;
                if (left != null)
                {
                    buffer.Advance();
                    var parameters = ParseSyntaxListWithComma(buffer, style, ParseVariable);
                    var right = buffer.Peek() as ErlangRightParenToken;
                    if (right != null)
                    {
                        buffer.Advance();
                        var arrow = buffer.Peek() as ErlangMinusGreaterToken;
                        if (arrow == null)
                        {
                            buffer.SetOffset(offset);
                            return null;
                        }

                        buffer.Advance();
                        var returnTypes = ParseSyntaxListWithPipe(buffer, style, ParseType);
                        var when = buffer.Peek() as ErlangKeywordToken;

                        IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> typeGuards = new ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>[0];
                        if (when?.Text == "when")
                        {
                            buffer.Advance();
                            typeGuards = ParseSyntaxListWithComma(buffer, style, ParseTypeWithPossibleGuard);
                        }

                        return new ErlangFunctionSpecificationSyntax(name, left, parameters, right, arrow, returnTypes, when, typeGuards);
                    }
                }
            }

            buffer.SetOffset(offset);
            return null;
        }

        private static ErlangFunctionInvocationSyntax ParseFunctionInvocation(TokenBuffer buffer, ParseStyle style)
        {
            var startPos = buffer.Offset;
            var module = buffer.Peek();
            if (module != null && (module.Kind == ErlangTokenKind.Atom || module.Kind == ErlangTokenKind.Variable))
            {
                buffer.Advance();
                var next = buffer.Peek();
                if (next != null)
                {
                    ErlangModuleReferenceSyntax moduleRef = null;
                    ErlangToken function = null;
                    List<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> parameters = null;
                    ErlangLeftParenToken leftParen = null;
                    ErlangRightParenToken rightParen = null;
                    if (next.GetType() == typeof(ErlangColonToken))
                    {
                        var colon = (ErlangColonToken)next;
                        moduleRef = new ErlangModuleReferenceSyntax(module, colon);
                        buffer.Advance();
                        next = buffer.Peek();
                        if (next != null)
                        {
                            if (next.Kind == ErlangTokenKind.Atom || next.Kind == ErlangTokenKind.Variable)
                            {
                                function = next;
                                buffer.Advance();
                                next = buffer.Peek();
                            }
                            else
                            {
                                buffer.Retreat();
                            }
                        }
                    }
                    else
                    {
                        // no module reference
                        function = module;
                    }

                    if (ErlangToken.IsLeftParen(next))
                    {
                        buffer.Advance();
                        leftParen = (ErlangLeftParenToken)next;
                        parameters = ParseSyntaxListWithComma(buffer, style, ParseExpression);
                        next = buffer.Peek();
                        if (ErlangToken.IsRightParen(next))
                        {
                            buffer.Advance();
                            rightParen = (ErlangRightParenToken)next;
                        }

                        // check for a function definition
                        next = buffer.Peek();
                        if (style == ParseStyle.Body && next != null && next.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)next).PunctuationKind == ErlangPunctuationKind.MinusGreater)
                        {
                            // looks like this is really a new function definition
                            buffer.SetOffset(startPos);
                            return null;
                        }
                        else
                        {
                            return new ErlangFunctionInvocationSyntax(moduleRef, function, leftParen, parameters, rightParen);
                        }
                    }
                }

                buffer.Retreat();
            }

            return null;
        }

        private static ErlangFunctionGroupSyntax ParseFunctionGroup(TokenBuffer buffer, ParseStyle style)
        {
            int airity = -1;
            var atom = buffer.Peek();
            if (atom != null && atom.Kind == ErlangTokenKind.Atom)
            {
                var name = atom.Text;
                var definitions = new List<ErlangFunctionDefinitionSyntax>();
                while (buffer.TokensRemain())
                {
                    var next = buffer.Peek();
                    if (next != null && next.Kind == ErlangTokenKind.Atom && next.Text == name)
                    {
                        // if same name, get next definition
                        var start = buffer.Offset;
                        var funDef = ParseFunctionDefinition(buffer, style);
                        if (funDef != null)
                        {
                            if (airity == -1)
                                airity = funDef.Airity;
                            if (funDef.Airity == airity)
                            {
                                definitions.Add(funDef);
                            }
                            else
                            {
                                // wrong airity.  reset and return
                                buffer.SetOffset(start);
                                if (definitions.Count > 0)
                                    return new ErlangFunctionGroupSyntax(name, airity, definitions);
                                else
                                    return null;
                            }
                        }
                        else
                            break;
                    }
                    else
                    {
                        break;
                    }
                }

                return new ErlangFunctionGroupSyntax(name, airity, definitions);
            }

            return null;
        }

        private static ErlangFunctionDefinitionSyntax ParseFunctionDefinition(TokenBuffer buffer, ParseStyle style)
        {
            var name = buffer.Peek();
            if (name != null && name.Kind == ErlangTokenKind.Atom)
            {
                buffer.Advance();
                ErlangLeftParenToken leftParen;
                ErlangRightParenToken rightParen;
                List<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> parameters;
                if (ParseParameters(buffer, style, out parameters, out leftParen, out rightParen))
                {
                    var guard = ParseGuard(buffer, style);
                    var arrow = buffer.Peek();
                    if (arrow != null && arrow.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)arrow).PunctuationKind == ErlangPunctuationKind.MinusGreater)
                    {
                        buffer.Advance();
                        var expressions = new List<ErlangTermintaedExpressionSyntax>();
                        while (buffer.TokensRemain())
                        {
                            var expr = ParseTerminatedExpression(buffer, style);
                            if (expr == null)
                                break;
                            expressions.Add(expr);
                            if (expr.Terminator != null &&
                                (expr.Terminator.PunctuationKind == ErlangPunctuationKind.Semicolon || expr.Terminator.PunctuationKind == ErlangPunctuationKind.Dot))
                                break;
                        }

                        return new ErlangFunctionDefinitionSyntax((ErlangAtomToken)name, leftParen, parameters, rightParen, guard, (ErlangMinusGreaterToken)arrow, expressions);
                    }
                }
            }

            return null;
        }

        private static ErlangGuardSyntax ParseGuard(TokenBuffer buffer, ParseStyle style)
        {
            var when = buffer.Peek();
            if (when != null && when.Kind == ErlangTokenKind.Keyword && ((ErlangKeywordToken)when).Text == "when")
            {
                buffer.Advance();
                var clauses = ParseSyntaxListWithComma(buffer, style, ParseExpression);
                return new ErlangGuardSyntax((ErlangKeywordToken)when, clauses);
            }

            return null;
        }

        private static ErlangTermintaedExpressionSyntax ParseTerminatedExpression(TokenBuffer buffer, ParseStyle style)
        {
            var expression = ParseExpression(buffer, style);
            if (expression == null)
                return null;
            var next = buffer.Peek();
            ErlangPunctuationToken terminator = null;
            if (next != null && next.Kind == ErlangTokenKind.Punctuation)
            {
                buffer.Advance();
                terminator = (ErlangPunctuationToken)next;
            }

            return new ErlangTermintaedExpressionSyntax(expression, terminator);
        }
    }

    public class SyntaxList<T> : IEnumerable<T> where T : ErlangSyntaxNode
    {
        private T[] items;

        public T this[int index]
        {
            get
            {
                return items[index];
            }
        }

        public int Count
        {
            get { return items.Length; }
        }

        public SyntaxList(IEnumerable<T> items)
        {
            this.items = items.ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public override string ToString()
        {
            return string.Join(string.Empty, (IEnumerable<T>)items);
        }
    }

    public abstract class ErlangExpressionSyntax : ErlangSyntaxNode
    {
    }

    public class ErlangUnaryOperationSyntax : ErlangExpressionSyntax
    {
        public ErlangOperatorToken Operator { get; private set; }
        public ErlangExpressionSyntax Expression { get; private set; }

        public ErlangUnaryOperationSyntax(ErlangOperatorToken op, ErlangExpressionSyntax expression)
        {
            Operator = op;
            Expression = expression;
        }

        public override string ToString()
        {
            return string.Concat(Operator, Expression);
        }
    }

    public class ErlangBinaryOperationSyntax : ErlangExpressionSyntax
    {
        public ErlangExpressionSyntax Left { get; private set; }
        public ErlangOperatorToken Operator { get; private set; }
        public ErlangExpressionSyntax Right { get; private set; }

        public ErlangBinaryOperationSyntax(ErlangExpressionSyntax left, ErlangOperatorToken op, ErlangExpressionSyntax right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override string ToString()
        {
            return string.Concat(Left, Operator, Right);
        }
    }

    public class ErlangAtomSyntax : ErlangExpressionSyntax
    {
        public ErlangAtomToken Atom { get; private set; }

        public ErlangAtomSyntax(ErlangAtomToken atom)
        {
            Atom = atom;
        }

        public ErlangAtomSyntax(string atom)
            : this(new ErlangAtomToken(atom))
        {
        }

        public override string ToString()
        {
            return Atom.ToString();
        }
    }

    public class ErlangVariableSyntax : ErlangExpressionSyntax
    {
        public ErlangVariableToken Variable { get; private set; }

        public ErlangVariableSyntax(ErlangVariableToken variable)
        {
            Variable = variable;
        }

        public override string ToString()
        {
            return Variable.ToString();
        }
    }

    public class ErlangConstantSyntax : ErlangExpressionSyntax
    {
        public ErlangNumberToken Token { get; private set; }

        public ErlangConstantSyntax(ErlangNumberToken token)
        {
            if (token.Kind != ErlangTokenKind.Number)
                throw new ArgumentException("Expected number");
            Token = token;
        }

        public override string ToString()
        {
            return Token.ToString();
        }
    }

    public class ErlangMacroExpressionSyntax : ErlangExpressionSyntax
    {
        public ErlangMacroToken Macro { get; }

        public ErlangMacroExpressionSyntax(ErlangMacroToken macro)
        {
            Macro = macro;
        }

        public override string ToString()
        {
            return Macro.ToString();
        }
    }

    public class ErlangParentheticalExpressionSyntax : ErlangExpressionSyntax
    {
        public ErlangLeftParenToken LeftParen { get; private set; }
        public ErlangExpressionSyntax Expression { get; private set; }
        public ErlangRightParenToken RightParen { get; private set; }

        public ErlangParentheticalExpressionSyntax(ErlangLeftParenToken leftParen, ErlangExpressionSyntax expression, ErlangRightParenToken rightParen)
        {
            LeftParen = leftParen;
            Expression = expression;
            RightParen = rightParen;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}", LeftParen, Expression, RightParen);
        }
    }

    public class ErlangTupleSyntax : ErlangExpressionSyntax
    {
        public ErlangLeftBraceToken LeftBrace { get; private set; }
        public SyntaxList<ErlangTupleItemSyntax> Items { get; private set; }
        public ErlangRightBraceToken RightBrace { get; private set; }

        public ErlangTupleSyntax(ErlangLeftBraceToken leftBrace, IEnumerable<ErlangTupleItemSyntax> items, ErlangRightBraceToken rightBrace)
        {
            LeftBrace = leftBrace;
            Items = new SyntaxList<ErlangTupleItemSyntax>(items);
            RightBrace = rightBrace;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}", LeftBrace, string.Join("", Items), RightBrace);
        }
    }

    public class ErlangTupleItemSyntax : ErlangSyntaxNode
    {
        public ErlangExpressionSyntax Item { get; private set; }
        public ErlangCommaToken Comma { get; private set; }

        public ErlangTupleItemSyntax(ErlangExpressionSyntax item, ErlangCommaToken comma)
        {
            Item = item;
            Comma = comma;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}", Item, Comma);
        }
    }

    public class ErlangFunctionReferenceSyntax : ErlangExpressionSyntax
    {
        public ErlangKeywordToken FunKeyword { get; private set; }
        public ErlangToken Module { get; private set; }
        public ErlangColonToken Colon { get; private set; }
        public ErlangToken Function { get; private set; }
        public ErlangSlashToken Slash { get; private set; }
        public ErlangNumberToken Airity { get; private set; }

        public ErlangFunctionReferenceSyntax(ErlangKeywordToken funKeyword, ErlangToken module, ErlangColonToken colon, ErlangToken function, ErlangSlashToken slash, ErlangNumberToken airity)
        {
            FunKeyword = funKeyword;
            Module = module;
            Colon = colon;
            Function = function;
            Slash = slash;
            Airity = airity;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}{3}{4}{5}{6}", FunKeyword, (FunKeyword == null ? string.Empty : " "), Module, Colon, Function, Slash, Airity);
        }
    }

    public class ErlangTermintaedExpressionSyntax : ErlangSyntaxNode
    {
        public ErlangExpressionSyntax Expression { get; private set; }
        public ErlangPunctuationToken Terminator { get; private set; }

        public ErlangTermintaedExpressionSyntax(ErlangExpressionSyntax expression, ErlangPunctuationToken terminator)
        {
            Expression = expression;
            Terminator = terminator;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}", Expression, Terminator);
        }
    }

    public class ErlangModuleReferenceSyntax : ErlangSyntaxNode
    {
        public ErlangToken Module { get; private set; } // atom or variable
        public ErlangColonToken Colon { get; private set; }

        public ErlangModuleReferenceSyntax(ErlangToken module, ErlangColonToken colon)
        {
            Module = module;
            Colon = colon;
        }

        public override string ToString()
        {
            return Module.Text + Colon.Text;
        }
    }

    public class ErlangFunctionInvocationSyntax : ErlangExpressionSyntax
    {
        public ErlangModuleReferenceSyntax ModuleReference { get; private set; } // can be null
        public ErlangToken Function { get; private set; } // can be either atom or variable
        public ErlangLeftParenToken LeftParen { get; private set; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> Parameters { get; private set; } // invocation parameters
        public ErlangRightParenToken RightParen { get; private set; }

        public ErlangFunctionInvocationSyntax(ErlangModuleReferenceSyntax moduleReference, ErlangToken function, ErlangLeftParenToken leftParen, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> parameters, ErlangRightParenToken rightParen)
        {
            ModuleReference = moduleReference;
            Function = function;
            LeftParen = leftParen;
            Parameters = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>>(parameters);
            RightParen = rightParen;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}{3}{4}", ModuleReference, Function, LeftParen, Parameters, RightParen);
        }
    }

    public class ErlangCaseSyntax : ErlangExpressionSyntax
    {
        public ErlangKeywordToken CaseKeyword { get; private set; }
        public ErlangExpressionSyntax Expression { get; private set; }
        public ErlangKeywordToken OfKeyword { get; private set; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangCaseBranchSyntax>> Branches { get; private set; }
        public ErlangKeywordToken EndKeyword { get; private set; }

        public ErlangCaseSyntax(ErlangKeywordToken caseKeyword, ErlangExpressionSyntax expression, ErlangKeywordToken ofKeyword, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangCaseBranchSyntax>> branches, ErlangKeywordToken endKeyword)
        {
            CaseKeyword = caseKeyword;
            Expression = expression;
            OfKeyword = ofKeyword;
            Branches = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangCaseBranchSyntax>>(branches);
            EndKeyword = endKeyword;
        }
    }

    public class ErlangCaseBranchSyntax : ErlangSyntaxNode
    {
        public ErlangExpressionSyntax Pattern { get; private set; }
        public ErlangGuardSyntax Guard { get; private set; }
        public ErlangMinusGreaterToken Arrow { get; private set; }
        public SyntaxList<ErlangTermintaedExpressionSyntax> Expressions { get; private set; }

        public ErlangCaseBranchSyntax(ErlangExpressionSyntax pattern, ErlangGuardSyntax guard, ErlangMinusGreaterToken arrow, IEnumerable<ErlangTermintaedExpressionSyntax> expressions)
        {
            Pattern = pattern;
            Guard = guard;
            Arrow = arrow;
            Expressions = new SyntaxList<ErlangTermintaedExpressionSyntax>(expressions);
        }
    }

    public class ErlangAttributeSyntax : ErlangAttributeOrFunctionGroupSyntax
    {
        public ErlangMinusToken Minus { get; }
        public ErlangAtomToken Name { get; }
        public ErlangLeftParenToken LeftParen { get; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> Parameters { get; }
        public ErlangRightParenToken RightParen { get; }
        public ErlangDotToken Dot { get; }

        public ErlangAttributeSyntax(ErlangMinusToken minus, ErlangAtomToken name, ErlangLeftParenToken leftParen, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> parameters, ErlangRightParenToken rightParen, ErlangDotToken dot)
        {
            Minus = minus;
            Name = name;
            LeftParen = leftParen;
            Parameters = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>>(parameters);
            RightParen = rightParen;
            Dot = dot;
        }

        public override string ToString()
        {
            return LeftParen != null || RightParen != null
                ? string.Format("{0}{1}{2}{3}{4}{5}", Minus, Name, LeftParen, Parameters, RightParen, Dot)
                : Parameters.Any()
                    ? string.Format("{0}{1} {2}{3}", Minus, Name, Parameters, Dot)
                    : string.Format("{0}{1}{2}", Minus, Name, Dot);
        }
    }

    public class ErlangSeparatedSyntaxNodeSyntax<TSyntax> : ErlangSyntaxNode where TSyntax : ErlangSyntaxNode
    {
        public TSyntax Value { get; }
        public ErlangPunctuationToken Separator { get; }

        public ErlangSeparatedSyntaxNodeSyntax(TSyntax value, ErlangPunctuationToken separator)
        {
            Value = value;
            Separator = separator;
        }

        public override string ToString()
        {
            return string.Concat(Value, Separator);
        }
    }

    public class ErlangTypeGuardSyntax : ErlangExpressionSyntax
    {
        public ErlangColonColonToken DoubleColon { get; private set; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> Restrictions { get; private set; }

        public ErlangTypeGuardSyntax(ErlangColonColonToken doubleColon, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> restrictions)
        {
            DoubleColon = doubleColon;
            Restrictions = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>>(restrictions);
        }

        public override string ToString()
        {
            return string.Concat(DoubleColon, string.Join(string.Empty, Restrictions));
        }
    }

    public class ErlangFunctionSpecificationSyntax : ErlangExpressionSyntax
    {
        public ErlangAtomToken FunctionName { get; }
        public ErlangLeftParenToken LeftParen { get; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangVariableSyntax>> Parameters { get; }
        public ErlangRightParenToken RightParen { get; }
        public ErlangMinusGreaterToken Arrow { get; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> ReturnTypes { get; }
        public ErlangKeywordToken WhenKeyword { get; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> TypeGuards { get; }

        public ErlangFunctionSpecificationSyntax(ErlangAtomToken functionName, ErlangLeftParenToken left, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangVariableSyntax>> parameters, ErlangRightParenToken right, ErlangMinusGreaterToken arrow, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> returnTypes, ErlangKeywordToken when, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>> typeGuards)
        {
            FunctionName = functionName;
            LeftParen = left;
            Parameters = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangVariableSyntax>>(parameters);
            RightParen = right;
            Arrow = arrow;
            ReturnTypes = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>>(returnTypes);
            WhenKeyword = when;
            TypeGuards = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangTypeSyntax>>(typeGuards);
        }

        public override string ToString()
        {
            var min = string.Concat(FunctionName, LeftParen, string.Join(string.Empty, Parameters), RightParen, Arrow, string.Join(string.Empty, ReturnTypes));
            return WhenKeyword == null
                ? min
                : string.Concat(min, " ", WhenKeyword, " ", string.Join(string.Empty, TypeGuards));
        }
    }

    public class ErlangFunctionDefinitionSyntax : ErlangSyntaxNode
    {
        public ErlangAtomToken Name { get; private set; }
        public int Airity { get { return Parameters.Count; } }
        public ErlangLeftParenToken LeftParen { get; private set; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> Parameters { get; private set; }
        public ErlangRightParenToken RightParen { get; private set; }
        public ErlangGuardSyntax Guard { get; private set; }
        public ErlangMinusGreaterToken Arrow { get; private set; }
        public SyntaxList<ErlangTermintaedExpressionSyntax> Expressions { get; private set; }

        public ErlangFunctionDefinitionSyntax(ErlangAtomToken name, ErlangLeftParenToken leftParen, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> parameters, ErlangRightParenToken rightParen, ErlangGuardSyntax guard, ErlangMinusGreaterToken arrow, IEnumerable<ErlangTermintaedExpressionSyntax> expressions)
        {
            Name = name;
            LeftParen = leftParen;
            Parameters = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>>(parameters);
            RightParen = rightParen;
            Guard = guard;
            Arrow = arrow;
            Expressions = new SyntaxList<ErlangTermintaedExpressionSyntax>(expressions);
        }
    }

    public class ErlangGuardSyntax : ErlangSyntaxNode
    {
        public ErlangKeywordToken WhenKeyword { get; private set; }
        public SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> Clauses { get; private set; }

        public ErlangGuardSyntax(ErlangKeywordToken whenKeyword, IEnumerable<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>> clauses)
        {
            WhenKeyword = whenKeyword;
            Clauses = new SyntaxList<ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax>>(clauses);
        }
    }

    public abstract class ErlangAttributeOrFunctionGroupSyntax : ErlangSyntaxNode
    {
    }

    public class ErlangFunctionGroupSyntax : ErlangAttributeOrFunctionGroupSyntax
    {
        public string Name { get; private set; }
        public int Airity { get; private set; }
        public SyntaxList<ErlangFunctionDefinitionSyntax> Definitions { get; private set; }

        public ErlangFunctionGroupSyntax(string name, int airity, IEnumerable<ErlangFunctionDefinitionSyntax> definitions)
        {
            Name = name;
            Airity = airity;
            Definitions = new SyntaxList<ErlangFunctionDefinitionSyntax>(definitions);
        }
    }

    public class ErlangModuleSyntax : ErlangSyntaxNode
    {
        public SyntaxList<ErlangAttributeOrFunctionGroupSyntax> Elements { get; private set; }
        public IEnumerable<ErlangAttributeSyntax> Attributes => Elements.OfType<ErlangAttributeSyntax>();
        public IEnumerable<ErlangFunctionGroupSyntax> FunctionGroups => Elements.OfType<ErlangFunctionGroupSyntax>();

        public ErlangModuleSyntax(IEnumerable<ErlangAttributeOrFunctionGroupSyntax> elements)
        {
            Elements = new SyntaxList<ErlangAttributeOrFunctionGroupSyntax>(elements);
        }
    }
}
