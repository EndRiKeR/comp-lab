using comp_lab.lab2.structs;

namespace comp_lab.lab2;

public class GrammarReaderIO
{
    public Grammar ReadGrammar(string inFilePath, bool checkForE = false)
    {
        var lines = File.ReadAllLines(inFilePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToArray();

        int idx = 0;
        var grammar = new Grammar();

        int numNonterms = int.Parse(lines[idx++]);
        string[] nonterms = ReadSymbols(lines[idx++], numNonterms);
        HashSet<Nonterm> nontermSet = new HashSet<Nonterm>();
        
        foreach (var nonterm in nonterms)
            nontermSet.Add(new Nonterm(nonterm));
        
        grammar.N = nontermSet;

        int numTerms = int.Parse(lines[idx++]);
        string[] terms = ReadSymbols(lines[idx++], numTerms);
        HashSet<Term> termSet = new HashSet<Term>();

        if (checkForE && terms.Contains(EmptyWord.Name))
        {
            Console.WriteLine($"Недопустимый терминал {EmptyWord.Name}");
            throw new Exception();
        }
        
        foreach (var term in terms)
            termSet.Add(new Term(term));
        
        grammar.E = termSet;
        
        int numProds = int.Parse(lines[idx++]);

        grammar.P = new Dictionary<Nonterm, List<List<GrammarPart>>>();

        for (int i = 0; i < numProds; i++)
        {
            string ruleLine = lines[idx++];
            var parts = ruleLine.Split("->");
            if (parts.Length == 2)
            {
                string left = parts[0].Trim();                     // Единственный нетерминал слева
                var leftNonterm = new Nonterm(left);
                
                string right = parts[1].Trim().Replace(" ", "");    // Правая часть без пробелов внутри
                var rightPart = new List<GrammarPart>();
                foreach (var ch in right)
                {
                    GrammarPart part;
                    if (char.IsUpper(ch))
                        part = new Nonterm(ch.ToString());
                    else
                        part = new Term(ch.ToString());
                    
                    rightPart.Add(part);
                }

                if (!grammar.P.TryGetValue(leftNonterm, out var rules))
                {
                    rules = new List<List<GrammarPart>>();
                    grammar.P[leftNonterm] = rules;
                }
                
                rules.Add(rightPart);
            }
        }

        grammar.S = new Nonterm(lines[idx][0].ToString());

        return grammar;
    }
    
    private string[] ReadSymbols(string line, int expectedCount)
    {
        var symbols = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (symbols.Length != expectedCount)
            throw new FormatException($"Ожидалось {expectedCount} символов, найдено {symbols.Length}");
        
        return symbols;
    }
    
    public void WriteGrammar(Grammar grammar, string outFilePath)
    {
        using var writer = new StreamWriter(outFilePath);

        writer.WriteLine(grammar.N.Count);
        writer.WriteLine(string.Join(" ", grammar.N));

        writer.WriteLine(grammar.E.Count);
        writer.WriteLine(string.Join(" ", grammar.E));

        writer.WriteLine(grammar.P.Sum(rule => rule.Value.Count));
        foreach (var (left, rulesList) in grammar.P)
        {
            if (rulesList.Count == 0)
            {
                writer.WriteLine($"{left}\t-> {EmptyWord.Name}");
            }
            
            foreach (var right in rulesList)
            {
                var rightRules = string.Join(" ", right);
                writer.WriteLine($"{left}\t-> {rightRules}");
            }
        }

        writer.WriteLine(grammar.S);
    }
    
    public void WriteGrammarToConsole(Grammar grammar)
    {
        Console.WriteLine(grammar.N.Count);
        Console.WriteLine(string.Join(" ", grammar.N));

        Console.WriteLine(grammar.E.Count);
        Console.WriteLine(string.Join(" ", grammar.E));

        Console.WriteLine(grammar.P.Sum(rule => rule.Value.Count));
        foreach (var (left, rulesList) in grammar.P)
        {
            if (rulesList.Count == 0)
            {
                Console.WriteLine($"{left}\t-> {EmptyWord.Name}");
            }
            
            foreach (var right in rulesList)
            {
                var rightRules = string.Join(" ", right);
                Console.WriteLine($"{left}\t-> {rightRules}");
            }
        }

        Console.WriteLine(grammar.S);
    }
}