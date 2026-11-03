namespace Nexel.Vm;

using System.Globalization;

public sealed class VirtualMachine
{
    private sealed class CallFrame
    {
        public CallFrame(int functionIndex, int returnAddress, int localCount, int stackBase)
        {
            FunctionIndex = functionIndex;
            ReturnAddress = returnAddress;
            Locals = new int[localCount];
            StackBase = stackBase;
        }

        public int FunctionIndex { get; }

        public int ReturnAddress { get; }

        public int[] Locals { get; }

        public int StackBase { get; }
    }

    public void Execute(
        IReadOnlyList<Instruction> program,
        TextReader input,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        List<Instruction> instructions = new List<Instruction>(program.Count + 1);
        instructions.AddRange(program);
        instructions.Add(new Instruction(OpCode.Return));

        VmFunction entry = new VmFunction(
            Name: "<entry>",
            EntryPoint: 0,
            ParameterCount: 0,
            LocalCount: 0,
            ReturnsValue: false);

        VmProgram vmProgram = new VmProgram(
            Instructions: instructions,
            Functions: new[] { entry },
            GlobalCount: 0,
            EntryFunctionIndex: 0);

        Execute(vmProgram, input, output);
    }

    public void Execute(
        VmProgram program,
        TextReader input,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        if (program.Instructions is null)
        {
            throw new VmException("Program instructions list cannot be null.");
        }

        if (program.Functions is null || program.Functions.Count == 0)
        {
            throw new VmException("Program must contain at least one function.");
        }

        if (program.GlobalCount < 0)
        {
            throw new VmException($"Invalid global variable count: {program.GlobalCount}.");
        }

        ValidateFunctionIndex(program, program.EntryFunctionIndex);
        VmFunction entryFunction = program.Functions[program.EntryFunctionIndex];

        if (entryFunction.ParameterCount != 0 || entryFunction.ReturnsValue)
        {
            throw new VmException("Entry function must be void and have 0 parameters.");
        }

        ValidateFunctionMetadata(entryFunction);

        int[] globals = new int[program.GlobalCount];
        List<int> stack = new List<int>();
        Stack<CallFrame> frames = new Stack<CallFrame>();

        frames.Push(new CallFrame(
            functionIndex: program.EntryFunctionIndex,
            returnAddress: -1,
            localCount: entryFunction.LocalCount,
            stackBase: 0));

        int ip = entryFunction.EntryPoint;

        while (frames.Count > 0)
        {
            if (ip < 0 || ip >= program.Instructions.Count)
            {
                throw new VmException($"Instruction pointer {ip} is out of program bounds.");
            }

            Instruction instruction = program.Instructions[ip];
            CallFrame currentFrame = frames.Peek();
            int nextIp = ip + 1;

            try
            {
                switch (instruction.Code)
                {
                    case OpCode.Push:
                        stack.Add(instruction.Operand);
                        break;

                    case OpCode.Pop:
                        PopValue(stack, currentFrame);
                        break;

                    case OpCode.Add:
                    {
                        int b = PopValue(stack, currentFrame);
                        int a = PopValue(stack, currentFrame);
                        stack.Add(checked(a + b));
                        break;
                    }

                    case OpCode.Sub:
                    {
                        int b = PopValue(stack, currentFrame);
                        int a = PopValue(stack, currentFrame);
                        stack.Add(checked(a - b));
                        break;
                    }

                    case OpCode.Mul:
                    {
                        int b = PopValue(stack, currentFrame);
                        int a = PopValue(stack, currentFrame);
                        stack.Add(checked(a * b));
                        break;
                    }

                    case OpCode.Div:
                    {
                        int b = PopValue(stack, currentFrame);
                        int a = PopValue(stack, currentFrame);

                        if (b == 0)
                        {
                            throw new VmException("Division by zero.");
                        }

                        stack.Add(checked(a / b));
                        break;
                    }

                    case OpCode.Mod:
                    {
                        int b = PopValue(stack, currentFrame);
                        int a = PopValue(stack, currentFrame);

                        if (b == 0)
                        {
                            throw new VmException("Modulo by zero.");
                        }

                        stack.Add(checked(a % b));
                        break;
                    }

                    case OpCode.Neg:
                    {
                        int value = PopValue(stack, currentFrame);
                        stack.Add(checked(-value));
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
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int value))
                        {
                            throw new VmException($"Invalid integer input: '{line}'.");
                        }

                        stack.Add(value);
                        break;
                    }

                    case OpCode.Print:
                    {
                        int value = PopValue(stack, currentFrame);
                        output.WriteLine(value);
                        break;
                    }

                    case OpCode.LoadLocal:
                    {
                        ValidateSlot(instruction.Operand, currentFrame.Locals.Length, "local");
                        stack.Add(currentFrame.Locals[instruction.Operand]);
                        break;
                    }

                    case OpCode.StoreLocal:
                    {
                        ValidateSlot(instruction.Operand, currentFrame.Locals.Length, "local");
                        int value = PopValue(stack, currentFrame);
                        currentFrame.Locals[instruction.Operand] = value;
                        break;
                    }

                    case OpCode.LoadGlobal:
                    {
                        ValidateSlot(instruction.Operand, globals.Length, "global");
                        stack.Add(globals[instruction.Operand]);
                        break;
                    }

                    case OpCode.StoreGlobal:
                    {
                        ValidateSlot(instruction.Operand, globals.Length, "global");
                        int value = PopValue(stack, currentFrame);
                        globals[instruction.Operand] = value;
                        break;
                    }

                    case OpCode.Call:
                    {
                        ValidateFunctionIndex(program, instruction.Operand);
                        VmFunction target = program.Functions[instruction.Operand];
                        ValidateFunctionMetadata(target);

                        int availableInFrame = stack.Count - currentFrame.StackBase;
                        if (availableInFrame < target.ParameterCount)
                        {
                            throw new VmException(
                                $"Stack underflow on Call to '{target.Name}': expected {target.ParameterCount} arguments, got {availableInFrame}.");
                        }

                        int newStackBase = stack.Count - target.ParameterCount;
                        CallFrame calleeFrame = new CallFrame(
                            functionIndex: instruction.Operand,
                            returnAddress: ip + 1,
                            localCount: target.LocalCount,
                            stackBase: newStackBase);

                        for (int i = target.ParameterCount - 1; i >= 0; i--)
                        {
                            calleeFrame.Locals[i] = PopValue(stack, currentFrame);
                        }

                        if (target.EntryPoint < 0 || target.EntryPoint >= program.Instructions.Count)
                        {
                            throw new VmException(
                                $"Invalid EntryPoint {target.EntryPoint} for function '{target.Name}'.");
                        }

                        frames.Push(calleeFrame);
                        nextIp = target.EntryPoint;
                        break;
                    }

                    case OpCode.Return:
                    {
                        VmFunction currentFunc = program.Functions[currentFrame.FunctionIndex];
                        int expectedHeight = currentFrame.StackBase + (currentFunc.ReturnsValue ? 1 : 0);

                        if (stack.Count != expectedHeight)
                        {
                            throw new VmException(
                                $"Unbalanced stack on Return from '{currentFunc.Name}': expected stack height {expectedHeight}, got {stack.Count}.");
                        }

                        frames.Pop();

                        if (frames.Count == 0)
                        {
                            return;
                        }

                        if (currentFrame.ReturnAddress < 0 || currentFrame.ReturnAddress >= program.Instructions.Count)
                        {
                            throw new VmException(
                                $"Invalid return address {currentFrame.ReturnAddress} when returning from '{currentFunc.Name}'.");
                        }

                        nextIp = currentFrame.ReturnAddress;
                        break;
                    }

                    case OpCode.Abs:
                    {
                        int value = PopValue(stack, currentFrame);
                        stack.Add(Math.Abs(value));
                        break;
                    }

                    case OpCode.Min:
                    {
                        int b = PopValue(stack, currentFrame);
                        int a = PopValue(stack, currentFrame);
                        stack.Add(Math.Min(a, b));
                        break;
                    }

                    case OpCode.Max:
                    {
                        int b = PopValue(stack, currentFrame);
                        int a = PopValue(stack, currentFrame);
                        stack.Add(Math.Max(a, b));
                        break;
                    }

                    default:
                        throw new VmException($"Unknown VM instruction: {(int)instruction.Code}.");
                }
            }
            catch (VmException)
            {
                throw;
            }
            catch (OverflowException exception)
            {
                throw new VmException(
                    $"Integer overflow while executing instruction at IP {ip}.",
                    exception);
            }

            ip = nextIp;
        }
    }

    private static int PopValue(List<int> stack, CallFrame frame)
    {
        if (stack.Count <= frame.StackBase)
        {
            throw new VmException("Stack underflow.");
        }

        int lastIndex = stack.Count - 1;
        int value = stack[lastIndex];
        stack.RemoveAt(lastIndex);
        return value;
    }

    private static void ValidateFunctionIndex(VmProgram program, int functionIndex)
    {
        if (functionIndex < 0 || functionIndex >= program.Functions.Count)
        {
            throw new VmException($"Invalid function index: {functionIndex}.");
        }
    }

    private static void ValidateFunctionMetadata(VmFunction function)
    {
        if (function.ParameterCount < 0)
        {
            throw new VmException($"Invalid parameter count {function.ParameterCount} in function '{function.Name}'.");
        }

        if (function.LocalCount < function.ParameterCount)
        {
            throw new VmException(
                $"Invalid local count {function.LocalCount} (less than parameter count {function.ParameterCount}) in function '{function.Name}'.");
        }
    }

    private static void ValidateSlot(int slot, int limit, string slotKind)
    {
        if (slot < 0 || slot >= limit)
        {
            throw new VmException($"Invalid {slotKind} slot index: {slot}.");
        }
    }
}
