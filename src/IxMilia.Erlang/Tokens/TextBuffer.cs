namespace IxMilia.Erlang
{
    public class TextBuffer
    {
        public string Text { get; private set; }

        public int Offset { get; private set; }

        public TextBuffer(string text)
        {
            Text = text;
            Offset = 0;
        }

        public void Advance()
        {
            Offset++;
        }

        public void Retreat()
        {
            Offset--;
        }

        public bool TextRemains()
        {
            return Offset < Text.Length;
        }

        public char Peek()
        {
            return (Offset >= Text.Length) ? '\0' : Text[Offset];
        }
    }
}
