public class NFA
{
    private static int nextStateId = 0;
    public State Start { get; private set; }
    public List<State> AcceptStates { get; private set; } = new List<State>();
    public HashSet<State> AllStates { get; } = new HashSet<State>();

    private static State NewState() => new State(nextStateId++);

    public static NFA BuildFromPolish(string polish)
    {
        var stack = new Stack<Fragment>();
        foreach (char token in polish)
        {
            if (char.IsLetter(token))
            {
                var start = NewState();
                var accept = NewState();
                start.AddTransition(token, accept);
                stack.Push(new Fragment(start, new List<State> { accept }));
            }
            else if (token == '_') // Конкатенация
            {
                var f2 = stack.Pop();
                var f1 = stack.Pop();
                f1.Accept[0].AddTransition('\0', f2.Start); // ε = '\0'
                f1.Accept = f2.Accept;
                stack.Push(f1);
            }
            else if (token == '+') // Union
            {
                var f2 = stack.Pop();
                var f1 = stack.Pop();
                var start = NewState();
                var accept = NewState();
                start.AddTransition('\0', f1.Start);
                start.AddTransition('\0', f2.Start);
                f1.Accept.ForEach(s => s.AddTransition('\0', accept));
                f2.Accept.ForEach(s => s.AddTransition('\0', accept));
                stack.Push(new Fragment(start, new List<State> { accept }));
            }
            else if (token == '*') // Kleene star
            {
                var f = stack.Pop();
                var start = NewState();
                var accept = NewState();
                start.AddTransition('\0', f.Start);
                start.AddTransition('\0', accept);
                f.Accept.ForEach(s => s.AddTransition('\0', f.Start));
                f.Accept.ForEach(s => s.AddTransition('\0', accept));
                stack.Push(new Fragment(start, new List<State> { accept }));
            }
        }
        
        var nfa = new NFA();
        var frag = stack.Pop();
        nfa.Start = frag.Start;
        nfa.AcceptStates = frag.Accept;
        nfa.AcceptStates.ForEach(s => s.IsAccept = true);
        nfa.CollectAllStates();  // Теперь собираем все состояния
        return nfa;
    }
    
    public void PrintAscii()
    {
        Console.WriteLine($"NFA: {nextStateId} states");
        Console.WriteLine("States: 0 (start) -> *accept*");
        var maxId = AllStates.Max(s => s.Id);
        for (int i = 0; i <= maxId; i++)
        {
            var state = AllStates.FirstOrDefault(s => s.Id == i);
            if (state == null) continue;
            Console.Write($"S{state.Id}{(state.IsAccept ? "*" : "")}: ");
            foreach (var kv in state.Transitions.OrderBy(t => t.Key))
            {
                char sym = kv.Key == '\0' ? 'ε' : kv.Key;
                Console.Write($"{sym}->[{string.Join(",", kv.Value.Select(s => $"S{s.Id}"))}] ");
            }
            Console.WriteLine();
        }
    }
    
    public void CollectAllStates()
    {
        AllStates.Clear();
        var visited = new HashSet<State>();
        void Dfs(State s)
        {
            if (visited.Contains(s)) return;
            visited.Add(s);
            AllStates.Add(s);
            foreach (var targets in s.Transitions.Values.SelectMany(t => t))
            {
                Dfs(targets);
            }
        }
        Dfs(Start);
    }

}
