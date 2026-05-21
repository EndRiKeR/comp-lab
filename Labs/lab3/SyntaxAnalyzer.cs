using comp_lab.Labs.lab2.structs;

namespace comp_lab.Labs.lab3;

public class SyntaxAnalyzer
{
    private List<GrammarPart> _inList;
    private int _currentIndex;
    private GrammarPart _currentGrammar => _inList[_currentIndex];
    private bool _inBounds => _currentIndex < _inList.Count;
    
    private static readonly HashSet<string> ReservedWords = ["div", "mod", "and", "or", "not"];

    public bool Analyze(List<GrammarPart> list)
    {
        _inList = list;
        _currentIndex = 0;

        if (Program())
        {
            return true;
        }
        else
        {
            Console.WriteLine();
            return false;
        }
    }

    private bool Program() // <программа> -> <блок>
    {
        return Block();
    }
    
    private bool Block() // <блок> -> { <список выражений> }
    {
        if (!_inBounds || _currentGrammar is not Term { Name: "{" })
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <программа> ожидалось '{{'");
            return false;
        }
        
        _currentIndex++;  // прошли '{'
        
        if (!ListOfExpression())
            return false;
        
        // здесь индекс уже стоит на '}'
        if (!_inBounds || _currentGrammar is not Term { Name: "}" })
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <список выражений> ожидалось '}}'");
            return false;
        }

        _currentIndex++;  // прошли '}'
        return true;
    }
    
    private bool ListOfExpression() // <список выражений> -> <выражение> <хвост>
    {
        if (!Expression())
            return false;
        
        if (!Tail())
            return false;

        return true;
    }
    
    private bool Tail() // <хвост> -> ; <выражение> <хвост> | ε
    {
        if (!_inBounds) // ловим пустое слово
            return true;

        if (_currentGrammar is not Term { Name: ";" })
            return true; // ε-переход
        
        _currentIndex++;

        if (!_inBounds)
        {
            Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После ';' ожидалось <выражение>");
            return false;
        }
        
        if (!Expression())
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После ';' ожидалось <выражение> или конец списка");
            return false;
        }
        
        if (!Tail())
            return false;
        
        return true;
    }
    
    private bool Expression() // <выражение> -> <простое выражение> <выражение'>
    {
        if (!SimpleExpression())
            return false;

        if (!ExpressionStreak())
            return false;
        
        return true;
    }
    
    private bool ExpressionStreak() // <выражение'> -> ε | <операция отношения> <простое выражение>
    {
        if (!_inBounds) // ловим пустое слово
            return true;
        
        if (!OperationsReferences())
            return true; // ε-переход
        
        _currentIndex++;
        
        if (!_inBounds)
        {
            Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После <операция отношения> ожидалось <простое выражение>");
            return false;
        }
        
        if (!SimpleExpression())
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <операция отношения> ожидалось <простое выражение>");
            return false;
        }
        
        return true;
    }
    
    private bool SimpleExpression() // <простое выражение> -> <терм> <простое выражение'> | <знак> <терм> <простое выражение'>
    {
        if (Term())
        {
            return SimpleExpressionStreak();
        }
        
        if (Sign())
        {
            _currentIndex++; // сдвиг на знак

            if (!_inBounds)
            {
                Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После знака '+' или '-' ожидалось <терм>");
                return false;
            }
            
            if (!Term())
            {
                Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После знака '+' или '-' ожидалось <терм>");
                return false;
            }
            
            return SimpleExpressionStreak();
        }
        
        if (!_inBounds)
        {
            Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! В <простое выражение> ожидалось <терм> или <знак>");
            return false;
        }
        
        Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! В <простое выражение> ожидалось <терм> или <знак>");
        return false;
    }
    
    private bool SimpleExpressionStreak() // <простое выражение'> -> <операция типа сложения> <терм> <простое выражение'> | ε
    {
        if (!_inBounds) // ловим пустое слово
            return true;
        
        if (!OperationsInComplications())
            return true; // ε-переход
        
        _currentIndex++;
        
        if (!_inBounds)
        {
            Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После <операция типа сложения> ожидался <терм>");
            return false;
        }
        
        if (!Term())
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <операция типа сложения> ожидался <терм>");
            return false;
        }
        
        if (!SimpleExpressionStreak())
            return false;
        
        return true;
    }
    
    private bool Term() // <терм> -> <фактор> <терм'>
    {
        if (!Factor())
            return false;
        
        if (!TermStreak())
            return false;
        
        return true;
    }
    
    private bool TermStreak() // <терм'> -> <операция типа умножения> <фактор> <терм'> | ε
    {
        if (!_inBounds) // ловим пустое слово
            return true;
        
        if (!MultiplicationOperationType())
            return true;  // ε-переход
        
        _currentIndex++;
        
        if (!_inBounds)
        {
            Console.WriteLine($"Символ 'конец' (индекс {_currentIndex}): Ошибка! После <операция типа умножения> ожидался <фактор>");
            return false;
        }
        
        if (!Factor())
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <операция типа умножения> ожидался <фактор>");
            return false;
        }
        
        if (!TermStreak())
            return false;
        
        return true;
    }
    
    private bool Factor() // <фактор> -> <идентификатор> | <константа> | ( <простое выражение> ) | not <фактор>
    {
        if (!_inBounds)
            return false;
        
        if (Identifier())
        {
            _currentIndex++;
            return true;
        }
        
        if (Constants())
        {
            _currentIndex++;
            return true;
        }

        if (_currentGrammar is Term { Name: "(" })
        {
            _currentIndex++;
            
            if (!SimpleExpression())
                return false;
            
            if (!_inBounds || _currentGrammar is not Term { Name: ")" })
            {
                Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <простое выражение> ожидалось ')'");
                return false;
            }
            
            _currentIndex++;
            return true;
        }
        
        if (_currentGrammar is Term { Name: "not" })
        {
            _currentIndex++;
            
            if (!Factor())
                return false;
            
            return true;
        }
        
        return false;
    }
    
    private bool OperationsReferences() // <операция отношения> -> == | != | < | <= | > | >=
    {
        if (!_inBounds) return false;
        switch (_currentGrammar)
        {
            case Term { Name: "==" }:
            case Term { Name: "!=" }:
            case Term { Name: "<" }:
            case Term { Name: "<=" }:
            case Term { Name: ">" }:
            case Term { Name: ">=" }:
                return true;
            default:
                return false;
        }
    }
    
    private bool Sign() // <знак> -> + | -
    {
        if (!_inBounds) return false;
        switch (_currentGrammar)
        {
            case Term { Name: "+" }:
            case Term { Name: "-" }:
                return true;
            default:
                return false;
        }
    }
    
    private bool OperationsInComplications() // <операция типа сложения> -> + | - | or
    {
        if (!_inBounds) return false;
        switch (_currentGrammar)
        {
            case Term { Name: "+" }:
            case Term { Name: "-" }:
            case Term { Name: "or" }:
                return true;
            default:
                return false;
        }
    }
    
    private bool MultiplicationOperationType() // <операция типа умножения> -> * | / | div | mod | and
    {
        if (!_inBounds) return false;
        switch (_currentGrammar)
        {
            case Term { Name: "*" }:
            case Term { Name: "/" }:
            case Term { Name: "div" }:
            case Term { Name: "mod" }:
            case Term { Name: "and" }:
                return true;
            default:
                return false;
        }
    }

    private bool Constants() // строка, состоящая только из цифр (хотя бы одна)
    {
        if (!_inBounds) return false;
        if (_currentGrammar is Term term)
        {
            return term.Name.Length > 0 && term.Name.All(char.IsDigit);
        }
        return false;
    }

    private bool Identifier() // строка, состоящая только из букв (без зарезервированных слов)
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