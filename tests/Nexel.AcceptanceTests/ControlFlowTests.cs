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

    private const string ShortCircuitSource = """
        void main() {
            bool first = false && (readInt() > 0);
            bool second = true || (readInt() > 0);
            if (!first && second) {
                print(1);
            }
        }
        """;

    [Theory]
    [InlineData("0\n", "1\n")]
    [InlineData("5\n", "120\n")]
    public void Factorial_ComputesCorrectly(string input, string expected)
    {
        Assert.Equal(expected, InterpreterTestRunner.Run(FactorialSource, input));
    }

    [Theory]
    [InlineData("1\n", "0\n")]
    [InlineData("2\n", "1\n")]
    [InlineData("9\n", "0\n")]
    [InlineData("17\n", "1\n")]
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
    [InlineData("4\n", "1\n")]
    [InlineData("5\n", "0\n")]
    public void MutualRecursion_EvenOdd(string input, string expected)
    {
        Assert.Equal(expected, InterpreterTestRunner.Run(MutualRecursionSource, input));
    }

    [Fact]
    public void ShortCircuit_DoesNotReadInput()
    {
        Assert.Equal("1\n", InterpreterTestRunner.Run(ShortCircuitSource));
    }
}