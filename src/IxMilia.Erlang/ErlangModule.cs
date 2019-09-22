using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Erlang.Syntax;

namespace IxMilia.Erlang
{
    public abstract class ErlangModule
    {
        public string Name { get; protected set; }
        public ErlangList ModuleInfo { get; protected set; }
        public ErlangList AllFunctions { get; protected set; }
        protected abstract ErlangValue EvaluateLocal(ErlangProcess process, string functionName, ErlangValue[] parameters);
        public abstract bool FunctionExists(string functionName, int airity);

        public ErlangValue Evaluate(ErlangProcess process, string functionName, ErlangValue[] parameters)
        {
            ErlangValue error = null;
            if (functionName == "module_info")
            {
                if (parameters.Length == 0)
                {
                    return ModuleInfo;
                }
                else if (parameters.Length == 1)
                {
                    if (parameters[0].Kind == ErlangValueKind.Atom)
                    {
                        var text = ((ErlangAtom)parameters[0]).Name;
                        if (text == "functions")
                        {
                            return AllFunctions;
                        }
                        else
                        {
                            // find matching tuple
                            var list = ModuleInfo;
                            while (list != null && list.Value != null)
                            {
                                Debug.Assert(list.Value is ErlangTuple);
                                var tuple = (ErlangTuple)list.Value;
                                Debug.Assert(tuple.Airity == 2);
                                Debug.Assert(tuple.Values[0].Kind == ErlangValueKind.Atom);
                                if (((ErlangAtom)tuple.Values[0]).Name == text)
                                {
                                    Debug.Assert(tuple.Values[1].Kind == ErlangValueKind.List);
                                    return tuple.Values[1];
                                }

                                list = list.Tail as ErlangList;
                            }

                            return new ErlangError("no matching tuple item");
                        }
                    }
                    else
                    {
                        error = new ErlangError("no matching function");
                    }
                }
                else
                {
                    error = new ErlangError("no matching function");
                }
            }
            error = error ?? parameters.FirstOrDefault(p => p.Kind == ErlangValueKind.Error);
            return error ?? EvaluateLocal(process, functionName, parameters);
        }

        public static ErlangModule Compile(string code)
        {
            var syntax = ErlangSyntaxNode.Parse(code);
            return Compile(syntax);
        }

        public static ErlangModule Compile(ErlangModuleSyntax syntax)
        {
            var module = new ErlangCompiledModule();

            // get the module's name
            var atom = syntax.Attributes.OfType<ErlangAttributeSyntax>().Single(at => at.Name.Text == "module").Parameters.Single().Value as ErlangAtomSyntax;
            module.Name = atom.Atom.Text;

            // get visibility
            var publicFunctions = new HashSet<Tuple<string, int>>();
            var exportAll =
                (from at in syntax.Attributes.OfType<ErlangAttributeSyntax>()
                 where at.Name.Text == "compile" &&
                     at.Parameters.Count == 1
                 let param = at.Parameters[0]
                 where param.Value is ErlangAtomSyntax && ((ErlangAtomSyntax)param.Value).Atom.Text == "export_all"
                 select true).Any(b => b);
            if (!exportAll)
            {
                foreach (var functionReference in
                from at in syntax.Attributes.OfType<ErlangAttributeSyntax>()
                where at.Name.Text == "export" && at.Parameters.Count == 1
                let param = at.Parameters[0]
                where param.Value is ErlangListRegularSyntax
                let list = (ErlangListRegularSyntax)param.Value
                from item in list.Items
                where item.Item is ErlangFunctionReferenceSyntax
                select (ErlangFunctionReferenceSyntax)item.Item)
                {
                    publicFunctions.Add(Tuple.Create(functionReference.Function.Text, (int)functionReference.Airity.IntegerValue));
                }
            }

            // compile functions
            var functionList = new List<ErlangTuple>();
            var exportedFunctions = new List<ErlangTuple>();
            foreach (var group in syntax.FunctionGroups)
            {
                var key = Tuple.Create(group.Name, group.Airity);
                functionList.Add(new ErlangTuple(new ErlangAtom(group.Name), new ErlangNumber(group.Airity)));
                var function = (ErlangFunctionGroupExpression)ErlangExpression.Compile(group);
                function.Module = module;
                function.IsPublic = exportAll || publicFunctions.Contains(key);
                if (function.IsPublic) exportedFunctions.Add(new ErlangTuple(new ErlangAtom(group.Name), new ErlangNumber(group.Airity)));
                module.AddFunction(key.Item1, key.Item2, function);
            }

            functionList.Add(new ErlangTuple(new ErlangAtom("module_info"), new ErlangNumber(0)));
            functionList.Add(new ErlangTuple(new ErlangAtom("module_info"), new ErlangNumber(1)));
            exportedFunctions.Add(new ErlangTuple(new ErlangAtom("module_info"), new ErlangNumber(0)));
            exportedFunctions.Add(new ErlangTuple(new ErlangAtom("module_info"), new ErlangNumber(1)));

            module.AllFunctions = new ErlangList(functionList.ToArray());
            module.ModuleInfo = new ErlangList(
                new ErlangTuple(new ErlangAtom("exports"), new ErlangList(exportedFunctions.ToArray())),
                new ErlangTuple(new ErlangAtom("imports"), new ErlangList()), // always empty.  may be removed in future versions
                new ErlangTuple(new ErlangAtom("attributes"), new ErlangList()), // TODO: populate attributes
                new ErlangTuple(new ErlangAtom("compile"), new ErlangList()) // TODO: process compile options
                );

            return module;
        }
    }
}
