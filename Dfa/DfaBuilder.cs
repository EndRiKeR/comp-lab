using comp_lab1;

public class DfaBuilder
{
    private int _nextStateId = 0;

    public Dfa CreateDfa(Nfa nfa)
    {
        var transitions = ConvertToDfa(nfa);
        var dfa = new Dfa(transitions);
        return dfa;
    }

    public Dictionary<DfaTransition, DfaState> ConvertToDfa(Nfa nfa)
    {
        Dictionary<DfaState, bool> statesDictionary = [];
        Dictionary<DfaTransition, DfaState> transitions = new ();

        var T0 = CreateDfaState(ECloser(nfa.Start));
        statesDictionary.Add(T0, false);
        PrintStates(T0);

        int iter = 0;

        while (statesDictionary.Values.Any(x => !x))
        {
            var pair = statesDictionary.First(x => !x.Value);
            statesDictionary[pair.Key] = true;
            var T = pair.Key;
            
            Console.Write($"Iteration {iter++}\t");
            Console.Write($"True: {statesDictionary.Count(x => x.Value)}\t");
            Console.Write($"False: {statesDictionary.Count(x => !x.Value)}\n");
            
            foreach (var alphabetChar in nfa.Alphabet)
            {
                var uMove = Move(T, alphabetChar);
                var uCloser = ECloser(uMove);
                DfaState uState = new DfaState(-1, uCloser);
                
                PrintStates(uState);

                bool isExist = CheckForExists(statesDictionary.Keys.ToList(), uState, out var exist);

                if (!isExist)
                {
                    uState = CreateDfaState(uCloser);
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
        {
            if (dfaState.States.Contains(nfa.Start))
            {
                dfaState.IsStart = true;
            }
            
            foreach (var nfaFinish in nfa.Finale)
            {
                if (dfaState.States.Count == 0 || dfaState.States.Contains(nfaFinish))
                {
                    dfaState.IsFinale = true;
                    break;
                }
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
            if (!state.Transitions.TryGetValue('\0', out var transition))
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
            if (!state.Transitions.TryGetValue(a, out var transition))
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