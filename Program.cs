// 1. По регулярному выражению строит НКА.
// 2. По НКА строит эквивалентный ему ДКА.
// 3. По ДКА строит эквивалентный ему КА, имеющий наименьшее возможное количество состояний по методу Бржовского.
// 4. Моделирует минимальный КА для входной цепочки из терминалов исходной грамматики

using comp_lab1;

var rxc = new RegexConverter();
var output = rxc.SplitRegex("aa*b(abc+cba)ca");
output = rxc.SplitRegex("(a+(b(a*)))");
rxc.AddSupportToken(output);
var root = rxc.SplitBySupportTokens(output);
root.PrintTree();


// foreach (var token in output)
// {
//     Console.WriteLine(token);
// }
