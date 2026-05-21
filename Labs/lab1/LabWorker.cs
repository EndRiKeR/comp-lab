using comp_lab.Labs.lab1.BrzozovskyAlgorithm;
using comp_lab.Labs.lab1.Dfa;
using comp_lab.Labs.lab1.Nfa;
using comp_lab.Labs.lab1.Test;

namespace comp_lab.Labs.lab1;

public class LabWorker
{
    public void CreateDfa(string testRegex, bool isCompact, bool withOutput, int i, out Dfa.Dfa dfa, out Dfa.Dfa minDfa)
    {
        Console.WriteLine($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        Console.WriteLine($"Начинаем работу с регулярным выражением {testRegex}");
        var postfixWithSupport = new PostfixConverter(testRegex);
        var builder = new NfaBuilder();
        var nfa = builder.CreateNfa(postfixWithSupport.PostfixExpr);

        if (withOutput)
        {
            Console.WriteLine("НКА");
            nfa.PrintConsole();
        }
        
        nfa.PrintDot($"D:\\myProgects\\repLab\\comp-lab1\\dots\\nfa_{i}.dot");

        // Замечание: -1 состояние - void
        var dfaBuilder = new DfaBuilder();
        dfa = dfaBuilder.CreateDfa(nfa, isCompact);
        dfa.PrintDfa( $"D:\\myProgects\\repLab\\comp-lab1\\dots\\dfa_{i}.dot");

        var brgozovsky = new BrzozovskyMinimizator(dfaBuilder);
        minDfa = brgozovsky.Minimize(dfa, isCompact, withOutput);
        minDfa.PrintDfa( $"D:\\myProgects\\repLab\\comp-lab1\\dots\\minDfa_{i}.dot");
    }

    public void TestWords(Dfa.Dfa dfa, List<string> testWords, string regex)
    {
        Console.WriteLine();
        Console.WriteLine($"Проверяем слова на принадлежность грамматике {regex}");
        int maxTabs = testWords.Max(w => w.Length) / 6 + 1;
        string tab = "\t";
        
        foreach (var word in testWords)
        {
            var tester = new DfaTester();
            var testResult = tester.TestRegexWithDfa(word, dfa);
            
            int tabsCount = maxTabs - word.Length / 6;
            var tabs = "";
            for (int i = 0; i < tabsCount; i++)
                tabs += tab;
            
            Console.WriteLine($"Результат теста {word}: {tabs}{testResult}");
        }
        Console.WriteLine($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        Console.WriteLine();
        Console.WriteLine();
    }
}

