using comp_lab.lab2.structs;

namespace comp_lab.lab2;

public class LeftRecursionEliminator
{
    public Grammar EliminateLeftRecursion(Grammar grammar)
    {
        var usedNonterms = new List<Nonterm>();
        var newRulesDictionary = new Dictionary<Nonterm, List<List<GrammarPart>>>();
        
        foreach (var (currentNonterm, listOfRules) in grammar.P)
        {
            usedNonterms.Add(currentNonterm);
            newRulesDictionary.Add(currentNonterm, new List<List<GrammarPart>>());
            
            var nontermShtrih = new Nonterm(currentNonterm.Name + "\'");
            newRulesDictionary.Add(nontermShtrih, new List<List<GrammarPart>>());
            
            grammar.N.Add(nontermShtrih);
            
            // устранить косвенные A -> B, B -> A
            var listOfRulesForCurrentNonterm = newRulesDictionary[currentNonterm];
            
            foreach (var usedNonterm in usedNonterms)
            {
                if (usedNonterm == currentNonterm)
                    continue;
                
                foreach (var rules in listOfRulesForCurrentNonterm.ToList())
                {
                    if (rules[0] is Nonterm firstNonterm && usedNonterms.Contains(firstNonterm))
                    {
                        listOfRulesForCurrentNonterm.Remove(rules);
                        rules.RemoveAt(0);
                        
                        var listOfRulesForUsedNonterm = newRulesDictionary[usedNonterm];
                        foreach (var usedRules in listOfRulesForUsedNonterm.ToList())
                        {
                            usedRules.AddRange(rules);
                            listOfRulesForCurrentNonterm.Add(usedRules);
                        }
                    }
                }
            }
            
            // устранить A -> A
            foreach (var rule in listOfRules)
            {
                var newRule = new List<GrammarPart>(rule);

                if (newRule[0] is Nonterm firstNonterm && usedNonterms.Contains(firstNonterm))
                {
                    newRule.RemoveAt(0);
                    newRule.Add(nontermShtrih);

                    newRulesDictionary[nontermShtrih].Add(newRule);
                }
                else
                {
                    newRule.Add(nontermShtrih);

                    newRulesDictionary[currentNonterm].Add(newRule);
                }
            }
            
            if (newRulesDictionary[nontermShtrih].Count == 0)
            {
                grammar.N.Remove(nontermShtrih);
                newRulesDictionary.Remove(nontermShtrih);
                
                foreach (var rule in newRulesDictionary[currentNonterm])
                {
                    if (rule[^1] is Nonterm lastNonterm && lastNonterm == nontermShtrih)
                    {
                        rule.RemoveAt(rule.Count - 1);
                    }
                }
            }
            else
            {
                newRulesDictionary[nontermShtrih].Add([EmptyWord.Term]);
                grammar.E.Add(EmptyWord.Term);
            }
        }
        
        grammar.P = newRulesDictionary;

        
        return grammar;
    }

    public Grammar LeftFactorization(Grammar grammar)
    {   
        Dictionary<Nonterm, List<List<GrammarPart>>> newRulesDictionary = new();
        
        foreach (var (nonterm, listOfRules) in grammar.P)
        {
            CommonPart commonPart = new CommonPart();

            var rulesMask = new int[listOfRules.Count];
            var commonPartsList = new List<CommonPart>();
            var currentStrings = new Dictionary<int, List<GrammarPart>>();
            for (var maskIndex = 0; maskIndex < rulesMask.Length; maskIndex++)
            {
                rulesMask[maskIndex] = 1;
                currentStrings.Add(maskIndex, new List<GrammarPart>());
            }

            while (!commonPart.IsStopped)
            {
                for (int stringIndex = 0; stringIndex < listOfRules.Max(x => x.Count); stringIndex++)
                {
                    for (int ruleIndex = 0; ruleIndex < listOfRules.Count; ruleIndex++)
                    {
                        if (rulesMask[ruleIndex] != 1)
                            continue;

                        if (stringIndex >= listOfRules[ruleIndex].Count)
                        {
                            foreach (var part in commonPartsList)
                            {
                                if (!part.RulesIndexes.Contains(ruleIndex))
                                    continue;

                                foreach (var index in part.RulesIndexes)
                                    rulesMask[index] = 0;

                                part.IsStopped = true;
                                
                                break;
                            }
                            
                            break;
                        }
                        
                        currentStrings[ruleIndex].Add(listOfRules[ruleIndex][stringIndex]);
                    }

                    for (int i = 0; i < listOfRules.Count-1; i++)
                    for (int j = i+1; j < listOfRules.Count; j++)
                    {
                        if (Comparator.IsEquals(currentStrings[i], currentStrings[j]))
                        {
                            if (commonPartsList.Any(part => part.RulesIndexes.Contains(i)))
                            {
                                var part = commonPartsList.First(part => part.RulesIndexes.Contains(i));
                                if (part.IsStopped)
                                    continue;
                                
                                part.Parts = currentStrings[i];

                                if (!part.RulesIndexes.Contains(j))
                                    part.RulesIndexes.Add(j);
                            }
                            else
                            {
                                commonPartsList.Add(new CommonPart
                                {
                                    RulesIndexes = [i, j],
                                    Parts = currentStrings[i],
                                    IsStopped = false,
                                });
                            }
                        }
                    }
                    
                    if (commonPartsList.Count == 0)
                    {
                        commonPart.IsStopped = true;
                        continue;
                    }

                    if (commonPartsList.All(part => part.IsStopped))
                    {
                        var maxCommon = commonPartsList.Max(part => part.Parts.Count);
                        commonPart = commonPartsList.First(part => part.Parts.Count == maxCommon);
                    }
                }
            }

            if (commonPartsList.Count == 0)
            {
                newRulesDictionary.Add(nonterm, listOfRules);
                continue;
            }
            
            var newNonterm = new Nonterm(nonterm.Name + "\'");
            grammar.N.Add(newNonterm);
            newRulesDictionary.Add(nonterm, new List<List<GrammarPart>>());
            newRulesDictionary.Add(newNonterm, new List<List<GrammarPart>>());
            commonPart.Parts.Add(newNonterm);
            newRulesDictionary[nonterm].Add(commonPart.Parts);
            
            for (var ruleIndex = 0; ruleIndex < listOfRules.Count; ruleIndex++)
            {
                var rule = listOfRules[ruleIndex];
                if (commonPart.RulesIndexes.Contains(ruleIndex))
                {
                    var lastPart = rule.Skip(commonPart.Parts.Count).ToList();
                    if (lastPart.Count == 0)
                    {
                        newRulesDictionary[newNonterm].Add([EmptyWord.Term]);
                        grammar.E.Add(EmptyWord.Term);
                    }
                    else
                    {
                        newRulesDictionary[newNonterm].Add(lastPart);
                    }
                }
                else
                {
                    newRulesDictionary[nonterm].Add(rule);
                }
            }
        }
        
        grammar.P = newRulesDictionary;
        
        return grammar;
    }
    
}