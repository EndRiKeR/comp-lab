namespace comp_lab.Labs.lab1.Nfa;

public class NfaState
{
    public int Id { get; set; }
    public bool IsStart { get; set; } = false;
    public bool IsFinale { get; set; } = false;

    public NfaState(int id)
    {
        Id = id;
    }
    
    public NfaState(int id, bool isStart, bool isFinale) : this(id)
    {
        IsStart = isStart;
        IsFinale = isFinale;
    }
    
    public static bool operator ==(NfaState first, NfaState second)
    {
        return first.Equals(second);
    }
    
    public static bool operator !=(NfaState first, NfaState second)
    {
        return !first.Equals(second);       
    }

    public override bool Equals(object? obj)
    {
        return Id == ((NfaState)obj!).Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}