namespace Nexel.AcceptanceTests;

using Nexel.Execution;
using Xunit;

public sealed class NewFeaturesTests
{
    private const string SkipReason = "Ждёт AST и Codegen";

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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Theory(Skip = SkipReason)]
    [InlineData("int invalid(int value, int value) { return value; } void main() { }")]
    [InlineData("int invalid(int value) { int value = 2; return value; } void main() { }")]
    public void DuplicateParameterOrParameterRedeclarationIsRejected(string source)
    {
        AssertCodegenFailure(source);
    }

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Theory(Skip = SkipReason)]
    [InlineData("int invalid() { print(1); } void main() { }")]
    [InlineData("int invalid() { return; } void main() { }")]
    [InlineData("void invalid() { return 1; } void main() { }")]
    public void FunctionReturnMustMatchDeclaredReturnType(string source)
    {
        AssertCodegenFailure(source);
    }

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    [Theory(Skip = SkipReason)]
    [InlineData(5, 5)]
    [InlineData(-5, 5)]
    [InlineData(0, 0)]
    public void AbsReturnsAbsoluteValue(int value, int expected)
    {
        string source = $"void main() {{ print(abs({value})); }}";

        Assert.Equal($"{expected}\n", Run(source));
    }

    [Fact(Skip = SkipReason)]
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

    [Theory(Skip = SkipReason)]
    [InlineData("void main() { print(readInt(1)); }")]
    [InlineData("void main() { print(); }")]
    [InlineData("void main() { print(abs(1, 2)); }")]
    [InlineData("void main() { print(min(1)); }")]
    [InlineData("void main() { print(max(1, 2, 3)); }")]
    [InlineData("int add(int left, int right) { return left + right; } void main() { print(add(1)); }")]
    public void FunctionsRequireExactArity(string source)
    {
        AssertCodegenFailure(source);
    }

    [Fact(Skip = SkipReason)]
    public void AbsOfMinimumIntThrowsVmException()
    {
        string source = """
            void main() {
                print(abs(-2147483647 - 1));
            }
            """;

        AssertVmFailure(source);
    }

    [Theory(Skip = SkipReason)]
    [InlineData("void main() { print(unknown); }")]
    [InlineData("void main() { print(unknown()); }")]
    [InlineData("void main() { print(value); int value = 1; }")]
    [InlineData("void main() { int value = 1; int value = 2; }")]
    [InlineData("int value = 1; int value = 2; void main() { }")]
    [InlineData("int item = 1; int item() { return 1; } void main() { }")]
    [InlineData("int helper() { return 1; } int helper() { return 2; } void main() { }")]
    [InlineData("int abs(int value) { return value; } void main() { }")]
    [InlineData("void main() { main(); }")]
    public void UnknownOrConflictingNamesAreRejected(string source)
    {
        AssertCodegenFailure(source);
    }

    [Theory(Skip = SkipReason)]
    [InlineData("void main() { int main = 1; }")]
    [InlineData("void main() { int print = 1; }")]
    [InlineData("int helper() { return 1; } void main() { int helper = 1; }")]
    [InlineData("int helper(int print) { return print; } void main() { }")]
    public void LocalVariablesAndParametersCannotUseFunctionNames(string source)
    {
        AssertCodegenFailure(source);
    }

    [Theory(Skip = SkipReason)]
    [InlineData("void noValue() { } void main() { print(noValue() + 1); }")]
    [InlineData("void noValue() { } int accept(int value) { return value; } void main() { print(accept(noValue())); }")]
    [InlineData("void noValue() { } void main() { int value = noValue(); }")]
    [InlineData("void noValue() { } int getValue() { return noValue(); } void main() { print(getValue()); }")]
    public void VoidCallCannotBeUsedAsAnIntValue(string source)
    {
        AssertCodegenFailure(source);
    }

    [Theory(Skip = SkipReason)]
    [InlineData("int first = 1; int second = first + 1; void main() { }")]
    [InlineData("int value = readInt(); void main() { }")]
    public void GlobalInitializerCannotReadNamesOrCallFunctions(string source)
    {
        AssertParserFailure(source);
    }

    [Fact(Skip = SkipReason)]
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

        Exception exception = Assert.ThrowsAny<Exception>(
            () => new Interpreter().Execute(source, input, output));

        Assert.Equal("CodegenException", exception.GetType().Name);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Theory(Skip = SkipReason)]
    [InlineData("-4\n5\n", "20\n")]
    public void RectangleAreaProgramProducesExpectedOutput(string input, string expectedOutput)
    {
        Assert.Equal(expectedOutput, Run(RectangleAreaProgram, input));
    }

    [Theory(Skip = SkipReason)]
    [InlineData("200\n3\n", "690\n")]
    public void OrderCostProgramProducesExpectedOutput(string input, string expectedOutput)
    {
        Assert.Equal(expectedOutput, Run(OrderCostProgram, input));
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

        Exception exception = Assert.ThrowsAny<Exception>(
            () => new Interpreter().Execute(source, reader, writer));

        Assert.Equal("CodegenException", exception.GetType().Name);
    }

    private static void AssertParserFailure(string source)
    {
        using StringReader reader = new StringReader(string.Empty);
        using StringWriter writer = new StringWriter();

        Exception exception = Assert.ThrowsAny<Exception>(
            () => new Interpreter().Execute(source, reader, writer));

        Assert.Equal("ParserException", exception.GetType().Name);
    }

    private static void AssertVmFailure(string source)
    {
        using StringReader reader = new StringReader(string.Empty);
        using StringWriter writer = new StringWriter();

        Exception exception = Assert.ThrowsAny<Exception>(
            () => new Interpreter().Execute(source, reader, writer));

        Assert.Equal("VmException", exception.GetType().Name);
    }
}