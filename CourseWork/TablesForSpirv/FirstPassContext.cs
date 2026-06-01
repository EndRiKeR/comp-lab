using CompLab.CourseWork.SpirV;

namespace comp_lab.CourseWork;

public class FirstPassContext
{
    public SymbolTable Symbols { get; } = new();
    public TypeCache Types { get; } = new();

    // Вспомогательные методы для добавления базовых типов
    public void RegisterBasicTypes()
    {
        var voidType = new VoidType();
        var boolType = new BoolType();
        var int32 = new IntType(32, true);
        var uint32 = new IntType(32, false);
        var float32 = new FloatType(32);

        Types.AddType(voidType);
        Types.AddType(boolType);
        Types.AddType(int32);
        Types.AddType(uint32);
        Types.AddType(float32);

        // также можно зарегистрировать их как именованные типы в символьной таблице?
        // В GLSL они не требуют явного объявления, поэтому пока не добавляем в Symbols.
    }
}