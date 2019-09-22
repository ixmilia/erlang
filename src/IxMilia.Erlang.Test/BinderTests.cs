using IxMilia.Erlang.Syntax;
using IxMilia.Erlang.Tokens;
using Xunit;

namespace IxMilia.Erlang.Test
{
    public class BinderTests
    {
        private static bool TryBind(string code, ErlangValue value)
        {
            var frame = new ErlangStackFrame("test", "test", 0);
            var expression = ErlangSyntaxNode.ParseExpression(new TokenBuffer(ErlangToken.Tokenize(new TextBuffer(code))));
            var compiledExpr = ErlangExpression.Compile(expression);
            return ErlangBinder.TryBindParameter(compiledExpr, value, frame);
        }

        private static ErlangValue BindAndFetchVariable(string code, ErlangValue valueToBind, string variable)
        {
            var frame = new ErlangStackFrame("test", "test", 0);
            var expression = ErlangSyntaxNode.ParseExpression(new TokenBuffer(ErlangToken.Tokenize(new TextBuffer(code))));
            var compiledExpr = ErlangExpression.Compile(expression);
            Assert.True(ErlangBinder.TryBindParameter(compiledExpr, valueToBind, frame), $"Failure binding to '{code}'.");
            return frame.GetVariable(variable);
        }

        [Fact]
        public void SimpleBinding()
        {
            Assert.True(TryBind("a", new ErlangAtom("a")), "atom binding");
            Assert.True(TryBind("{a, 2}", new ErlangTuple(new ErlangAtom("a"), new ErlangNumber(2))), "tuple binding");
        }
        [Fact]
        public void ListBinding()
        {
            // [] /= [1]
            Assert.False(TryBind("[]", ErlangList.FromItems(new ErlangNumber(1))));

            // [1|Tail] = [1],
            // Tail = []
            Assert.Equal(
                ErlangList.FromItems(),
                BindAndFetchVariable(
                    "[1|Tail]",
                    ErlangList.FromItems(new ErlangNumber(1)),
                    "Tail"));

            // [1|Tail] = [1,2],
            // Tail = [2]
            Assert.Equal(
                ErlangList.FromItems(new ErlangNumber(2)),
                BindAndFetchVariable(
                    "[1|Tail]",
                    ErlangList.FromItems(new ErlangNumber(1), new ErlangNumber(2)),
                    "Tail"));

            // [1|Tail] = [1,2,3],
            // Tail = [2,3]
            Assert.Equal(
                ErlangList.FromItems(new ErlangNumber(2), new ErlangNumber(3)),
                BindAndFetchVariable(
                    "[1|Tail]",
                    ErlangList.FromItems(new ErlangNumber(1), new ErlangNumber(2), new ErlangNumber(3)),
                    "Tail"));
        }
    }
}
