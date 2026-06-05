using System.Numerics;
using System.Text;
using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._3._AstToBinary
{
    public class SpirvModuleWorker
    {
        private SpirvModule _module;

        public SpirvModuleWorker(SpirvModule module)
        {
            _module = module;
        }
        
        public uint GetNextId() => _module.GetNextId();
        
        public void AddFunctionInstruction(Instruction instruction)
        {
            _module.AddFunctionInstruction(instruction);
        }

        public int FunctionInstructionCount()
        {
            return _module.CountFunctionInstruction();
        }
        
        public Instruction LastFunctionInstruction()
        {
            return _module.LastFunctionInstruction();
        }

        // ~~~~~~~~~~~~~~~~~~~
        // Header Instructions
        // ~~~~~~~~~~~~~~~~~~~
        
        public void AddCapability(uint cap)
        {
            // OpCapability Shader
            var instruction = new Instruction
            {
                Opcode = Opcode.OpCapability,
                Operands = { cap }
            };
            
            _module.AddHeaderInstruction(instruction);
        }

        public void AddExtInstImport(uint resultId, string name)
        {
            // OpExtInstImport "GLSL.std.450"
            var instruction = new Instruction
            {
                Opcode = Opcode.OpExtInstImport,
                ResultId = resultId,
                Operands = { name }
            };
            
            _module.AddHeaderInstruction(instruction);
        }

        public void AddMemoryModel(uint addressingModel, uint memoryModel)
        {
            // OpMemoryModel Logical GLSL450
            var instruction = new Instruction
            {
                Opcode = Opcode.OpMemoryModel,
                Operands = { addressingModel, memoryModel }
            };
            
            _module.AddHeaderInstruction(instruction);
        }
        
        public void AddEntryPoint(uint execModel, uint entryPointId, string entryPointName, params uint[] interfaceIds)
        {
            // OpEntryPoint GLCompute %6 "main" %7
            var instruction = new Instruction
            {
                Opcode = Opcode.OpEntryPoint,
                Operands = { execModel, entryPointId, entryPointName }
            };
            foreach (var id in interfaceIds)
                instruction.Operands.Add(id);
            _module.AddHeaderInstruction(instruction);
        }
        
        public void AddExecutionMode(uint mainId)
        {
            // OpExecutionMode %???
            var instruction = new Instruction
            {
                Opcode = Opcode.OpExecutionMode,
                Operands = { mainId, 7u }
            };
            
            _module.AddHeaderInstruction(instruction);
        }
        
        public void AddExecutionMode(uint mainId, uint localSizeX, uint localSizeY, uint localSizeZ)
        {
            // OpExecutionMode %6 LocalSize 64 1 1
            var instruction = new Instruction
            {
                Opcode = Opcode.OpExecutionMode,
                Operands = { mainId, 17u, localSizeX, localSizeY, localSizeZ }
            };
            
            _module.AddHeaderInstruction(instruction);
        }
        
        // ~~~~~~~~~~~~~~~~~~~
        // Debug Instructions
        // ~~~~~~~~~~~~~~~~~~~
        
        // public void AddExecutionMode(uint mainId, uint localSizeX, uint localSizeY, uint localSizeZ)
        // {
        //     // OpExecutionMode %6 LocalSize 64 1 1
        //     var instruction = new Instruction
        //     {
        //         Opcode = Opcode.OpExecutionMode,
        //         Operands = { mainId, 17u, localSizeX, localSizeY, localSizeZ }
        //     };
        //     
        //     Module.AddHeaderInstruction(instruction);
        // }
        
        //        public void Name(uint target, string name)
        // => Module.AddInstruction(new Instruction
        // { Opcode = Opcode.OpName, Operands = { target, name } });
        
        public void AddMemberName(uint target, uint member, string name)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpMemberName,
                Operands = { target, member, name }
            };
    
            _module.AddDebugInstruction(instruction);
        }
        
        // ~~~~~~~~~~~~~~~~~~~
        // Annotations Instructions
        // ~~~~~~~~~~~~~~~~~~~
        
        public void AddDecorate(uint target, uint decoration, params object[] args)
        {
            // OpDecorate %7 DescriptorSet 0
            // или
            // OpDecorate %7 Binding 0
            var instruction = new Instruction
            {
                Opcode = Opcode.OpDecorate,
                Operands = { target, decoration }
            };
            
            foreach (var a in args)
                instruction.Operands.Add(a);
            
            _module.AddAnnotationInstruction(instruction);
        }
        
        public void AddMemberDecorate(uint structType, uint member, uint decoration, params object[] args)
        {
            // OpMemberDecorate %17 1 Offset 16
            var instruction = new Instruction
            {
                Opcode = Opcode.OpMemberDecorate,
                Operands = { structType, member, decoration }
            };
            
            foreach (var a in args)
                instruction.Operands.Add(a);
            
            _module.AddAnnotationInstruction(instruction);
        }
        
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Types, variables and constants Instructions
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        
        public void AddTypeVoid(uint resultId)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeVoid,
                ResultId = resultId
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }
        
        public void AddTypeBool(uint resultId)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeBool,
                ResultId = resultId
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }

        public void AddTypeInt(uint resultId, uint width, uint signedness)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeInt,
                ResultId = resultId,
                Operands = { width, signedness }
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }

        public void AddTypeFloat(uint resultId, uint width)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeFloat,
                ResultId = resultId,
                Operands = { width }
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }

        public void AddTypeVector(uint resultId, uint componentType, uint componentCount)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeVector,
                ResultId = resultId,
                Operands = { componentType, componentCount }
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }

        public void AddTypePointer(uint resultId, uint storageClass, uint pointeeType)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypePointer,
                ResultId = resultId,
                Operands = { storageClass, pointeeType }
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }

        public void AddTypeStruct(uint resultId, params uint[] memberTypes)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeStruct,
                ResultId = resultId
            };
            
            foreach (var mt in memberTypes)
                instruction.Operands.Add(mt);
            
            _module.AddTypeAndVariableInstruction(instruction);
        }
        
        public void AddTypeMatrix(uint resultId, uint columnType, uint columnCount)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeMatrix,
                ResultId = resultId,
                Operands = { columnType, columnCount }
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }
        
        public void AddTypeArray(uint resultId, uint elementType, uint length)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeArray,
                ResultId = resultId,
                Operands = { elementType, length }
            };
    
            _module.AddTypeAndVariableInstruction(instruction);
        }

        public void AddTypeRuntimeArray(uint resultId, uint elementType)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeRuntimeArray,
                ResultId = resultId,
                Operands = { elementType }
            };
    
            _module.AddTypeAndVariableInstruction(instruction);
        }
        
        public void AddConstant(uint resultType, uint resultId, object value)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpConstant,
                ResultType = resultType,
                ResultId = resultId,
                Operands = { value }
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }
        
        public void AddConstantTrue(uint resultType, uint resultId)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpConstantTrue,
                ResultType = resultType,
                ResultId = resultId
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }
        
        public void AddConstantFalse(uint resultType, uint resultId)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpConstantFalse,
                ResultType = resultType,
                ResultId = resultId
            };
            
            _module.AddTypeAndVariableInstruction(instruction);
        }

        public void AddNonFunctionVariable(uint resultType, uint resultId, uint storageClass, uint? initializer = null)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpVariable,
                ResultType = resultType,
                ResultId = resultId,
                Operands = { storageClass }
            };
            
            if (initializer.HasValue) 
                instruction.Operands.Add(initializer.Value);
            
            _module.AddTypeAndVariableInstruction(instruction);
        }
        
        public void AddConstantComposite(uint resultTypeId, uint compositeId, List<uint> constituents)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpConstantComposite,
                ResultType = resultTypeId,
                ResultId = compositeId
            };
            
            foreach (var c in constituents)
                instruction.Operands.Add(c);
            
            _module.AddFunctionInstruction(instruction);
        }
        
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Function Instructions
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        
        public void AddFunctionVariable(uint resultType, uint resultId, uint storageClass, uint? initializer = null)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpVariable,
                ResultType = resultType,
                ResultId = resultId,
                Operands = { storageClass }
            };
            
            if (initializer.HasValue) 
                instruction.Operands.Add(initializer.Value);
            
            _module.AddFunctionInstruction(instruction);
        }
        
        public void AddLoad(uint resultType, uint resultId, uint pointer, params uint[] memOps)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpLoad,
                ResultType = resultType,
                ResultId = resultId,
                Operands = { pointer }
            };
            
            foreach (var m in memOps)
                instruction.Operands.Add(m);
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddStore(uint pointer, uint value, params uint[] memOps)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpStore,
                Operands = { pointer, value }
            };
            
            foreach (var m in memOps)
                instruction.Operands.Add(m);
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddFunction(uint resultType, uint resultId, uint functionControl, uint functionType)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpFunction,
                ResultType = resultType,
                ResultId = resultId,
                Operands = { functionControl, functionType }
            };
            
            _module.AddFunctionInstruction(instruction);
        }
        
        public void AddTypeFunction(uint resultId, uint returnTypeId, uint[] paramTypeIds)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTypeFunction,
                ResultId = resultId,
                Operands = { returnTypeId }
            };
            
            foreach (var pt in paramTypeIds)
                instruction.Operands.Add(pt);
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddFunctionParameter(uint resultType, uint resultId)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpFunctionParameter,
                ResultType = resultType,
                ResultId = resultId
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddFunctionEnd()
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpFunctionEnd
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddReturn()
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpReturn
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddReturnValue(uint value)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpReturnValue,
                Operands = { value }
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddLabel(uint resultId)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpLabel,
                ResultId = resultId
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddBranch(uint target)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpBranch,
                Operands = { target }
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddBranchConditional(uint condition, uint trueLabel, uint falseLabel)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpBranchConditional,
                Operands = { condition, trueLabel, falseLabel }
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddSelectionMerge(uint mergeBlock, uint selectionControl)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpSelectionMerge,
                Operands = { mergeBlock, selectionControl }
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddLoopMerge(uint mergeBlock, uint continueTarget, uint loopControl)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpLoopMerge,
                 Operands = { mergeBlock, continueTarget, loopControl }
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddCompositeConstruct(uint resultType, uint resultId, params uint[] constituents)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpCompositeConstruct,
                ResultType = resultType,
                ResultId = resultId
            };
            
            foreach (var c in constituents)
                instruction.Operands.Add(c);
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddCompositeExtract(uint resultType, uint resultId, uint composite, uint index)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpCompositeExtract,
                ResultType = resultType,
                ResultId = resultId,
                Operands = { composite, index }
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddAccessChain(uint resultType, uint resultId, uint basePtr, uint[] indexes)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpAccessChain,
                ResultType = resultType,
                ResultId = resultId,
                Operands = { basePtr }
            };
            
            foreach (var idx in indexes)
                instruction.Operands.Add(idx);
            
            _module.AddFunctionInstruction(instruction);
        }
        
        public void AddTerminateInvocation()
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpTerminateInvocation,
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddNot(uint resultType, uint resultId, uint operand)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpNot,
                ResultType = resultType,
                ResultId = resultId,
                Operands = { operand }
            };
            
            _module.AddFunctionInstruction(instruction);
        }

        public void AddPhi(uint resultType, uint resultId, List<(uint variable, uint parent)> pairs)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpPhi,
                ResultType = resultType,
                ResultId = resultId
            };
            
            foreach (var (v, p) in pairs)
            {
                instruction.Operands.Add(v);
                instruction.Operands.Add(p);
            }
            
            _module.AddFunctionInstruction(instruction);
        }
        
        public void AddSelect(uint resultType, uint resultId, uint condVal, uint trueVal, uint falseVal)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpSelect,
                ResultType = resultType,
                ResultId = resultId,
                Operands = { condVal, trueVal, falseVal }
            };
            
            _module.AddFunctionInstruction(instruction);
        }
        
        public void AddFunctionCall(uint retTypeId, uint callId, uint funcId, params uint[] args)
        {
            var instruction = new Instruction
            {
                Opcode = Opcode.OpFunctionCall,
                ResultType = retTypeId,
                ResultId = callId,
                Operands = { funcId }
            };
            
            foreach (var arg in args)
                instruction.Operands.Add(arg);
            
            _module.AddFunctionInstruction(instruction);
        }
        
        // ── Serialiser ───────────────────────────────────────────────────
        // SPIR-V binary layout: each instruction is one or more 32-bit words.
        // The first word encodes [WordCount(16) | Opcode(16)].
        // All string operands must be null-terminated and padded to 4-byte boundary.
        public byte[] Serialize()
        {
            using var ms     = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            // ── Module header ────────────────────────────────────────────
            writer.Write(0x07230203U); // magic
            writer.Write(_module.Version);
            writer.Write(0U);          // generator magic
            writer.Write(_module.Bound);
            writer.Write(0U);          // schema (reserved)

            // ── Instructions ─────────────────────────────────────────────
            foreach (var inst in _module.GetInstructions())
            {
                // Calculate word count correctly (strings count as multiple words).
                uint wordCount = 1; // the opcode word itself
                
                if (inst.ResultType.HasValue)
                    wordCount++;
                
                if (inst.ResultId.HasValue)  
                    wordCount++;
                
                foreach (var op in inst.Operands)
                    wordCount += op is string s ? StringWordCount(s) : 1u;

                // First word: WordCount in upper 16 bits, opcode in lower 16 bits.
                writer.Write((wordCount << 16) | (ushort)inst.Opcode);

                if (inst.ResultType.HasValue)
                    writer.Write(inst.ResultType.Value);
                
                if (inst.ResultId.HasValue)  
                    writer.Write(inst.ResultId.Value);

                foreach (var operand in inst.Operands)
                {
                    switch (operand)
                    {
                        case uint  u: writer.Write(u); break;
                        case int   i: writer.Write(i); break;
                        case float f: writer.Write(f); break;
                        case string str: WriteString(writer, str); break;
                        default:
                            throw new InvalidOperationException(
                                $"Unsupported operand type: {operand.GetType().Name}");
                    }
                }
            }

            return ms.ToArray();
        }

        // ── String helpers ────────────────────────────────────────────────
        // SPIR-V string: UTF-8 bytes, null-terminated, padded to 4-byte boundary.
        private static uint StringWordCount(string s)
        {
            int byteLen = Encoding.UTF8.GetByteCount(s) + 1; // +1 for null terminator
            return (uint)((byteLen + 3) / 4);                // ceil to 4-byte words
        }

        private static void WriteString(BinaryWriter writer, string s)
        {
            byte[] bytes  = Encoding.UTF8.GetBytes(s);
            int    total  = ((bytes.Length + 1 + 3) / 4) * 4; // null-terminated, padded
            byte[] padded = new byte[total];
            Array.Copy(bytes, padded, bytes.Length);
            // remaining bytes are already 0 (null terminator + padding)
            for (int i = 0; i < padded.Length; i += 4)
                writer.Write(BitConverter.ToUInt32(padded, i));
        }
    }
}