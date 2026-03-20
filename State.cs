public class State
{
    public int Id { get; }
    public Dictionary<char, List<State>> Transitions { get; } = new Dictionary<char, List<State>>();
    public bool IsFinale { get; set; }

    public State(int id)
    {
        Id = id;
    }

    public void AddTransition(char symbol, State target)
    {
        if (!Transitions.ContainsKey(symbol))
            Transitions[symbol] = new List<State>();
        
        Transitions[symbol].Add(target);
    }
}

