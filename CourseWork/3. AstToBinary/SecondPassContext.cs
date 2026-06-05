using comp_lab.CourseWork._2._AstToTables;
using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._3._AstToBinary
{
    public class SecondPassContext
    {
        public SymbolTable Symbols { get; }
        public TypeCache Types { get; }
        public FirstPassContext FirstPass { get; }
        public Dictionary<SpirvType, uint> TypeIdMap { get; } = new();
        public Dictionary<object, uint> ConstantIdMap { get; } = new();
        public Dictionary<string, uint> ImportedSetIds { get; } = new();
        public readonly List<SpirvType> PendingTypes = new();

        public uint? CurrentFunction { get; set; }
        public uint? CurrentBlock { get; set; }
        public uint? CurrentMergeLabel { get; set; }
        public uint? CurrentContinueTarget { get; set; }

        private readonly Stack<Dictionary<string, SymbolInfo>> _localScopes = new();
        private readonly List<(SpirvType type, uint constId, object value)> _pendingConstants = new();
        private SpirvModuleWorker _spirvModuleWorker;
        
        
        public SecondPassContext(FirstPassContext firstPass, SpirvModuleWorker moduleWorker)
        {
            _spirvModuleWorker = moduleWorker;
            FirstPass = firstPass;
            Symbols = firstPass.Symbols;
            Types = firstPass.Types;
        }
        
        public void EnterLocalScope() => _localScopes.Push(new Dictionary<string, SymbolInfo>());
        public void ExitLocalScope() => _localScopes.Pop();
        public void AddLocalSymbol(string name, SymbolInfo info) => _localScopes.Peek()[name] = info;
        public SymbolInfo? LookupLocal(string name)
        {
            foreach (var scope in _localScopes)
                if (scope.TryGetValue(name, out var info))
                    return info;
            return null;
        }
        public SymbolInfo? Lookup(string name) => LookupLocal(name) ?? Symbols.Lookup(name);
        
        public uint MapType(SpirvType type)
        {
            if (TypeIdMap.TryGetValue(type, out var id))
                return id;
            id = _spirvModuleWorker.GetNextId();
            TypeIdMap[type] = id;
            PendingTypes.Add(type);
            return id;
        }

        public uint GetConstantId(SpirvType type, object value)
        {
            var typeId = MapType(type);
            object keyValue = value is bool b ? (b ? 1u : 0u) : value;
            var key = (typeId, keyValue);

            if (ConstantIdMap.TryGetValue(key, out var existingId))
                return existingId;

            var constId = _spirvModuleWorker.GetNextId();
            ConstantIdMap[key] = constId;
            _pendingConstants.Add((type, constId, value));
            return constId;
        }
        
        public void EmitPendingConstants()
        {
            foreach (var (type, constId, value) in _pendingConstants)
            {
                var typeId = MapType(type);
                if (type is BoolType && (bool)value)
                {
                    _spirvModuleWorker.AddConstantTrue(typeId, constId);
                }
                else if (type is BoolType && !(bool)value)
                {
                    _spirvModuleWorker.AddConstantFalse(typeId, constId);
                }
                else
                {
                    uint operand = ConvertConstantValue(type, value);
                    _spirvModuleWorker.AddConstant(typeId, constId, operand);
                }
            }
            _pendingConstants.Clear();
        }
        
        private static uint ConvertConstantValue(SpirvType type, object value)
        {
            if (value is int i) return unchecked((uint)i);
            if (value is uint u) return u;
            if (value is float f) return BitConverter.SingleToUInt32Bits(f);
            if (value is bool b) return b ? 1u : 0u;
            if (value is double d)
            {
                // SPIR-V 64-битные константы передаются как два 32-битных слова
                // Здесь предполагается, что ваш Module.Constant поддерживает это, иначе нужно уточнить
                // Пока вернём младшие 32 бита – уточните реализацию под ваш Module
                ulong bits = BitConverter.DoubleToUInt64Bits(d);
                return (uint)(bits & 0xFFFFFFFF); // или обработайте иначе
            }
            throw new NotSupportedException($"Unsupported constant value type: {value.GetType()}");
        }
    }
}