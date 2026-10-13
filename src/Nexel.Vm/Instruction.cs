namespace Nexel.Vm;

public readonly record struct Instruction(OpCode Code, int Operand = 0);
