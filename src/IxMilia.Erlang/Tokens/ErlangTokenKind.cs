namespace IxMilia.Erlang.Tokens
{
    public enum ErlangTokenKind
    {
        Atom = 1,
        Number = 2,
        Variable = 3,
        Operator = 4,
        Punctuation,
        Keyword,
        Error,
        String,
        Macro
    }
}
