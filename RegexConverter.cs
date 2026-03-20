using System.Diagnostics;
using System.Xml;

namespace comp_lab1;

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
                case ' ':
                    continue;
                case '+':
                    states.Add(new StateToken { Output = ch, Type = TokenType.Plus });
                    break;
                case '_':
                    states.Add(new StateToken { Output = ch, Type = TokenType.Chain });
                    break;
                case '*':
                    states[^1].IsRepeatable = true;
                    break;
                default:
                    states.Add(new StateToken { Output = ch, Type = TokenType.Output });
                    break;
            }
        }

        return states;
    }
}