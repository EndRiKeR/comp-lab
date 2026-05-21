namespace comp_lab.Labs.lab2.structs;

public enum TokenKind
{
    Identifier,
    Keyword,
    IntConstant,
    UIntConstant,
    FloatConstant,
    DoubleConstant,
    Operator,
    Separator,
    // можно добавить специфические типы для каждого оператора, но не обязательно
}

public class GrammarPart
{
    public readonly string Name;

    public GrammarPart(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GrammarPart other)
            return false;
        return Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public static bool operator ==(GrammarPart? first, GrammarPart? second)
    {
        if (ReferenceEquals(first, second))
            return true;
        if (first is null || second is null)
            return false;
        return first.Equals(second);
    }

    public static bool operator !=(GrammarPart? first, GrammarPart? second)
    {
        return !(first == second);
    }
}

public class Term : GrammarPart
{
    public TokenKind Kind { get; }
    
    public Term(string name) : base(name) { }
    
    public Term(string name, TokenKind kind = TokenKind.Identifier) : base(name)
    {
        Kind = kind;
    }

    public override string ToString()
    {
        return Name;
    }

    public static bool operator ==(Term? first, Term? second)
    {
        if (ReferenceEquals(first, second))
            return true;
        if (first is null || second is null)
            return false;
        return first.Equals(second);
    }

    public static bool operator !=(Term? first, Term? second)
    {
        return !(first == second);
    }
}

public class Nonterm : GrammarPart
{
    public Nonterm(string name) : base(name) { }

    public override string ToString()
    {
        return Name;
    }

    public static bool operator ==(Nonterm? first, Nonterm? second)
    {
        if (ReferenceEquals(first, second))
            return true;
        if (first is null || second is null)
            return false;
        return first.Equals(second);
    }

    public static bool operator !=(Nonterm? first, Nonterm? second)
    {
        return !(first == second);
    }
}