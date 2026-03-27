namespace comp_lab1;

public class DfaTransition
{
    public DfaState State { get; set; }
    public char Output { get; set; }
    
    public override bool Equals(object? obj)
    {
        return State == ((DfaTransition)obj!).State && Output == ((DfaTransition)obj!).Output;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(State, Output);
    }
}