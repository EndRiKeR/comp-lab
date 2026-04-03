using comp_lab.lab2;

// Общая часть
// 1. Постройте программу, которая принимает приведенную КС-грамматику G = (N, E, P, S)
string inPath = "D:\\myProgects\\repLab\\comp-lab\\lab2\\files\\In_4_9.txt";
string outPath = "D:\\myProgects\\repLab\\comp-lab\\lab2\\files\\Out.txt";
var reader = new GrammarReaderIO();
var inGrammar = reader.ReadGrammar(inPath);

// 2. И преобразует ее в эквивалентную КС-грамматику G' без левой рекурсии.
// 2.1. Устранение левой рекурсии. Воспользоваться алгоритмом 2.13 [1] и 4.8 [2]
var eliminator = new LeftRecursionEliminator();
var outGrammar = eliminator.EliminateLeftRecursion(inGrammar);
Console.WriteLine("Устранили левые рекурсии");
reader.WriteGrammarToConsole(outGrammar);

// 2.2. Левая факторизация. Воспользоваться алгоритмами и 4.10 [2]
outGrammar = eliminator.LeftFactorization(outGrammar);
Console.WriteLine("\nЛевая факторизация");
reader.WriteGrammarToConsole(outGrammar);
reader.WriteGrammar(outGrammar, outPath);

// Вариант 4
// 1. Постройте программу, которая принимает приведенную КС-грамматику G = (N, E, P, S) без e-правил
// 2. И преобразует ее в эквивалентную КС-грамматику G'  без e-правил и без цепных правил.
// Указания. Воспользоваться алгоритмом 2.11. [1]. При тестировании воспользоваться примером 2.24. [1].
