using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._2._AstToTables;

public class FirstPassContext
{
    public SymbolTable Symbols { get; } = new();
    public TypeCache Types { get; } = new();
    public Dictionary<SpirvType, string> PrecisionInfo { get; } = new();
    public TypeQualifierNode? CurrentTypeQualifiers { get; set; }

    private Stack<SpirvType?> _expressionTypes = new();


    public void SetLastExpressionType(SpirvType? type) 
    {
        _expressionTypes.Push(type);
    }
    
    public SpirvType? GetLastExpressionType() => _expressionTypes.Count > 0 ? _expressionTypes.Peek() : null;
}