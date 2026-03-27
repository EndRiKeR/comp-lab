namespace comp_lab1.BrzozovskyAlgorithm;

public class BrzozovskyMinimizator(DfaBuilder dfaBuilder) // трепещи, Перри Утконос
{
    public Dfa Minimize(Dfa dfa)
    {
        return Determinate(Reverse(Determinate(Reverse(dfa))));
    }
    
    private Dfa Reverse(Dfa dfa)
    {
        (dfa.Start, dfa.Finale) = (dfa.Finale, dfa.Start);
        
        Dictionary<DfaTransition, DfaState> transitions = new Dictionary<DfaTransition, DfaState>();

        foreach (var (transition, destination) in dfa.Transitions.ToList())
        {
            var newTransition = new DfaTransition{ State = destination, Output =  transition.Output};
            transitions[newTransition] = transition.State;
        }
        
        dfa.Transitions = transitions;
        
        return dfa;
    }

    private Dfa Determinate(Dfa dfa)
    {
        return dfaBuilder.CreateDfa(dfa);
    }
}