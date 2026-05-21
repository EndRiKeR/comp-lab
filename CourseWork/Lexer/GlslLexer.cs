using System.Text;
using comp_lab.Labs.lab2.structs;

namespace comp_lab.Labs.glsl;

public class GlslLexer
{
    private string _input;
    private int _position;
    private int _length;

    // Множество ключевых слов GLSL (упрощённое)
    private static readonly HashSet<string> Keywords = new HashSet<string>
    {
        "const", "uniform", "buffer", "readonly", "in", "out", "inout",
        "struct", "void", "bool", "int", "uint", "float", "double",
        "vec2", "vec3", "vec4", "dvec2", "dvec3", "dvec4",
        "true", "false",
        "if", "else", "switch", "case", "default",
        "while", "do", "for", "break", "continue", "return", "discard",
        "layout"
    };

    public List<GrammarPart> Tokenize(string input)
    {
        _input = input;
        _position = 0;
        _length = input.Length;
        var tokens = new List<GrammarPart>();

        while (_position < _length)
        {
            char c = CurrentChar();

            // Пропуск пробелов и управляющих символов
            if (char.IsWhiteSpace(c))
            {
                _position++;
                continue;
            }

            // Комментарии
            if (c == '/')
            {
                if (PeekChar() == '/')
                {
                    SkipLineComment();
                    continue;
                }
                if (PeekChar() == '*')
                {
                    SkipBlockComment();
                    continue;
                }
            }

            // Директивы препроцессора (начинаются с #)
            if (c == '#')
            {
                SkipDirective();
                continue;
            }

            // Идентификаторы и ключевые слова
            if (char.IsLetter(c) || c == '_')
            {
                string word = ReadIdentifier();
                if (Keywords.Contains(word))
                {
                    tokens.Add(new Term(word, TokenKind.Keyword));
                }
                else
                {
                    tokens.Add(new Term(word, TokenKind.Identifier));
                }
                continue;
            }

            // Числовые литералы
            if (char.IsDigit(c) || (c == '.' && PeekChar() >= '0' && PeekChar() <= '9'))
            {
                tokens.Add(ReadNumber());
                continue;
            }

            // Многосимвольные операторы
            string op = ReadMultiCharOperator();
            if (op != null)
            {
                tokens.Add(new Term(op, TokenKind.Operator));
                continue;
            }

            // Одиночные разделители и операторы
            if (IsSingleCharToken(c))
            {
                // Различаем Separator (скобки, точка, запятая, точка с запятой) и Operator (остальные)
                TokenKind kind = IsSeparator(c) ? TokenKind.Separator : TokenKind.Operator;
                tokens.Add(new Term(c.ToString(), kind));
                _position++;
                continue;
            }

            // Если ничего не подошло – ошибка
            throw new Exception($"Неожиданный символ '{c}' в позиции {_position}");
        }

        return tokens;
    }

    private char CurrentChar() => _input[_position];
    private char PeekChar() => (_position + 1 < _length) ? _input[_position + 1] : '\0';
    private char PeekChar(int offset) => (_position + offset < _length) ? _input[_position + offset] : '\0';

    private void SkipLineComment()
    {
        _position += 2;
        while (_position < _length && CurrentChar() != '\n' && CurrentChar() != '\r')
            _position++;
        if (_position < _length && (CurrentChar() == '\n' || CurrentChar() == '\r'))
        {
            _position++;
            if (_position < _length && CurrentChar() == '\n' && _input[_position - 1] == '\r')
                _position++;
        }
    }

    private void SkipBlockComment()
    {
        _position += 2;
        while (_position + 1 < _length && !(CurrentChar() == '*' && PeekChar() == '/'))
            _position++;
        if (_position + 1 >= _length)
            throw new Exception("Незакрытый многострочный комментарий");
        _position += 2;
    }

    private void SkipDirective()
    {
        while (_position < _length && CurrentChar() != '\n' && CurrentChar() != '\r')
            _position++;
        if (_position < _length && (CurrentChar() == '\n' || CurrentChar() == '\r'))
        {
            _position++;
            if (_position < _length && CurrentChar() == '\n' && _input[_position - 1] == '\r')
                _position++;
        }
    }

    private string ReadIdentifier()
    {
        int start = _position;
        while (_position < _length && (char.IsLetterOrDigit(CurrentChar()) || CurrentChar() == '_'))
            _position++;
        return _input.Substring(start, _position - start);
    }

    private Term ReadNumber()
    {
        int start = _position;
        bool isHex = false;
        bool isFloat = false;
        bool isUnsigned = false;

        // Шестнадцатеричный префикс
        if (CurrentChar() == '0' && PeekChar() == 'x')
        {
            isHex = true;
            _position += 2;
            if (_position >= _length || !IsHexDigit(CurrentChar()))
                throw new Exception($"Ожидалась шестнадцатеричная цифра после 0x в позиции {_position}");
            while (_position < _length && IsHexDigit(CurrentChar()))
                _position++;
        }
        else
        {
            // Целая часть
            while (_position < _length && char.IsDigit(CurrentChar()))
                _position++;

            // Дробная часть
            if (_position < _length && CurrentChar() == '.')
            {
                isFloat = true;
                _position++;
                while (_position < _length && char.IsDigit(CurrentChar()))
                    _position++;
            }

            // Экспоненциальная часть
            if (_position < _length && char.ToLower(CurrentChar()) == 'e')
            {
                isFloat = true;
                _position++;
                if (_position < _length && (CurrentChar() == '+' || CurrentChar() == '-'))
                    _position++;
                if (_position >= _length || !char.IsDigit(CurrentChar()))
                    throw new Exception($"Ожидалась цифра после экспоненты в позиции {_position}");
                while (_position < _length && char.IsDigit(CurrentChar()))
                    _position++;
            }
        }

        // Суффиксы
        if (_position < _length)
        {
            char c = char.ToLower(CurrentChar());
            if (c == 'u')
            {
                isUnsigned = true;
                _position++;
                // Проверяем, нет ли f после u (недопустимо, но игнорируем)
            }
            else if (c == 'f')
            {
                isFloat = true;
                _position++;
            }
            else if (c == 'l' && PeekChar() == 'f')
            {
                isFloat = true;
                _position += 2;
            }
        }

        string lexeme = _input.Substring(start, _position - start);

        // Определяем Kind
        if (isFloat)
        {
            // Проверяем суффикс lf/LF для double
            if (lexeme.EndsWith("lf", StringComparison.OrdinalIgnoreCase))
                return new Term(lexeme, TokenKind.DoubleConstant);
            else
                return new Term(lexeme, TokenKind.FloatConstant);
        }
        else if (isUnsigned)
            return new Term(lexeme, TokenKind.UIntConstant);
        else
            return new Term(lexeme, TokenKind.IntConstant);
    }

    private bool IsHexDigit(char c) => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private string ReadMultiCharOperator()
    {
        char c = CurrentChar();
        char next = PeekChar();

        switch (c)
        {
            case '+': if (next == '+') return ConsumeTwo("++"); if (next == '=') return ConsumeTwo("+="); break;
            case '-': if (next == '-') return ConsumeTwo("--"); if (next == '=') return ConsumeTwo("-="); break;
            case '*': if (next == '=') return ConsumeTwo("*="); break;
            case '/': if (next == '=') return ConsumeTwo("/="); break;
            case '%': if (next == '=') return ConsumeTwo("%="); break;
            case '=': if (next == '=') return ConsumeTwo("=="); break;
            case '!': if (next == '=') return ConsumeTwo("!="); break;
            case '<':
                if (next == '=') return ConsumeTwo("<=");
                if (next == '<')
                {
                    if (PeekChar(2) == '=') return ConsumeThree("<<=");
                    return ConsumeTwo("<<");
                }
                break;
            case '>':
                if (next == '=') return ConsumeTwo(">=");
                if (next == '>')
                {
                    if (PeekChar(2) == '=') return ConsumeThree(">>=");
                    return ConsumeTwo(">>");
                }
                break;
            case '&':
                if (next == '&') return ConsumeTwo("&&");
                if (next == '=') return ConsumeTwo("&=");
                break;
            case '|':
                if (next == '|') return ConsumeTwo("||");
                if (next == '=') return ConsumeTwo("|=");
                break;
            case '^':
                if (next == '^') return ConsumeTwo("^^");
                if (next == '=') return ConsumeTwo("^=");
                break;
        }
        return null;
    }

    private string ConsumeTwo(string op)
    {
        _position += 2;
        return op;
    }

    private string ConsumeThree(string op)
    {
        _position += 3;
        return op;
    }

    private bool IsSingleCharToken(char c)
    {
        return c == ';' || c == '{' || c == '}' || c == '(' || c == ')' ||
               c == '[' || c == ']' || c == ',' || c == '.' || c == '?' || c == ':' ||
               c == '~' || c == '!' || c == '&' || c == '|' || c == '^' ||
               c == '+' || c == '-' || c == '*' || c == '/' || c == '%' ||
               c == '<' || c == '>' || c == '=';
    }

    private bool IsSeparator(char c)
    {
        return c == ';' || c == '{' || c == '}' || c == '(' || c == ')' ||
               c == '[' || c == ']' || c == ',' || c == '.' || c == '?' || c == ':';
    }
}