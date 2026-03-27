using comp_lab1;

public class DfaBuilder
{
    private int _nextStateId = 0;
    private Dictionary<NfaTransition, List<NfaState>> _nfaTransitions;

    public Dfa CreateDfa(Nfa nfa, bool isCompact = false)
    {
        var transitions = ConvertToDfa(nfa, isCompact);
        return new Dfa(transitions, nfa.Transitions);
    }
    
    public Dfa CreateDfa(Dfa oldDfa, bool isCompact = false)
    {
        var transitions = ConvertToDfa(oldDfa, isCompact);
        return new Dfa(transitions, oldDfa.NfaTransitions);
    }
    
    private Dictionary<DfaTransition, DfaState> ConvertToDfa(Nfa nfa, bool isCompact = false)
    {
        List<NfaState> startStates = nfa.Start.ToList();
        List<NfaState> endStates = nfa.Finale.ToList();
        HashSet<char> alphabet = nfa.Alphabet;
        
        return ConvertToDfa(startStates, endStates, alphabet, nfa.Transitions, isCompact);
    }
    
    private Dictionary<DfaTransition, DfaState> ConvertToDfa(Dfa dfa, bool isCompact = false)
    {
        List<NfaState> startStates = dfa.Finale.SelectMany(x => x.States).ToList();
        List<NfaState> endStates = dfa.Finale.SelectMany(x => x.States).ToList();
        HashSet<char> alphabet = dfa.Alphabet;
        
        return ConvertToDfa(startStates, endStates, alphabet, dfa.NfaTransitions, isCompact);
    }
    
    private Dictionary<DfaTransition, DfaState> ConvertToDfa(
        List<NfaState> startStates,
        List<NfaState> endStates,
        HashSet<char> alphabet,
        Dictionary<NfaTransition, List<NfaState>> nfaTransitions,
        bool isCompact = false)
    {
        _nfaTransitions = nfaTransitions;
        Dictionary<DfaState, bool> statesDictionary = [];
        Dictionary<DfaTransition, DfaState> transitions = new ();

        var T0 = CreateDfaState(ECloser(startStates));
        T0.IsStart = true;
        statesDictionary.Add(T0, false);

        while (statesDictionary.Values.Any(x => !x))
        {
            var pair = statesDictionary.First(x => !x.Value);
            statesDictionary[pair.Key] = true;
            var T = pair.Key;
            
            foreach (var alphabetChar in alphabet)
            {
                var uMove = Move(T, alphabetChar);
                var uCloser = ECloser(uMove);
                DfaState uState = new DfaState(-1, uCloser);
                
                if (uState.States.Count == 0 && isCompact)
                    continue;

                bool isExist = CheckForExists(statesDictionary.Keys.ToList(), uState, out var exist);

                if (!isExist)
                {
                    uState = CreateDfaState(uCloser);
                    
                    if (uState.States.Count == 0)
                        uState.Id = -1;
                    
                    statesDictionary.Add(uState, false);
                }
                else
                {
                    uState = exist;
                }
                
                var transition = new DfaTransition { State = T, Output = alphabetChar };
                transitions[transition] = uState;
            }
        }

        foreach (var dfaState in statesDictionary.Keys)
        foreach (var nfaFinish in endStates)
        {
            if (dfaState.States.Contains(nfaFinish))
            {
                dfaState.IsFinale = true;
                break;
            }
        }

        return transitions;
    }
    
    private DfaState CreateDfaState(List<NfaState> nfaStates) => new (_nextStateId++, nfaStates);
    
    private List<NfaState> ECloser(NfaState s)
    {
        return ECloser([s]);
    }
    
    private List<NfaState> ECloser(List<NfaState> list)
    {
        List<NfaState> eCloser = [..list]; // new(T);
        
        Stack<NfaState> stack = new Stack<NfaState>();
        foreach (var state in eCloser)
            stack.Push(state);

        while (stack.Count > 0)
        {
            var state = stack.Pop();
            if (!_nfaTransitions.TryGetValue(new NfaTransition(state, '\0'), out var transition))
                continue;
            
            foreach (var dest in transition)
            {
                if (eCloser.Contains(dest))
                    continue;
                
                eCloser.Add(dest);
                stack.Push(dest);
            }
        }

        return eCloser;
    }

    private List<NfaState> Move(DfaState T, char a)
    {
        HashSet<NfaState> destByA = new HashSet<NfaState>();

        foreach (var state in T.States)
        {
            if (!_nfaTransitions.TryGetValue(new NfaTransition(state, a), out var transition))
                continue;
            
            foreach (var dest in transition)
                destByA.Add(dest);
        }
        
        return destByA.ToList();
    }

    private void PrintStates(DfaState dfaState)
    {
        if (dfaState.States.Count == 0)
        {
            Console.WriteLine($"Dfa id: {dfaState.Id} is EMPTY");
            return;
        }
        
        string output = $"Dfa id: {dfaState.Id}. Nfa id's: ";
        foreach (var state in dfaState.States)
        {
            output += $"{state.Id} ";
        }

        Console.WriteLine(output);
    }

    private bool CheckForExists(List<DfaState> states, DfaState U, out DfaState exist)
    {
        foreach (var other in states)
        {
            if (other == U)
            {
                exist = other;
                return true;
            }
        }
        
        exist = null;
        return false;
    }
}