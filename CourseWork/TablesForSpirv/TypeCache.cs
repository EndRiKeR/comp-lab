using CompLab.CourseWork.SpirV;

namespace comp_lab.CourseWork;

public class TypeCache
{
    private readonly HashSet<SpirvType> _types = new();

    public void AddType(SpirvType type) => _types.Add(type);
    public bool Contains(SpirvType type) => _types.Contains(type);
    public IEnumerable<SpirvType> AllTypes => _types;
}