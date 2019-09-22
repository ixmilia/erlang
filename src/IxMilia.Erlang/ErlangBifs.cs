using System;

namespace IxMilia.Erlang
{
    [ErlangModule("erlang")]
    internal class ErlangBifs
    {
        private ErlangProcess Process;

        public ErlangBifs(ErlangProcess process)
        {
            Process = process;
        }

        [ErlangFunction("date")]
        public ErlangValue Date()
        {
            var today = DateTime.Today;
            return new ErlangTuple(new ErlangNumber(today.Year), new ErlangNumber(today.Month), new ErlangNumber(today.Day));
        }

        [ErlangFunction("time")]
        public ErlangValue Time()
        {
            var now = DateTime.Now;
            return new ErlangTuple(new ErlangNumber(now.Hour), new ErlangNumber(now.Minute), new ErlangNumber(now.Second));
        }

        [ErlangFunction("is_list")]
        public ErlangValue IsList(ErlangValue value)
        {
            return IsKind(value, ErlangValueKind.List);
        }

        [ErlangFunction("is_number")]
        public ErlangValue IsNumber(ErlangValue value)
        {
            return IsKind(value, ErlangValueKind.Number);
        }

        [ErlangFunction("is_float")]
        public ErlangValue IsFloat(ErlangValue value)
        {
            return value.Kind == ErlangValueKind.Number && !((ErlangNumber)value).IsIntegral
                ? ErlangAtom.True
                : ErlangAtom.False;
        }

        [ErlangFunction("is_integer")]
        public ErlangValue IsInteger(ErlangValue value)
        {
            return value.Kind == ErlangValueKind.Number && ((ErlangNumber)value).IsIntegral
                ? ErlangAtom.True
                : ErlangAtom.False;
        }

        [ErlangFunction("is_atom")]
        public ErlangValue IsAtom(ErlangValue value)
        {
            return IsKind(value, ErlangValueKind.Atom);
        }

        [ErlangFunction("is_tuple")]
        public ErlangValue IsTuple(ErlangValue value)
        {
            return IsKind(value, ErlangValueKind.Tuple);
        }

        [ErlangFunction("is_function")]
        public ErlangValue IsFunction(ErlangValue value)
        {
            return IsKind(value, ErlangValueKind.Fun);
        }

        [ErlangFunction("is_function")]
        public ErlangValue IsFunction(ErlangValue value, ErlangValue airity)
        {
            // TODO: implement this
            return new ErlangError("is_function/2 nyi");
        }

        [ErlangFunction("is_reference")]
        public ErlangValue IsReference(ErlangValue value)
        {
            return IsKind(value, ErlangValueKind.Reference);
        }

        [ErlangFunction("is_bitstring")]
        public ErlangValue IsBitString(ErlangValue value)
        {
            return IsKind(value, ErlangValueKind.BitString);
        }

        [ErlangFunction("is_port")]
        public ErlangValue IsPort(ErlangValue value)
        {
            return IsKind(value, ErlangValueKind.Port);
        }

        [ErlangFunction("is_pid")]
        public ErlangValue IsPid(ErlangValue value)
        {
            return IsKind(value, ErlangValueKind.Pid);
        }

        [ErlangFunction("get_module_info")]
        public ErlangValue GetModuleInfo(ErlangValue module)
        {
            if (module.Kind == ErlangValueKind.Atom)
            {
                return Process.Evaluate(((ErlangAtom)module).Name, "module_info");
            }
            else
            {
                return new ErlangError("must be called with atom");
            }
        }

        [ErlangFunction("length")]
        public ErlangValue Length(ErlangValue list)
        {
            if (list.Kind == ErlangValueKind.List)
            {
                var length = ((ErlangList)list).Length;
                if (length < 0)
                    return new ErlangError("not a proper list");
                return new ErlangNumber(length);
            }

            return new ErlangError("not a list");
        }

        private static ErlangValue IsKind(ErlangValue value, ErlangValueKind kind)
        {
            return value.Kind == kind ? ErlangAtom.True : ErlangAtom.False;
        }
    }
}
