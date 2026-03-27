namespace comp_lab1.BrzozovskyAlgorithm;

public class BrzozovskyMinimizator(DfaBuilder dfaBuilder) // трепещи, Перри Утконос
{
    public Dfa Minimize(Dfa dfa, bool isCompact = false)
    {
        dfa.PrintConsole();
        
        var firstReverse = Reverse(dfa);
        var firstDeterminate = Determinate(firstReverse, isCompact);
        var secondReverse = Reverse(firstDeterminate);
        var secondDeterminate = Determinate(secondReverse, isCompact);
        
        firstReverse.PrintConsole();
        firstDeterminate.PrintConsole();
        secondReverse.PrintConsole();
        secondDeterminate.PrintConsole();
        
        return secondDeterminate;
    }
    
    private Dfa Reverse(Dfa dfa)
    {
        foreach (var start in dfa.Start)
        {
            start.IsStart = false;
            start.IsFinale = true;
        }
        
        foreach (var finale in dfa.Finale)
        {
            finale.IsFinale = false;
            finale.IsStart = true;
        }
        
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

    private Dfa Determinate(Dfa dfa, bool isCompact = false)
    {
        return dfaBuilder.CreateDfa(dfa, isCompact);
    }
}