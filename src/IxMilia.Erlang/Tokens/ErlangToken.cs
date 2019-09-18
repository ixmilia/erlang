using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IxMilia.Erlang.Tokens
{
    public abstract class ErlangToken
    {
        public ErlangTokenKind Kind { get; protected set; }

        public string Text { get; protected set; }

        public string Error { get; protected set; }

        public IEnumerable<ErlangTrivia> LeadingTrivia { get; protected set; }

        public IEnumerable<ErlangTrivia> TrailingTrivia { get; protected set; }

        public int Offset { get; private set; }
        public int Line { get; private set; }

        public static ErlangToken LexToken()
        {
            return null;
        }

        public override string ToString()
        {
            return Text;
        }

        public string ToFullString()
        {
            var sb = new StringBuilder();
            if (LeadingTrivia != null)
            {
                sb.Append(string.Join(string.Empty, LeadingTrivia));
            }

            sb.Append(Text);
            if (TrailingTrivia != null)
            {
                sb.Append(string.Join(string.Empty, TrailingTrivia));
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is ErlangToken other)
            {
                return Kind == other.Kind && Text == other.Text;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Kind.GetHashCode() ^ Text.GetHashCode();
        }

        public static bool IsLeftParen(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.LeftParen;
        }

        public static bool IsRightParen(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.RightParen;
        }

        public static bool IsLeftBrace(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.LeftBrace;
        }

        public static bool IsRightBrace(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.RightBrace;
        }

        public static bool IsLeftBracket(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.LeftBracket;
        }

        public static bool IsRightBracket(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.RightBracket;
        }

        public static bool IsDot(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.Dot;
        }

        public static bool IsComma(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.Comma;
        }

        public static bool IsSemicolon(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.Semicolon;
        }

        public static bool IsColon(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Operator && ((ErlangOperatorToken)token).OperatorKind == ErlangOperatorKind.Colon;
        }

        public static bool IsSlash(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Operator && ((ErlangOperatorToken)token).OperatorKind == ErlangOperatorKind.Slash;
        }

        public static bool IsPlus(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Operator && ((ErlangOperatorToken)token).OperatorKind == ErlangOperatorKind.Plus;
        }

        public static bool IsMinus(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Operator && ((ErlangOperatorToken)token).OperatorKind == ErlangOperatorKind.Minus;
        }

        public static bool IsNot(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Operator && ((ErlangOperatorToken)token).OperatorKind == ErlangOperatorKind.Not;
        }

        public static bool IsBNot(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Operator && ((ErlangOperatorToken)token).OperatorKind == ErlangOperatorKind.BNot;
        }

        public static bool IsPipe(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.Pipe;
        }

        public static bool IsDoublePipe(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.PipePipe;
        }

        public static bool IsRightArrow(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.MinusGreater;
        }

        public static bool IsLeftArrow(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Punctuation && ((ErlangPunctuationToken)token).PunctuationKind == ErlangPunctuationKind.LessMinus;
        }

        public static bool IsCaseKeyword(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Keyword && ((ErlangKeywordToken)token).Text == "case";
        }

        public static bool IsOfKeyword(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Keyword && ((ErlangKeywordToken)token).Text == "of";
        }

        public static bool IsEndKeyword(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Keyword && ((ErlangKeywordToken)token).Text == "end";
        }

        public static bool IsWhenKeyword(ErlangToken token)
        {
            return (token as ErlangKeywordToken)?.Text == "when";
        }

        public static bool IsNumber(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.Number;
        }

        public static bool IsString(ErlangToken token)
        {
            return token != null && token.Kind == ErlangTokenKind.String;
        }

        public static IEnumerable<ErlangToken> Tokenize(TextBuffer buffer)
        {
            var tokenList = new List<ErlangToken>();
            var triviaList = new List<ErlangTrivia>();
            var whitespace = new StringBuilder();
            var lastStart = 0;
            var whitespaceStart = 0;
            var lastLine = 1;
            void FlushWhitespace()
            {
                if (whitespace.Length > 0)
                {
                    triviaList.Add(new ErlangWhitespaceTrivia(whitespace.ToString(), whitespaceStart));
                    whitespace.Clear();
                }
            }
            void FlushAndAdd(ErlangToken token)
            {
                if (token != null)
                {
                    FlushWhitespace();
                    token.LeadingTrivia = triviaList;
                    token.Offset = lastStart;
                    token.Line = lastLine;
                    lastStart = buffer.Offset;
                    tokenList.Add(token);
                    triviaList = new List<ErlangTrivia>();
                }
            }

            while (buffer.TextRemains())
            {
                var c = buffer.Peek();
                if (IsWhitespace(c))
                {
                    if (whitespace.Length == 0)
                    {
                        whitespaceStart = buffer.Offset;
                    }
                    else
                    {
                        whitespaceStart++;
                    }

                    whitespace.Append(c);
                    lastStart++;
                    if (c == '\n')
                    {
                        lastLine++;
                    }

                    buffer.Advance();
                }
                else if (IsCommentStart(c))
                {
                    FlushWhitespace();
                    var comment = LexComment(buffer);
                    triviaList.Add(comment);
                }
                else if (c == '(')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangLeftParenToken());
                }
                else if (c == ')')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangRightParenToken());
                }
                else if (c == ',')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangCommaToken());
                }
                else if (c == '*')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangAsteriskToken());
                }
                else if (c == '.')
                {
                    buffer.Advance();
                    if (buffer.TextRemains() && buffer.Peek() == '.')
                    {
                        // found a second dot
                        buffer.Advance();
                        if (buffer.TextRemains() && buffer.Peek() == '.')
                        {
                            // found a third dot
                            buffer.Advance();
                            FlushAndAdd(new ErlangDotDotDotToken());
                        }
                        else
                        {
                            FlushAndAdd(new ErlangDotDotToken());
                        }
                    }
                    else
                    {
                        FlushAndAdd(new ErlangDotToken());
                    }
                }
                else if (c == '[')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangLeftBracketToken());
                }
                else if (c == ']')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangRightBracketToken());
                }
                else if (c == '{')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangLeftBraceToken());
                }
                else if (c == '}')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangRightBraceToken());
                }
                else if (c == ';')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangSemicolonToken());
                }
                else if (c == '!')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangBangToken());
                }
                else if (c == '#')
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangHashToken());
                }
                else if (c == ':')
                {
                    FlushWhitespace();
                    buffer.Advance();
                    if (buffer.TextRemains() && buffer.Peek() == ':')
                    {
                        buffer.Advance();
                        FlushAndAdd(new ErlangColonColonToken());
                    }
                    else
                    {
                        FlushAndAdd(new ErlangColonToken());
                    }
                }
                else if (c == '+')
                {
                    FlushWhitespace();
                    buffer.Advance();
                    if (buffer.TextRemains() && buffer.Peek() == '+')
                    {
                        buffer.Advance();
                        FlushAndAdd(new ErlangPlusPlusToken());
                    }
                    else
                    {
                        FlushAndAdd(new ErlangPlusToken());
                    }
                }
                else if (c == '-')
                {
                    FlushWhitespace();
                    buffer.Advance();
                    if (buffer.TextRemains())
                    {
                        switch (buffer.Peek())
                        {
                            case '-':
                                buffer.Advance();
                                FlushAndAdd(new ErlangMinusMinusToken());
                                break;
                            case '>':
                                buffer.Advance();
                                FlushAndAdd(new ErlangMinusGreaterToken());
                                break;
                            default:
                                FlushAndAdd(new ErlangMinusToken());
                                break;
                        }
                    }
                    else
                    {
                        FlushAndAdd(new ErlangMinusToken());
                    }
                }
                else if (c == '/')
                {
                    FlushWhitespace();
                    buffer.Advance();
                    if (buffer.TextRemains() && buffer.Peek() == '=')
                    {
                        buffer.Advance();
                        FlushAndAdd(new ErlangSlashEqualsToken());
                    }
                    else
                    {
                        FlushAndAdd(new ErlangSlashToken());
                    }
                }
                else if (c == '>')
                {
                    FlushWhitespace();
                    buffer.Advance();
                    if (buffer.TextRemains())
                    {
                        switch (buffer.Peek())
                        {
                            case '>':
                                buffer.Advance();
                                FlushAndAdd(new ErlangGreaterGreaterToken());
                                break;
                            case '=':
                                buffer.Advance();
                                FlushAndAdd(new ErlangGreaterEqualsToken());
                                break;
                            default:
                                FlushAndAdd(new ErlangGreaterToken());
                                break;
                        }
                    }
                    else
                    {
                        FlushAndAdd(new ErlangGreaterToken());
                    }
                }
                else if (c == '<')
                {
                    FlushWhitespace();
                    buffer.Advance();
                    if (buffer.TextRemains())
                    {
                        switch (buffer.Peek())
                        {
                            case '<':
                                buffer.Advance();
                                FlushAndAdd(new ErlangLessLessToken());
                                break;
                            case '-':
                                buffer.Advance();
                                FlushAndAdd(new ErlangLessMinusToken());
                                break;
                            default:
                                FlushAndAdd(new ErlangLessToken());
                                break;
                        }
                    }
                    else
                    {
                        FlushAndAdd(new ErlangLessToken());
                    }
                }
                else if (c == '=')
                {
                    FlushWhitespace();
                    buffer.Advance();
                    if (buffer.TextRemains())
                    {
                        switch (buffer.Peek())
                        {
                            case '=':
                                buffer.Advance();
                                FlushAndAdd(new ErlangEqualsEqualsToken());
                                break;
                            case '<':
                                buffer.Advance();
                                FlushAndAdd(new ErlangEqualsLessToken());
                                break;
                            case ':':
                                buffer.Advance();
                                if (buffer.TextRemains() && buffer.Peek() == '=')
                                {
                                    buffer.Advance();
                                    FlushAndAdd(new ErlangEqualsColonEqualsToken());
                                }
                                else
                                {
                                    buffer.Retreat();
                                    FlushAndAdd(new ErlangEqualsToken());
                                }
                                break;
                            case '/':
                                buffer.Advance();
                                if (buffer.TextRemains() && buffer.Peek() == '=')
                                {
                                    buffer.Advance();
                                    FlushAndAdd(new ErlangEqualsSlashEqualsToken());
                                }
                                else
                                {
                                    buffer.Retreat();
                                    FlushAndAdd(new ErlangEqualsToken());
                                }
                                break;
                            default:
                                FlushAndAdd(new ErlangEqualsToken());
                                break;
                        }
                    }
                    else
                    {
                        FlushAndAdd(new ErlangEqualsToken());
                    }
                }
                else if (c == '|')
                {
                    FlushWhitespace();
                    buffer.Advance();
                    if (buffer.TextRemains() && buffer.Peek() == '|')
                    {
                        buffer.Advance();
                        FlushAndAdd(new ErlangPipePipeToken());
                    }
                    else
                    {
                        FlushAndAdd(new ErlangPipeToken());
                    }
                }
                else if (c == '$')
                {
                    buffer.Advance();
                    if (buffer.TextRemains())
                    {
                        var next = buffer.Peek();
                        buffer.Advance();
                        FlushAndAdd(new ErlangNumberToken(string.Format("${0}", next), (double)next));
                    }
                    else
                    {
                        FlushAndAdd(new ErlangErrorToken(c, "Unexpected end of stream"));
                    }
                }
                else if (ErlangAtomToken.IsAtomStart(c))
                {
                    var atom = ErlangAtomToken.Lex(buffer);
                    FlushAndAdd(atom);
                }
                else if (ErlangVariableToken.IsVariableStart(c))
                {
                    var variable = ErlangVariableToken.Lex(buffer);
                    FlushAndAdd(variable);
                }
                else if (ErlangNumberToken.IsNumberStart(c))
                {
                    var number = ErlangNumberToken.Lex(buffer);
                    FlushAndAdd(number);
                }
                else if (ErlangStringToken.IsStringStart(c))
                {
                    var str = ErlangStringToken.Lex(buffer);
                    FlushAndAdd(str);
                }
                else if (ErlangMacroToken.IsMacroStart(c))
                {
                    var macro = ErlangMacroToken.Lex(buffer);
                    FlushAndAdd(macro);
                }
                else
                {
                    buffer.Advance();
                    FlushAndAdd(new ErlangErrorToken(c, "Unexpected operator"));
                }
            }

            // add final trailing trivia
            if (tokenList.Count > 0 || triviaList.Count > 0)
            {
                tokenList.Last().TrailingTrivia = triviaList;
            }

            return tokenList;
        }

        private static ErlangCommentTrivia LexComment(TextBuffer buffer)
        {
            var comment = new StringBuilder();
            var offset = buffer.Offset;
            comment.Append(buffer.Peek());
            buffer.Advance();
            while (buffer.TextRemains())
            {
                var c = buffer.Peek();
                if (IsNewline(c))
                {
                    break;
                }
                comment.Append(c);
                buffer.Advance();
            }

            return new ErlangCommentTrivia(comment.ToString(), offset);
        }

        protected static bool IsLower(char c)
        {
            return char.IsLower(c);
        }

        protected static bool IsUpper(char c)
        {
            return char.IsUpper(c);
        }

        protected static bool IsDigit(char c)
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

        protected static bool IsSingleQuote(char c)
        {
            return c == '\'';
        }

        protected static bool IsUnderscore(char c)
        {
            return c == '_';
        }

        protected static bool IsAtSign(char c)
        {
            return c == '@';
        }

        protected static bool IsCommentStart(char c)
        {
            return c == '%';
        }

        protected static bool IsWhitespace(char c)
        {
            return char.IsWhiteSpace(c);
        }

        protected static bool IsNewline(char c)
        {
            return c == '\n' || c == '\r';
        }
    }
}
