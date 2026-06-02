namespace comp_lab.CourseWork.Common;

public class Instruction
{
    public Opcode Opcode { get; set; }
    public uint? ResultType { get; set; }
    public uint? ResultId { get; set; }
    public List<object> Operands { get; } = new();
}