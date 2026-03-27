using System.Text;

namespace comp_lab1;

public struct Dfa
{
    public HashSet<DfaState> Start;
    public HashSet<DfaState> Finale;
    public HashSet<DfaState> AllStates;
    public HashSet<char> Alphabet;
    public Dictionary<DfaTransition, DfaState> Transitions;
    public Dictionary<NfaTransition, List<NfaState>> NfaTransitions;
    
    public Dfa(Dictionary<DfaTransition, DfaState> transitions,
                Dictionary<NfaTransition, List<NfaState>> nfaTransitions)
    {
        CollectAllStates(transitions,
            out var allStates,
            out var alphabet,
            out var finales,
            out var start);

        Start = start;
        Finale = finales;
        Alphabet = alphabet;
        AllStates = allStates;
        Transitions = transitions;
        
        NfaTransitions = nfaTransitions;
    }
    
    private void CollectAllStates(Dictionary<DfaTransition, DfaState> transitions,
                                    out HashSet<DfaState> allStates,
                                    out HashSet<char> alphabet,
                                    out HashSet<DfaState> finales,
                                    out HashSet<DfaState> starts)
    {
        allStates = new HashSet<DfaState>();
        alphabet = new HashSet<char>();
        finales = new HashSet<DfaState>();
        starts = new HashSet<DfaState>();

        foreach (var pair in transitions)
        {
            var from = pair.Key.State;
            var to = pair.Value;
            var output = pair.Key.Output;
            
            allStates.Add(from);
            allStates.Add(to);
            
            if (output != '\0')
                alphabet.Add(output);

            if (from.IsFinale)
                finales.Add(from);
            if (to.IsFinale)
                finales.Add(to);
            if (from.IsStart)
                starts.Add(from);
            if (to.IsStart)
                starts.Add(to);
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
            sb.AppendLine($"  \"{transition.State.Id}\" -> \"{target.Id}\" [label=\"{transition.Output}\"];");
        }
        
        sb.AppendLine("}");
        File.WriteAllText(filename, sb.ToString());
    }
    
    public void PrintConsole()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    DFA Transitions                    ║");
        Console.WriteLine("╠═══════════════════════════════════════════════════════╣");
    
        // Группируем переходы по исходному состоянию
        var grouped = Transitions
            .GroupBy(t => t.Key.State.Id)
            .OrderBy(g => g.Key)
            .ToList();
    
        foreach (var group in grouped)
        {
            int stateId = group.Key;
            var state = group.First().Key.State;
        
            // Маркер состояния
            string marker = state.IsStart ? "🚀" : (state.IsFinale ? "🏁" : "●");
            string stateLine = $"║ {marker}\t{stateId,-2} ";
        
            // Переходы
            foreach (var trans in group.OrderBy(t => t.Key.Output))
            {
                char sym = trans.Key.Output;
                int targetId = trans.Value.Id;
                stateLine += $"──{sym}──► {targetId,-2} \t│";
            }
        
            Console.WriteLine(stateLine);
        }
    
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
    
        // Статистика
        Console.WriteLine($"\n📊 Total transitions: {Transitions.Count}");
        Console.WriteLine($"📊 Alphabet size: {Alphabet.Count}");
    }
}