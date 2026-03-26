namespace comp_lab1;

public class DfaState
{
    public int Id { get; }
    
    public List<State> State;
    
    public Dictionary<char, List<DfaState>> Transitions { get; } = new ();
    
    public bool IsFinale { get; set; }

    public DfaState(int id)
    {
        Id = id;
    }

    public void AddTransition(char symbol, DfaState target)
    {
        if (!Transitions.ContainsKey(symbol))
            Transitions[symbol] = new List<DfaState>();
        
        Transitions[symbol].Add(target);
    }
}

public class NfaConverter
{
    public void ConvertToDfa(Nfa nfa)
    {
        
        Dictionary<State, bool> destStates = [];
        // добавляем базу
        
        while (destStates.Values.Any(x => !x))      // если хотя бы одно состояние не помечено
        {
            var unmarkedDest =  destStates.First(x => !x.Value);
            destStates[unmarkedDest.Key] = true;
            foreach (var ch in nfa.Alphabet)
            {
                var U = ECloser(Move([unmarkedDest.Key], ch));
                if (!destStates.ContainsKey(U[0]))
                {
                    destStates[U[0]] = false;
                }
                // DTran[T, a] = U;
            }
        }
    }

    private List<State> ECloser(State s)
    {
        return ECloser([s]);
    }
    
    private List<State> ECloser(List<State> T)
    {
        List<State> eCloser = [..T]; // new(T);
        
        Stack<State> stack = new Stack<State>();
        foreach (var state in T)
            stack.Push(state);

        while (stack.Count > 0)
        {
            var state = stack.Pop();
            foreach (var dest in state.Transitions['\0'])
            {
                if (eCloser.Contains(dest))
                    continue;
                
                eCloser.Add(dest);
                stack.Push(dest);
            }
        }

        return eCloser;
    }

    private List<State> Move(List<State> T, char a) // a - выход
    {
        List<State> destByA = new List<State>();
        
        foreach (var state in T)
            foreach (var dest in state.Transitions[a])
                destByA.Add(dest);
        
        return destByA;
    }
}