using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._2._AstToTables;

public class TypeCache
{
    private readonly HashSet<SpirvType> _types = new();

    public void AddType(SpirvType type) => _types.Add(type);
    public bool Contains(SpirvType type) => _types.Contains(type);
    public IEnumerable<SpirvType> AllTypes => _types;
}