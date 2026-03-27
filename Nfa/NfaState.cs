public class NfaState
{
    public int Id { get; set; }
    public Dictionary<char, List<NfaState>> Transitions { get; } = new Dictionary<char, List<NfaState>>();
    public bool IsFinale { get; set; }

    public NfaState(int id)
    {
        Id = id;
    }

    public void AddTransition(char symbol, NfaState target)
    {
        if (!Transitions.ContainsKey(symbol))
            Transitions[symbol] = new List<NfaState>();
        
        Transitions[symbol].Add(target);
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

