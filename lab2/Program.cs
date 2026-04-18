// using comp_lab.lab2;
//
// // Общая часть
// // 1. Постройте программу, которая принимает приведенную КС-грамматику G = (N, E, P, S)
// Console.WriteLine("Введите название тест-файла (In, In_4_7, In_4_9, In_4_11)");
// string input = Console.ReadLine();
// string inPath = $"C:\\Work\\Repos\\comp-lab1\\lab2\\files\\{input}.txt";
// string outPath = "C:\\Work\\Repos\\comp-lab1\\lab2\\files\\Out.txt";
// var reader = new GrammarReaderIO();
// Console.WriteLine("Оригинальная грамматика");
// var inGrammar = reader.ReadGrammar(inPath);
// reader.WriteGrammarToConsole(inGrammar);
//
// // 2. И преобразует ее в эквивалентную КС-грамматику G' без левой рекурсии.
// // 2.1. Устранение левой рекурсии. Воспользоваться алгоритмом 2.13 [1] и 4.8 [2]
// var eliminator = new LeftRecursionEliminator();
// var outGrammar = eliminator.EliminateLeftRecursion(inGrammar);
// Console.WriteLine("Устранение левых рекурсий");
// reader.WriteGrammarToConsole(outGrammar);
// reader.WriteGrammar(outGrammar, outPath);
//
// // 2.2. Левая факторизация. Воспользоваться алгоритмами и 4.10 [2]
// outGrammar = eliminator.LeftFactorization(outGrammar);
// Console.WriteLine("\nЛевая факторизация");
// reader.WriteGrammarToConsole(outGrammar);
// // reader.WriteGrammar(outGrammar, outPath);
//
// // Вариант 4
// // 1. Постройте программу, которая принимает приведенную КС-грамматику G = (N, E, P, S) без e-правил
// var chainGrammar = reader.ReadGrammar(inPath, true);
// // 2. И преобразует ее в эквивалентную КС-грамматику G'  без e-правил и без цепных правил.
// // Указания. Воспользоваться алгоритмом 2.11. [1]. При тестировании воспользоваться примером 2.24. [1].
// var chainKiller = new ChainKiller();
// var clearGrammar = chainKiller.FindAndDestroyChains(chainGrammar);
// Console.WriteLine("\nБез цепных правил");
// reader.WriteGrammarToConsole(clearGrammar);

using comp_lab.lab2;

var reader = new GrammarReaderForTokensIO();
var grammar = reader.ReadGrammar("C:\\Work\\Repos\\comp-lab1\\lab2\\files\\InTokens.txt");
reader.WriteGrammar(grammar, "C:\\Work\\Repos\\comp-lab1\\lab2\\files\\OutTokens.txt");