namespace comp_lab.lab2.structs;

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
    public Term(string name) : base(name) { }

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