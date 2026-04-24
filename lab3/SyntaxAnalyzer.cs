using comp_lab.lab2.structs;

namespace comp_lab.lab3;

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

        if (Программа())
        {
            return true;
        }
        else
        {
            Console.WriteLine();
            return false;
        }
    }

    private bool Программа() // <программа> -> <блок>
    {
        return Блок();
    }
    
    private bool Блок() // <блок> -> { <список выражений> }
    {
        if (_currentGrammar is not Term { Name: "{" })
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <программа> ожидалось '{{'");
            return false;
        }
        
        _currentIndex++;
        
        if (!СписокВыражений())
            return false;
        
        _currentIndex++;
        
        if (_currentGrammar is not Term { Name: "}" })
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <список выражений> ожидалось '}}'");
            return false;
        }

        return true;
    }
    
    private bool СписокВыражений() // <список выражений> -> <выражение> <хвост>
    {
        if (!Выражение())
            return false;
        
        _currentIndex++;
        
        if (!Хвост())
            return false;

        return true;
    }
    
    private bool Хвост() // <хвост> -> ; <выражение> <хвост> | ε
    {
        if (!_inBounds) // ловим пустое слово
            return true;

        if (_currentGrammar is not Term { Name: ";" })
            return true;
        
        _currentIndex++;

        if (!Выражение())
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После ';' ожидалось <выражение> или конец списка");
            return false;
        }
        
        if (!Хвост())
            return false;
        
        return true;
    }
    
    private bool Выражение() // <выражение> -> <простое выражение> <выражение'>
    {
        if (!ПростоеВыражение())
            return false;
        
        _currentIndex++;

        if (!ВыражениеШтрих())
            return false;
        
        return true;
    }
    
    private bool ВыражениеШтрих() // <выражение'> -> ε | <операция отношения> <простое выражение>
    {
        if (!_inBounds) // ловим пустое слово
            return true;
        
        if (!ОперацияОтношения())
            return true; // значит пустое слово
        
        _currentIndex++;
        
        if (!ПростоеВыражение())
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <операция отношения> ожидалось <простое выражение>");
            return false;
        }
        
        return true;
    }
    
    private bool ПростоеВыражение() // <простое выражение> -> <терм> <простое выражение'> | <знак> <терм> <простое выражение'>
    {
        if (Терм())
        {
            _currentIndex++;
            
            return ПростоеВыражениеШтрих();
        }
        
        if (Знак())
        {
            _currentIndex++;
            
            if (!Терм())
            {
                Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После знака '+' или '-' ожидалось <терм>");
                return false;
            }
            
            _currentIndex++;
            
            return ПростоеВыражениеШтрих();
        }
        
        Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! В <простое выражение> ожидалось <терм> или <знак>");
        return false;
    }
    
    private bool ПростоеВыражениеШтрих() // <простое выражение'> -> <операция типа сложения> <терм> <простое выражение'> | ε
    {
        if (!_inBounds) // ловим пустое слово
            return true;
        
        if (!ОперацияТипаСложения())
            return true; // значит пустое слово
        
        _currentIndex++;
        
        if (!Терм()) // Терм() сам выводит ошибку
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <операция типа сложения> ожидался <терм>");
            return false;
        }
        
        _currentIndex++;
        
        if (!ПростоеВыражениеШтрих())
            return false;
        
        return true;
    }
    
    private bool Терм() // <терм> -> <фактор> <терм'>
    {
        if (!Фактор())
            return false;
        
        _currentIndex++;
        
        if (!ТермШтрих())
            return false;
        
        return true;
    }
    
    private bool ТермШтрих() // <терм'> -> <операция типа умножения> <фактор> <терм'> | ε
    {
        if (!_inBounds) // ловим пустое слово
            return true;
        
        if (!ОперацияТипаУмножения())
            return true;  // значит пустое слово
        
        _currentIndex++;
        
        if (!Фактор())
        {
            Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <операция типа умножения> ожидался <фактор>");
            return false;
        }
        
        _currentIndex++;
        
        if (!ТермШтрих())
            return false;
        
        return true;
    }
    
    private bool Фактор() // <фактор> -> <идентификатор> | <константа> | ( <простое выражение> ) | not <фактор>
    {
        if (Идентификатор())
            return true;
        
        if (Константа())
            return true;

        if (_currentGrammar is Term { Name: "(" })
        {
            _currentIndex++;
            
            if (!ПростоеВыражение())
                return false;
            
            _currentIndex++;
            
            if (_currentGrammar is not Term { Name: ")" })
            {
                Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! После <простое выражение> ожидалось ')'");
                return false;
            }
            
            return true;
        }
        
        if (_currentGrammar is Term { Name: "not" })
        {
            _currentIndex++;
            
            if (!Фактор())
                return false;
            
            return true;
        }
        
        Console.WriteLine($"Символ '{(_inBounds ? _currentGrammar.ToString() : "конец")}' (индекс {_currentIndex}): Ошибка! В <фактор> ожидалось <идентификатор>, <константа>, '(' или 'not'");
        return false;
    }
    
    private bool ОперацияОтношения() // <операция отношения> -> == | != | < | <= | > | >=
    {
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
    
    private bool Знак() // <знак> -> + | -
    {
        switch (_currentGrammar)
        {
            case Term { Name: "+" }:
            case Term { Name: "-" }:
                return true;
            default:
                return false;
        }
    }
    
    private bool ОперацияТипаСложения() // <операция типа сложения> -> + | - | or
    {
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
    
    private bool ОперацияТипаУмножения() // <операция типа умножения> -> * | / | div | mod | and
    {
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

    private bool Константа() // строка, состоящая только из цифр (хотя бы одна)
    {
        if (_currentGrammar is Term term)
        {
            return term.Name.Length > 0 && term.Name.All(char.IsDigit);
        }
        
        return false;
    }

    private bool Идентификатор() // строка, состоящая только из букв (без зарезервированных слов)
    {
        if (_currentGrammar is Term term)
        {
            string name = term.Name;

            if (name.Length == 0)
                return false;

            if (!name.All(char.IsLetter))
                return false;

            return !ReservedWords.Contains(name.ToLowerInvariant());
        }
        
        return false;
    }
}