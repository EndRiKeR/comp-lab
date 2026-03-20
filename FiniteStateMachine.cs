using System.Text;

namespace comp_lab1;

public class Nfa
{
    public State Start { get; private set; }
    public List<State> AcceptStates { get; private set; }
    public HashSet<State> AllStates { get; }

    public Nfa(State start, List<State> acceptStates)
    {
        Start = start;
        AcceptStates = acceptStates;
        AllStates = CollectAllStates();
    }
    
    public void PrintAscii()
    {
        var maxId = AllStates.Max(s => s.Id);
        
        Console.WriteLine($"NFA: {maxId + 1} states");      // тк считаю с 0
        Console.WriteLine("States: 0 (start) -> *accept*");
        
        for (int i = 0; i <= maxId; i++)
        {
            var state = AllStates.FirstOrDefault(s => s.Id == i);
            if (state == null) continue;
            Console.Write($"S{state.Id}{(state.IsFinale ? "*" : "")}: ");
            foreach (var kv in state.Transitions.OrderBy(t => t.Key))
            {
                char sym = kv.Key == '\0' ? 'ε' : kv.Key;
                Console.Write($"{sym}->[{string.Join(",", kv.Value.Select(s => $"S{s.Id}"))}] ");
            }
            Console.WriteLine();
        }
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
            var color = state.Id == Start.Id ? ",color=green,style=filled,fillcolor=lightgreen" : "";
            sb.AppendLine($"  {state.Id} [shape={shape}{color}];");
        }

        // Дуги: группируем мульти-дуги
        var seenEdges = new HashSet<string>();
        foreach (var from in AllStates)
        {
            foreach (var kv in from.Transitions)
            {
                char labelChar = kv.Key == '\0' ? 'ε' : kv.Key;
                foreach (var to in kv.Value.Distinct())  // Убираем дубли
                {
                    string edgeKey = $"{from.Id}->{to.Id}:{labelChar}";
                    if (seenEdges.Contains(edgeKey)) continue;
                    seenEdges.Add(edgeKey);
                    sb.AppendLine($"  {from.Id} -> {to.Id} [label=\"{labelChar}\"];");
                }
            }
        }

        sb.AppendLine("}");
        File.WriteAllText(filename, sb.ToString());
    }

    
    private HashSet<State> CollectAllStates()
    {
        var visited = new HashSet<State>();
        
        void Dfs(State state)
        {
            if (visited.Contains(state))
                return;
            
            visited.Add(state);

            var targets = state.Transitions.Values.SelectMany(t => t);
            foreach (var target in targets)
                Dfs(target);
        }
        Dfs(Start);

        return visited;
    }
}
