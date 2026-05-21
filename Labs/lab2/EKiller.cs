using comp_lab.Labs.lab2.structs;

namespace comp_lab.Labs.lab2;

public class EKiller
{
    public Grammar FindAndDestroyChains(Grammar grammar)
    {
        // (1)
        var N = new List<List<Nonterm>>();
        
        foreach (var nonterm in grammar.N)
        {
            var oldList = new List<Nonterm>();
            var newList = new List<Nonterm>();
            Stack<Nonterm> stack = new();
            
            newList.Add(nonterm);

            do
            {
                oldList.Clear();
                oldList.AddRange(newList);
                
                foreach (var nt in newList)
                    stack.Push(nt);

                while (stack.Count != 0)
                {
                    var nt = stack.Pop();
                    var listOfRules = grammar.P[nt];
                    foreach (var rule in listOfRules)
                    {
                        if (rule.Count != 1)
                            continue;
                        
                        foreach (var part in rule)
                        {
                            if (part is Nonterm partNonterm && !newList.Contains(partNonterm))
                            {
                                newList.Add(partNonterm);
                                stack.Push(partNonterm);
                            }
                        }
                    }
                
                }
            } while (!Comparator.IsEquals(oldList, newList));
            
            N.Add(newList);
        }

        // (2)
        var newListOfRules = new Dictionary<Nonterm, List<List<GrammarPart>>>();
        for (var index = 0; index < N.Count; index++)
        {
            var n = N[index];
            List<List<GrammarPart>> parts = new();
            foreach (var nonterm in n)
            {
                foreach (var rule in grammar.P[nonterm])
                {
                    if (rule.Count == 1 && rule[0] is Nonterm)
                        continue;

                    parts.Add(rule);
                }
            }

            newListOfRules[grammar.N.ToList()[index]] = parts;
        }
        
        // (3)
        grammar.P = newListOfRules;

        return grammar;
    }
}