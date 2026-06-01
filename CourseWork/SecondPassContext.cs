using comp_lab.CourseWork;
using CompLab.CourseWork.SpirV;

public class SecondPassContext
{
    public SpirvModule Module { get; } = new();
    public SymbolTable Symbols { get; }
    public TypeCache Types { get; }
    public Dictionary<SpirvType, uint> TypeIdMap { get; } = new();
    public Dictionary<object, uint> ConstantIdMap { get; } = new();
    public Dictionary<string, uint> ImportedSetIds { get; } = new();

    // Текущая функция и блок
    public uint? CurrentFunction { get; set; }
    public uint? CurrentBlock { get; set; }
    public uint? CurrentMergeLabel { get; set; }
    public uint? CurrentContinueTarget { get; set; }

    // Для локальных символов (переменные Function storage class)
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

    public SecondPassContext(SymbolTable symbols, TypeCache types)
    {
        Symbols = symbols;
        Types = types;
    }

    public uint MapType(SpirvType type)
    {
        if (TypeIdMap.TryGetValue(type, out var id))
            return id;
        id = Module.GetNextId();
        TypeIdMap[type] = id;
        // Отложенная генерация OpType... будет выполнена перед инструкциями функций
        return id;
    }
}