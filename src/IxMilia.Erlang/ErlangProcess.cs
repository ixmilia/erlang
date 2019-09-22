using System.Collections.Generic;

namespace IxMilia.Erlang
{
    public class ErlangProcess
    {
        public ErlangCallStack CallStack { get; }

        private Dictionary<string, ErlangModule> modules;

        private string[] defaultModules = new string[]
        {
            "erlang"
        };

        public ErlangProcess()
        {
            CallStack = new ErlangCallStack();
            modules = new Dictionary<string, ErlangModule>();
            AddModule(new ErlangNativeModule(new ErlangBifs(this)));
        }

        public void AddModule(ErlangModule module)
        {
            modules.Add(module.Name, module);
        }

        public ErlangValue Evaluate(string module, string function, params ErlangValue[] parameters)
        {
            if (module == null)
            {
                // try default modules
                for (int i = 0; i < defaultModules.Length; i++)
                {
                    if (modules.ContainsKey(defaultModules[i]))
                    {
                        if (modules[defaultModules[i]].FunctionExists(function, parameters.Length))
                        {
                            CallStack.Push(new ErlangStackFrame(defaultModules[i], function, parameters.Length));
                            var result = modules[defaultModules[i]].Evaluate(this, function, parameters);
                            CallStack.Pop();
                            return result;
                        }
                    }
                }

                return new ErlangError($"no matching function '{function}/{parameters.Length}'");
            }
            else if (modules.ContainsKey(module))
            {
                var mod = modules[module];
                CallStack.Push(new ErlangStackFrame(module, function, parameters.Length));
                var result = mod.Evaluate(this, function, parameters);
                CallStack.Pop();
                return result;
            }
            else
            {
                return new ErlangError("no matching module found: " + module);
            }
        }
    }
}
