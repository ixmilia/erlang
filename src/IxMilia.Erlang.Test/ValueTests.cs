using Xunit;

namespace IxMilia.Erlang.Test
{
    public class ValueTests
    {
        [Fact]
        public void NumericComparisons()
        {
            // less
            Assert.True(new ErlangNumber(0.8) < new ErlangNumber(0.9), "less double");
            Assert.True(new ErlangNumber(1) < new ErlangNumber(2), "less int");
            Assert.True(new ErlangNumber(0.9) < new ErlangNumber(1), "less double int");
            Assert.True(new ErlangNumber(1) < new ErlangNumber(1.1), "less int double");

            // less equal
            Assert.True(new ErlangNumber(0.8) <= new ErlangNumber(0.8), "less equal double 1");
            Assert.True(new ErlangNumber(0.8) <= new ErlangNumber(0.9), "less equal double 2");
            Assert.True(new ErlangNumber(1) <= new ErlangNumber(1), "less equal int 1");
            Assert.True(new ErlangNumber(1) <= new ErlangNumber(2), "less equal int 2");
            Assert.True(new ErlangNumber(1.0) <= new ErlangNumber(1), "less equal double int 1");
            Assert.True(new ErlangNumber(0.9) <= new ErlangNumber(1), "less equal double int 2");
            Assert.True(new ErlangNumber(1) <= new ErlangNumber(1.0), "less equal int double 1");
            Assert.True(new ErlangNumber(1) <= new ErlangNumber(1.1), "less equal int double 2");

            // greater
            Assert.True(new ErlangNumber(0.9) > new ErlangNumber(0.8), "greater double");
            Assert.True(new ErlangNumber(2) > new ErlangNumber(1), "greater int");
            Assert.True(new ErlangNumber(1.1) > new ErlangNumber(1), "greater double int");
            Assert.True(new ErlangNumber(1) > new ErlangNumber(0.9), "greater int double");

            // greater equal
            Assert.True(new ErlangNumber(0.8) >= new ErlangNumber(0.8), "greater equal double 1");
            Assert.True(new ErlangNumber(0.9) >= new ErlangNumber(0.8), "greater equal double 2");
            Assert.True(new ErlangNumber(1) >= new ErlangNumber(1), "greater equal int 1");
            Assert.True(new ErlangNumber(2) >= new ErlangNumber(1), "greater equal int 2");
            Assert.True(new ErlangNumber(1.0) >= new ErlangNumber(1), "greater equal double int 1");
            Assert.True(new ErlangNumber(1.1) >= new ErlangNumber(1), "greater equal double int 2");
            Assert.True(new ErlangNumber(1) >= new ErlangNumber(1.0), "greater equal int double 1");
            Assert.True(new ErlangNumber(1) >= new ErlangNumber(0.9), "greater equal int double 2");

            // equal
            Assert.True(new ErlangNumber(1.0) == new ErlangNumber(1.0), "equal double");
            Assert.True(new ErlangNumber(1) == new ErlangNumber(1), "equal int");
            Assert.True(new ErlangNumber(1.0) == new ErlangNumber(1), "equal double int");
            Assert.True(new ErlangNumber(1) == new ErlangNumber(1.0), "equal int double");

            // not equal
            Assert.True(new ErlangNumber(0.8) != new ErlangNumber(0.9), "not equal double");
            Assert.True(new ErlangNumber(1) != new ErlangNumber(2), "not equal int");
            Assert.True(new ErlangNumber(1.1) != new ErlangNumber(1), "not equal double int");
            Assert.True(new ErlangNumber(1) != new ErlangNumber(1.1), "not equal int double");

            // exactly equal
            Assert.Equal(ErlangAtom.True, ErlangValue.EqualsColonEquals(new ErlangNumber(1.0), new ErlangNumber(1.0))); // double
            Assert.Equal(ErlangAtom.True, ErlangValue.EqualsColonEquals(new ErlangNumber(1), new ErlangNumber(1))); // int
            Assert.Equal(ErlangAtom.False, ErlangValue.EqualsColonEquals(new ErlangNumber(1.0), new ErlangNumber(1))); // double int
            Assert.Equal(ErlangAtom.False, ErlangValue.EqualsColonEquals(new ErlangNumber(1), new ErlangNumber(1.0))); // int double

            // exactly not equal
            Assert.Equal(ErlangAtom.False, ErlangValue.EqualsSlashEquals(new ErlangNumber(1.0), new ErlangNumber(1.0))); // double
            Assert.Equal(ErlangAtom.False, ErlangValue.EqualsSlashEquals(new ErlangNumber(1), new ErlangNumber(1))); // int
            Assert.Equal(ErlangAtom.True, ErlangValue.EqualsSlashEquals(new ErlangNumber(1.0), new ErlangNumber(1))); // double int
            Assert.Equal(ErlangAtom.True, ErlangValue.EqualsSlashEquals(new ErlangNumber(1), new ErlangNumber(1.0))); // int double
        }

        [Fact]
        public void UnaryOperators()
        {
            // not
            Assert.Equal(ErlangAtom.True, ErlangValue.Not(ErlangAtom.False));
            Assert.Equal(ErlangAtom.True, ErlangValue.Not(ErlangValue.Not(ErlangAtom.True)));

            // bnot
            Assert.Equal(new ErlangNumber(-17), ErlangNumber.BNot(new ErlangNumber(16)));
            Assert.Equal(new ErlangNumber(16), ErlangNumber.BNot(new ErlangNumber(-17)));
            Assert.Equal(new ErlangNumber(-1), ErlangNumber.BNot(new ErlangNumber(0)));
            Assert.Equal(new ErlangNumber(0), ErlangNumber.BNot(new ErlangNumber(-1)));
        }
    }
}
