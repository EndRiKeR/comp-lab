namespace comp_lab.CourseWork.Common;

public class SymbolInfo
{
    public SymbolKind Kind { get; }
    public SpirvType Type { get; }           // для переменной или типа; для функции — возвращаемый тип
    public StorageClass? StorageClass { get; } // для переменных
    public List<SpirvType>? ParameterTypes { get; } // для функций
    public uint? Id { get; set; }              // заполняется во втором проходе

    public SymbolInfo(SymbolKind kind, SpirvType type, StorageClass? storageClass = null, List<SpirvType>? paramTypes = null)
    {
        Kind = kind;
        Type = type;
        StorageClass = storageClass;
        ParameterTypes = paramTypes;
    }
}