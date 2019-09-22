using System;
using System.Collections.Generic;

namespace IxMilia.Erlang
{
    public class ErlangCompiledModule : ErlangModule
    {
        private Dictionary<Tuple<string, int>, ErlangFunctionGroupExpression> functions = new Dictionary<Tuple<string, int>, ErlangFunctionGroupExpression>();

        internal void AddFunction(string functionName, int airity, ErlangFunctionGroupExpression functionGroup)
        {
            functions.Add(Tuple.Create(functionName, airity), functionGroup);
        }

        internal ErlangFunctionGroupExpression GetFunction(string functionName, int airity)
        {
            ErlangFunctionGroupExpression function;
            if (functions.TryGetValue(Tuple.Create(functionName, airity), out function))
            {
                return function;
            }

            return null;
        }

        public override bool FunctionExists(string function, int airity)
        {
            var key = Tuple.Create(function, airity);
            return functions.ContainsKey(key) && functions[key].IsPublic;
        }

        public bool FunctionExistsInternal(string function, int airity)
        {
            return functions.ContainsKey(Tuple.Create(function, airity));
        }

        protected override ErlangValue EvaluateLocal(ErlangProcess process, string functionName, ErlangValue[] parameters)
        {
            var function = GetFunction(functionName, parameters.Length);
            if (function == null)
            {
                return new ErlangError("no matching function found");
            }

            if (!function.IsPublic)
            {
                return new ErlangError("no matching function found");
            }

            var overload = function.GetFunctionOverload(process, parameters);
            if (overload != null)
            {
                return overload.Evaluate(process);
            }
            else
            {
                return new ErlangError("no matching function found");
            }
        }

        internal ErlangValue EvaluateInternal(ErlangProcess process, string functionName, ErlangValue[] parameters)
        {
            ErlangValue result;
            var function = GetFunction(functionName, parameters.Length);
            process.CallStack.Push(new ErlangStackFrame(Name, functionName, parameters.Length));
            var overload = function.GetFunctionOverload(process, parameters);
            if (overload != null)
            {
                result = overload.Evaluate(process);
            }
            else
            {
                result = new ErlangError("no matching function found");
            }

            process.CallStack.Pop();
            return result;
        }
    }
}
