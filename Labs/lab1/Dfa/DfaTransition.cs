namespace comp_lab.Labs.lab1.Dfa;

public class DfaTransition
{
    public DfaState State { get; set; }
    public char Output { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is DfaTransition other &&
               State == other.State &&
               Output == other.Output;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(State.GetHashCode(), Output.GetHashCode());
    }
    
    public static bool operator ==(DfaTransition? left, DfaTransition? right) =>
        Equals(left, right);

    public static bool operator !=(DfaTransition? left, DfaTransition? right) =>
        !Equals(left, right);
}

