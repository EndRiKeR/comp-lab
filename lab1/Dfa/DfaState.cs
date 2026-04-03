namespace comp_lab1;

public class DfaState
{
    public int Id { get; set; }

    public bool IsStart { get; set; } = false;
    public bool IsFinale { get; set; } = false;

    public List<NfaState> States;
    
    public DfaState(int id, List<NfaState> states)
    {
        Id = id;
        States = states;
    }

    public NfaState ToNfaState() => new NfaState(Id, IsStart, IsFinale);

    public static bool operator ==(DfaState first, DfaState second)
    {
        if (first.States.Count != second.States.Count)
            return false;
        
        foreach (var state in first.States)
        {
            if (!second.States.Contains(state))
                return false;
        }

        return true;
    }
    
    public static bool operator !=(DfaState first, DfaState second)
    {
        return !(first == second);          
    }
    
    public override bool Equals(object? obj)
    {
        return this == (DfaState)obj!;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, States);
    }
}
