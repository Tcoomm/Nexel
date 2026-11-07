namespace Nexel.AcceptanceTests;

using Nexel.Execution;
using Nexel.Parser;
using Nexel.Vm;
using Nexel.VmCodegen;

using Xunit;

public sealed class NewFeaturesTests
{
    private const string RectangleAreaProgram = """
        int area(int width, int height) {
            return abs(width) * abs(height);
        }

        void main() {
            int width = readInt();
            int height = readInt();
            print(area(width, height));
        }
        """;

    private const string OrderCostProgram = """
        int delivery = 150;

        int total(int price, int count) {
            int subtotal = price * count;
            int discount = min(subtotal / 10, 300);
            return max(subtotal - discount, 0) + delivery;
        }

        void showTotal(int price, int count) {
            print(total(price, count));
            return;
        }

        void main() {
            int price = readInt();
            int count = readInt();
            showTotal(price, count);
        }
        """;

    [Fact]
    public void LocalVariableCanBeReadAndAssigned()
    {
        string source = """
            void main() {
                int value = 1;
                print(value);
                value = 2;
                print(value);
            }
            """;

        Assert.Equal("1\n2\n", Run(source));
    }

    [Fact]
    public void NestedBlockCanShadowOuterVariableWithoutChangingIt()
    {
        string source = """
            void main() {
                int value = 1;
                {
                    int value = 2;
                    print(value);
                }
                print(value);
            }
            """;

        Assert.Equal("2\n1\n", Run(source));
    }

    [Fact]
    public void LocalVariableIsNotVisibleAfterItsBlock()
    {
        string source = """
            void main() {
                {
                    int value = 1;
                }
                print(value);
            }
            """;

        AssertCodegenFailure(source);
    }

    [Fact]
    public void FunctionCannotReadLocalVariableFromCaller()
    {
        string source = """
            int readLocal() {
                return localValue;
            }

            void main() {
                int localValue = 9;
                print(readLocal());
            }
            """;

        AssertCodegenFailure(source);
    }

    [Fact]
    public void FunctionReadsGlobalInsteadOfCallersShadowingLocal()
    {
        string source = """
            int value = 1;

            int getValue() {
                return value;
            }

            void main() {
                int value = 9;
                print(getValue());
            }
            """;

        Assert.Equal("1\n", Run(source));
    }

    [Fact]
    public void ParametersArePassedByValue()
    {
        string source = """
            int increment(int value) {
                value = value + 1;
                return value;
            }

            void main() {
                int original = 5;
                print(increment(original));
                print(original);
            }
            """;

        Assert.Equal("6\n5\n", Run(source));
    }

    [Fact]
    public void ParametersOccupySlotsInDeclarationOrder()
    {
        string source = """
            int combine(int first, int second) {
                return first * 100 + second;
            }

            void main() {
                print(combine(3, 7));
            }
            """;

        Assert.Equal("307\n", Run(source));
    }

    public static TheoryData<string> DuplicateParameterOrParameterRedeclarationIsRejectedCases()
    {
        return new TheoryData<string>
        {
            { "int invalid(int value, int value) { return value; } void main() { }" },
            { "int invalid(int value) { int value = 2; return value; } void main() { }" },
        };
    }

    [Theory]
    [MemberData(nameof(DuplicateParameterOrParameterRedeclarationIsRejectedCases))]
    public void DuplicateParameterOrParameterRedeclarationIsRejected(string source)
    {
        AssertCodegenFailure(source);
    }

    [Fact]
    public void NestedBlockCanShadowFunctionParameter()
    {
        string source = """
            int test(int value) {
                {
                    int value = 2;
                    print(value);
                }
                return value;
            }

            void main() {
                print(test(1));
            }
            """;

        Assert.Equal("2\n1\n", Run(source));
    }

    [Fact]
    public void GlobalsAreInitializedBeforeMainAndCanBeChangedByFunctions()
    {
        string source = """
            int counter = 40 + 2;

            int increment() {
                counter = counter + 1;
                return counter;
            }

            void main() {
                print(counter);
                print(increment());
                print(counter);
            }
            """;

        Assert.Equal("42\n43\n43\n", Run(source));
    }

    [Fact]
    public void LocalVariableCanShadowGlobalVariable()
    {
        string source = """
            int value = 1;

            void main() {
                {
                    int value = 2;
                    print(value);
                }
                print(value);
            }
            """;

        Assert.Equal("2\n1\n", Run(source));
    }

    [Fact]
    public void FunctionCanBeCalledBeforeItsDefinition()
    {
        string source = """
            void main() {
                print(add(2, 3));
            }

            int add(int left, int right) {
                return left + right;
            }
            """;

        Assert.Equal("5\n", Run(source));
    }

    [Fact]
    public void FunctionsCanBeCalledWithoutArgumentsAndNestedInExpressions()
    {
        string source = """
            int answer() {
                return 42;
            }

            int add(int left, int right) {
                return left + right;
            }

            int twice(int value) {
                return add(value, value);
            }

            void main() {
                print(answer());
                print(twice(add(2, 3)));
            }
            """;

        Assert.Equal("42\n10\n", Run(source));
    }

    [Fact]
    public void VoidFunctionCanBeCalledAsAStatementAndReturnImplicitly()
    {
        string source = """
            void notify() {
                print(7);
            }

            void main() {
                notify();
                return;
            }
            """;

        Assert.Equal("7\n", Run(source));
    }

    [Fact]
    public void ReturnInsideNestedBlockReturnsFromWholeFunction()
    {
        string source = """
            int getValue() {
                {
                    return 7;
                }
            }

            void main() {
                print(getValue());
            }
            """;

        Assert.Equal("7\n", Run(source));
    }

    public static TheoryData<string> FunctionReturnMustMatchDeclaredReturnTypeCases()
    {
        return new TheoryData<string>
        {
            { "int invalid() { print(1); } void main() { }" },
            { "int invalid() { return; } void main() { }" },
            { "void invalid() { return 1; } void main() { }" },
        };
    }

    [Theory]
    [MemberData(nameof(FunctionReturnMustMatchDeclaredReturnTypeCases))]
    public void FunctionReturnMustMatchDeclaredReturnType(string source)
    {
        AssertCodegenFailure(source);
    }

    [Fact]
    public void FunctionCallDoesNotCorruptEarlierStackOperand()
    {
        string source = """
            int doubleValue(int value) {
                return value * 2;
            }

            void main() {
                print(100 + doubleValue(2));
            }
            """;

        Assert.Equal("104\n", Run(source));
    }

    [Fact]
    public void FunctionArgumentsAreEvaluatedFromLeftToRight()
    {
        string source = """
            int mark(int value) {
                print(value);
                return value;
            }

            int pair(int first, int second) {
                return first * 10 + second;
            }

            void main() {
                print(pair(mark(1), mark(2)));
            }
            """;

        Assert.Equal("1\n2\n12\n", Run(source));
    }

    [Fact]
    public void ReadIntArgumentsAreEvaluatedFromLeftToRight()
    {
        string source = """
            int difference(int left, int right) {
                return left - right;
            }

            void main() {
                print(difference(readInt(), readInt()));
            }
            """;

        Assert.Equal("7\n", Run(source, "10\n3\n"));
    }

    public static TheoryData<int, int> AbsReturnsAbsoluteValueCases()
    {
        return new TheoryData<int, int>
        {
            { 5, 5 },
            { -5, 5 },
            { 0, 0 },
        };
    }

    [Theory]
    [MemberData(nameof(AbsReturnsAbsoluteValueCases))]
    public void AbsReturnsAbsoluteValue(int value, int expected)
    {
        string source = $"void main() {{ print(abs({value})); }}";

        Assert.Equal($"{expected}\n", Run(source));
    }

    [Fact]
    public void MinAndMaxHandleOrdinaryAndEqualArguments()
    {
        string source = """
            void main() {
                print(min(3, 7));
                print(max(3, 7));
                print(min(4, 4));
                print(max(4, 4));
            }
            """;

        Assert.Equal("3\n7\n4\n4\n", Run(source));
    }

    public static TheoryData<string> FunctionsRequireExactArityCases()
    {
        return new TheoryData<string>
        {
            { "void main() { print(readInt(1)); }" },
            { "void main() { print(); }" },
            { "void main() { print(abs(1, 2)); }" },
            { "void main() { print(min(1)); }" },
            { "void main() { print(max(1, 2, 3)); }" },
            { "int add(int left, int right) { return left + right; } void main() { print(add(1)); }" },
        };
    }

    [Theory]
    [MemberData(nameof(FunctionsRequireExactArityCases))]
    public void FunctionsRequireExactArity(string source)
    {
        AssertCodegenFailure(source);
    }

    [Fact]
    public void AbsOfMinimumIntThrowsVmException()
    {
        string source = """
            void main() {
                print(abs(-2147483647 - 1));
            }
            """;

        AssertVmFailure(source);
    }

    public static TheoryData<string> UnknownOrConflictingNamesAreRejectedCases()
    {
        return new TheoryData<string>
        {
            { "void main() { print(unknown); }" },
            { "void main() { print(unknown()); }" },
            { "void main() { print(value); int value = 1; }" },
            { "void main() { int value = 1; int value = 2; }" },
            { "int value = 1; int value = 2; void main() { }" },
            { "int item = 1; int item() { return 1; } void main() { }" },
            { "int helper() { return 1; } int helper() { return 2; } void main() { }" },
            { "int abs(int value) { return value; } void main() { }" },
            { "void main() { main(); }" },
        };
    }

    [Theory]
    [MemberData(nameof(UnknownOrConflictingNamesAreRejectedCases))]
    public void UnknownOrConflictingNamesAreRejected(string source)
    {
        AssertCodegenFailure(source);
    }

    public static TheoryData<string> LocalVariablesAndParametersCannotUseFunctionNamesCases()
    {
        return new TheoryData<string>
        {
            { "void main() { int main = 1; }" },
            { "void main() { int print = 1; }" },
            { "int helper() { return 1; } void main() { int helper = 1; }" },
            { "int helper(int print) { return print; } void main() { }" },
        };
    }

    [Theory]
    [MemberData(nameof(LocalVariablesAndParametersCannotUseFunctionNamesCases))]
    public void LocalVariablesAndParametersCannotUseFunctionNames(string source)
    {
        AssertCodegenFailure(source);
    }

    public static TheoryData<string> VoidCallCannotBeUsedAsAnIntValueCases()
    {
        return new TheoryData<string>
        {
            { "void noValue() { } void main() { print(noValue() + 1); }" },
            { "void noValue() { } int accept(int value) { return value; } void main() { print(accept(noValue())); }" },
            { "void noValue() { } void main() { int value = noValue(); }" },
            { "void noValue() { } int getValue() { return noValue(); } void main() { print(getValue()); }" },
        };
    }

    [Theory]
    [MemberData(nameof(VoidCallCannotBeUsedAsAnIntValueCases))]
    public void VoidCallCannotBeUsedAsAnIntValue(string source)
    {
        AssertCodegenFailure(source);
    }

    public static TheoryData<string> GlobalInitializerCannotReadNamesOrCallFunctionsCases()
    {
        return new TheoryData<string>
        {
            { "int first = 1; int second = first + 1; void main() { }" },
            { "int value = readInt(); void main() { }" },
        };
    }

    [Theory]
    [MemberData(nameof(GlobalInitializerCannotReadNamesOrCallFunctionsCases))]
    public void GlobalInitializerCannotReadNamesOrCallFunctions(string source)
    {
        AssertParserFailure(source);
    }

    [Fact]
    public void SemanticErrorPreventsAllProgramOutput()
    {
        string source = """
            void main() {
                print(1);
                print(missing);
            }
            """;

        using StringReader input = new StringReader(string.Empty);
        using StringWriter output = new StringWriter();

        Assert.Throws<CodegenException>(
            () => new Interpreter().Execute(source, input, output));
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void RectangleAreaProgramProducesExpectedOutput()
    {
        Assert.Equal("20\n", Run(RectangleAreaProgram, "-4\n5\n"));
    }

    [Fact]
    public void OrderCostProgramProducesExpectedOutput()
    {
        Assert.Equal("690\n", Run(OrderCostProgram, "200\n3\n"));
    }

    private static string Run(string source, string input = "")
    {
        using StringReader reader = new StringReader(input);
        using StringWriter writer = new StringWriter();

        new Interpreter().Execute(source, reader, writer);

        return writer.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static void AssertCodegenFailure(string source)
    {
        using StringReader reader = new StringReader(string.Empty);
        using StringWriter writer = new StringWriter();

        Assert.Throws<CodegenException>(
            () => new Interpreter().Execute(source, reader, writer));
    }

    private static void AssertParserFailure(string source)
    {
        using StringReader reader = new StringReader(string.Empty);
        using StringWriter writer = new StringWriter();

        Assert.Throws<ParserException>(
            () => new Interpreter().Execute(source, reader, writer));
    }

    private static void AssertVmFailure(string source)
    {
        using StringReader reader = new StringReader(string.Empty);
        using StringWriter writer = new StringWriter();

        Assert.Throws<VmException>(
            () => new Interpreter().Execute(source, reader, writer));
    }
}