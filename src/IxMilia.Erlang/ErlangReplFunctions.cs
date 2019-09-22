using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IxMilia.Erlang
{
    internal class ErlangReplFunctions
    {
        private ErlangProcess process;
        private Dictionary<Tuple<string, int>, MethodInfo> functionMap = new Dictionary<Tuple<string, int>, MethodInfo>();

        public ErlangReplFunctions(ErlangProcess process)
        {
            this.process = process;
            foreach (var methodInfo in GetType().GetTypeInfo().DeclaredMethods)
            {
                var parameters = methodInfo.GetParameters();
                var methodAttrs = methodInfo.GetCustomAttributes(typeof(ErlangFunctionAttribute), false).OfType<ErlangFunctionAttribute>().ToArray();
                if (parameters.All(p => p.ParameterType == typeof(ErlangExpression))
                    && methodInfo.ReturnType == typeof(ErlangValue)
                    && methodAttrs.Length == 1)
                {
                    functionMap.Add(Tuple.Create(methodAttrs[0].Name, parameters.Length), methodInfo);
                }
            }
        }

        public ErlangValue TryEvaluate(ErlangExpression expression)
        {
            if (expression is ErlangFunctionInvocationExpression)
            {
                var func = (ErlangFunctionInvocationExpression)expression;
                if (func.Module == null)
                {
                    var key = Tuple.Create(func.Function, func.Parameters.Length);
                    if (functionMap.ContainsKey(key))
                    {
                        return (ErlangValue)functionMap[key].Invoke(this, func.Parameters);
                    }
                }
            }

            return null;
        }

        [ErlangFunction("f")]
        public ErlangValue Forget()
        {
            process.CallStack.CurrentFrame.UnsetAllVariables();
            return new ErlangAtom("ok");
        }

        [ErlangFunction("f")]
        public ErlangValue Forget(ErlangExpression expression)
        {
            if (expression is ErlangVariableExpression)
            {
                process.CallStack.CurrentFrame.UnsetVariable(((ErlangVariableExpression)expression).Variable);
                return new ErlangAtom("ok");
            }

            return new ErlangError("not a variable");
        }
    }
}
