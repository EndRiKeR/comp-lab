// 1. По регулярному выражению строит НКА.
// 2. По НКА строит эквивалентный ему ДКА.
// 3. По ДКА строит эквивалентный ему КА, имеющий наименьшее возможное количество состояний по методу Бржовского.
// 4. Моделирует минимальный КА для входной цепочки из терминалов исходной грамматики

using System.Diagnostics;
using comp_lab1;
using comp_lab1.BrzozovskyAlgorithm;
using comp_lab1.Test;

bool isCompact = true;

// 1. По регулярному выражению строит НКА.
var grammar = "(a|b)+ab*";
// var grammar = "(a|b)*abb";
var regex = "cbcbcbcbabc";
var postfixWithSupport = new PostfixConverter(grammar);
// var postfixWithSupport = new PostfixConverter("(a|b)*_a_b_b");
var builder = new NfaBuilder();
var nfa = builder.CreateNfa(postfixWithSupport.PostfixExpr);
nfa.PrintConsole();
nfa.PrintDot("D:\\myProgects\\repLab\\comp-lab1\\dots\\nfa.dot");

// 2. По НКА строит эквивалентный ему ДКА.
// Замечание: -1 состояние - void
var dfaBuilder = new DfaBuilder();
var dfa = dfaBuilder.CreateDfa(nfa, isCompact);
dfa.PrintDfa( "D:\\myProgects\\repLab\\comp-lab1\\dots\\dfa.dot");

// 3. По ДКА строит эквивалентный ему КА, имеющий наименьшее возможное количество состояний по методу Бржовского.
var brgozovsky = new BrzozovskyMinimizator(dfaBuilder);
var minDfa = brgozovsky.Minimize(dfa, isCompact);
minDfa.PrintDfa( "D:\\myProgects\\repLab\\comp-lab1\\dots\\minDfa.dot");

// 4. Моделирует минимальный КА для входной цепочки из терминалов исходной грамматики
var tester = new DfaTester();
var testResult = tester.TestRegexWithDfa(regex, dfa);

Console.WriteLine($"Результат теста {regex} для грамматики {grammar}: {testResult}");