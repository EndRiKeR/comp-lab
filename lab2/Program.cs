using comp_lab.lab2;
using comp_lab.lab3;

string inUserPath = "D:\\myProgects\\repLab\\comp-lab\\lab2\\files\\InUser.txt";
string inGrammarPath = "D:\\myProgects\\repLab\\comp-lab\\lab2\\files\\InTokens.txt";
string outGrammarPath = "D:\\myProgects\\repLab\\comp-lab\\lab2\\files\\OutTokens.txt";
var reader = new GrammarReaderForTokensIO();
Console.WriteLine("Оригинальная грамматика");
var inGrammar = reader.ReadGrammar(inGrammarPath);

var eliminator = new LeftRecursionEliminator();
Console.WriteLine("Устранение левых рекурсий");
var outGrammar = eliminator.EliminateLeftRecursion(inGrammar);
reader.WriteToConsoleGrammar(outGrammar);

Console.WriteLine("\nЛевая факторизация");
outGrammar = eliminator.LeftFactorization(outGrammar);
reader.WriteToConsoleGrammar(outGrammar);
reader.WriteGrammar(outGrammar, outGrammarPath);


var userInput = reader.ReadFirstString(inUserPath);
var tokens = reader.Tokenize(userInput);

var analyzer = new SyntaxAnalyzer();
var result = analyzer.Analyze(tokens);

if (result)
    Console.WriteLine("All ok");
else
    Console.WriteLine("All bad =(");