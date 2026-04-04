namespace comp_lab.lab2.structs;

public static class Comparator
{
    public static bool IsEquals(List<GrammarPart> first, List<GrammarPart> second)
    {
        if (first.Count != second.Count)
            return false;
        
        foreach (var el in first)
        {
            if (!second.Contains(el))
                return false;
        }

        return true;
    }
    
    public static bool IsEquals(List<Nonterm> first, List<Nonterm> second)
    {
        if (first.Count != second.Count)
            return false;
        
        foreach (var el in first)
        {
            if (!second.Contains(el))
                return false;
        }

        return true;
    }
}