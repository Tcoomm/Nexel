namespace Nexel.Vm;

public sealed class VirtualMachine
{
    public void Execute(
        IReadOnlyList<Instruction> program,
        TextReader input,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        Stack<int> stack = new Stack<int>();

        for (int instructionPointer = 0;
             instructionPointer < program.Count;
             instructionPointer++)
        {
            Instruction instruction = program[instructionPointer];

            try
            {
                ExecuteInstruction(instruction, stack, input, output);
            }
            catch (VmException)
            {
                throw;
            }
            catch (OverflowException exception)
            {
                throw new VmException(
                    $"Integer overflow while executing instruction at IP {instructionPointer}.",
                    exception);
            }
        }
    }

    private static void ExecuteInstruction(
        Instruction instruction,
        Stack<int> stack,
        TextReader input,
        TextWriter output)
    {
        switch (instruction.Code)
        {
            case OpCode.Push:
                stack.Push(instruction.Operand);
                break;

            case OpCode.Add:
                {
                    int b = PopValue(stack);
                    int a = PopValue(stack);
                    stack.Push(checked(a + b));
                    break;
                }

            case OpCode.Sub:
                {
                    int b = PopValue(stack);
                    int a = PopValue(stack);
                    stack.Push(checked(a - b));
                    break;
                }

            case OpCode.Mul:
                {
                    int b = PopValue(stack);
                    int a = PopValue(stack);
                    stack.Push(checked(a * b));
                    break;
                }

            case OpCode.Div:
                {
                    int b = PopValue(stack);
                    int a = PopValue(stack);

                    if (b == 0)
                    {
                        throw new VmException("Division by zero.");
                    }

                    stack.Push(checked(a / b));
                    break;
                }

            case OpCode.Mod:
                {
                    int b = PopValue(stack);
                    int a = PopValue(stack);

                    if (b == 0)
                    {
                        throw new VmException("Modulo by zero.");
                    }

                    stack.Push(checked(a % b));
                    break;
                }

            case OpCode.Neg:
                {
                    int value = PopValue(stack);
                    stack.Push(checked(-value));
                    break;
                }

            case OpCode.ReadInt:
                {
                    string? line = input.ReadLine();

                    if (line is null)
                    {
                        throw new VmException("Unexpected end of input.");
                    }

                    if (!int.TryParse(
                        line,
                        System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out int value))
                    {
                        throw new VmException($"Invalid integer input: '{line}'.");
                    }

                    stack.Push(value);
                    break;
                }

            case OpCode.Print:
                {
                    int value = PopValue(stack);
                    output.WriteLine(value);
                    break;
                }

            case OpCode.Pop:
                PopValue(stack);
                break;

            default:
                throw new VmException(
                    $"Unknown VM instruction: {(int)instruction.Code}.");
        }
    }

    private static int PopValue(Stack<int> stack)
    {
        if (stack.Count == 0)
        {
            throw new VmException("Stack underflow.");
        }

        return stack.Pop();
    }
}