using System.Text.RegularExpressions;
using comp_lab.Labs.lab2.structs;

namespace comp_lab.Labs.lab2;

public class GrammarReaderForTokensIO
{
    private const string Epsilon = "ε";

    public string ReadFirstString(string filePath)
    {
        string[] allLines = File.ReadAllLines(filePath);
        return allLines[0];
    }
    
    public string[] ReadAllString(string filePath)
    {
        return File.ReadAllLines(filePath);
    }

    public Grammar ReadGrammar(string filePath)
    {
        string[] allLines = File.ReadAllLines(filePath);
        
        var lines = new List<string>();
        foreach (var raw in allLines)
        {
            string trimmed = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;
                
            string trimmedStart = trimmed.TrimStart();
            if (trimmedStart.StartsWith("//"))
                continue;
                
            lines.Add(trimmed);
        }

        var grammar = new Grammar
        {
            N = new HashSet<Nonterm>(),
            E = new HashSet<Term>(),
            P = new Dictionary<Nonterm, List<List<GrammarPart>>>()
        };

        var ruleStartRegex = new Regex(@"^\s*<([^>]+)>\s*->\s*(.*)$", RegexOptions.Compiled);

        Nonterm? currentLeft = null;
        var currentRightLines = new List<string>(); // Накапливаем строки правой части

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            var match = ruleStartRegex.Match(line);
            if (match.Success)
            {
                // Сохраняем предыдущее правило
                if (currentLeft != null)
                {
                    string fullRight = string.Join(" ", currentRightLines).Trim();
                    grammar.P[currentLeft] = ParseRightSide(fullRight);
                }

                string leftName = match.Groups[1].Value;
                currentLeft = new Nonterm(leftName);
                grammar.N.Add(currentLeft);

                string rightPart = match.Groups[2].Value.Trim();
                currentRightLines.Clear();
                if (!string.IsNullOrEmpty(rightPart))
                {
                    currentRightLines.Add(rightPart);
                }
            }
            else
            {
                if (currentLeft == null)
                    throw new FormatException($"Строка не является началом правила и нет активного левого нетерминала: {line}");

                currentRightLines.Add(line);
            }
        }

        if (currentLeft != null)
        {
            string fullRight = string.Join(" ", currentRightLines).Trim();
            grammar.P[currentLeft] = ParseRightSide(fullRight);
        }

        grammar.S = new Nonterm("программа");
        if (!grammar.N.Contains(grammar.S))
        {
            throw new Exception("В грамматике отсутствует начальный нетерминал <программа>.");
        }

        grammar.E = ExtractTerminals(grammar.P);
        return grammar;
    }

    private List<List<GrammarPart>> ParseRightSide(string rightText)
    {
        var alternatives = new List<List<GrammarPart>>();
        // Если правая часть пустая – это ε (например, A -> )
        if (string.IsNullOrEmpty(rightText))
        {
            var emptyList = new List<GrammarPart> { new Term(Epsilon) };
            alternatives.Add(emptyList);
            return alternatives;
        }

        var parts = SplitByPipeOutsideAngleBrackets(rightText);
        foreach (var part in parts)
        {
            string trimmedPart = part.Trim();
            // Пропускаем пустые фрагменты (например, от висящих '|')
            if (string.IsNullOrEmpty(trimmedPart))
                continue;

            var sequence = ParseSequence(trimmedPart);
            alternatives.Add(sequence);
        }

        return alternatives;
    }

    private string[] SplitByPipeOutsideAngleBrackets(string input)
    {
        var result = new List<string>();
        int lastIndex = 0;
        bool insideNonterm = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '<')
            {
                bool validNonterm = false;
                for (int j = i + 1; j < input.Length; j++)
                {
                    if (input[j] == '>')
                    {
                        string inside = input.Substring(i + 1, j - i - 1);
                        if (!string.IsNullOrEmpty(inside) && inside.IndexOf('<') == -1 && inside.IndexOf('>') == -1)
                        {
                            validNonterm = true;
                        }
                        break;
                    }
                    // Теперь останавливаемся только на другом '<', пробелы игнорируем
                    if (input[j] == '<')
                        break;
                }
                if (validNonterm)
                    insideNonterm = true;
            }
            else if (c == '>')
            {
                if (insideNonterm)
                    insideNonterm = false;
            }
            else if (c == '|' && !insideNonterm)
            {
                result.Add(input.Substring(lastIndex, i - lastIndex).Trim());
                lastIndex = i + 1;
            }
        }

        if (lastIndex < input.Length)
            result.Add(input.Substring(lastIndex).Trim());

        var array = new string[result.Count];
        for (int i = 0; i < result.Count; i++)
            array[i] = result[i];
        return array;
    }

    private List<GrammarPart> ParseSequence(string text)
    {
        var sequence = new List<GrammarPart>();
        if (string.IsNullOrEmpty(text))
        {
            sequence.Add(new Term(Epsilon));
            return sequence;
        }

        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '<')
            {
                int end = -1;
                bool isValidNonterm = false;

                for (int j = i + 1; j < text.Length; j++)
                {
                    if (text[j] == '>')
                    {
                        end = j;
                        break;
                    }
                    // Останавливаемся только на вложенном '<', чтобы избежать неоднозначностей
                    if (text[j] == '<')
                    {
                        break;
                    }
                    // Пробелы и любые другие символы допускаются внутри угловых скобок
                }

                if (end != -1)
                {
                    string inside = text.Substring(i + 1, end - i - 1);
                    // Дополнительная проверка: имя нетерминала не может содержать угловые скобки
                    if (!string.IsNullOrEmpty(inside) && inside.IndexOf('<') == -1 && inside.IndexOf('>') == -1)
                    {
                        isValidNonterm = true;
                    }
                }

                if (isValidNonterm)
                {
                    string name = text.Substring(i + 1, end - i - 1).Trim();
                    sequence.Add(new Nonterm(name));
                    i = end + 1;
                    continue;
                }
                else
                {
                    // Некорректная конструкция – обрабатываем '<' как терминал
                    sequence.Add(new Term("<"));
                    i++;
                }
            }
            else if (c == '>')
            {
                sequence.Add(new Term(">"));
                i++;
            }
            else if (c == 'ε')
            {
                sequence.Add(new Term(Epsilon));
                i++;
            }
            else if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else
            {
                int start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '<' && text[i] != '>')
                {
                    i++;
                }
                string termName = text.Substring(start, i - start);
                sequence.Add(new Term(termName));
            }
        }
        return sequence;
    }

    private HashSet<Term> ExtractTerminals(Dictionary<Nonterm, List<List<GrammarPart>>> productions)
    {
        var terminals = new HashSet<Term>();
        foreach (var rule in productions.Values)
        {
            foreach (var alternative in rule)
            {
                foreach (var part in alternative)
                {
                    if (part is Term term && term.Name != Epsilon)
                    {
                        terminals.Add(term);
                    }
                }
            }
        }
        return terminals;
    }

    public void WriteGrammar(Grammar grammar, string outFilePath)
    {
        using var writer = new StreamWriter(outFilePath);
        foreach (var kv in grammar.P)
        {
            var left = kv.Key;
            var alternatives = kv.Value;
            for (int i = 0; i < alternatives.Count; i++)
            {
                var alt = alternatives[i];
                var parts = new List<string>();
                foreach (var p in alt)
                {
                    if (p is Nonterm)
                        parts.Add($"<{p.Name}>");
                    else
                        parts.Add(p.Name);
                }
                string rightStr = string.Join(" ", parts);
                
                if (i == 0)
                    writer.WriteLine($"<{left.Name}> -> {rightStr}");
                else
                    writer.WriteLine($"\t| {rightStr}");
            }
            writer.WriteLine();
        }
    }
    
    public void WriteToConsoleGrammar(Grammar grammar)
    {
        foreach (var kv in grammar.P)
        {
            var left = kv.Key;
            var alternatives = kv.Value;
            for (int i = 0; i < alternatives.Count; i++)
            {
                var alt = alternatives[i];
                var parts = new List<string>();
                foreach (var p in alt)
                {
                    if (p is Nonterm)
                        parts.Add($"<{p.Name}>");
                    else
                        parts.Add(p.Name);
                }
                string rightStr = string.Join(" ", parts);
                
                if (i == 0)
                    Console.WriteLine($"<{left.Name}> -> {rightStr}");
                else
                    Console.WriteLine($"\t| {rightStr}");
            }
            Console.WriteLine();
        }
    }
    
    /// <summary>
    /// Разбирает входную строку выражения на токены согласно грамматике.
    /// </summary>
    public List<GrammarPart> Tokenize(string input)
    {
        var tokens = new List<GrammarPart>();
        int pos = 0;
        int length = input.Length;

        while (pos < length)
        {
            char current = input[pos];

            // Пропуск пробельных символов
            if (char.IsWhiteSpace(current))
            {
                pos++;
                continue;
            }

            // Идентификаторы и ключевые слова (начинаются с буквы)
            if (char.IsLetter(current))
            {
                int start = pos;
                while (pos < length && (char.IsLetterOrDigit(input[pos]) || input[pos] == '_'))
                    pos++;

                string word = input.Substring(start, pos - start);
                string lower = word.ToLowerInvariant();

                // Проверка на ключевые слова грамматики
                if (lower == "div" || lower == "mod" || lower == "and" || lower == "or" || lower == "not")
                {
                    tokens.Add(new Term(lower)); // ключевое слово как терминал
                }
                else
                {
                    tokens.Add(new Term(word));  // идентификатор
                }
                continue;
            }

            // Константы – последовательности цифр
            if (char.IsDigit(current))
            {
                int start = pos;
                while (pos < length && char.IsDigit(input[pos]))
                    pos++;

                string number = input.Substring(start, pos - start);
                tokens.Add(new Term(number));
                continue;
            }

            // Многосимвольные операторы: ==, !=, <=, >=
            if (current == '=' && pos + 1 < length && input[pos + 1] == '=')
            {
                tokens.Add(new Term("=="));
                pos += 2;
                continue;
            }
            if (current == '!' && pos + 1 < length && input[pos + 1] == '=')
            {
                tokens.Add(new Term("!="));
                pos += 2;
                continue;
            }
            if (current == '<' && pos + 1 < length && input[pos + 1] == '=')
            {
                tokens.Add(new Term("<="));
                pos += 2;
                continue;
            }
            if (current == '>' && pos + 1 < length && input[pos + 1] == '=')
            {
                tokens.Add(new Term(">="));
                pos += 2;
                continue;
            }

            // Одиночные операторы и разделители
            switch (current)
            {
                case '<':
                case '>':
                case '+':
                case '-':
                case '*':
                case '/':
                case ';':
                case '{':
                case '}':
                case '(':
                case ')':
                    tokens.Add(new Term(current.ToString()));
                    pos++;
                    break;

                default:
                    throw new Exception($"Неизвестный символ '{current}' в позиции {pos}");
            }
        }

        return tokens;
    }
}