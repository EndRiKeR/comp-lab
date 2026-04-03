namespace comp_lab1;

public class NfaTransition
{
    public NfaState State { get; set; }
    public char Output { get; set; }

    public NfaTransition(NfaState state, char output)
    {
        State = state;
        Output = output;
    }

    public override bool Equals(object? obj)
    {
        return obj is NfaTransition other &&
               State == other.State &&
               Output == other.Output;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(State.GetHashCode(), Output.GetHashCode());
    }
    
    public static bool operator ==(NfaTransition? left, NfaTransition? right) =>
        Equals(left, right);

    public static bool operator !=(NfaTransition? left, NfaTransition? right) =>
        !Equals(left, right);
}

