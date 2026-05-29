namespace comp_lab.CourseWork;

public class TypeSpecifier : AstNode
{
    public string TypeName { get; }           // "int", "float", "vec4", "MyStruct"
    public bool IsArray { get; }
    public int? ArraySize { get; }            // null - массив без размера, иначе конкретный размер
    public TypeSpecifier(string name, bool isArray = false, int? arraySize = null)
    {
        TypeName = name;
        IsArray = isArray;
        ArraySize = arraySize;
    }
    public string ToString(int indent) => 
        $"{new string(' ', indent)}Type: {TypeName}" + (IsArray ? (ArraySize.HasValue ? $"[{ArraySize}]" : "[]") : "");
}