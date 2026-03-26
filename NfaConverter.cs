using System.Text;
using comp_lab1;

public class NfaConverter
{
    public Dictionary<Transition, DfaState> ConvertToDfa(Nfa nfa)
    {
        Dictionary<DfaState, bool> destStates = [];
        Dictionary<Transition, DfaState> DTran = new ();

        var T0 = new DfaState(ECloser(nfa.Start));
        destStates.Add(T0, false);

        int iter = 0;

        while (destStates.Values.Any(x => !x)) // если хотя бы одно состояние не помечено
        {
            var pair =  destStates.First(x => !x.Value);
            destStates[pair.Key] = true;
            var T = pair.Key;
            
            Console.Write($"Iteration {iter++}\t");
            Console.Write($"True: {destStates.Count(x => x.Value)}\t");
            Console.Write($"False: {destStates.Count(x => !x.Value)}\n");
            
            foreach (var a in nfa.Alphabet)
            {
                var U = new DfaState(ECloser(new DfaState(Move(T, a))));
                
                if (CheckForU(destStates.Keys.ToList(), U))
                    continue;
                
                destStates.Add(U, false);
                var transition = new Transition { T = T, a = a };
                DTran[transition] = U;
            }
        }

        return DTran;
    }

    private bool CheckForU(List<DfaState> states, DfaState U)
    {
        foreach (var other in states)
        {
            if (other == U)
                return true;
        }

        return false;
    }

    private List<State> ECloser(State s)
    {
        return ECloser(new DfaState([s]));
    }
    
    private List<State> ECloser(DfaState dfaState)
    {
        List<State> eCloser = [..dfaState.States]; // new(T);
        
        Stack<State> stack = new Stack<State>();
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

    private List<State> Move(DfaState T, char a)
    {
        List<State> destByA = new List<State>();

        foreach (var state in T.States)
        {
            if (!state.Transitions.TryGetValue(a, out var transition))
                continue;
            
            foreach (var dest in transition)
                destByA.Add(dest);
        }
        
        return destByA;
    }
    
    public void PrintDfa(Dictionary<Transition, DfaState> dTran, string filename)
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph DFA {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine("  node [shape=circle];");
        
        // Уникальные состояния
        var states = dTran.Values.Distinct().ToList();
        states.AddRange(dTran.Keys.Select(t => t.T).Distinct());
        states = states.Distinct().ToList();
        
        // Объявление узлов
        foreach (var state in states)
        {
            sb.AppendLine($"  \"{state.Id}\" [label=\"{state.Id}\"];");
        }
        
        // Рёбра переходов
        foreach (var kvp in dTran)
        {
            var transition = kvp.Key;
            var target = kvp.Value;
            sb.AppendLine($"  \"{transition.T.Id}\" -> \"{target.Id}\" [label=\"'{transition.a}'\"];");
        }
        
        sb.AppendLine("}");
        File.WriteAllText(filename, sb.ToString());
    }
}