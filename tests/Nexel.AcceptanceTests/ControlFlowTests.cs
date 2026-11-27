namespace Nexel.AcceptanceTests;

public sealed class ControlFlowTests
{
    private const string FactorialSource = """
        int factorial(int n) {
            if (n <= 1) {
                return 1;
            } else {
                return n * factorial(n - 1);
            }
        }

        void main() {
            print(factorial(readInt()));
        }
        """;

    private const string IsPrimeSource = """
        bool isPrime(int n) {
            if (n < 2) {
                return false;
            }
            for (int i = 2; i <= n / i; i = i + 1) {
                if (n % i == 0) {
                    return false;
                }
            }
            return true;
        }

        void main() {
            if (isPrime(readInt())) {
                print(1);
            } else {
                print(0);
            }
        }
        """;

    private const string WhileLoopSource = """
        void main() {
            int sum = 0;
            int n = 5;
            while (n > 0) {
                sum = sum + n;
                n = n - 1;
            }
            print(sum);
        }
        """;

    private const string MutualRecursionSource = """
        bool even(int n) {
            if (n == 0) {
                return true;
            }
            return odd(n - 1);
        }

        bool odd(int n) {
            if (n == 0) {
                return false;
            }
            return even(n - 1);
        }

        void main() {
            if (even(readInt())) {
                print(1);
            } else {
                print(0);
            }
        }
        """;

    private const string ShortCircuitSkipSource = """
        void main() {
            bool first = false && (readInt() > 0);
            bool second = true || (readInt() > 0);
            if (!first && second) {
                print(1);
            }
        }
        """;

    private const string ShortCircuitEvaluateSource = """
        void main() {
            bool first = true && (readInt() > 0);
            bool second = false || (readInt() > 0);
            if (first && second) {
                print(1);
            } else {
                print(0);
            }
        }
        """;

    private const string VoidFunctionIfFalseSource = """
        void main() {
            if (false) {
                return;
            }
            print(1);
        }
        """;

    private const string VoidFunctionIfElseBothReturnSource = """
        void f(int x) {
            if (x > 0) {
                return;
            } else {
                return;
            }
        }

        void main() {
            f(1);
            print(1);
        }
        """;

    public static TheoryData<string, string> FactorialData =>
        new TheoryData<string, string>
        {
            { "0\n", "1\n" },
            { "5\n", "120\n" },
        };

    public static TheoryData<string, string> IsPrimeData =>
        new TheoryData<string, string>
        {
            { "1\n", "0\n" },
            { "2\n", "1\n" },
            { "9\n", "0\n" },
            { "17\n", "1\n" },
        };

    public static TheoryData<string, string> MutualRecursionData =>
        new TheoryData<string, string>
        {
            { "4\n", "1\n" },
            { "5\n", "0\n" },
        };

    public static TheoryData<string, string> ShortCircuitEvaluateData =>
        new TheoryData<string, string>
        {
            { "1\n2\n", "1\n" },
            { "1\n-1\n", "0\n" },
            { "-1\n2\n", "0\n" },
        };

    [Theory]
    [MemberData(nameof(FactorialData))]
    public void Factorial_ComputesCorrectly(string input, string expected)
    {
        Assert.Equal(expected, InterpreterTestRunner.Run(FactorialSource, input));
    }

    [Theory]
    [MemberData(nameof(IsPrimeData))]
    public void IsPrime_ComputesCorrectly(string input, string expected)
    {
        Assert.Equal(expected, InterpreterTestRunner.Run(IsPrimeSource, input));
    }

    [Fact]
    public void WhileLoop_SumOneToFive_Returns15()
    {
        Assert.Equal("15\n", InterpreterTestRunner.Run(WhileLoopSource));
    }

    [Theory]
    [MemberData(nameof(MutualRecursionData))]
    public void MutualRecursion_EvenOdd(string input, string expected)
    {
        Assert.Equal(expected, InterpreterTestRunner.Run(MutualRecursionSource, input));
    }

    [Fact]
    public void ShortCircuit_SkipsRightOperand_DoesNotReadInput()
    {
        Assert.Equal("1\n", InterpreterTestRunner.Run(ShortCircuitSkipSource));
    }

    [Theory]
    [MemberData(nameof(ShortCircuitEvaluateData))]
    public void ShortCircuit_EvaluatesRightOperand_WhenNeeded(string input, string expected)
    {
        Assert.Equal(expected, InterpreterTestRunner.Run(ShortCircuitEvaluateSource, input));
    }

    [Fact]
    public void VoidFunction_IfFalseWithReturn_DoesNotFallThrough()
    {
        Assert.Equal("1\n", InterpreterTestRunner.Run(VoidFunctionIfFalseSource));
    }

    [Fact]
    public void VoidFunction_IfElseBothReturn_DoesNotFallThrough()
    {
        Assert.Equal("1\n", InterpreterTestRunner.Run(VoidFunctionIfElseBothReturnSource));
    }
}