using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._2._AstToTables;

public class FirstPassContext
{
    public SymbolTable Symbols { get; } = new();
    public TypeCache Types { get; } = new();
    public Dictionary<SpirvType, string> PrecisionInfo { get; } = new();
    public TypeQualifierNode? CurrentTypeQualifiers { get; set; }
    public HashSet<(SpirvType type, object value)> RequiredConstants { get; } = new();

    private Stack<SpirvType?> _expressionTypes = new();
    
    public uint? LocalSizeX { get; set; }
    public uint? LocalSizeY { get; set; }
    public uint? LocalSizeZ { get; set; }


    public void SetLastExpressionType(SpirvType? type) 
    {
        _expressionTypes.Push(type);
    }
    
    public SpirvType? GetLastExpressionType() => _expressionTypes.Count > 0 ? _expressionTypes.Peek() : null;
    
    public void AddRequiredConstant(SpirvType type, object value) => RequiredConstants.Add((type, value));
}