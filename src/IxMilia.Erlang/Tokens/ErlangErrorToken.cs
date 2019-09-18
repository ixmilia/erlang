namespace IxMilia.Erlang.Tokens
{
    public class ErlangErrorToken : ErlangToken
    {
        public string Message { get; private set; }

        public ErlangErrorToken(char op, string message)
        {
            Text = op.ToString();
            Message = message;
            Kind = ErlangTokenKind.Error;
        }
    }
}
