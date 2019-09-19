using System.Linq;
using IxMilia.Erlang.Syntax;
using Xunit;

namespace IxMilia.Erlang.Test
{
    public class SyntaxTests : TestBase
    {
        private static void VerifyModuleAttributes(string text, params string[] expected)
        {
            var module = ErlangSyntaxNode.Parse(text);
            var actual = module.Attributes.Select(a => a.ToString()).ToArray();
            ArrayEquals(expected, actual);
        }

        [Fact]
        public void TestRegularAttributes()
        {
            VerifyModuleAttributes(@"
-module(foo).
-some_attribute().
-no_parens_no_arg.
-no_parens_one_arg foo.
",
                "-module(foo).",
                "-some_attribute().",
                "-no_parens_no_arg.",
                "-no_parens_one_arg foo.");
        }

        [Fact]
        public void TestTypeAttributes()
        {
            VerifyModuleAttributes(@"
-type boolean() :: true | false.
-type elements(T) :: non_neg_integer()
                   | element_tuple(T)
                   | nil().
-type array_opt()  :: {'fixed', boolean()} | 'fixed'     % from `array.erl` line 179
                    | {'default', Type :: term()}
                    | {'size', N :: non_neg_integer()}
                    | (N :: non_neg_integer()).
",
                "-type boolean()::true|false.",
                "-type elements(T)::non_neg_integer()|element_tuple(T)|nil().",
                "-type array_opt()::{'fixed',boolean()}|'fixed'|{'default',Type::term()}|{'size',N::non_neg_integer()}|(N::non_neg_integer()).");
        }

        [Fact]
        public void TestSpecAttribute()
        {
            VerifyModuleAttributes(@"
-spec foo() -> integer().
-spec foo() -> integer() | boolean().
-spec nth(N, List) -> Elem when
      N :: pos_integer(),
      List :: [T, ...],
      Elem :: T,
      T :: term().
-spec foo(A, B) -> boolean() when
      A :: {atom, 4..6},
      B :: 17.
",
                "-spec foo()->integer().",
                "-spec foo()->integer()|boolean().",
                "-spec nth(N,List)->Elem when N::pos_integer(),List::[T,...],Elem::T,T::term().",
                "-spec foo(A,B)->boolean() when A::{atom,4..6},B::17.");
        }

        [Fact]
        public void TestRecordAttribute()
        {
            VerifyModuleAttributes(@"
-record(array, {size :: non_neg_integer(),         % from `array.erl` line 161
        max :: non_neg_integer(),
        default,
        elements :: elements(_)
        }).
",
                "-record(array,{size::non_neg_integer(),max::non_neg_integer(),default,elements::elements(_)}).");
        }
    }
}
