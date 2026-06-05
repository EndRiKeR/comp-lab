using System.Text;
using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._3._AstToBinary
{
    public class SpirvModule
    {
        public uint Version { get; set; } = 0x00010600; // SPIR-V 1.6
        public uint Bound => _nextId;
        
        // Наборы инструкций, которые пишутся в бинарник в данном порядке
        // инструкции заголовка - генерируются автоматически, единообразны
        private List<Instruction> HeaderInstructions { get; } = new();
        private List<Instruction> DebugInstructions { get; } = new();
        private List<Instruction> AnnotationInstructions { get; } = new();
        private List<Instruction> TypeAndVariableInstructions { get; } = new();
        private List<Instruction> FunctionInstructions { get; } = new();
        
        private uint _nextId = 1;
        
        public uint GetNextId() => _nextId++;

        public void AddHeaderInstruction(Instruction instruction)
        {
            HeaderInstructions.Add(instruction);
        }

        public void AddDebugInstruction(Instruction instruction)
        {
            DebugInstructions.Add(instruction);
        }

        public void AddAnnotationInstruction(Instruction instruction)
        {
            AnnotationInstructions.Add(instruction);
        }

        public void AddTypeAndVariableInstruction(Instruction instruction)
        {
            TypeAndVariableInstructions.Add(instruction);
        }

        public void AddFunctionInstruction(Instruction instruction)
        {
            FunctionInstructions.Add(instruction);
        }
        
        public int CountFunctionInstruction()
        {
            return FunctionInstructions.Count;
        }
        
        public Instruction LastFunctionInstruction()
        {
            return FunctionInstructions.Last();
        }

        public List<Instruction> GetInstructions()
        {
            var allInstructions = new List<Instruction>();
            
            allInstructions.AddRange(HeaderInstructions);
            allInstructions.AddRange(DebugInstructions);
            allInstructions.AddRange(AnnotationInstructions);
            allInstructions.AddRange(TypeAndVariableInstructions);
            allInstructions.AddRange(FunctionInstructions);
            
            return allInstructions;
        }
    }
}