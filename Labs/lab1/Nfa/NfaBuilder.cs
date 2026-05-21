namespace comp_lab.Labs.lab1.Nfa;

public class NfaBuilder
{
    private const char _emptyWord = '\0';
    private int _nextStateId = 0;
    
    public Nfa CreateNfa(string regexInPostfix)
    {
        var transitions = BuildFromPostfix(regexInPostfix);
        var nfa = new Nfa(transitions);
        return nfa;
    }
    
    private Dictionary<NfaTransition, List<NfaState>> BuildFromPostfix(string polish)
    {
        Dictionary<NfaTransition, List<NfaState>> transitions = new();
        
        var stack = new Stack<Fragment>();
        foreach (char token in polish)
        {
            if (char.IsLetter(token))
            {
                var head = NewState();
                var tail = NewState();
                AddTransition(transitions, head, token, tail);
                stack.Push(new Fragment(head, new List<NfaState> { tail }));
            }
            else if (token == '_')
            {
                var f2 = stack.Pop();
                var f1 = stack.Pop();

                var f2Transitions = transitions.Keys.Where(x => Equals(x.State, f2.Head)).ToList();
                foreach (var transition in f2Transitions)
                {
                    var destinations = transitions[transition];
                    foreach (var destination in destinations.ToList())
                    {
                        RemoveTransition(transitions, f2.Head, transition.Output, destination);
                        AddTransition(transitions, f1.Tail[0], transition.Output, destination);
                    }
                }
                
                f1.Tail = f2.Tail;
                stack.Push(f1);
            }
            else if (token == '|')
            {
                var f2 = stack.Pop();
                var f1 = stack.Pop();
                var head = NewState();
                var tail = NewState();
                AddTransition(transitions, head, _emptyWord, f1.Head);
                AddTransition(transitions, head, _emptyWord, f2.Head);

                foreach (var node in f1.Tail)
                    AddTransition(transitions, node, _emptyWord, tail);
                
                foreach (var node in f2.Tail)
                    AddTransition(transitions, node, _emptyWord, tail);
                
                stack.Push(new Fragment(head, new List<NfaState> { tail }));
            }
            else if (token == '*')
            {
                var f = stack.Pop();
                var head = NewState();
                var tail = NewState();
                
                AddTransition(transitions, head, _emptyWord, f.Head);
                AddTransition(transitions, head, _emptyWord, tail);

                foreach (var node in f.Tail)
                {
                    AddTransition(transitions, node, _emptyWord, f.Head);
                    AddTransition(transitions, node, _emptyWord, tail);
                }
                
                stack.Push(new Fragment(head, new List<NfaState> { tail }));
            }
            else if (token == '+')
            {
                var f = stack.Pop();
                var head = NewState();
                var tail = NewState();
                
                AddTransition(transitions, head, _emptyWord, f.Head);

                foreach (var node in f.Tail)
                {
                    AddTransition(transitions, node, _emptyWord, f.Head);
                    AddTransition(transitions, node, _emptyWord, tail);
                }
                
                stack.Push(new Fragment(head, new List<NfaState> { tail }));
            }
        }

        var mainFragment = stack.Peek();
        mainFragment.Head.IsStart = true;
        
        foreach (var node in mainFragment.Tail)
            node.IsFinale = true;
        
        return transitions;
    }
    
    private NfaState NewState() => new NfaState(_nextStateId++);

    private void AddTransition(Dictionary<NfaTransition, List<NfaState>> transitions, NfaState from, char output, NfaState to)
    {
        var transition = new NfaTransition(from, output);
        
        if (transitions.TryGetValue(transition, out var list))
            list.Add(to);
        else
            transitions.Add(transition, new List<NfaState> { to });
    }
    
    private void RemoveTransition(Dictionary<NfaTransition, List<NfaState>> transitions, NfaState from, char output, NfaState to)
    {
        var transition = new NfaTransition(from, output);

        if (transitions.TryGetValue(transition, out var list))
        {
            list.Remove(to);
            if (list.Count == 0)
                transitions.Remove(transition);
        }
            
    }
}