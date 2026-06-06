using comp_lab.lab2;
using comp_lab.lab3;

string inUserPath = "C:\\Work\\Repos\\comp-lab1\\lab2\\files\\InUser.txt";
string inGrammarPath = "C:\\Work\\Repos\\comp-lab1\\lab2\\files\\InTokens.txt";
string outGrammarPath = "C:\\Work\\Repos\\comp-lab1\\lab2\\files\\OutTokens.txt";

var reader = new GrammarReaderForTokensIO();
Console.WriteLine("Оригинальная грамматика");
var inGrammar = reader.ReadGrammar(inGrammarPath);
reader.WriteToConsoleGrammar(inGrammar);

var eliminator = new LeftRecursionEliminator();
Console.WriteLine("Устранение левых рекурсий");
var outGrammar = eliminator.EliminateLeftRecursion(inGrammar);
reader.WriteToConsoleGrammar(outGrammar);

Console.WriteLine("\nЛевая факторизация");
outGrammar = eliminator.LeftFactorization(outGrammar);
reader.WriteToConsoleGrammar(outGrammar);
reader.WriteGrammar(outGrammar, outGrammarPath);

var userInputs = reader.ReadAllString(inUserPath);
foreach (var userInput in userInputs)
{
    Console.WriteLine("\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
    Console.WriteLine($"Выражение: {userInput}");
    
    try
    {
        var tokens = reader.Tokenize(userInput);
        var analyzer = new SyntaxAnalyzer();
        var result = analyzer.Analyze(tokens);

        if (result != null)
        {
            Console.WriteLine("All ok =)");
            Console.WriteLine("Префиксная нотация:");
            foreach (var exprRpn in result)
                Console.WriteLine(exprRpn);
        }
        else
        {
            Console.WriteLine("All bad =(");
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
    
    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
}
