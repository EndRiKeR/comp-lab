// 1. По регулярному выражению строит НКА.
// 2. По НКА строит эквивалентный ему ДКА.
// 3. По ДКА строит эквивалентный ему КА, имеющий наименьшее возможное количество состояний по методу Бржовского.
// 4. Моделирует минимальный КА для входной цепочки из терминалов исходной грамматики

using comp_lab1;

List<string> testRegex = new()
{
    "ab+c",                 // тест 1
    "(a|b)+ab*",            // тест 2
    "(a|b)*abb",            // из методички
    "(abc|cba*)*|(b(a*))"   // мой сложный пример
};

List<List<string>> testWords = new()
{
    new List<string>()      // для "ab+c"
    {
        "abc",
        "aabc",
        "abbbbc",
        "adsfr",
        "ab"
    },
    new List<string>()      // для "(a|b)+ab*"
    {
        "aab",
        "baba",
        "abababab",
        "cdcd",
        "aaaaaaab",
        "bbbbbbbbbbb"
    },
    new List<string>()      // для "(a|b)*abb"
    {
        "aab",
        "baba",
        "abababab",
        "cdcd",
        "aaaaaaabb",
        "bbbbbbbbbbb"
    },
    new List<string>()      // "(abc|cba*)*|(b(a*))"
    {
        "abccb",
        "cbacb",
        "baaaaa",
        "cbcbb",
        "aaaaaaab",
        "abcabcabcba"
    },
};

var worker = new LabWorker();

bool isCompact = true;
bool withOutput = false;

for (int i = 0; i < testWords.Count; i++)
{
    worker.CreateDfa(testRegex[i], isCompact, withOutput, i, out var dfa, out var minDfa);
    worker.TestWords(dfa, testWords[i], testRegex[i]);
}

// worker.CreateDfa(testRegex[i], isCompact, out var dfa, out var minDfa);
// worker.TestWords(dfa, testWords[i], testRegex[i]);

