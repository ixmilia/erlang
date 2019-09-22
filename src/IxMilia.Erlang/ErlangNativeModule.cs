using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IxMilia.Erlang
{
    public class ErlangModuleAttribute : Attribute
    {
        public string Name { get; private set; }

        public ErlangModuleAttribute(string name)
        {
            Name = name;
        }
    }

    public class ErlangFunctionAttribute : Attribute
    {
        public string Name { get; private set; }

        public ErlangFunctionAttribute(string name)
        {
            Name = name;
        }
    }

    public class ErlangNativeModule : ErlangModule
    {
        public object Target { get; private set; }
        private Dictionary<Tuple<string, int>, MethodInfo> functionMap;

        public ErlangNativeModule(object target)
        {
            // get name
            var methodAttribute = target.GetType().GetTypeInfo().GetCustomAttributes(typeof(ErlangModuleAttribute), false).OfType<ErlangModuleAttribute>().Single();
            Name = methodAttribute.Name;

            functionMap = new Dictionary<Tuple<string, int>, MethodInfo>();
            Target = target;

            foreach (var methodInfo in Target.GetType().GetTypeInfo().DeclaredMethods)
            {
                var parameters = methodInfo.GetParameters();
                var methodAttrs = methodInfo.GetCustomAttributes(typeof(ErlangFunctionAttribute), false).OfType<ErlangFunctionAttribute>().ToArray();
                if (parameters.All(p => p.ParameterType == typeof(ErlangValue))
                    && methodInfo.ReturnType == typeof(ErlangValue)
                    && methodAttrs.Length == 1)
                {
                    functionMap.Add(Tuple.Create(methodAttrs[0].Name, parameters.Length), methodInfo);
                }
            }

            AllFunctions = new ErlangList(functionMap.Keys
                .Select(f => new ErlangTuple(new ErlangAtom(f.Item1), new ErlangNumber(f.Item2)))
                .Concat(new ErlangTuple[]
                {
                    new ErlangTuple(new ErlangAtom("module_info"), new ErlangNumber(0)),
                    new ErlangTuple(new ErlangAtom("module_info"), new ErlangNumber(1))
                })
                .ToArray());

            ModuleInfo = new ErlangList(
                new ErlangTuple(
                    new ErlangAtom("exports"),
                    AllFunctions
                ),
                new ErlangTuple(
                    new ErlangAtom("imports"), new ErlangList() // an empty list.  may go away in the future
                ),
                new ErlangTuple(
                    new ErlangAtom("attributes"), new ErlangList() // TODO: populate attributes
                ),
                new ErlangTuple(
                    new ErlangAtom("compile"), new ErlangList() // TODO: populate compile options
                )
            );
        }

        protected override ErlangValue EvaluateLocal(ErlangProcess process, string function, ErlangValue[] parameters)
        {
            MethodInfo methodInfo;
            if (functionMap.TryGetValue(Tuple.Create(function, parameters.Length), out methodInfo))
            {
                return (ErlangValue)methodInfo.Invoke(Target, parameters);
            }

            return new ErlangError("no matching function found");
        }

        public override bool FunctionExists(string functionName, int airity)
        {
            return functionMap.ContainsKey(Tuple.Create(functionName, airity));
        }
    }
}
