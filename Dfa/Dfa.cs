using System.Text;

namespace comp_lab1;

public class Dfa
{
    public DfaState Start { get; private set; }
    public HashSet<DfaState> Finale { get; private set; }
    public HashSet<DfaState> AllStates { get; private set; }
    public HashSet<char> Alphabet { get; private set; }
    public Dictionary<DfaTransition, DfaState> Transitions { get; private set; }
    
    public Dfa(Dictionary<DfaTransition, DfaState> transitions)
    {
        CollectAllStates(transitions, out var allStates, out var alphabet, out var finales, out var start);

        Start = start;
        Finale = finales;
        Alphabet = alphabet;
        AllStates = allStates;
        Transitions = transitions;
    }
    
    private void CollectAllStates(Dictionary<DfaTransition, DfaState> transitions,
                                    out HashSet<DfaState> allStates,
                                    out HashSet<char> alphabet,
                                    out HashSet<DfaState> finales,
                                    out DfaState start)
    {
        allStates = new HashSet<DfaState>();
        alphabet = new HashSet<char>();
        finales = new HashSet<DfaState>();
        start = null;
        bool startSetted = false;

        foreach (var pair in transitions)
        {
            var from = pair.Key.State;
            var to = pair.Value;
            var output = pair.Key.Output;
            
            allStates.Add(from);
            allStates.Add(to);
            alphabet.Add(output);

            if (from.IsFinale)
                finales.Add(from);
            if (to.IsFinale)
                finales.Add(to);
            
            if (!startSetted)
            {
                if (from.IsStart)
                {
                    start = from;
                    startSetted = true;
                }
                if (to.IsStart)
                {
                    start = to;
                    startSetted = true;
                }
            }
        }
    }
    
    public void PrintDfa(string filename)
    {
        var dTran = Transitions;
        
        var sb = new StringBuilder();
        sb.AppendLine("digraph DFA {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine("  node [shape=circle];");
        
        // Уникальные состояния
        var states = dTran.Values.Distinct().ToList();
        states.AddRange(dTran.Keys.Select(t => t.State).Distinct());
        states = states.Distinct().ToList();
        
        // Объявление узлов
        foreach (var state in states)
        {
            var shape = state.IsFinale ? "doublecircle" : "circle";
            var color = state.IsStart ? ",color=green,style=filled,fillcolor=lightgreen" : "";
            sb.AppendLine($"  \"{state.Id}\" [label=\"{state.Id}\"] [shape={shape}{color}];");
        }
        
        // Рёбра переходов
        foreach (var kvp in dTran)
        {
            var transition = kvp.Key;
            var target = kvp.Value;
            sb.AppendLine($"  \"{transition.State.Id}\" -> \"{target.Id}\" [label=\"'{transition.Output}'\"];");
        }
        
        sb.AppendLine("}");
        File.WriteAllText(filename, sb.ToString());
    }
}