// 1. По регулярному выражению строит НКА.
// 2. По НКА строит эквивалентный ему ДКА.
// 3. По ДКА строит эквивалентный ему КА, имеющий наименьшее возможное количество состояний по методу Бржовского.
// 4. Моделирует минимальный КА для входной цепочки из терминалов исходной грамматики

using comp_lab1;
using comp_lab1.BrzozovskyAlgorithm;

// 1. По регулярному выражению строит НКА.
var postfixWithSupport = new PostfixConverter("(a_b_c|c_b_a*)*|(b_(a*))");
// var postfixWithSupport = new PostfixConverter("(a|b)*_a_b_b");
// ожидаем - "a b _c _c b _a *_|*b a *_|";
var builder = new NfaBuilder();
var nfa = builder.CreateNfa(postfixWithSupport.PostfixExpr);
nfa.PrintAscii();
nfa.PrintDot("D:\\myProgects\\repLab\\comp-lab1\\dots\\nfa.dot");

// 2. По НКА строит эквивалентный ему ДКА.
// Замечание: -1 состояние - void
var dfaBuilder = new DfaBuilder();
var dfa = dfaBuilder.CreateDfa(nfa);
dfa.PrintDfa( "D:\\myProgects\\repLab\\comp-lab1\\dots\\dfa.dot");

// 3. По ДКА строит эквивалентный ему КА, имеющий наименьшее возможное количество состояний по методу Бржовского.
var brgozovsky = new BrzozovskyMinimizator(dfaBuilder);
var minDfa = brgozovsky.Minimize(dfa);
minDfa.PrintDfa( "D:\\myProgects\\repLab\\comp-lab1\\dots\\minDfa.dot");

