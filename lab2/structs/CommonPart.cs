namespace comp_lab.lab2.structs;

public class CommonPart
{
    public List<GrammarPart> Parts;
    public List<int> RulesIndexes;
    public bool IsStopped = false;
    
    public override bool Equals(object? obj)
    {
        var commonPart = obj as CommonPart;
        return Parts == commonPart.Parts && RulesIndexes == commonPart.RulesIndexes && IsStopped == commonPart.IsStopped;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Parts, RulesIndexes, IsStopped);
    }
    
    public static bool operator ==(CommonPart first, CommonPart second)
    {
        return first.Equals(second);       
    }
    
    public static bool operator !=(CommonPart first, CommonPart second)
    {
        return !first.Equals(second);       
    }
}