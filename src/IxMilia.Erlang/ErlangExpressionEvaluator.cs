using IxMilia.Erlang.Syntax;
using IxMilia.Erlang.Tokens;

namespace IxMilia.Erlang
{
    public class ErlangExpressionEvaluator
    {
        public ErlangProcess Process { get; }

        public ErlangExpressionEvaluator()
        {
            Process = new ErlangProcess();
        }

        public ErlangExpression Parse(string code)
        {
            var expression = ErlangSyntaxNode.ParseExpression(new TokenBuffer(ErlangToken.Tokenize(new TextBuffer(code))));
            var compiledExpr = ErlangExpression.Compile(expression);
            return compiledExpr;
        }

        public ErlangValue Evaluate(string code)
        {
            var expression = Parse(code);
            return Evaluate(expression);
        }

        public ErlangValue Evaluate(ErlangExpression expression)
        {
            return expression.Evaluate(Process);
        }
    }
}
