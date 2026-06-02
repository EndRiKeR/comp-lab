using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._2._AstToTables;

public class SymbolTable
{
    private readonly Stack<Dictionary<string, SymbolInfo>> _scopes = new();
    private readonly Dictionary<string, SymbolInfo> _global = new();
    private int _depth = 0;

    public void EnterScope()
    {
        _scopes.Push(new Dictionary<string, SymbolInfo>());
        _depth++;
    }

    public void ExitScope()
    {
        _depth--;
    }
    
    public bool IsInsideFunction() => _depth > 0;

    public void AddSymbol(string name, SymbolInfo info, bool local = false)
    {
        if (local)
        {
            if (_scopes.Count == 0)
                throw new InvalidOperationException("No local scope active");
            
            _scopes.Peek()[name] = info;
        }
        else
        {
            _global[name] = info;
        }
    }

    public bool TryLookup(string name, out SymbolInfo? info)
    {
        for (int i = 1; i <= _scopes.Count && i <= _depth; i++)
        {
            if (_scopes.ToArray()[^i].TryGetValue(name, out info))
                return true;
        }
        
        return _global.TryGetValue(name, out info);
    }

    public SymbolInfo? Lookup(string name) => TryLookup(name, out var info) ? info : null;
    
    public IEnumerable<KeyValuePair<string, SymbolInfo>> GetGlobalSymbols() => _global;
}