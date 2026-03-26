namespace comp_lab1;

public class DfaState
{
    private static int NextIdIndex = 0;
    
    public int Id { get; }
    
    public List<State> States;
    
    public DfaState(List<State> states)
    {
        Id = NextIdIndex++;
        States = states;
    }

    public static bool operator ==(DfaState first,DfaState second)
    {
        foreach (var state in first.States)
        {
            if (!second.States.Contains(state))
                return false;
        }

        return true;
    }
    
    public static bool operator !=(DfaState first,DfaState second)
    {
        foreach (var state in first.States)
        {
            if (!second.States.Contains(state))
                return true;
        }

        return false;            
    }
}