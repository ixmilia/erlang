using System.Linq;
using IxMilia.Erlang.Tokens;
using Xunit;

namespace IxMilia.Erlang.Test
{
    public class TokenTests : TestBase
    {
        private static ErlangToken[] Tokenize(string text)
        {
            return ErlangToken.Tokenize(new TextBuffer(text)).ToArray();
        }

        private static void Verify(string text, params ErlangToken[] expected)
        {
            var actual = Tokenize(text);
            ArrayEquals(expected, actual);
        }

        private static void VerifyOffsets(string text, params int[] expected)
        {
            var actual = Tokenize(text).Select(t => t.Offset).ToArray();
            ArrayEquals(expected, actual);
        }

        [Fact]
        public void SimpleTokenizeTest()
        {
            Verify("foo", new ErlangAtomToken("foo")); // atom
            Verify("foo@bar", new ErlangAtomToken("foo@bar"));
            Verify("'an atom'", new ErlangAtomToken("'an atom'"));
            Verify("?MACRO", new ErlangMacroToken("?MACRO")); // macro
            Verify("?MD5SZ", new ErlangMacroToken("?MD5SZ"));
            Verify("4.5", new ErlangNumberToken("4.5", 4.5)); // number
            Verify("$a", new ErlangNumberToken("$a", 97));
            Verify("1.0e-10", new ErlangNumberToken("1.0e-10", 1.0e-10));
            Verify("=:=", new ErlangEqualsColonEqualsToken()); // operator
            Verify("<-", new ErlangLessMinusToken()); // punctuation
            Verify("\"foo bar\"", new ErlangStringToken("foo bar")); // string
            Verify("Variable", new ErlangVariableToken("Variable")); // variable
            Verify("_", new ErlangVariableToken("_"));
        }

        [Fact]
        public void UnaryOperatorNegateTokensizeTests()
        {
            // with numbers
            Verify("-1", new ErlangMinusToken(), new ErlangNumberToken("1", 1));
            Verify("-1-1", new ErlangMinusToken(), new ErlangNumberToken("1", 1), new ErlangMinusToken(), new ErlangNumberToken("1", 1));
            Verify("-1- -1", new ErlangMinusToken(), new ErlangNumberToken("1", 1), new ErlangMinusToken(), new ErlangMinusToken(), new ErlangNumberToken("1", 1));
            Verify("1+-1", new ErlangNumberToken("1", 1), new ErlangPlusToken(), new ErlangMinusToken(), new ErlangNumberToken("1", 1));
            Verify("-(1)", new ErlangMinusToken(), new ErlangLeftParenToken(), new ErlangNumberToken("1", 1), new ErlangRightParenToken());
            Verify("-(1+1)", new ErlangMinusToken(), new ErlangLeftParenToken(), new ErlangNumberToken("1", 1), new ErlangPlusToken(), new ErlangNumberToken("1", 1), new ErlangRightParenToken());
            Verify("((1))-1", new ErlangLeftParenToken(), new ErlangLeftParenToken(), new ErlangNumberToken("1", 1), new ErlangRightParenToken(), new ErlangRightParenToken(), new ErlangMinusToken(), new ErlangNumberToken("1", 1));

            // with variables
            Verify("-X", new ErlangMinusToken(), new ErlangVariableToken("X"));
            Verify("-X-Y", new ErlangMinusToken(), new ErlangVariableToken("X"), new ErlangMinusToken(), new ErlangVariableToken("Y"));
            Verify("-X- -Y", new ErlangMinusToken(), new ErlangVariableToken("X"), new ErlangMinusToken(), new ErlangMinusToken(), new ErlangVariableToken("Y"));
            Verify("X+-Y", new ErlangVariableToken("X"), new ErlangPlusToken(), new ErlangMinusToken(), new ErlangVariableToken("Y"));
            Verify("-(X)", new ErlangMinusToken(), new ErlangLeftParenToken(), new ErlangVariableToken("X"), new ErlangRightParenToken());
            Verify("-(X+Y)", new ErlangMinusToken(), new ErlangLeftParenToken(), new ErlangVariableToken("X"), new ErlangPlusToken(), new ErlangVariableToken("Y"), new ErlangRightParenToken());
            Verify("((X))-Y", new ErlangLeftParenToken(), new ErlangLeftParenToken(), new ErlangVariableToken("X"), new ErlangRightParenToken(), new ErlangRightParenToken(), new ErlangMinusToken(), new ErlangVariableToken("Y"));

            // after a function call
            Verify("foo()-1", new ErlangAtomToken("foo"), new ErlangLeftParenToken(), new ErlangRightParenToken(), new ErlangMinusToken(), new ErlangNumberToken("1", 1));
            Verify("foo()- -1", new ErlangAtomToken("foo"), new ErlangLeftParenToken(), new ErlangRightParenToken(), new ErlangMinusToken(), new ErlangMinusToken(), new ErlangNumberToken("1", 1));
        }

        [Fact]
        public void UnaryOperatorPositiveTokensizeTests()
        {
            // with numbers
            Verify("+1", new ErlangPlusToken(), new ErlangNumberToken("1", 1));
            Verify("+1-1", new ErlangPlusToken(), new ErlangNumberToken("1", 1), new ErlangMinusToken(), new ErlangNumberToken("1", 1));
            Verify("+1-+1", new ErlangPlusToken(), new ErlangNumberToken("1", 1), new ErlangMinusToken(), new ErlangPlusToken(), new ErlangNumberToken("1", 1));
            Verify("1+ +1", new ErlangNumberToken("1", 1), new ErlangPlusToken(), new ErlangPlusToken(), new ErlangNumberToken("1", 1));
            Verify("+(1)", new ErlangPlusToken(), new ErlangLeftParenToken(), new ErlangNumberToken("1", 1), new ErlangRightParenToken());
            Verify("+(1+1)", new ErlangPlusToken(), new ErlangLeftParenToken(), new ErlangNumberToken("1", 1), new ErlangPlusToken(), new ErlangNumberToken("1", 1), new ErlangRightParenToken());
            Verify("((1))+1", new ErlangLeftParenToken(), new ErlangLeftParenToken(), new ErlangNumberToken("1", 1), new ErlangRightParenToken(), new ErlangRightParenToken(), new ErlangPlusToken(), new ErlangNumberToken("1", 1));

            // with variables
            Verify("+X", new ErlangPlusToken(), new ErlangVariableToken("X"));
            Verify("+X-Y", new ErlangPlusToken(), new ErlangVariableToken("X"), new ErlangMinusToken(), new ErlangVariableToken("Y"));
            Verify("+X-+Y", new ErlangPlusToken(), new ErlangVariableToken("X"), new ErlangMinusToken(), new ErlangPlusToken(), new ErlangVariableToken("Y"));
            Verify("X+ +Y", new ErlangVariableToken("X"), new ErlangPlusToken(), new ErlangPlusToken(), new ErlangVariableToken("Y"));
            Verify("+(X)", new ErlangPlusToken(), new ErlangLeftParenToken(), new ErlangVariableToken("X"), new ErlangRightParenToken());
            Verify("+(X+Y)", new ErlangPlusToken(), new ErlangLeftParenToken(), new ErlangVariableToken("X"), new ErlangPlusToken(), new ErlangVariableToken("Y"), new ErlangRightParenToken());
            Verify("((X))+Y", new ErlangLeftParenToken(), new ErlangLeftParenToken(), new ErlangVariableToken("X"), new ErlangRightParenToken(), new ErlangRightParenToken(), new ErlangPlusToken(), new ErlangVariableToken("Y"));

            // after a function call
            Verify("foo()+1", new ErlangAtomToken("foo"), new ErlangLeftParenToken(), new ErlangRightParenToken(), new ErlangPlusToken(), new ErlangNumberToken("1", 1));
            Verify("foo()+ +1", new ErlangAtomToken("foo"), new ErlangLeftParenToken(), new ErlangRightParenToken(), new ErlangPlusToken(), new ErlangPlusToken(), new ErlangNumberToken("1", 1));
        }

        [Fact]
        public void TokenOffsetTests()
        {
            VerifyOffsets("foo() + 1", 0, 3, 4, 6, 8);

            // verify trivia offsets
            var tokens = Tokenize("Variable + 1 % comment");
            Assert.Equal(3, tokens.Length);
            Assert.Equal(8, ((ErlangPlusToken)tokens[1]).LeadingTrivia.Single().Offset);

            var ErlangNumberToken = (ErlangNumberToken)tokens[2];
            Assert.Equal(10, ((ErlangWhitespaceTrivia)ErlangNumberToken.LeadingTrivia.Single()).Offset);
            Assert.Equal(12, ((ErlangWhitespaceTrivia)ErlangNumberToken.TrailingTrivia.First()).Offset);
            Assert.Equal(13, ((ErlangCommentTrivia)ErlangNumberToken.TrailingTrivia.Last()).Offset);
        }
    }
}
