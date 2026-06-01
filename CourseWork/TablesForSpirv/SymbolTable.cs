namespace comp_lab.CourseWork;

public class SymbolTable
{
    private readonly Stack<Dictionary<string, SymbolInfo>> _scopes = new();
    private readonly Dictionary<string, SymbolInfo> _global = new();

    public void EnterScope() => _scopes.Push(new Dictionary<string, SymbolInfo>());
    public void ExitScope() => _scopes.Pop();

    public void AddSymbol(string name, SymbolInfo info, bool local = false)
    {
        if (local)
        {
            if (_scopes.Count == 0) throw new InvalidOperationException("No local scope active");
            _scopes.Peek()[name] = info;
        }
        else
        {
            _global[name] = info;
        }
    }

    public bool TryLookup(string name, out SymbolInfo? info)
    {
        // ищем в локальных областях (от текущей наружу)
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out info))
                return true;
        }
        // затем в глобальной
        return _global.TryGetValue(name, out info);
    }

    public SymbolInfo? Lookup(string name) => TryLookup(name, out var info) ? info : null;

    // Для удобства получения глобальных символов (например, для функций)
    public IEnumerable<KeyValuePair<string, SymbolInfo>> GetGlobalSymbols() => _global;
}