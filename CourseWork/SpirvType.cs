namespace CompLab.CourseWork.SpirV
{
    public abstract class SpirvType : IEquatable<SpirvType>
    {
        public abstract bool Equals(SpirvType? other);
        public override abstract int GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as SpirvType);
    }

    public class VoidType : SpirvType
    {
        public override bool Equals(SpirvType? other) => other is VoidType;
        public override int GetHashCode() => 0;
    }

    public class BoolType : SpirvType
    {
        public override bool Equals(SpirvType? other) => other is BoolType;
        public override int GetHashCode() => 1;
    }

    public class IntType : SpirvType
    {
        public int Width { get; }
        public bool Signed { get; }

        public IntType(int width, bool signed)
        {
            Width = width;
            Signed = signed;
        }

        public override bool Equals(SpirvType? other) =>
            other is IntType otherInt && Width == otherInt.Width && Signed == otherInt.Signed;

        public override int GetHashCode() => HashCode.Combine(Width, Signed);
    }

    public class FloatType : SpirvType
    {
        public int Width { get; }

        public FloatType(int width)
        {
            Width = width;
        }

        public override bool Equals(SpirvType? other) =>
            other is FloatType otherFloat && Width == otherFloat.Width;

        public override int GetHashCode() => Width.GetHashCode();
    }

    public class VectorType : SpirvType
    {
        public SpirvType ComponentType { get; }
        public int ComponentCount { get; }

        public VectorType(SpirvType componentType, int componentCount)
        {
            ComponentType = componentType;
            ComponentCount = componentCount;
        }

        public override bool Equals(SpirvType? other) =>
            other is VectorType otherVec &&
            ComponentCount == otherVec.ComponentCount &&
            ComponentType.Equals(otherVec.ComponentType);

        public override int GetHashCode() => HashCode.Combine(ComponentType, ComponentCount);
    }

    public class MatrixType : SpirvType
    {
        public VectorType ColumnType { get; }
        public int ColumnCount { get; }

        public MatrixType(VectorType columnType, int columnCount)
        {
            ColumnType = columnType;
            ColumnCount = columnCount;
        }

        public override bool Equals(SpirvType? other) =>
            other is MatrixType otherMat &&
            ColumnCount == otherMat.ColumnCount &&
            ColumnType.Equals(otherMat.ColumnType);

        public override int GetHashCode() => HashCode.Combine(ColumnType, ColumnCount);
    }

    public class ArrayType : SpirvType
    {
        public SpirvType ElementType { get; }
        public uint? Length { get; } // null = runtime array

        public ArrayType(SpirvType elementType, uint? length)
        {
            ElementType = elementType;
            Length = length;
        }

        public override bool Equals(SpirvType? other) =>
            other is ArrayType otherArr &&
            Length == otherArr.Length &&
            ElementType.Equals(otherArr.ElementType);

        public override int GetHashCode() => HashCode.Combine(ElementType, Length);
    }

    public class StructType : SpirvType
    {
        public IReadOnlyList<SpirvType> MemberTypes { get; }

        public StructType(IEnumerable<SpirvType> memberTypes)
        {
            MemberTypes = memberTypes.ToList().AsReadOnly();
        }

        public override bool Equals(SpirvType? other) =>
            other is StructType otherStruct &&
            MemberTypes.SequenceEqual(otherStruct.MemberTypes);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var t in MemberTypes)
                hash.Add(t);
            return hash.ToHashCode();
        }
    }

    public class PointerType : SpirvType
    {
        public StorageClass StorageClass { get; }
        public SpirvType PointeeType { get; }

        public PointerType(StorageClass storageClass, SpirvType pointeeType)
        {
            StorageClass = storageClass;
            PointeeType = pointeeType;
        }

        public override bool Equals(SpirvType? other) =>
            other is PointerType otherPtr &&
            StorageClass == otherPtr.StorageClass &&
            PointeeType.Equals(otherPtr.PointeeType);

        public override int GetHashCode() => HashCode.Combine(StorageClass, PointeeType);
    }

    // Добавим также SamplerType, ImageType, SampledImageType при необходимости
}