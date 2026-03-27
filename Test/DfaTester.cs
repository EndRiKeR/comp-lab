namespace comp_lab1.Test;

public class DfaTester
{
    public bool TestRegexWithDfa(string regex, Dfa dfa)
    {
        return TestRegex(regex, dfa);
    }

    private bool TestRegex(string regex, Dfa dfa)
    {
        var currentState = dfa.Start.First();
        var dfaTransitions = dfa.Transitions;
        var currentRegex = regex;

        while (currentRegex.Length > 0)
        {
            var currentChar = currentRegex[0];
            var transition = new DfaTransition{ State = currentState, Output = currentChar };

            if (!dfaTransitions.TryGetValue(transition, out var dfaTransition))
                return false;
            
            currentState = dfaTransition;

            if (currentRegex.Length > 1)
                currentRegex = currentRegex.Substring(1);
            else
                break;
        }
        
        return currentState.IsFinale;
    }
}