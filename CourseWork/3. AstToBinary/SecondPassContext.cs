using comp_lab.CourseWork._2._AstToTables;
using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._3._AstToBinary
{
    public class SecondPassContext
    {
        public SpirvModule Module { get; } = new();
        public SymbolTable Symbols { get; }
        public TypeCache Types { get; }
        public FirstPassContext FirstPass { get; }
        public Dictionary<SpirvType, uint> TypeIdMap { get; } = new();
        public Dictionary<object, uint> ConstantIdMap { get; } = new();
        public Dictionary<string, uint> ImportedSetIds { get; } = new();

        public uint? CurrentFunction { get; set; }
        public uint? CurrentBlock { get; set; }
        public uint? CurrentMergeLabel { get; set; }
        public uint? CurrentContinueTarget { get; set; }

        private readonly Stack<Dictionary<string, SymbolInfo>> _localScopes = new();
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

        public SecondPassContext(FirstPassContext firstPass)
        {
            FirstPass = firstPass;
            Symbols = firstPass.Symbols;
            Types = firstPass.Types;
        }

        public uint MapType(SpirvType type)
        {
            if (TypeIdMap.TryGetValue(type, out var id))
                return id;
            id = Module.GetNextId();
            TypeIdMap[type] = id;
            return id;
        }

        public uint GetConstantId(SpirvType type, object value)
        {
            var key = (MapType(type), value);
            if (ConstantIdMap.TryGetValue(key, out var id))
                return id;
            id = Module.GetNextId();
            ConstantIdMap[key] = id;
            if (value is int i)
                Module.Constant(MapType(type), id, i);
            else if (value is uint u)
                Module.Constant(MapType(type), id, u);
            else if (value is float f)
                Module.Constant(MapType(type), id, f);
            else if (value is bool b)
            {
                if (b) Module.ConstantTrue(MapType(type), id);
                else Module.ConstantFalse(MapType(type), id);
            }
            else throw new NotSupportedException($"Constant type {value.GetType()}");
            return id;
        }
    }
}