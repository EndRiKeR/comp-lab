using System.Text;
using comp_lab.Labs.lab1.Nfa;

namespace comp_lab.Labs.lab1.Dfa;

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
    
    public Dfa(Dfa dfa)
    {
        Start = dfa.Start;
        Finale = dfa.Finale;
        Alphabet = dfa.Alphabet;
        AllStates = dfa.AllStates;
        Transitions = dfa.Transitions;
        
        NfaTransitions = dfa.NfaTransitions;
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
    
    // Вывод ДКА в формате .dot
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
    
    // Вывод ДКА в консоль
    public void PrintConsole()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    DFA Transitions                    ║");
        Console.WriteLine("╠═══════════════════════════════════════════════════════╣");
        
        foreach (var state in AllStates.OrderBy(s => s.Id))
        {
            // Маркер состояния
            string marker = state.IsStart || state.IsFinale ? "" : "●";
            if (state.IsStart) marker += "S";
            if (state.IsFinale) marker += "F";
        
            Console.Write($"   {marker}   {state.Id}");
        
            bool hasTransitions = false;
        
            // Итерируем по ВСЕМ символам алфавита
            var allSymbols = Alphabet.OrderBy(c => c).ToList();
        
            foreach (char symbol in allSymbols)
            {
                var transitionKey = new DfaTransition { State = state, Output = symbol };
            
                if (Transitions.TryGetValue(transitionKey, out var targetState))
                {
                    int targetId = targetState.Id;
                    Console.Write($"──{symbol}──► [{targetId}]\t");
                    hasTransitions = true;
                }
                else
                {
                    Console.Write($"──{symbol}──► ∅\t");
                }
            }
        
            if (!hasTransitions)
                Console.Write("─∅─");
            
            Console.Write("\n");
        }

        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
    }
}
