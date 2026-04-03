namespace comp_lab1.BrzozovskyAlgorithm;

public class BrzozovskyMinimizator(DfaBuilder dfaBuilder) // трепещи, Перри Утконос
{
    public Dfa Minimize(Dfa dfa, bool isCompact = false, bool withOutput = false)
    {
        var firstReverse = Reverse(dfa);
        var firstDeterminate = Determinate(firstReverse, isCompact);
        var secondReverse = Reverse(firstDeterminate);
        var secondDeterminate = Determinate(secondReverse, isCompact);

        if (withOutput)
        {
            Console.WriteLine("DFA до минимизации");
            dfa.PrintConsole();
            Console.WriteLine("Обратная DFA");
            firstReverse.PrintConsole();
            Console.WriteLine("DFA от обратной DFA");
            firstDeterminate.PrintConsole();
            Console.WriteLine("Обратная DFA от обратной DFA");
            secondReverse.PrintConsole();
            Console.WriteLine("DFA от обратной DFA, которая от обратной DFA");
            secondDeterminate.PrintConsole();
        }
        
        return secondDeterminate;
    }
    
    private Nfa Reverse(Dfa dfa)
    {
        Dictionary<NfaTransition, List<NfaState>> nfaTransitions = new();

        foreach (var (transition, destination) in dfa.Transitions.ToList())
        {
            var nfaDestination = destination.ToNfaState();
            var nfaTransition = transition.State.ToNfaState();

            ReverseStart(nfaDestination);
            ReverseStart(nfaTransition);
            
            var newTransition = new NfaTransition(nfaDestination, transition.Output);
            if (nfaTransitions.TryGetValue(newTransition, out var states))
            {
                states.Add(nfaTransition);
            }
            else
            {
                nfaTransitions.Add(newTransition, new List<NfaState> {nfaTransition});
            }
        }

        return new Nfa(nfaTransitions);
    }

    private Dfa Determinate(Nfa nfa, bool isCompact = false)
    {
        return dfaBuilder.CreateDfa(nfa, isCompact);
    }

    private void ReverseStart(NfaState state)
    {
        if (state.IsStart && state.IsFinale)
            return;

        if (state.IsStart)
        {
            state.IsStart = false;
            state.IsFinale = true;
        }
        else if (state.IsFinale)
        {
            state.IsFinale = false;
            state.IsStart = true;
        }
    }
}

