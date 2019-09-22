using IxMilia.Erlang.Tokens;

namespace IxMilia.Erlang
{
    internal static class ErlangBinder
    {
        public static bool TryBindParameter(ErlangExpression expression, ErlangValue value, ErlangStackFrame frame, bool bindBinary = false)
        {
            var type = expression.GetType();
            if (bindBinary && type == typeof(ErlangBinaryExpression))
            {
                var bin = (ErlangBinaryExpression)expression;
                switch (bin.Operator)
                {
                    case ErlangOperatorKind.Equals:
                        // if the right is a variable, bind to the left then to the right
                        if (bin.Right is ErlangVariableExpression)
                        {
                            if (TryBindParameter(bin.Left, value, frame))
                            {
                                return TryBindParameter(bin.Right, value, frame);
                            }
                        }
                        break;
                        // TODO: bind list concatenation '++', etc.
                }

                return false;
            }
            else if (type == typeof(ErlangVariableExpression))
            {
                var variable = (ErlangVariableExpression)expression;
                if (variable.Variable == "_")
                {
                    // always matches, never binds
                    return true;
                }
                else
                {
                    var current = frame.GetVariable(variable.Variable);
                    if (current == null)
                    {
                        // set the value
                        frame.SetVariable(variable.Variable, value);
                        return true;
                    }
                    else
                    {
                        // ensure the same value
                        return current.Equals(value);
                    }
                }
            }
            else if (type == typeof(ErlangAtomExpression) && value.Kind == ErlangValueKind.Atom)
                return ((ErlangAtomExpression)expression).Atom == ((ErlangAtom)value).Name;
            else if (type == typeof(ErlangConstantExpression) && value.Kind == ErlangValueKind.Number)
            {
                return ((ErlangConstantExpression)expression).Value == (ErlangNumber)value;
            }
            else if (type == typeof(ErlangTupleExpression) && value.Kind == ErlangValueKind.Tuple)
            {
                var tuple1 = (ErlangTupleExpression)expression;
                var tuple2 = (ErlangTuple)value;
                if (tuple1.Elements.Length == tuple2.Airity)
                {
                    for (int j = 0; j < tuple1.Elements.Length; j++)
                    {
                        if (!TryBindParameter(tuple1.Elements[j], tuple2.Values[j], frame))
                            return false;
                    }

                    return true;
                }
            }
            else if (type == typeof(ErlangListExpression) && value.Kind == ErlangValueKind.List)
            {
                // TODO: support list comprehensions
                var expressionList = (ErlangListExpression)expression;
                var ErlangList = (ErlangList)value;

                if (ErlangList.Value == null)
                {
                    // if the Erlang list is empty, the expression list must be, too
                    return expressionList.Elements.Length == 0 && expressionList.Tail == null;
                }

                // gather Erlang list values
                var head = ErlangList;
                int i;
                for (i = 0; i < expressionList.Elements.Length && head != null && head.Value != null; i++)
                {
                    if (!TryBindParameter(expressionList.Elements[i], head.Value, frame))
                        return false;
                    if (head.Tail != null && head.Tail.Kind == ErlangValueKind.List)
                        head = (ErlangList)head.Tail;
                    else
                        head = null;
                }

                if (expressionList.Elements.Length > 0 && i < expressionList.Elements.Length)
                {
                    // didn't make it through the expression list
                    return false;
                }

                // expressionList.Tail == null and head.Value == null matches
                if (expressionList.Tail == null)
                {
                    if (head == null || head.Value == null)
                        return true;
                    else
                        return false;
                }
                else
                {
                    if (head == null)
                        return false;
                    else
                        return TryBindParameter(expressionList.Tail, head, frame);
                }
            }

            return false;
        }
    }
}
