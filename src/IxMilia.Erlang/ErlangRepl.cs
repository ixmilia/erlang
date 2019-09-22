using System.IO;

namespace IxMilia.Erlang
{
    public class ErlangRepl
    {
        public TextWriter Out { get; }
        public TextWriter Error { get; }
        public TextReader Input { get; }

        public ErlangRepl(TextWriter @out, TextWriter error, TextReader input)
        {
            Out = @out;
            Error = error;
            Input = input;
        }

        public void Run()
        {
            var ee = new ErlangExpressionEvaluator();
            var replFunctions = new ErlangReplFunctions(ee.Process);
            while (true)
            {
                Out.Write("> ");
                var input = Input.ReadLine();
                if (input == "q().")
                {
                    break;
                }

                // TODO: should be ParseExpressionBlock, or similar, to allow comma and not evaluate until dot
                var expression = ee.Parse(input);
                var result = replFunctions.TryEvaluate(expression) ?? ee.Evaluate(expression);
                Out.WriteLine(result);
            }
        }
    }
}
