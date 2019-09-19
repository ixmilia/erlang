using System.Collections.Generic;
using System.Linq;
using IxMilia.Erlang.Tokens;

namespace IxMilia.Erlang.Syntax
{
    public class TokenBuffer
    {
        public ErlangToken[] Tokens { get; private set; }

        public int Offset { get; private set; }

        public TokenBuffer(IEnumerable<ErlangToken> tokens)
        {
            Tokens = tokens.ToArray();
        }

        public void Advance()
        {
            Offset++;
        }

        public void Retreat()
        {
            Offset--;
        }

        public bool TokensRemain()
        {
            return Offset < Tokens.Length;
        }

        public ErlangToken Peek()
        {
            return TokensRemain() ? Tokens[Offset] : null;
        }

        public void SetOffset(int offset)
        {
            Offset = offset;
        }
    }
}
