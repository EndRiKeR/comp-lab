using System.Text;
using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._3._AstToBinary
{
    public class SpirvModule
    {
        public uint Version { get; set; } = 0x00010600; // SPIR-V 1.6
        public uint Bound { get; set; }
        public List<Instruction> Instructions { get; } = new();
        private uint _nextId = 1;

        public uint GetNextId() => _nextId++;
        public uint GetCurrentBound() => _nextId;
        public void SetBound() => Bound = GetCurrentBound();
        public void AddInstruction(Instruction inst) => Instructions.Add(inst);

        // Базовые фабричные методы
        public void Capability(uint capability) => AddInstruction(new Instruction { Opcode = Opcode.OpCapability, Operands = { capability } });
        public void ExtInstImport(uint resultId, string name) => AddInstruction(new Instruction { Opcode = Opcode.OpExtInstImport, ResultId = resultId, Operands = { name } });
        public void MemoryModel(uint addressingModel, uint memoryModel) => AddInstruction(new Instruction { Opcode = Opcode.OpMemoryModel, Operands = { addressingModel, memoryModel } });
        public void Name(uint target, string name) => AddInstruction(new Instruction { Opcode = Opcode.OpName, Operands = { target, name } });
        public void Decorate(uint target, uint decoration, params object[] args)
        {
            var inst = new Instruction { Opcode = Opcode.OpDecorate, Operands = { target, decoration } };
            foreach (var arg in args) inst.Operands.Add(arg);
            AddInstruction(inst);
        }
        public void TypeVoid(uint resultId) => AddInstruction(new Instruction { Opcode = Opcode.OpTypeVoid, ResultId = resultId });
        public void TypeInt(uint resultId, uint width, uint signedness) => AddInstruction(new Instruction { Opcode = Opcode.OpTypeInt, ResultId = resultId, Operands = { width, signedness } });
        public void TypeFloat(uint resultId, uint width) => AddInstruction(new Instruction { Opcode = Opcode.OpTypeFloat, ResultId = resultId, Operands = { width } });
        public void TypeVector(uint resultId, uint componentType, uint componentCount) => AddInstruction(new Instruction { Opcode = Opcode.OpTypeVector, ResultId = resultId, Operands = { componentType, componentCount } });
        public void TypePointer(uint resultId, uint storageClass, uint pointeeType) => AddInstruction(new Instruction { Opcode = Opcode.OpTypePointer, ResultId = resultId, Operands = { storageClass, pointeeType } });
        public void TypeStruct(uint resultId, params uint[] memberTypes)
        {
            var inst = new Instruction { Opcode = Opcode.OpTypeStruct, ResultId = resultId };
            foreach (var mt in memberTypes) inst.Operands.Add(mt);
            AddInstruction(inst);
        }
        public void Constant(uint resultType, uint resultId, object value) => AddInstruction(new Instruction { Opcode = Opcode.OpConstant, ResultType = resultType, ResultId = resultId, Operands = { value } });
        public void ConstantTrue(uint resultType, uint resultId) => AddInstruction(new Instruction { Opcode = Opcode.OpConstantTrue, ResultType = resultType, ResultId = resultId });
        public void ConstantFalse(uint resultType, uint resultId) => AddInstruction(new Instruction { Opcode = Opcode.OpConstantFalse, ResultType = resultType, ResultId = resultId });
        public void Variable(uint resultType, uint resultId, uint storageClass, uint? initializer = null)
        {
            var inst = new Instruction { Opcode = Opcode.OpVariable, ResultType = resultType, ResultId = resultId, Operands = { storageClass } };
            if (initializer.HasValue) inst.Operands.Add(initializer.Value);
            AddInstruction(inst);
        }
        public void Load(uint resultType, uint resultId, uint pointer, params uint[] memoryOperands)
        {
            var inst = new Instruction { Opcode = Opcode.OpLoad, ResultType = resultType, ResultId = resultId, Operands = { pointer } };
            foreach (var m in memoryOperands) inst.Operands.Add(m);
            AddInstruction(inst);
        }
        public void Store(uint pointer, uint value, params uint[] memoryOperands)
        {
            var inst = new Instruction { Opcode = Opcode.OpStore, Operands = { pointer, value } };
            foreach (var m in memoryOperands) inst.Operands.Add(m);
            AddInstruction(inst);
        }
        public void Function(uint resultType, uint resultId, uint functionControl, uint functionType) => AddInstruction(new Instruction { Opcode = Opcode.OpFunction, ResultType = resultType, ResultId = resultId, Operands = { functionControl, functionType } });
        public void FunctionParameter(uint resultType, uint resultId) => AddInstruction(new Instruction { Opcode = Opcode.OpFunctionParameter, ResultType = resultType, ResultId = resultId });
        public void FunctionEnd() => AddInstruction(new Instruction { Opcode = Opcode.OpFunctionEnd });
        public void Return() => AddInstruction(new Instruction { Opcode = Opcode.OpReturn });
        public void ReturnValue(uint value) => AddInstruction(new Instruction { Opcode = Opcode.OpReturnValue, Operands = { value } });
        public void Label(uint resultId) => AddInstruction(new Instruction { Opcode = Opcode.OpLabel, ResultId = resultId });
        public void Branch(uint target) => AddInstruction(new Instruction { Opcode = Opcode.OpBranch, Operands = { target } });
        public void BranchConditional(uint condition, uint trueLabel, uint falseLabel) => AddInstruction(new Instruction { Opcode = Opcode.OpBranchConditional, Operands = { condition, trueLabel, falseLabel } });
        public void SelectionMerge(uint mergeBlock, uint selectionControl) => AddInstruction(new Instruction { Opcode = Opcode.OpSelectionMerge, Operands = { mergeBlock, selectionControl } });
        public void LoopMerge(uint mergeBlock, uint continueTarget, uint loopControl) => AddInstruction(new Instruction { Opcode = Opcode.OpLoopMerge, Operands = { mergeBlock, continueTarget, loopControl } });

        // Дополнительные методы для инструкций, используемых в генераторе
        public void Not(uint resultType, uint resultId, uint operand)
        {
            AddInstruction(new Instruction { Opcode = Opcode.OpNot, ResultType = resultType, ResultId = resultId, Operands = { operand } });
        }
        
        public void CompositeConstruct(uint resultType, uint resultId, params uint[] constituents)
        {
            var inst = new Instruction { Opcode = Opcode.OpCompositeConstruct, ResultType = resultType, ResultId = resultId };
            foreach (var c in constituents) inst.Operands.Add(c);
            AddInstruction(inst);
        }
        
        public void CompositeExtract(uint resultType, uint resultId, uint composite, uint index)
        {
            AddInstruction(new Instruction { Opcode = Opcode.OpCompositeExtract, ResultType = resultType, ResultId = resultId, Operands = { composite, index } });
        }
        
        public void AccessChain(uint resultType, uint resultId, uint basePtr, params uint[] indexes)
        {
            var inst = new Instruction { Opcode = Opcode.OpAccessChain, ResultType = resultType, ResultId = resultId, Operands = { basePtr } };
            foreach (var idx in indexes) inst.Operands.Add(idx);
            AddInstruction(inst);
        }
        
        public void Phi(uint resultType, uint resultId, List<(uint variable, uint parent)> pairs)
        {
            var inst = new Instruction { Opcode = Opcode.OpPhi, ResultType = resultType, ResultId = resultId };
            foreach (var (variable, parent) in pairs)
            {
                inst.Operands.Add(variable);
                inst.Operands.Add(parent);
            }
            AddInstruction(inst);
        }
        
        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Заголовок
            writer.Write(0x07230203U);          // Magic number
            writer.Write(Version);
            writer.Write(0U);                   // Generator magic (0 = unknown)
            writer.Write(Bound);
            writer.Write(0U);                   // Reserved

            // Инструкции
            foreach (var inst in Instructions)
            {
                uint wordCount = 1; // код операции
                if (inst.ResultType.HasValue) wordCount++;
                if (inst.ResultId.HasValue) wordCount++;
                wordCount += (uint)inst.Operands.Count;

                uint firstWord = (wordCount << 16) | (uint)inst.Opcode;
                writer.Write(firstWord);

                if (inst.ResultType.HasValue) writer.Write(inst.ResultType.Value);
                if (inst.ResultId.HasValue) writer.Write(inst.ResultId.Value);

                foreach (var operand in inst.Operands)
                {
                    switch (operand)
                    {
                        case uint u:
                            writer.Write(u);
                            break;
                        case int i:
                            writer.Write(i);
                            break;
                        case float f:
                            writer.Write(f);
                            break;
                        case string s:
                            byte[] bytes = Encoding.UTF8.GetBytes(s);
                            byte[] padded = new byte[(bytes.Length + 4) & ~3];
                            Array.Copy(bytes, padded, bytes.Length);
                            for (int i = 0; i < padded.Length; i += 4)
                                writer.Write(BitConverter.ToUInt32(padded, i));
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported operand type {operand.GetType()}");
                    }
                }
            }

            return ms.ToArray();
        }
    }
}