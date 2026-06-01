namespace CompLab.CourseWork.SpirV
{
    public enum Opcode : ushort
    {
        OpNop = 0,
        OpUndef = 1,
        OpSourceContinued = 2,
        OpSource = 3,
        OpSourceExtension = 4,
        OpName = 5,
        OpMemberName = 6,
        OpString = 7,
        OpLine = 8,
        OpNoLine = 9,
        OpModuleProcessed = 10,
        OpDecorate = 11,
        OpMemberDecorate = 12,
        // OpGroupDecorate = 13,
        // OpGroupMemberDecorate = 14,
        OpExtension = 15,
        OpExtInstImport = 16,
        OpExtInst = 17,
        OpExtInstWithForwardRefsKHR = 18,
        OpMemoryModel = 19,
        OpEntryPoint = 20,
        OpExecutionMode = 21,
        OpCapability = 22,
        OpTypeVoid = 23,
        OpTypeBool = 24,
        OpTypeInt = 25,
        OpTypeFloat = 26,
        OpTypeVector = 27,
        OpTypeMatrix = 28,
        OpTypeImage = 29,
        OpTypeSampler = 30,
        OpTypeSampledImage = 31,
        OpTypeArray = 32,
        OpTypeRuntimeArray = 33,
        OpTypeStruct = 34,
        OpTypeOpaque = 35,
        OpTypePointer = 36,
        OpTypeFunction = 37,
        OpTypeEvent = 38,
        OpTypeDeviceEvent = 39,
        OpTypeReserveId = 40,
        OpTypeQueue = 41,
        OpTypePipe = 42,
        OpTypeForwardPointer = 43,
        OpConstantTrue = 44,
        OpConstantFalse = 45,
        OpConstant = 46,
        OpConstantComposite = 47,
        OpConstantSampler = 48,
        OpConstantNull = 49,
        OpSpecConstantTrue = 50,
        OpSpecConstantFalse = 51,
        OpSpecConstant = 52,
        OpSpecConstantComposite = 53,
        OpSpecConstantOp = 54,
        OpFunction = 55,
        OpFunctionParameter = 56,
        OpFunctionEnd = 57,
        OpFunctionCall = 58,
        OpVariable = 59,
        OpImageTexelPointer = 60,
        OpLoad = 61,
        OpStore = 62,
        OpCopyMemory = 63,
        OpCopyMemorySized = 64,
        OpAccessChain = 65,
        OpInBoundsAccessChain = 66,
        OpPtrAccessChain = 67,
        OpArrayLength = 68,
        OpGenericPtrMemSemantics = 69,
        OpInBoundsPtrAccessChain = 70,
        OpDecorateId = 71,
        OpMemberDecorateId = 72,
        OpGroupMemberDecorate = 73,
        OpGroupDecorate = 74,
        OpDecorateString = 75,
        OpMemberDecorateString = 76,
        OpExecutionModeId = 77,
        OpLabel = 78,
        OpBranch = 79,
        OpBranchConditional = 80,
        OpSwitch = 81,
        OpKill = 82,
        OpReturn = 83,
        OpReturnValue = 84,
        OpUnreachable = 85,
        OpLifetimeStart = 86,
        OpLifetimeStop = 87,
        OpSelectionMerge = 88,
        OpLoopMerge = 89,
        OpPhi = 90,
        OpDPdx = 91,
        OpDPdy = 92,
        OpFwidth = 93,
        OpDPdxFine = 94,
        OpDPdyFine = 95,
        OpFwidthFine = 96,
        OpDPdxCoarse = 97,
        OpDPdyCoarse = 98,
        OpFwidthCoarse = 99,
        OpEmitVertex = 100,
        OpEndPrimitive = 101,
        OpEmitStreamVertex = 102,
        OpEndStreamPrimitive = 103,
        OpControlBarrier = 104,
        OpMemoryBarrier = 105,
        OpAtomicLoad = 106,
        OpAtomicStore = 107,
        OpAtomicExchange = 108,
        OpAtomicCompareExchange = 109,
        OpAtomicCompareExchangeWeak = 110,
        OpAtomicIIncrement = 111,
        OpAtomicIDecrement = 112,
        OpAtomicIAdd = 113,
        OpAtomicISub = 114,
        OpAtomicSMin = 115,
        OpAtomicUMin = 116,
        OpAtomicSMax = 117,
        OpAtomicUMax = 118,
        OpAtomicAnd = 119,
        OpAtomicOr = 120,
        OpAtomicXor = 121,
        OpImageSampleImplicitLod = 122,
        OpImageSampleExplicitLod = 123,
        OpImageSampleDrefImplicitLod = 124,
        OpImageSampleDrefExplicitLod = 125,
        OpImageSampleProjImplicitLod = 126,
        OpImageSampleProjExplicitLod = 127,
        OpImageSampleProjDrefImplicitLod = 128,
        OpImageSampleProjDrefExplicitLod = 129,
        OpImageFetch = 130,
        OpImageGather = 131,
        OpImageDrefGather = 132,
        OpImageRead = 133,
        OpImageWrite = 134,
        OpImage = 135,
        OpImageQueryFormat = 136,
        OpImageQueryOrder = 137,
        OpImageQuerySizeLod = 138,
        OpImageQuerySize = 139,
        OpImageQueryLod = 140,
        OpImageQueryLevels = 141,
        OpImageQuerySamples = 142,
        OpConvertFToU = 143,
        OpConvertFToS = 144,
        OpConvertSToF = 145,
        OpConvertUToF = 146,
        OpUConvert = 147,
        OpSConvert = 148,
        OpFConvert = 149,
        OpQuantizeToF16 = 150,
        OpConvertPtrToU = 151,
        OpSatConvertSToU = 152,
        OpSatConvertUToS = 153,
        OpConvertUToPtr = 154,
        OpPtrCastToGeneric = 155,
        OpGenericCastToPtr = 156,
        OpGenericCastToPtrExplicit = 157,
        OpBitcast = 158,
        OpSNegate = 159,
        OpFNegate = 160,
        OpIAdd = 161,
        OpFAdd = 162,
        OpISub = 163,
        OpFSub = 164,
        OpIMul = 165,
        OpFMul = 166,
        OpUDiv = 167,
        OpSDiv = 168,
        OpFDiv = 169,
        OpUMod = 170,
        OpSRem = 171,
        OpSMod = 172,
        OpFRem = 173,
        OpFMod = 174,
        OpVectorTimesScalar = 175,
        OpMatrixTimesScalar = 176,
        OpVectorTimesMatrix = 177,
        OpMatrixTimesVector = 178,
        OpMatrixTimesMatrix = 179,
        OpOuterProduct = 180,
        OpDot = 181,
        OpIAddCarry = 182,
        OpISubBorrow = 183,
        OpUMulExtended = 184,
        OpSMulExtended = 185,
        OpAny = 186,
        OpAll = 187,
        OpIsNan = 188,
        OpIsInf = 189,
        OpIsFinite = 190,
        OpIsNormal = 191,
        OpSignBitSet = 192,
        OpLessOrGreater = 193,
        OpOrdered = 194,
        OpUnordered = 195,
        OpLogicalEqual = 196,
        OpLogicalNotEqual = 197,
        OpLogicalOr = 198,
        OpLogicalAnd = 199,
        OpLogicalNot = 200,
        OpSelect = 201,
        OpIEqual = 202,
        OpINotEqual = 203,
        OpUGreaterThan = 204,
        OpSGreaterThan = 205,
        OpUGreaterThanEqual = 206,
        OpSGreaterThanEqual = 207,
        OpULessThan = 208,
        OpSLessThan = 209,
        OpULessThanEqual = 210,
        OpSLessThanEqual = 211,
        OpFOrdEqual = 212,
        OpFUnordEqual = 213,
        OpFOrdNotEqual = 214,
        OpFUnordNotEqual = 215,
        OpFOrdLessThan = 216,
        OpFUnordLessThan = 217,
        OpFOrdGreaterThan = 218,
        OpFUnordGreaterThan = 219,
        OpFOrdLessThanEqual = 220,
        OpFUnordLessThanEqual = 221,
        OpFOrdGreaterThanEqual = 222,
        OpFUnordGreaterThanEqual = 223,
        OpShiftRightLogical = 224,
        OpShiftRightArithmetic = 225,
        OpShiftLeftLogical = 226,
        OpBitwiseOr = 227,
        OpBitwiseXor = 228,
        OpBitwiseAnd = 229,
        OpNot = 230,
        OpBitFieldInsert = 231,
        OpBitFieldSExtract = 232,
        OpBitFieldUExtract = 233,
        OpBitReverse = 234,
        OpBitCount = 235
    }
    
    public class Instruction
    {
        public Opcode Opcode { get; set; }
        public uint? ResultType { get; set; }
        public uint? ResultId { get; set; }
        public List<object> Operands { get; } = new();
    }
    
    public class SpirvModule
    {
        public uint Version { get; set; } = 0x00010600; // SPIR-V 1.6
        public uint Bound { get; set; }
        public List<Instruction> Instructions { get; } = new();
        private uint _nextId = 1;
        
        public uint GetNextId() => _nextId++;
        
        public uint GetCurrentBound() => _nextId;
        
        public void SetBound() => Bound = GetCurrentBound();
        
        public void AddInstruction(Instruction inst)
        {
            Instructions.Add(inst);
        }
        
        // Удобные фабричные методы:
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
        // ... добавляйте по необходимости
    }
}