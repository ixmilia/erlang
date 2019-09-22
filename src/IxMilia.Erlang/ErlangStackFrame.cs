using System.Collections.Generic;

namespace IxMilia.Erlang
{
    public class ErlangStackFrame
    {
        public string Module { get; private set; }

        public string Function { get; private set; }

        public int Airity { get; private set; }

        private Stack<Dictionary<string, ErlangValue>> scopedVariables;

        internal int ScopeDepth
        {
            get { return scopedVariables.Count; }
        }

        public ErlangStackFrame(string module, string function, int airity)
        {
            scopedVariables = new Stack<Dictionary<string, ErlangValue>>();
            scopedVariables.Push(new Dictionary<string, ErlangValue>());
            Module = module;
            Function = function;
            Airity = airity;
        }

        public void SetVariable(string name, ErlangValue value)
        {
            scopedVariables.Peek().Add(name, value);
        }

        public ErlangValue GetVariable(string name)
        {
            foreach (var scope in scopedVariables)
            {
                if (scope.ContainsKey(name))
                    return scope[name];
            }

            return null;
        }

        internal void UnsetVariable(string name)
        {
            scopedVariables.Peek().Remove(name);
        }

        internal void UnsetAllVariables()
        {
            scopedVariables.Pop();
            scopedVariables.Push(new Dictionary<string, ErlangValue>());
        }

        public void IncreaseScopeLevel()
        {
            scopedVariables.Push(new Dictionary<string, ErlangValue>());
        }

        public void DecreaseScopeLevel()
        {
            scopedVariables.Pop();
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}/{2}", Module, Function, Airity);
        }

        internal static bool IsTailCallCandidate(ErlangStackFrame a, ErlangStackFrame b)
        {
            return a.Module == b.Module && a.Function == b.Function && a.Airity == b.Airity;
        }
    }
}
