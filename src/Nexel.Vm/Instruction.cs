namespace Nexel.Vm;

public readonly record struct Instruction(OpCode OpCode, int? Operand = null);