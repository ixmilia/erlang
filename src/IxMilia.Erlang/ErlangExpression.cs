using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Erlang.Syntax;
using IxMilia.Erlang.Tokens;

namespace IxMilia.Erlang
{
    public abstract class ErlangExpression
    {
        public ErlangExpression Parent { get; private set; }

        internal bool IsLastChild { get; private set; }

        public virtual ErlangValue Evaluate(ErlangProcess process)
        {
            throw new NotSupportedException();
        }

        internal void SetParent(ErlangExpression parent, bool isLastChild = false)
        {
            Parent = parent;
            IsLastChild = isLastChild;
        }

        public ErlangCompiledModule GetModule()
        {
            if (Parent == null)
            {
                return null;
            }
            else
            {
                var parent = Parent;
                while (parent.Parent != null)
                    parent = parent.Parent;
                if (parent is ErlangFunctionGroupExpression)
                    return ((ErlangFunctionGroupExpression)parent).Module;
                else
                    return null;
            }
        }

        public static ErlangExpression Compile(ErlangSyntaxNode syntax)
        {
            Func<IEnumerable<ErlangSyntaxNode>, ErlangExpression[]> CompileChildren = (ErlangSyntaxNodes) => ErlangSyntaxNodes.Select(n => Compile(n)).ToArray();
            var type = syntax.GetType();
            if (type == typeof(ErlangAtomSyntax))
                return new ErlangAtomExpression(((ErlangAtomSyntax)syntax).Atom.Text);
            else if (type == typeof(ErlangVariableSyntax))
                return new ErlangVariableExpression(((ErlangVariableSyntax)syntax).Variable.Text);
            else if (type == typeof(ErlangConstantSyntax))
            {
                var token = ((ErlangConstantSyntax)syntax).Token;
                return new ErlangConstantExpression(token.IsIntegral ? new ErlangNumber(token.IntegerValue) : new ErlangNumber(token.DoubleValue));
            }
            else if (type == typeof(ErlangTupleSyntax))
                return new ErlangTupleExpression(CompileChildren(((ErlangTupleSyntax)syntax).Items.Select(i => i.Item)));
            else if (type == typeof(ErlangUnaryOperationSyntax))
            {
                var unary = (ErlangUnaryOperationSyntax)syntax;
                return new ErlangUnaryExpression(Compile(unary.Expression), unary.Operator.OperatorKind);
            }
            else if (type == typeof(ErlangBinaryOperationSyntax))
            {
                var binop = (ErlangBinaryOperationSyntax)syntax;
                return new ErlangBinaryExpression(Compile(binop.Left), Compile(binop.Right), binop.Operator.OperatorKind);
            }
            else if (type == typeof(ErlangParentheticalExpressionSyntax))
            {
                return Compile(((ErlangParentheticalExpressionSyntax)syntax).Expression);
            }
            else if (type == typeof(ErlangListRegularSyntax))
            {
                var list = (ErlangListRegularSyntax)syntax;
                return new ErlangListExpression(list.Items.Select(i => Compile(i.Item)).ToArray(), list.Tail == null ? null : Compile(list.Tail));
            }
            else if (type == typeof(ErlangListStringSyntax))
            {
                var list = (ErlangListStringSyntax)syntax;
                return new ErlangListExpression(list.String.Value.ToCharArray().Select(c => new ErlangConstantExpression(c)).ToArray(), null);
            }
            else if (type == typeof(ErlangFunctionInvocationSyntax))
            {
                var func = (ErlangFunctionInvocationSyntax)syntax;
                var module = func.ModuleReference == null ? null : func.ModuleReference.Module.Text;
                return new ErlangFunctionInvocationExpression(module, func.Function.Text, func.Parameters.Select(p => Compile(p.Value)).ToArray());
            }
            else if (type == typeof(ErlangCaseSyntax))
            {
                var @case = (ErlangCaseSyntax)syntax;
                var expression = Compile(@case.Expression);
                return new ErlangCaseExpression(expression, @case.Branches.Select(b => Compile(b)).Cast<ErlangCaseBranchExpression>().ToArray());
            }
            else if (type == typeof(ErlangCaseBranchSyntax))
            {
                var branch = (ErlangCaseBranchSyntax)syntax;
                var pattern = Compile(branch.Pattern);
                return new ErlangCaseBranchExpression(pattern,
                    branch.Guard == null ? null : CompileGuard(branch.Guard),
                    branch.Expressions.Select(s => Compile(s.Expression)).ToArray());
            }
            else if (type == typeof(ErlangFunctionDefinitionSyntax))
            {
                var func = (ErlangFunctionDefinitionSyntax)syntax;
                return new ErlangFunctionOverloadExpression(
                    func.Parameters.Select(p => Compile(p.Value)).ToArray(),
                    func.Guard == null ? null : CompileGuard(func.Guard),
                    func.Expressions.Select(s => Compile(s.Expression)).ToArray());
            }
            else if (type == typeof(ErlangFunctionGroupSyntax))
            {
                var func = (ErlangFunctionGroupSyntax)syntax;
                return new ErlangFunctionGroupExpression(func.Name, func.Airity, func.Definitions.Select(def => Compile(def)).Cast<ErlangFunctionOverloadExpression>().ToArray());
            }
            else if (type == typeof(ErlangModuleSyntax))
                throw new ArgumentException("Cannot compile module syntax.  Use ErlangCompiledModule.Compile() instead.");
            throw new ArgumentException("Cannot compile syntax; unsupported type.");
        }

        private static ErlangGuardExpression CompileGuard(ErlangGuardSyntax guard)
        {
            var clauses = new ErlangGuardClauseExpression[guard.Clauses.Count];
            for (int i = 0; i < guard.Clauses.Count; i++)
            {
                clauses[i] = CompileGuardClause(guard.Clauses[i]);
            }

            return new ErlangGuardExpression(clauses);
        }

        private static ErlangGuardClauseExpression CompileGuardClause(ErlangSeparatedSyntaxNodeSyntax<ErlangExpressionSyntax> clause)
        {
            // TODO: function invocations can only be imported functions like is_list/1, etc.
            return new ErlangGuardClauseExpression(Compile(clause.Value), clause.Separator != null && clause.Separator.Text == ",");
        }
    }

    public abstract class ErlangExpressionBlockExpression : ErlangExpression
    {
        public abstract ErlangExpression[] GetChildren();

        private static bool UseTailCalls = true;

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            ErlangValue last = null;
            ErlangExpressionBlockExpression def = this;
            ErlangCompiledModule module = GetModule();
            var children = def.GetChildren();
            bool doTailCall = false;
            do
            {
                doTailCall = false; // ensure we're reset from the last loop
                // evaluate each expression
                Debug.Assert(children.Length > 0);
                for (int i = 0; !doTailCall && i < children.Length; i++)
                {
                    var expression = children[i];
                    string moduleName = null;
                    ErlangFunctionInvocationExpression function = null;
                    ErlangStackFrame tailCallCandidate = null;
                    if (UseTailCalls && i == children.Length - 1 && expression is ErlangFunctionInvocationExpression && expression.IsLastChild)
                    {
                        // if last expression and it's a function invocation
                        function = (ErlangFunctionInvocationExpression)expression;
                        moduleName = function.Module ?? module.Name;
                        tailCallCandidate = process.CallStack.GetTailCallCandidate(
                            moduleName,
                            function.Function,
                            function.Parameters.Length);
                        doTailCall = tailCallCandidate != null;
                    }

                    if (doTailCall)
                    {
                        // evaluate parameters
                        var evaledParams = new ErlangValue[function.Parameters.Length];
                        for (int j = 0; j < function.Parameters.Length; j++)
                        {
                            var value = function.Parameters[j].Evaluate(process);
                            if (value.Kind == ErlangValueKind.Error)
                                return value;
                            evaledParams[j] = value;
                        }

                        // prepare new frame
                        var newFrame = new ErlangStackFrame(tailCallCandidate.Module,
                            tailCallCandidate.Function,
                            tailCallCandidate.Airity);
                        process.CallStack.RewindForTailCall(newFrame);

                        // find the new definition
                        var group = module.GetFunction(function.Function, evaledParams.Length);
                        def = group.GetFunctionOverload(process, evaledParams);
                        if (def == null)
                            return new ErlangAtom("no_such_tailcall_function");
                        children = def.GetChildren();
                    }
                    else
                    {
                        // not a tailcall, just invoke normally
                        last = children[i].Evaluate(process);
                        if (last.Kind == ErlangValueKind.Error)
                            return last; // decrease scope?
                    }
                }
            } while (doTailCall);

            return last;
        }

        public static bool TryBindParameters(ErlangProcess process, ErlangExpression[] parameters, ErlangValue[] values)
        {
            if (parameters.Length != values.Length)
                return false;
            var frame = process.CallStack.CurrentFrame;
            for (int i = 0; i < values.Length; i++)
            {
                if (!ErlangBinder.TryBindParameter(parameters[i], values[i], frame, true))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class ErlangFunctionGroupExpression : ErlangExpression
    {
        public string Name { get; private set; }
        public int Airity { get; private set; }
        public ErlangCompiledModule Module { get; internal set; }
        public ErlangFunctionOverloadExpression[] Overloads { get; private set; }
        public bool IsPublic { get; internal set; }

        public ErlangFunctionGroupExpression(string name, int airity, ErlangFunctionOverloadExpression[] overloads)
        {
            Name = name;
            Airity = airity;
            Overloads = overloads;
            foreach (var o in Overloads)
                o.SetParent(this);
        }

        public ErlangFunctionOverloadExpression GetFunctionOverload(ErlangProcess process, ErlangValue[] parameters)
        {
            Debug.Assert(parameters.Length == Airity);
            for (int i = 0; i < Overloads.Length; i++)
            {
                if (Overloads[i].TryBindParameters(process, parameters))
                {
                    var guard = Overloads[i].Guard;
                    if (guard == null || ErlangAtom.IsTrue(guard.Evaluate(process)))
                        return Overloads[i];
                }
            }

            return null;
        }
    }

    public class ErlangFunctionOverloadExpression : ErlangExpressionBlockExpression
    {
        public ErlangExpression[] Parameters { get; private set; }
        public ErlangGuardExpression Guard { get; private set; }
        public ErlangExpression[] BodyExpressions { get; private set; }
        public bool IsExported { get; private set; }

        public ErlangFunctionOverloadExpression(ErlangExpression[] parameters, ErlangGuardExpression guard, ErlangExpression[] body)
        {
            // TODO: verify parameters are only, atom, variable, constant, list, tuple, or binaryexp of these
            Parameters = parameters;
            Guard = guard;
            BodyExpressions = body;
            for (int i = 0; i < Parameters.Length; i++)
                Parameters[i].SetParent(this);
            if (Guard != null)
                Guard.SetParent(this);
            for (int i = 0; i < BodyExpressions.Length; i++)
                BodyExpressions[i].SetParent(this, i == BodyExpressions.Length - 1);
        }

        public override ErlangExpression[] GetChildren()
        {
            return BodyExpressions;
        }

        public bool TryBindParameters(ErlangProcess process, ErlangValue[] values)
        {
            return TryBindParameters(process, Parameters, values);
        }
    }

    public class ErlangGuardExpression : ErlangExpression
    {
        public ErlangGuardClauseExpression[] Clauses { get; private set; }

        public ErlangGuardExpression(ErlangGuardClauseExpression[] clauses)
        {
            Clauses = clauses;
            for (int i = 0; i < Clauses.Length; i++)
                Clauses[i].SetParent(this);
        }

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            Debug.Assert(Clauses.Length > 0);
            var isAnd = Clauses[0].IsAnd;
            var result = ErlangAtom.IsTrue(Clauses[0].Evaluate(process));
            for (int i = 1; i < Clauses.Length; i++)
            {
                var next = Clauses[i].Evaluate(process);
                if (isAnd)
                    result = result && ErlangAtom.IsTrue(Clauses[i].Evaluate(process));
                else
                    result = result || ErlangAtom.IsTrue(Clauses[i].Evaluate(process));

                isAnd = Clauses[i].IsAnd;
            }

            return result ? ErlangAtom.True : ErlangAtom.False;
        }
    }

    public class ErlangGuardClauseExpression : ErlangExpression
    {
        public ErlangExpression Expression { get; private set; }
        public bool IsAnd { get; private set; }

        public ErlangGuardClauseExpression(ErlangExpression expression, bool isAnd)
        {
            Expression = expression;
            IsAnd = isAnd;
            Expression.SetParent(this);
        }

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            return Expression.Evaluate(process);
        }
    }

    public abstract class ErlangSimpleExpression : ErlangExpression
    {
        public abstract ErlangValue Evaluate();

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            return Evaluate();
        }
    }

    public class ErlangAtomExpression : ErlangSimpleExpression
    {
        public string Atom { get; private set; }

        public ErlangAtomExpression(string atom)
        {
            Atom = atom;
        }

        public override ErlangValue Evaluate()
        {
            return new ErlangAtom(Atom);
        }
    }

    public class ErlangVariableExpression : ErlangExpression
    {
        public string Variable { get; private set; }

        public ErlangVariableExpression(string variable)
        {
            Variable = variable;
        }

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            var value = process.CallStack.CurrentFrame.GetVariable(Variable);
            return value ?? new ErlangError(string.Format("No such variable {0}", Variable));
        }
    }

    public class ErlangConstantExpression : ErlangSimpleExpression
    {
        public ErlangNumber Value { get; private set; }

        public ErlangConstantExpression(char c)
        {
            Value = new ErlangNumber(c);
        }

        public ErlangConstantExpression(ErlangNumber value)
        {
            Value = value;
        }

        public override ErlangValue Evaluate()
        {
            return Value;
        }
    }

    public class ErlangUnaryExpression : ErlangExpression
    {
        public ErlangExpression Expression { get; private set; }
        public ErlangOperatorKind Operator { get; private set; }

        public ErlangUnaryExpression(ErlangExpression expression, ErlangOperatorKind op)
        {
            Expression = expression;
            Operator = op;
        }

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            var result = Expression.Evaluate(process);
            if (result.Kind == ErlangValueKind.Error)
                return result;
            switch (Operator)
            {
                case ErlangOperatorKind.Plus:
                    return ErlangValue.Multiply(result, ErlangNumber.One);
                case ErlangOperatorKind.Minus:
                    return ErlangValue.Multiply(result, ErlangNumber.NegativeOne);
                case ErlangOperatorKind.Not:
                    return ErlangValue.Not(result);
                case ErlangOperatorKind.BNot:
                    if (result.Kind == ErlangValueKind.Number)
                    {
                        return ErlangNumber.BNot((ErlangNumber)result);
                    }
                    else
                    {
                        return new ErlangError("not integral");
                    }
                default:
                    return new ErlangError(string.Format("Invalid unary operator: {0}", Operator));
            }
        }
    }

    public class ErlangBinaryExpression : ErlangExpression
    {
        public ErlangExpression Left { get; private set; }
        public ErlangExpression Right { get; private set; }
        public ErlangOperatorKind Operator { get; private set; }

        public ErlangBinaryExpression(ErlangExpression left, ErlangExpression right, ErlangOperatorKind op)
        {
            Left = left;
            Right = right;
            Operator = op;
            Left.SetParent(this);
            Right.SetParent(this);
        }

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            // variable assignment or pattern match
            if (Operator == ErlangOperatorKind.Equals)
            {
                // first evaluate the right side
                var right = Right.Evaluate(process);
                if (right.Kind == ErlangValueKind.Error)
                    return right;

                // now match to the left
                if (ErlangBinder.TryBindParameter(Left, right, process.CallStack.CurrentFrame))
                {
                    return right;
                }
                else
                {
                    return new ErlangError("bad match");
                }
            }

            var left = Left.Evaluate(process);
            if (left.Kind == ErlangValueKind.Error)
                return left;
            if (IsShortCircuitOperator(Operator))
            {
                switch (Operator)
                {
                    case ErlangOperatorKind.AndAlso:
                        if (ErlangAtom.IsTrue(left))
                            return Right.Evaluate(process);
                        else
                            return ErlangAtom.False;
                    case ErlangOperatorKind.OrElse:
                        if (ErlangAtom.IsTrue(left))
                            return left;
                        else
                            return Right.Evaluate(process);
                    default:
                        throw new ArgumentException("invalid operator");
                }
            }
            else
            {
                var right = Right.Evaluate(process);
                if (right.Kind == ErlangValueKind.Error)
                    return right;
                switch (Operator)
                {
                    case ErlangOperatorKind.Plus:
                        return ErlangValue.Add(left, right);
                    case ErlangOperatorKind.Minus:
                        return ErlangValue.Subtract(left, right);
                    case ErlangOperatorKind.Asterisk:
                        return ErlangValue.Multiply(left, right);
                    case ErlangOperatorKind.Slash:
                        return ErlangValue.Divide(left, right);
                    case ErlangOperatorKind.And:
                        return ErlangValue.And(left, right);
                    case ErlangOperatorKind.Or:
                        return ErlangValue.Or(left, right);
                    case ErlangOperatorKind.Less:
                        return ErlangValue.Less(left, right);
                    case ErlangOperatorKind.EqualsLess:
                        return ErlangValue.LessEquals(left, right);
                    case ErlangOperatorKind.Greater:
                        return ErlangValue.Greater(left, right);
                    case ErlangOperatorKind.GreaterEquals:
                        return ErlangValue.GreaterEquals(left, right);
                    case ErlangOperatorKind.EqualsEquals:
                        return ErlangValue.EqualsEquals(left, right);
                    case ErlangOperatorKind.SlashEquals:
                        return ErlangValue.SlashEquals(left, right);
                    case ErlangOperatorKind.EqualsColonEquals:
                        return ErlangValue.EqualsColonEquals(left, right);
                    case ErlangOperatorKind.EqualsSlashEquals:
                        return ErlangValue.EqualsSlashEquals(left, right);
                    case ErlangOperatorKind.PlusPlus:
                        return ErlangValue.PlusPlus(left, right);
                    default:
                        throw new ArgumentException("invalid operator");
                }
            }
        }

        private static bool IsShortCircuitOperator(ErlangOperatorKind op)
        {
            switch (op)
            {
                case ErlangOperatorKind.AndAlso:
                case ErlangOperatorKind.OrElse:
                    return true;
                default:
                    return false;
            }
        }
    }

    public class ErlangTupleExpression : ErlangExpression
    {
        public ErlangExpression[] Elements { get; private set; }

        public ErlangTupleExpression(ErlangExpression[] elements)
        {
            Elements = elements;
            foreach (var elem in Elements)
                elem.SetParent(this);
        }

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            var values = new ErlangValue[Elements.Length];
            for (int i = 0; i < Elements.Length; i++)
            {
                var value = Elements[i].Evaluate(process);
                if (value.Kind == ErlangValueKind.Error)
                    return value;
                values[i] = value;
            }

            return new ErlangTuple(values);
        }
    }

    public class ErlangListExpression : ErlangExpression
    {
        public ErlangExpression[] Elements { get; private set; }
        public ErlangExpression Tail { get; private set; }

        public ErlangListExpression(ErlangExpression[] elements, ErlangExpression tail)
        {
            Elements = elements;
            Tail = tail;
            foreach (var elem in Elements)
                elem.SetParent(this);
            if (Tail != null)
                Tail.SetParent(this);
        }

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            var values = new ErlangValue[Elements.Length];
            for (int i = 0; i < Elements.Length; i++)
            {
                var value = Elements[i].Evaluate(process);
                if (value.Kind == ErlangValueKind.Error)
                    return value;
                values[i] = value;
            }

            var list = new ErlangList(values, Tail == null ? null : Tail.Evaluate(process));
            return list;
        }

        public static ErlangListExpression Empty
        {
            get { return new ErlangListExpression(new ErlangExpression[0], null); }
        }
    }

    //public class ListComprehensionExpression : Expression
    //{
    //}

    public class ErlangFunctionInvocationExpression : ErlangExpression
    {
        public string Module { get; private set; }
        public string Function { get; private set; }
        public ErlangExpression[] Parameters { get; private set; }

        public ErlangFunctionInvocationExpression(string module, string function, ErlangExpression[] parameters)
        {
            Module = module;
            Function = function;
            Parameters = parameters;
            foreach (var param in Parameters)
                param.SetParent(this);
        }

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            ErlangValue result = null;
            bool evaluated = false;
            var parameters = Parameters.Select(p => p.Evaluate(process)).ToArray();
            if (Module == null)
            {
                var module = GetModule();
                if (module != null)
                {
                    // try to execute from the current module
                    if (module.FunctionExistsInternal(Function, parameters.Length))
                    {
                        result = module.EvaluateInternal(process, Function, parameters);
                        evaluated = true;
                    }
                }
            }

            if (!evaluated)
            {
                // couldn't execute from this module, kick it back to the ErlangProcess to find the function
                result = process.Evaluate(Module, Function, parameters);
            }

            return result;
        }
    }

    public class ErlangCaseExpression : ErlangExpression
    {
        public ErlangExpression Expression { get; private set; }
        public ErlangCaseBranchExpression[] Branches { get; private set; }

        public ErlangCaseExpression(ErlangExpression expression, ErlangCaseBranchExpression[] branches)
        {
            Expression = expression;
            Branches = branches;
            Expression.SetParent(this);
            for (int i = 0; i < Branches.Length; i++)
                Branches[i].SetParent(this);
        }

        public override ErlangValue Evaluate(ErlangProcess process)
        {
            var value = Expression.Evaluate(process);
            if (value.Kind == ErlangValueKind.Error)
                return value;
            foreach (var branch in Branches)
            {
                process.CallStack.CurrentFrame.IncreaseScopeLevel();
                if (ErlangBinder.TryBindParameter(branch.Pattern, value, process.CallStack.CurrentFrame))
                {
                    if (branch.Guard == null || ErlangAtom.IsTrue(branch.Guard.Evaluate(process)))
                    {
                        var result = branch.Evaluate(process);
                        process.CallStack.CurrentFrame.DecreaseScopeLevel();
                        return result;
                    }
                }
                process.CallStack.CurrentFrame.DecreaseScopeLevel();
            }

            return new ErlangError("bad match on case");
        }
    }

    public class ErlangCaseBranchExpression : ErlangExpressionBlockExpression
    {
        public ErlangExpression Pattern { get; private set; }
        public ErlangGuardExpression Guard { get; private set; }
        public ErlangExpression[] Body { get; private set; }

        public ErlangCaseBranchExpression(ErlangExpression pattern, ErlangGuardExpression guard, ErlangExpression[] body)
        {
            Pattern = pattern;
            Guard = guard;
            Body = body;
            Pattern.SetParent(this);
            if (Guard != null)
                Guard.SetParent(this);
            for (int i = 0; i < Body.Length; i++)
                Body[i].SetParent(this);
        }

        public override ErlangExpression[] GetChildren()
        {
            return Body;
        }
    }
}
