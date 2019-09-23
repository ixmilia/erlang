using System;
using System.IO;
using IxMilia.Erlang.Syntax;
using IxMilia.Erlang.Tokens;

namespace IxMilia.Erlang.Test
{
    public class IntegrationTests
    {
        private const string OTPSources = @"C:\Program Files\erl10.5\lib\stdlib-3.10\src";

        [FactPathTest(OTPSources)]
        public void ParseOtp()
        {
            foreach (var file in Directory.EnumerateFiles(OTPSources, "*.erl"))
            {
                Console.WriteLine($"Parsing {file}.");
                var code = File.ReadAllText(file);
                var expression = ErlangSyntaxNode.ParseExpression(new TokenBuffer(ErlangToken.Tokenize(new TextBuffer(code))));
                var compiledExpr = ErlangExpression.Compile(expression);
            }
        }
    }
}
