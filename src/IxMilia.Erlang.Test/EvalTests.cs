using Xunit;

namespace IxMilia.Erlang.Test
{
    public class EvalTests
    {
        private static ErlangValue Eval(string code)
        {
            var ee = new ErlangExpressionEvaluator();
            return ee.Evaluate(code);
        }

        [Fact]
        public void MathOperations()
        {
            Assert.Equal(new ErlangNumber(3), Eval("1 + 2"));

            Assert.Equal(ErlangAtom.True, Eval("0.8 < 0.9"));
            Assert.Equal(ErlangAtom.True, Eval("0.8 =< 0.8"));
            Assert.Equal(ErlangAtom.True, Eval("0.8 =< 0.9"));

            Assert.Equal(ErlangAtom.True, Eval("0.9 > 0.8"));
            Assert.Equal(ErlangAtom.True, Eval("0.8 >= 0.8"));
            Assert.Equal(ErlangAtom.True, Eval("0.9 >= 0.8"));

            Assert.Equal(ErlangAtom.True, Eval("1.0 == 1.0"));
            Assert.Equal(ErlangAtom.True, Eval("1 == 1"));
            Assert.Equal(ErlangAtom.True, Eval("1.0 == 1"));
            Assert.Equal(ErlangAtom.True, Eval("1 == 1.0"));

            Assert.Equal(ErlangAtom.True, Eval("1.0 =:= 1.0"));
            Assert.Equal(ErlangAtom.True, Eval("1 =:= 1"));
            Assert.Equal(ErlangAtom.False, Eval("1.0 =:= 1"));
            Assert.Equal(ErlangAtom.False, Eval("1 =:= 1.0"));

            Assert.Equal(ErlangAtom.True, Eval("not false"));
            Assert.Equal(ErlangAtom.True, Eval("not (not true)"));
            Assert.Equal(new ErlangNumber(-17), Eval("bnot 16"));
            Assert.Equal(new ErlangNumber(16), Eval("bnot (-17)"));
            Assert.Equal(new ErlangNumber(-1), Eval("-1"));
        }

        [Fact]
        public void ListOperations()
        {
            Assert.Equal(Eval("[1, 2, 3]"), Eval("[1, 2, 3]"));
            Assert.Equal(ErlangList.FromItems(new ErlangNumber(1), new ErlangNumber(2)), Eval("[1] ++ [2]"));
            Assert.Equal(ErlangList.FromItems(new ErlangNumber(1), new ErlangNumber(2), new ErlangNumber(3)), Eval("[1] ++ [2] ++ [3]"));
            Assert.Equal(Eval("[1, 2, 3]"), Eval("[1] ++ [2] ++ [3]"));
            Assert.Equal(Eval("[1, 2 | 3]"), Eval("[1] ++ [2] ++ 3"));
        }
    }
}
