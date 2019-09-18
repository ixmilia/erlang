namespace IxMilia.Erlang.Tokens
{
    public abstract class ErlangTrivia
    {
        public string Text { get; private set; }

        public int Offset { get; private set; }

        protected ErlangTrivia(string text, int offset)
        {
            Text = text;
            Offset = offset;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public class ErlangCommentTrivia : ErlangTrivia
    {
        public ErlangCommentTrivia(string text, int offset)
            : base(text, offset)
        {
        }
    }

    public class ErlangWhitespaceTrivia : ErlangTrivia
    {
        public ErlangWhitespaceTrivia(string text, int offset)
            : base(text, offset)
        {
        }
    }
}
