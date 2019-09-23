using System.IO;
using Xunit;

namespace IxMilia.Erlang.Test
{
    public class FactPathTestAttribute : FactAttribute
    {
        public FactPathTestAttribute(string path)
        {
            if (!Directory.Exists(path))
            {
                Skip = $"Path does not exist: '{path}'";
            }
        }
    }
}
