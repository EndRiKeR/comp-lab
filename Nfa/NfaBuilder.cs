namespace comp_lab1;

public class NfaBuilder
{
    private const char _emptyWord = '\0';
    private int _nextStateId = 0;
    
    public Nfa CreateNfa(string regexInPostfix)
    {
        var stack = BuildFromPostfix(regexInPostfix);
        var frag = stack.Pop();
        var nfa = new Nfa(frag.Head, frag.Tail);
        return nfa;
    }
    
    private Stack<Fragment> BuildFromPostfix(string polish)
    {
        var stack = new Stack<Fragment>();
        foreach (char token in polish)
        {
            if (char.IsLetter(token))
            {
                var head = NewState();
                var tail = NewState();
                head.AddTransition(token, tail);
                stack.Push(new Fragment(head, new List<NfaState> { tail }));
            }
            else if (token == '_')
            {
                var f2 = stack.Pop();
                var f1 = stack.Pop();

                foreach (var (output, states) in f2.Head.Transitions)
                foreach (var state in states)
                {
                    f1.Tail[0].AddTransition(output, state);
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
                head.AddTransition(_emptyWord, f1.Head);
                head.AddTransition(_emptyWord, f2.Head);

                foreach (var node in f1.Tail)
                    node.AddTransition(_emptyWord, tail);
                
                foreach (var node in f2.Tail)
                    node.AddTransition(_emptyWord, tail);
                
                stack.Push(new Fragment(head, new List<NfaState> { tail }));
            }
            else if (token == '*')
            {
                var f = stack.Pop();
                var head = NewState();
                var tail = NewState();
                head.AddTransition(_emptyWord, f.Head);
                head.AddTransition(_emptyWord, tail);
                
                foreach (var node in f.Tail)
                    node.AddTransition(_emptyWord, f.Head);
                
                foreach (var node in f.Tail)
                    node.AddTransition(_emptyWord, tail);
                
                stack.Push(new Fragment(head, new List<NfaState> { tail }));
            }
        }

        foreach (var node in stack.Peek().Tail)
            node.IsFinale = true;
        
        return stack;
    }
    
    private NfaState NewState() => new NfaState(_nextStateId++);
}