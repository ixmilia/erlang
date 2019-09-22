using Xunit;

namespace IxMilia.Erlang.Test
{
    public class ModuleTests
    {
        private static ErlangValue EvaluateModuleFunction(string code, string functionName, params ErlangValue[] parameters)
        {
            var moduleCode = $@"
-module(test).
-compile(export_all).

{code}
";
            var process = new ErlangProcess();
            var module = ErlangModule.Compile(moduleCode);
            return module.Evaluate(process, functionName, parameters);
        }

        [Fact]
        public void TailCall()
        {
            var result = EvaluateModuleFunction(@"
sum() -> sum(0, 1000000).
sum(Acc, 0) -> Acc;
sum(Acc, N) -> sum(Acc + 1, N - 1).
", "sum");
            Assert.Equal(new ErlangNumber(1000000), result);
        }

        [Fact]
        public void FunctionsFromDefaultModules()
        {
            var result = EvaluateModuleFunction(@"
list_length() ->
    L1 = length([a,b,c]),           % implicit reference
    L2 = erlang:length([d,e,f]),    % explicit reference
    L1 + L2.
", "list_length");
            Assert.Equal(new ErlangNumber(6), result);
        }
    }
}
