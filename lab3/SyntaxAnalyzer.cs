// File: SyntaxAnalyzer.cs (изменённая версия)

using comp_lab.lab2.structs;

namespace comp_lab.lab3;

public class SyntaxAnalyzer
{
    private List<GrammarPart> _inList;
    private int _currentIndex;
    private GrammarPart _currentGrammar => _inList[_currentIndex];
    private bool _inBounds => _currentIndex < _inList.Count;
    
    private static readonly HashSet<string> ReservedWords = ["div", "mod", "and", "or", "not"];

    /// <summary>
    /// Выполняет синтаксический анализ и возвращает список строк в обратной польской нотации
    /// для каждого выражения внутри блока.
    /// </summary>
    public List<string>? Analyze(List<GrammarPart> list)
    {
        _inList = list;
        _currentIndex = 0;

        if (Program(out var expressions))
            return expressions;
        
        return null;
    }

    // <программа> -> <блок>
    private bool Program(out List<string> expressionsRpn)
    {
        expressionsRpn = null!;
        if (Block(out expressionsRpn))
            return true;
        return false;
    }
    
    // <блок> -> { <список выражений> }
    private bool Block(out List<string> expressionsRpn)
    {
        expressionsRpn = null!;
        if (!_inBounds || _currentGrammar is not Term { Name: "{" })
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <программа> ожидалось '{{'");
            return false;
        }
        _currentIndex++;  // прошли '{'
        
        if (!ListOfExpression(out expressionsRpn))
            return false;
        
        if (!_inBounds || _currentGrammar is not Term { Name: "}" })
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <список выражений> ожидалось '}}'");
            return false;
        }
        _currentIndex++;  // прошли '}'
        return true;
    }
    
    // <список выражений> -> <выражение> <хвост>
    private bool ListOfExpression(out List<string> expressionsRpn)
    {
        expressionsRpn = null!;
        if (!Expression(out string firstRpn))
            return false;
        
        if (!Tail(out List<string> tailRpn))
            return false;
        
        expressionsRpn = new List<string> { firstRpn };
        expressionsRpn.AddRange(tailRpn);
        return true;
    }
    
    // <хвост> -> ; <выражение> <хвост> | ε
    private bool Tail(out List<string> tailRpn)
    {
        tailRpn = new List<string>();
        if (!_inBounds) 
            return true; // ε-переход

        if (_currentGrammar is not Term { Name: ";" })
            return true; // ε-переход
        
        _currentIndex++; // прошли ';'

        if (!_inBounds)
        {
            Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После ';' ожидалось <выражение>");
            return false;
        }
        
        if (!Expression(out string exprRpn))
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После ';' ожидалось <выражение> или конец списка");
            return false;
        }
        
        if (!Tail(out List<string> restRpn))
            return false;
        
        tailRpn = new List<string> { exprRpn };
        tailRpn.AddRange(restRpn);
        return true;
    }
    
    // <выражение> -> <простое выражение> <выражение'>
    // где <выражение'> -> ε | <операция отношения> <простое выражение>
    private bool Expression(out string rpn)
    {
        rpn = null!;
        if (!SimpleExpression(out string leftRpn))
            return false;

        // Теперь проверим, есть ли операция отношения
        if (!_inBounds)
        {
            rpn = leftRpn;
            return true;
        }

        if (OperationsReferences(out string op))
        {
            _currentIndex++; // съедаем оператор
            if (!_inBounds)
            {
                Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После <операция отношения> ожидалось <простое выражение>");
                return false;
            }
            if (!SimpleExpression(out string rightRpn))
            {
                Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <операция отношения> ожидалось <простое выражение>");
                return false;
            }
            rpn = $"{leftRpn} {rightRpn} {op}";
            return true;
        }
        else
        {
            rpn = leftRpn;
            return true;
        }
    }
    
    // <простое выражение> -> <терм> <простое выражение'> | <знак> <терм> <простое выражение'>
    // где <простое выражение'> -> <операция типа сложения> <терм> <простое выражение'> | ε
    private bool SimpleExpression(out string rpn)
    {
        rpn = null!;
        // Вариант: <знак> <терм>
        if (Sign(out string sign))
        {
            _currentIndex++; // съедаем знак
            if (!_inBounds)
            {
                Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После знака '{sign}' ожидалось <терм>");
                return false;
            }
            if (!Term(out string termRpn))
            {
                Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После знака '{sign}' ожидалось <терм>");
                return false;
            }
            // Унарный минус: добавляем "u-" после операнда
            string initialRpn = sign == "-" ? $"{termRpn} u-" : termRpn;
            // Далее обрабатываем возможные операции сложения
            if (!SimpleExpressionStreak(initialRpn, out rpn))
                return false;
            return true;
        }
        // Вариант: <терм>
        else if (Term(out string baseRpn))
        {
            if (!SimpleExpressionStreak(baseRpn, out rpn))
                return false;
            return true;
        }
        
        Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! В <простое выражение> ожидалось <терм> или <знак>");
        return false;
    }
    
    // <простое выражение'> -> <операция типа сложения> <терм> <простое выражение'> | ε
    private bool SimpleExpressionStreak(string leftRpn, out string resultRpn)
    {
        resultRpn = leftRpn;
        // Если нет операции сложения – ε-переход
        if (!_inBounds) return true;
        if (!OperationsInComplications(out string op))
            return true;
        
        _currentIndex++; // съедаем оператор
        if (!_inBounds)
        {
            Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После <операция типа сложения> ожидался <терм>");
            return false;
        }
        if (!Term(out string termRpn))
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <операция типа сложения> ожидался <терм>");
            return false;
        }
        string newLeft = $"{leftRpn} {termRpn} {op}";
        if (!SimpleExpressionStreak(newLeft, out resultRpn))
            return false;
        return true;
    }
    
    // <терм> -> <фактор> <терм'>
    private bool Term(out string rpn)
    {
        rpn = null!;
        if (!Factor(out string factorRpn))
            return false;
        if (!TermStreak(factorRpn, out rpn))
            return false;
        return true;
    }
    
    // <терм'> -> <операция типа умножения> <фактор> <терм'> | ε
    private bool TermStreak(string leftRpn, out string resultRpn)
    {
        resultRpn = leftRpn;
        if (!_inBounds) return true;
        if (!MultiplicationOperationType(out string op))
            return true;
        
        _currentIndex++; // съедаем оператор
        if (!_inBounds)
        {
            Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После <операция типа умножения> ожидался <фактор>");
            return false;
        }
        if (!Factor(out string factorRpn))
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <операция типа умножения> ожидался <фактор>");
            return false;
        }
        string newLeft = $"{leftRpn} {factorRpn} {op}";
        if (!TermStreak(newLeft, out resultRpn))
            return false;
        return true;
    }
    
    // <фактор> -> <идентификатор> | <константа> | ( <простое выражение> ) | not <фактор>
    private bool Factor(out string rpn)
    {
        rpn = null!;
        if (!_inBounds) return false;
        
        // идентификатор
        if (Identifier())
        {
            rpn = _currentGrammar.ToString();
            _currentIndex++;
            return true;
        }
        // константа
        if (Constants())
        {
            rpn = _currentGrammar.ToString();
            _currentIndex++;
            return true;
        }
        // ( <простое выражение> )
        if (_currentGrammar is Term { Name: "(" })
        {
            _currentIndex++;
            if (!SimpleExpression(out string innerRpn))
                return false;
            if (!_inBounds || _currentGrammar is not Term { Name: ")" })
            {
                Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <простое выражение> ожидалось ')'");
                return false;
            }
            _currentIndex++;
            rpn = innerRpn; // скобки не добавляются в ОПН
            return true;
        }
        // not <фактор>
        if (_currentGrammar is Term { Name: "not" })
        {
            _currentIndex++;
            if (!Factor(out string operandRpn))
                return false;
            rpn = $"{operandRpn} not";
            return true;
        }
        
        return false;
    }
    
    // Вспомогательные методы для распознавания операторов и возврата их строкового представления
    
    private bool OperationsReferences(out string op)
    {
        op = null!;
        if (!_inBounds) return false;
        switch (_currentGrammar)
        {
            case Term { Name: "==" }: op = "=="; return true;
            case Term { Name: "!=" }: op = "!="; return true;
            case Term { Name: "<" }:  op = "<";  return true;
            case Term { Name: "<=" }: op = "<="; return true;
            case Term { Name: ">" }:  op = ">";  return true;
            case Term { Name: ">=" }: op = ">="; return true;
            default: return false;
        }
    }
    
    private bool Sign(out string sign)
    {
        sign = null!;
        if (!_inBounds) return false;
        switch (_currentGrammar)
        {
            case Term { Name: "+" }: sign = "+"; return true;
            case Term { Name: "-" }: sign = "-"; return true;
            default: return false;
        }
    }
    
    private bool OperationsInComplications(out string op)
    {
        op = null!;
        if (!_inBounds) return false;
        switch (_currentGrammar)
        {
            case Term { Name: "+" }:  op = "+";  return true;
            case Term { Name: "-" }:  op = "-";  return true;
            case Term { Name: "or" }: op = "or"; return true;
            default: return false;
        }
    }
    
    private bool MultiplicationOperationType(out string op)
    {
        op = null!;
        if (!_inBounds) return false;
        switch (_currentGrammar)
        {
            case Term { Name: "*" }:   op = "*";   return true;
            case Term { Name: "/" }:   op = "/";   return true;
            case Term { Name: "div" }: op = "div"; return true;
            case Term { Name: "mod" }: op = "mod"; return true;
            case Term { Name: "and" }: op = "and"; return true;
            default: return false;
        }
    }

    private bool Constants()
    {
        if (!_inBounds) return false;
        if (_currentGrammar is Term term)
        {
            return term.Name.Length > 0 && term.Name.All(char.IsDigit);
        }
        return false;
    }

    private bool Identifier()
    {
        if (!_inBounds) return false;
        if (_currentGrammar is Term term)
        {
            string name = term.Name;
            if (name.Length == 0) return false;
            if (!name.All(char.IsLetter)) return false;
            return !ReservedWords.Contains(name.ToLowerInvariant());
        }
        return false;
    }
}