using System.Text;

namespace comp_lab.Labs.lab1.Nfa;

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
    
    // Вывод ДКА в формате .dot
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
    
    // Вывод ДКА в консоль
    public void PrintConsole()
    {
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║           NFA Transitions            ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
    
        // ИТЕРАЦИЯ ПО ВСЕМ СОСТОЯНИЯМ (сортировка по Id)
        foreach (var state in AllStates.OrderBy(s => s.Id))
        {
            // Маркер состояния
            string marker = state.IsStart || state.IsFinale ? "" : "●";
            if (state.IsStart) marker += "S";
            if (state.IsFinale) marker += "F";
        
            Console.Write($"\t{marker,-3}   {state.Id,-2}");
            
            var outgoing = Transitions
                .Where(t => t.Key.State == state)
                .ToLookup(t => t.Key.Output);
        
            bool hasTransitions = false;
            var allSymbols = Alphabet.Append('\0').OrderBy(c => c == '\0' ? '\uFFFF' : c).ToList();

            foreach (char symbol in allSymbols)
            {
                // Ищем переходы по этому символу ИЗ ТЕКУЩЕГО состояния
                var matchingTransitions = Transitions
                    .Where(t => t.Key.State == state && t.Key.Output == symbol)
                    .ToList();
    
                if (matchingTransitions.Any(t => t.Value.Count != 0))
                {
                    char sym = symbol == '\0' ? 'ε' : symbol;
                    var targets = string.Join(", ", 
                        matchingTransitions
                            .Where(t => t.Value.Count != 0)
                            .SelectMany(t => t.Value)  // Все цели
                            .Distinct()
                            .Select(to => to.Id)
                    );
        
                    Console.Write($"──{sym}──► [{targets}] ");
                    hasTransitions = true;
                }
            }

            if (!hasTransitions)
                Console.Write("─∅─");
            
            Console.Write("\n");
        }

        Console.WriteLine("╚══════════════════════════════════════╝");
    }
}


