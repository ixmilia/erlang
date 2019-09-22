using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace IxMilia.Erlang
{
    public enum ErlangValueKind
    {
        Error = 0,
        Number = 1,
        Atom = 2,
        Reference = 3,
        Fun = 4,
        Port = 5,
        Pid = 6,
        Tuple = 7,
        List = 8,
        BitString = 9
    }

    public abstract class ErlangValue
    {
        public abstract ErlangValueKind Kind { get; }

        public static ErlangValue Multiply(ErlangValue a, ErlangValue b)
        {
            return OperationWithNumbers(a, b, (x, y) => x * y);
        }

        public static ErlangValue Add(ErlangValue a, ErlangValue b)
        {
            return OperationWithNumbers(a, b, (x, y) => x + y);
        }

        public static ErlangValue Divide(ErlangValue a, ErlangValue b)
        {
            return OperationWithNumbers(a, b, (x, y) => x / y);
        }

        public static ErlangValue Subtract(ErlangValue a, ErlangValue b)
        {
            return OperationWithNumbers(a, b, (x, y) => x - y);
        }

        public static ErlangValue Div(ErlangValue a, ErlangValue b)
        {
            return OperationWithNumbers(a, b, (x, y) => ErlangNumber.Div(x, y));
        }

        public static ErlangValue Remainder(ErlangValue a, ErlangValue b)
        {
            return OperationWithNumbers(a, b, (x, y) => ErlangNumber.Remainder(x, y));
        }

        public static ErlangValue And(ErlangValue a, ErlangValue b)
        {
            return OperationWithBooleans(a, b, (x, y) => x && y);
        }

        public static ErlangValue Or(ErlangValue a, ErlangValue b)
        {
            return OperationWithBooleans(a, b, (x, y) => x && y);
        }

        public static ErlangValue Less(ErlangValue a, ErlangValue b)
        {
            return DoBooleanOp(a, b, (x, y) => x < y, (x, y) => x.CompareTo(y) < 0, (x, y) => x < y);
        }

        public static ErlangValue LessEquals(ErlangValue a, ErlangValue b)
        {
            return DoBooleanOp(a, b, (x, y) => x <= y, (x, y) => x.CompareTo(y) <= 0, (x, y) => x <= y);
        }

        public static ErlangValue Greater(ErlangValue a, ErlangValue b)
        {
            return DoBooleanOp(a, b, (x, y) => x > y, (x, y) => x.CompareTo(y) > 0, (x, y) => x > y);
        }

        public static ErlangValue GreaterEquals(ErlangValue a, ErlangValue b)
        {
            return DoBooleanOp(a, b, (x, y) => x >= y, (x, y) => x.CompareTo(y) >= 0, (x, y) => x >= y);
        }

        public static ErlangValue EqualsEquals(ErlangValue a, ErlangValue b)
        {
            return a.Equals(b) ? ErlangAtom.True : ErlangAtom.False;
        }

        public static ErlangValue EqualsColonEquals(ErlangValue a, ErlangValue b)
        {
            // this only matters for numbers
            if (a.Kind == ErlangValueKind.Number && b.Kind == ErlangValueKind.Number)
                return ErlangNumber.ExactlyEquals((ErlangNumber)a, (ErlangNumber)b) ? ErlangAtom.True : ErlangAtom.False;
            return EqualsEquals(a, b);
        }

        public static ErlangValue EqualsSlashEquals(ErlangValue a, ErlangValue b)
        {
            // this only matters for numbers
            if (a.Kind == ErlangValueKind.Number && b.Kind == ErlangValueKind.Number)
                return ErlangNumber.ExactlyNotEquals((ErlangNumber)a, (ErlangNumber)b) ? ErlangAtom.True : ErlangAtom.False;
            return SlashEquals(a, b);
        }

        public static ErlangValue Not(ErlangValue value)
        {
            if (ErlangAtom.IsTrue(value))
                return ErlangAtom.False;
            else if (ErlangAtom.IsFalse(value))
                return ErlangAtom.True;
            else
                return new ErlangError("not a boolean");
        }

        public static ErlangValue SlashEquals(ErlangValue a, ErlangValue b)
        {
            return a.Equals(b) ? ErlangAtom.False : ErlangAtom.True;
        }

        public static ErlangValue PlusPlus(ErlangValue list, ErlangValue tail)
        {
            if (list.Kind != ErlangValueKind.List)
                return new ErlangError("not a list");
            return ErlangList.CloneWithTail((ErlangList)list, tail);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ErlangValue);
        }

        public bool Equals(ErlangValue other)
        {
            if (other is null)
                return false;
            if (other.Kind != Kind)
                return false;

            switch (Kind)
            {
                case ErlangValueKind.Atom:
                    return ((ErlangAtom)this).Name == ((ErlangAtom)other).Name;
                case ErlangValueKind.Number:
                    return (ErlangNumber)this == (ErlangNumber)other;
                case ErlangValueKind.Tuple:
                    var a = (ErlangTuple)this;
                    var b = (ErlangTuple)other;
                    if (a.Airity != b.Airity)
                        return false;
                    for (int i = 0; i < a.Values.Length; i++)
                    {
                        if (!a.Values[i].Equals(b.Values[i]))
                            return false;
                    }

                    return true;
                case ErlangValueKind.List:
                    // since lists are immutable, we can potentially short-circuit with a ref-equals
                    if (ReferenceEquals(this, other))
                        return true;

                    // otherwise, check each item
                    var al = (ErlangList)this;
                    var bl = (ErlangList)other;
                    if (al.Value == null && bl.Value == null)
                        return true; // values are both null (this assumes that the tails are both null, too)
                    if (al.Value == null || bl.Value == null)
                        return false; // only one value was null
                    if (!al.Value.Equals(bl.Value))
                        return false; // values are different
                    if (al.Tail == null && bl.Tail == null)
                        return true; // neither has a tail
                    if (al.Tail != null && bl.Tail != null)
                        return al.Tail.Equals(bl.Tail); // both have a tail

                    return false; // only one had a tail
                default:
                    throw new Exception("nyi comparison");
            }
        }

        private static ErlangValue OperationWithNumbers(ErlangValue a, ErlangValue b, Func<ErlangNumber, ErlangNumber, ErlangValue> operation)
        {
            if (a == null || a.Kind != ErlangValueKind.Number || b == null || b.Kind != ErlangValueKind.Number)
                return new ErlangError("not numbers");
            return operation((ErlangNumber)a, (ErlangNumber)b);
        }

        private static ErlangValue OperationWithBooleans(ErlangValue a, ErlangValue b, Func<bool, bool, bool> operation)
        {
            if (!(ErlangAtom.IsTrue(a) || ErlangAtom.IsFalse(a)) || !(ErlangAtom.IsTrue(b) || ErlangAtom.IsFalse(b)))
                return new ErlangError("not numbers");
            return operation(ErlangAtom.IsTrue(a), ErlangAtom.IsTrue(b)) ? ErlangAtom.True : ErlangAtom.False;
        }

        private static ErlangValue DoBooleanOp(ErlangValue a, ErlangValue b,
            Func<int, int, bool> kindOp,
            Func<string, string, bool> atomOp,
            Func<ErlangNumber, ErlangNumber, bool> numOp)
        {
            var ai = (int)a.Kind;
            var bi = (int)b.Kind;
            bool result;
            if (ai == bi)
            {
                switch (a.Kind)
                {
                    case ErlangValueKind.Atom:
                        result = atomOp(((ErlangAtom)a).Name, ((ErlangAtom)b).Name);
                        break;
                    case ErlangValueKind.Number:
                        result = numOp((ErlangNumber)a, (ErlangNumber)b);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                result = kindOp(ai, bi);
            }

            return result ? ErlangAtom.True : ErlangAtom.False;
        }
    }

    public class ErlangError : ErlangValue
    {
        public override ErlangValueKind Kind => ErlangValueKind.Error;

        public string Message { get; }

        public ErlangError(string message)
        {
            Message = message;
        }

        public override string ToString()
        {
            return string.Format("Error: {0}", Message);
        }
    }

    public class ErlangNumber : ErlangValue
    {
        public override ErlangValueKind Kind => ErlangValueKind.Number;

        public BigInteger IntegerValue
        {
            get
            {
                if (!IsIntegral)
                    throw new InvalidOperationException("Value is a floating point");
                return bigIntValue;
            }
        }

        public double DoubleValue
        {
            get
            {
                if (IsIntegral)
                    throw new InvalidOperationException("Value is an integer");
                return doubleValue;
            }
        }

        public bool IsIntegral { get; }

        private BigInteger bigIntValue;
        private double doubleValue;

        public ErlangNumber(double value)
        {
            doubleValue = value;
            IsIntegral = false;
        }

        public ErlangNumber(int value)
            : this((BigInteger)value)
        {
        }

        public ErlangNumber(BigInteger value)
        {
            bigIntValue = value;
            IsIntegral = true;
        }

        public double AsDouble()
        {
            return IsIntegral ? (double)bigIntValue : doubleValue;
        }

        public override string ToString()
        {
            return IsIntegral ? bigIntValue.ToString() : doubleValue.ToString();
        }

        public static ErlangNumber One => new ErlangNumber(1);

        public static ErlangNumber NegativeOne => new ErlangNumber(-1);

        private static ErlangNumber DoNumericOp(ErlangNumber a, ErlangNumber b, Func<double, double, double> doubleOp, Func<BigInteger, BigInteger, BigInteger> bigOp)
        {
            if (a.IsIntegral && b.IsIntegral)
                return new ErlangNumber(bigOp(a.IntegerValue, b.IntegerValue));
            else
                return new ErlangNumber(doubleOp(a.AsDouble(), b.AsDouble()));
        }

        private static bool DoBooleanOp(ErlangNumber a, ErlangNumber b, Func<double, double, bool> doubleOp, Func<BigInteger, BigInteger, bool> bigOp)
        {
            if (a.IsIntegral && b.IsIntegral)
                return bigOp(a.IntegerValue, b.IntegerValue);
            else
                return doubleOp(a.AsDouble(), b.AsDouble());
        }

        public static ErlangNumber operator +(ErlangNumber a, ErlangNumber b)
        {
            return DoNumericOp(a, b, (x, y) => x + y, (x, y) => x + y);
        }

        public static ErlangNumber operator -(ErlangNumber a, ErlangNumber b)
        {
            return DoNumericOp(a, b, (x, y) => x - y, (x, y) => x - y);
        }

        public static ErlangNumber operator *(ErlangNumber a, ErlangNumber b)
        {
            return DoNumericOp(a, b, (x, y) => x * y, (x, y) => x * y);
        }

        public static ErlangNumber operator /(ErlangNumber a, ErlangNumber b)
        {
            return DoNumericOp(a, b, (x, y) => x / y, (x, y) => x / y);
        }

        public static bool operator <(ErlangNumber a, ErlangNumber b)
        {
            return DoBooleanOp(a, b, (x, y) => x < y, (x, y) => x < y);
        }

        public static bool operator <=(ErlangNumber a, ErlangNumber b)
        {
            return DoBooleanOp(a, b, (x, y) => x <= y, (x, y) => x <= y);
        }

        public static bool operator >(ErlangNumber a, ErlangNumber b)
        {
            return DoBooleanOp(a, b, (x, y) => x > y, (x, y) => x > y);
        }

        public static bool operator >=(ErlangNumber a, ErlangNumber b)
        {
            return DoBooleanOp(a, b, (x, y) => x >= y, (x, y) => x >= y);
        }

        public static bool operator ==(ErlangNumber a, ErlangNumber b)
        {
            return DoBooleanOp(a, b, (x, y) => x == y, (x, y) => x == y);
        }

        public static bool operator !=(ErlangNumber a, ErlangNumber b)
        {
            return DoBooleanOp(a, b, (x, y) => x != y, (x, y) => x != y);
        }

        public static bool ExactlyEquals(ErlangNumber a, ErlangNumber b)
        {
            if (a.IsIntegral != b.IsIntegral)
                return false;
            if (a.IsIntegral)
                return a.IntegerValue == b.IntegerValue;
            else
                return a.DoubleValue == b.DoubleValue;
        }

        public static bool ExactlyNotEquals(ErlangNumber a, ErlangNumber b)
        {
            if (a.IsIntegral != b.IsIntegral)
                return true;
            if (a.IsIntegral)
                return a.IntegerValue != b.IntegerValue;
            else
                return a.DoubleValue != b.DoubleValue;
        }

        public static ErlangValue Div(ErlangNumber a, ErlangNumber b)
        {
            if (!a.IsIntegral || !b.IsIntegral)
                return new ErlangError("not integers");
            return new ErlangNumber(a.IntegerValue / b.IntegerValue);
        }

        public static ErlangValue Remainder(ErlangNumber a, ErlangNumber b)
        {
            if (!a.IsIntegral || !b.IsIntegral)
                return new ErlangError("not integers");
            return new ErlangNumber(a.IntegerValue % b.IntegerValue);
        }

        public static ErlangValue BNot(ErlangNumber n)
        {
            return n.IsIntegral
                ? (ErlangValue)new ErlangNumber((n.IntegerValue + 1) * -1)
                : new ErlangError("not integral");
        }

        public override bool Equals(object obj)
        {
            if (obj is ErlangNumber)
                return this == (ErlangNumber)obj;
            return false;
        }

        public override int GetHashCode()
        {
            return IsIntegral ? bigIntValue.GetHashCode() : doubleValue.GetHashCode();
        }
    }

    public class ErlangAtom : ErlangValue
    {
        public override ErlangValueKind Kind => ErlangValueKind.Atom;

        private static readonly ErlangAtom _true = new ErlangAtom("true");

        private static readonly ErlangAtom _false = new ErlangAtom("false");

        public string Name { get; }

        public ErlangAtom(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public static ErlangAtom True => _true;

        public static ErlangAtom False => _false;

        public static bool IsTrue(ErlangValue atom)
        {
            return atom != null && atom.Kind == ErlangValueKind.Atom && ((ErlangAtom)atom).Name == "true";
        }

        public static bool IsFalse(ErlangValue atom)
        {
            return atom != null && atom.Kind == ErlangValueKind.Atom && ((ErlangAtom)atom).Name == "false";
        }
    }

    public class ErlangList : ErlangValue
    {
        public override ErlangValueKind Kind => ErlangValueKind.List;

        public ErlangValue Value { get; }

        public ErlangValue Tail { get; }

        public int Length { get; }

        public ErlangList(ErlangValue value, ErlangValue tail)
        {
            if (value == null && tail != null)
                throw new ArgumentException("Tail cannot be present without a value.");
            Value = value;
            Tail = tail;
            if (Value == null && Tail == null)
                Length = 0;
            else if (Tail == null || Tail.Kind != ErlangValueKind.List)
                Length = -1;
            else
            {
                var tailLength = ((ErlangList)Tail).Length + 1;
                Length = tailLength < 0 ? -1 : tailLength + 1;
            }
        }

        public ErlangList()
            : this(null, 0, null)
        {
        }

        public ErlangList(params ErlangValue[] values)
            : this(values, 0, null)
        {
        }

        public ErlangList(ErlangValue[] values, ErlangValue tail)
            : this(values, 0, tail)
        {
        }

        private ErlangList(ErlangValue[] values, int index, ErlangValue tail)
        {
            if (values != null && index < values.Length)
            {
                Value = values[index];
                if (index == values.Length - 1)
                    Tail = tail ?? new ErlangList();
                else
                    Tail = new ErlangList(values, index + 1, tail);
                var tailLength = Tail.Kind == ErlangValueKind.List
                    ? ((ErlangList)Tail).Length
                    : -1;
                Length = tailLength < 0 ? -1 : tailLength + 1;
            }
        }

        internal static ErlangValue CloneWithTail(ErlangList list, ErlangValue tail)
        {
            if (IsEmptyList(list.Tail))
            {
                return new ErlangList(list.Value, tail);
            }
            else if (list.Tail != null && list.Tail.Kind != ErlangValueKind.List)
            {
                return new ErlangError("can't concat");
            }
            else
            {
                var newTail = CloneWithTail((ErlangList)list.Tail, tail);
                if (newTail.Kind == ErlangValueKind.Error)
                    return newTail;
                return new ErlangList(list.Value, newTail);
            }
        }

        public static ErlangList FromItems(params ErlangValue[] items)
        {
            return new ErlangList(items);
        }

        public static bool IsEmptyList(ErlangValue item)
        {
            if (item != null && item.Kind == ErlangValueKind.List)
            {
                var list = (ErlangList)item;
                return list.Value == null && list.Tail == null;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return ToRealString() ?? ToListString();
        }

        private string ToListString()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            if (Value != null)
            {
                sb.Append(Value);
            }

            var tail = Tail;
            while (tail != null)
            {
                if (tail.Kind == ErlangValueKind.List)
                {
                    var tailList = (ErlangList)tail;
                    if (tailList.Value != null)
                    {
                        sb.Append(',');
                        sb.Append(tailList.Value);
                    }
                    tail = tailList.Tail;
                }
                else
                {
                    sb.Append('|');
                    sb.Append(tail);
                    tail = null;
                }
            }

            sb.Append(']');
            return sb.ToString();
        }

        private string ToRealString()
        {
            var sb = new StringBuilder();
            sb.Append('"');

            var head = this;
            while (head != null && head.Value != null)
            {
                if (head.Value.Kind != ErlangValueKind.Number)
                    return null;
                var num = (ErlangNumber)head.Value;
                if (!num.IsIntegral)
                    return null; // non-integer value
                if (num.IntegerValue < 32 || num.IntegerValue > 127)
                    return null; // non-ascii
                sb.Append((char)num.IntegerValue); // add character
                if (head.Tail != null && head.Tail.Kind != ErlangValueKind.List) // if tail isn't a list, bail
                    return null;
                head = head.Tail as ErlangList;
            }

            sb.Append('"');
            return sb.ToString();
        }

        public static ErlangList FromString(string value)
        {
            return new ErlangList(value.ToCharArray().Select(c => new ErlangNumber(c)).ToArray());
        }
    }

    public class ErlangTuple : ErlangValue
    {
        public override ErlangValueKind Kind => ErlangValueKind.Tuple;

        public ErlangValue[] Values { get; }

        public int Airity => Values.Length;

        public ErlangTuple(params ErlangValue[] values)
        {
            Values = values;
        }

        public override string ToString()
        {
            return string.Format("{{{0}}}", string.Join(", ", (IEnumerable<ErlangValue>)Values));
        }
    }
}
