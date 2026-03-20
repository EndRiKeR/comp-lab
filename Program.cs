// 1. По регулярному выражению строит НКА.
// 2. По НКА строит эквивалентный ему ДКА.
// 3. По ДКА строит эквивалентный ему КА, имеющий наименьшее возможное количество состояний по методу Бржовского.
// 4. Моделирует минимальный КА для входной цепочки из терминалов исходной грамматики

using comp_lab1;

var postfixWithSupport = new PostfixConverter("(a_b_c+c_b_a*)*+(b_(a*))");
// "a b _c _c b _a *_+*b a *_+";
var builder = new NfaBuilder();
var nfa = builder.CreateNfa(postfixWithSupport.PostfixExpr);
nfa.PrintAscii();
nfa.PrintDot("D:\\myProgects\\repLab\\comp-lab1\\nfa.dot");
