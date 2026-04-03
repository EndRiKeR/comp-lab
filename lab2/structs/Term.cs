using Microsoft.VisualBasic.CompilerServices;

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
        return Name == ((GrammarPart)obj!).Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name);
    }
    
    public static bool operator ==(GrammarPart first, GrammarPart second)
    {
        return first.Equals(second);       
    }
    
    public static bool operator !=(GrammarPart first, GrammarPart second)
    {
        return !first.Equals(second);       
    }
}

public class Term : GrammarPart
{
    public Term(string name) : base(name) { }
    
    public override string ToString()
    {
        return Name;
    }
    
    public static bool operator ==(Term first, Term second)
    {
        return first.Equals(second);       
    }
    
    public static bool operator !=(Term first, Term second)
    {
        return !first.Equals(second);       
    }
}

public class Nonterm : GrammarPart
{
    public Nonterm(string name) : base(name) { }
    
    public override string ToString()
    {
        return Name;
    }
    
    public static bool operator ==(Nonterm first, Nonterm second)
    {
        return first.Equals(second);       
    }
    
    public static bool operator !=(Nonterm first, Nonterm second)
    {
        return !first.Equals(second);       
    }
}