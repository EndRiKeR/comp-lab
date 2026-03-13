using System.Diagnostics;
using System.Xml;

namespace comp_lab1;

public enum TokenType
{
    Plus,
    Output,
    Chain,
    Open,
    Close,
    Start
}

public class StateToken
{
    public string Output;
    public TokenType Type;
    public bool IsRepeatable = false;

    public override string ToString()
    {
        if (IsRepeatable)
            return $"{Type}: {Output}*";
        else
            return $"{Type}: {Output}";
    }
}

public class RegexConverter
{
    public List<StateToken> StateTokens = new();

    public List<StateToken> SplitRegex(string input)
    {
        List<StateToken> states = new();
        
        foreach (var ch in input)
        {
            switch (ch)
            {
                case '(':
                    states.Add(new StateToken { Output = ch.ToString(), Type = TokenType.Open });
                    break;
                case ')':
                    states.Add(new StateToken { Output = ch.ToString(), Type = TokenType.Close });
                    break;
                case '+':
                    states.Add(new StateToken { Output = ch.ToString(), Type = TokenType.Plus });
                    break;
                case '*':
                    states[^1].IsRepeatable = true;
                    break;
                default:
                    states.Add(new StateToken { Output = ch.ToString(), Type = TokenType.Output });
                    break;
            }
        }

        return states;
    }

    public void AddSupportToken(List<StateToken> states)
    {
        for (int i = 0; i < states.Count - 1; i++)
        {
            var j = Math.Min(i + 1, states.Count - 1);
            if (states[i].Type == TokenType.Output && states[j].Type == TokenType.Output ||
                states[i].Type == TokenType.Output && states[j].Type == TokenType.Open ||
                states[i].Type == TokenType.Close && states[j].Type == TokenType.Output)
            {
                states.Insert(j, new StateToken { Output = "--", Type = TokenType.Chain });
            }
        }
    }

    public TreeNode SplitBySupportTokens(List<StateToken> states)
    {
        TreeNode root = new TreeNode(new StateToken { Output = "Start", Type = TokenType.Start });
        CreateTree(states, root);
        
        return root;
    }

    public TreeNode CreateTree(List<StateToken> states, TreeNode root)
    {
        for (var i = 0; i < states.Count; i++)
        {
            var token = states[i];
            
            if (token.Type == TokenType.Chain)
                continue;
            
            var node = new TreeNode(token);
            
            if (token.Type == TokenType.Open)
            {
                var end = -1;
                var deep = 0;
                for (var j = i + 1; j < states.Count; j++)
                {
                    if (states[j].Type == TokenType.Open)
                    {
                        deep++;
                    }
                    else if (states[j].Type == TokenType.Close)
                    {
                        if (deep != 0)
                        {
                            deep--;
                        }
                        else
                        {
                            end = j;
                            break;
                        }
                    }
                }
                
                if (end != i + 1)
                {
                    var section = states.GetRange(i + 1, end - i - 1);
                    CreateTree(section, node);
                }
                i = end + 1;
            }
            
            root.Nodes.Add(node);
        }
        
        return root;
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    public FiniteStateMachine Convert(string regex)
    {
        FiniteStateMachine fsm = new();
        string output = "";
        string lastOutput = "";
        
        foreach (var ch in regex)
        {
            // a-z, *, (), +
            // aa*b + b
            switch (ch)
            {
                case '*':
                    
                    break;
                case '+':
                    
                    break;
                case ' ':
                    
                    break;
                case '(':
                    
                    break;
                case ')':
                    
                    break;
                // a-z
                default:
                    AddState(fsm, output, false);
                    lastOutput = output;
                    output = "";
                    break;
            }
        }
        
        return fsm;
    }

    public void AddState(FiniteStateMachine fsm, string output, bool isFinish)
    {
        State newState = new();
        newState.Name = $"{fsm.States.Count + 1}";
        
        fsm.States.Add(newState);
        fsm.Rules.Add(newState, new List<Destination>());
        fsm.Rules[fsm.States[^1]].Add(new Destination()
        {
            Output = output
        });

        if (fsm.StartState == null)
            fsm.StartState = newState;
        
        if (isFinish)
            fsm.EndStates.Add(newState);

        foreach (var ch in output)
            if (!fsm.Alphabet.Contains(ch))
                fsm.Alphabet += ch;
    }
    
    public void AddCycle(FiniteStateMachine fsm, string lastOutput)
    {
        // fsm.Rules[fsm.States[^1]].Add(new Destination()
        // {
        //     Output = lastOutput,
        //     Dest = fsm.States[^1]
        // });
        //
        // if (fsm.StartState == null)
        //     fsm.StartState = newState;
        //
        // if (isFinish)
        //     fsm.EndStates.Add(newState);
        //
        // foreach (var ch in output)
        //     if (!fsm.Alphabet.Contains(ch))
        //         fsm.Alphabet += ch;
    }
}