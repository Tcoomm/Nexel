namespace Nexel.Vm;

public enum OpCode
{
    Push,
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Neg,
    Pop,
    ReadInt,
    Print,
    LoadLocal,
    StoreLocal,
    LoadGlobal,
    StoreGlobal,
    Call,
    Return,
    Abs,
    Min,
    Max,
}
