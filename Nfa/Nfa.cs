using System.Text;

namespace comp_lab1;

public struct Nfa
{
    public HashSet<NfaState> Start;
    public HashSet<NfaState> Finale;
    public HashSet<NfaState> AllStates;
    public HashSet<char> Alphabet;
    public Dictionary<NfaTransition, List<NfaState>> Transitions;

    public Nfa(Dictionary<NfaTransition, List<NfaState>> transitions)
    {
        CollectAllStates(transitions,
            out var allStates,
            out var alphabet,
            out var finale,
            out var start);
        
        Start = start;
        Finale = finale;
        Alphabet = alphabet;
        AllStates = allStates;
        Transitions = transitions;
    }
    
    public void PrintDot(string filename)
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph NFA {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine("  node [fontname=\"Helvetica,Arial,sans-serif\"];");
        sb.AppendLine("  edge [fontname=\"Helvetica,Arial,sans-serif\"];");

        // Узлы
        foreach (var state in AllStates.OrderBy(s => s.Id))
        {
            var shape = state.IsFinale ? "doublecircle" : "circle";
            var color = state.IsStart ? ",color=green,style=filled,fillcolor=lightgreen" : "";
            sb.AppendLine($"  \"{state.Id}\" [label=\"{state.Id}\"] [shape={shape}{color}];");
        }

        // Дуги: используем Transitions вместо state.Transitions
        var seenEdges = new HashSet<string>();
        foreach (var transition in Transitions)
        {
            NfaState from = transition.Key.State;
            char output = transition.Key.Output;
            char labelChar = output == '\0' ? 'ε' : output;
        
            foreach (var to in transition.Value.Distinct())  // Убираем дубли
            {
                string edgeKey = $"{from.Id}->{to.Id}:{labelChar}";
                if (seenEdges.Contains(edgeKey)) continue;
                seenEdges.Add(edgeKey);
                sb.AppendLine($"  \"{from.Id}\" -> \"{to.Id}\" [label=\"{labelChar}\"];");
            }
        }

        sb.AppendLine("}");
        File.WriteAllText(filename, sb.ToString());
    }
    
    public void PrintConsole()
    {
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║           NFA Transitions            ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
    
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
        
            // Группируем по символам (NFA может иметь несколько целей)
            var bySymbol = group.ToLookup(t => t.Key.Output);
        
            foreach (var symbolGroup in bySymbol.OrderBy(s => s.Key == '\0' ? '\uFFFF' : s.Key))
            {
                char sym = symbolGroup.Key == '\0' ? 'ε' : symbolGroup.Key;

                var targets = string.Join(", ", 
                    symbolGroup
                        .Where(t => t.Value.Any())
                        .Select(t => t.Value.First().Id)
                );
            
                if (!string.IsNullOrEmpty(targets))
                    stateLine += $"──{sym}──► [{targets}] \t│";
            }
        
            Console.WriteLine(stateLine);
        }
    
        Console.WriteLine("╚══════════════════════════════════════╝");
    
        // Статистика
        Console.WriteLine($"\n📊 Total transitions: {Transitions.Count}");
        Console.WriteLine($"📊 Alphabet size: {Alphabet.Count} (+ ε)");
    }
    
    private void CollectAllStates(Dictionary<NfaTransition, List<NfaState>> transitions,
                                    out HashSet<NfaState> allStates,
                                    out HashSet<char> alphabet,
                                    out HashSet<NfaState> finales,
                                    out HashSet<NfaState> starts)
    {
        allStates = new HashSet<NfaState>();
        alphabet = new HashSet<char>();
        finales = new HashSet<NfaState>();
        starts = new HashSet<NfaState>();

        foreach (var pair in transitions)
        {
            var from = pair.Key.State;
            var to = pair.Value;
            var output = pair.Key.Output;
            
            allStates.Add(from);
            
            if (output != '\0')
                alphabet.Add(output);

            if (from.IsFinale)
                finales.Add(from);

            if (from.IsStart)
                starts.Add(from);

            
            foreach (var t in to)
            {
                allStates.Add(t);
                
                if (t.IsFinale)
                    finales.Add(t);
                
                if (t.IsStart)
                    starts.Add(t);
            }
        }
    }
}
