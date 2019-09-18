using Xunit;

namespace IxMilia.Erlang.Test
{
    public class TestBase
    {
        public static void ArrayEquals<T>(T[] expected, T[] actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            Assert.False(expected == null || actual == null);
            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }
    }
}
