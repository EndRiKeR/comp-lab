using comp_lab.Labs.lab2.structs;

namespace comp_lab.Labs.glsl;

public class GlslSyntaxAnalyzer
{
    private List<GrammarPart> _tokens;
    private int _pos;
    private Term Current => (_pos < _tokens.Count) ? (Term)_tokens[_pos] : null;
    private bool HasTokens => _pos < _tokens.Count;

    public bool Analyze(List<GrammarPart> tokens)
    {
        _tokens = tokens;
        _pos = 0;

        bool result = TranslationUnit();
        if (result && HasTokens)
        {
            Console.WriteLine($"Ошибка: лишние токены после завершения программы, начиная с {Current?.Name ?? "EOF"}");
            return false;
        }
        return result;
    }

    // ---------- Правила грамматики ----------

    private bool LayoutQualifier()
    {
        if (!Match(TokenKind.Keyword, "layout"))
            return false;
        if (!Match(TokenKind.Separator, "("))
            return false;
        // Пропускаем всё содержимое до закрывающей скобки
        int depth = 1;
        while (HasTokens && depth > 0)
        {
            if (Current.Kind == TokenKind.Separator && Current.Name == "(")
                depth++;
            else if (Current.Kind == TokenKind.Separator && Current.Name == ")")
                depth--;
            _pos++;
        }
        if (depth != 0)
        {
            Console.WriteLine("Ошибка: незакрытая скобка в layout");
            return false;
        }
        return true;
    }
    
    // translation_unit -> external_declaration*
    private bool TranslationUnit()
    {
        while (HasTokens && !IsEof())
        {
            if (!ExternalDeclaration())
                return false;
        }
        return true;
    }

    // external_declaration -> function_definition | declaration | ';'
    private bool ExternalDeclaration()
    {
        if (IsFunctionPrototype())
            return FunctionDefinition();
        if (IsDeclaration())
            return Declaration();
        if (Match(TokenKind.Separator, ";"))
            return true; // пустое объявление
        Console.WriteLine($"Ожидалось объявление или определение функции, найдено {Current?.Name ?? "EOF"}");
        return false;
    }

    // Определение функции: fully_specified_type IDENTIFIER '(' parameters? ')' compound_statement
    private bool FunctionDefinition()
    {
        if (!FullySpecifiedType()) return false;
        if (!ExpectIdentifier(out string funcName)) return false;
        if (!Match(TokenKind.Separator, "(")) return false;
        if (!FunctionParameters()) return false;
        if (!Match(TokenKind.Separator, ")")) return false;
        if (!CompoundStatement()) return false;
        return true;
    }

    // Проверка, что следующий токен – прототип функции (просто смотрим тип и идентификатор)
    private bool IsFunctionPrototype()
    {
        int save = _pos;
        bool ok = FullySpecifiedType() && Current?.Kind == TokenKind.Identifier && Peek(1)?.Name == "(";
        _pos = save;
        return ok;
    }

    // Параметры функции: parameter (',' parameter)* | ε
    private bool FunctionParameters()
    {
        if (Current?.Name == ")") return true; // пусто
        if (!ParameterDeclaration()) return false;
        while (Match(TokenKind.Separator, ","))
        {
            if (!ParameterDeclaration()) return false;
        }
        return true;
    }

    // parameter_declaration -> type_qualifier* fully_specified_type (IDENTIFIER array_specifier?)?
    private bool ParameterDeclaration()
    {
        while (IsTypeQualifier()) _pos++;
        if (!FullySpecifiedType()) return false;
        if (Current?.Kind == TokenKind.Identifier)
        {
            _pos++;
            ArraySpecifier();
        }
        return true;
    }

    // ---------- Объявления переменных, типов, блоков ----------

    private bool IsDeclaration()
    {
        int save = _pos;
        bool ok = FullySpecifiedType() && (Current?.Kind == TokenKind.Identifier || Current?.Name == "{");
        _pos = save;
        return ok;
    }

    // declaration -> init_declarator_list ';' 
    //             | fully_specified_type IDENTIFIER '{' struct_declaration_list '}' (IDENTIFIER array_specifier?)? ';'
    private bool Declaration()
    {
        if (!FullySpecifiedType()) return false;

        // ---------- НОВЫЙ БЛОК: обработка uniform / buffer блоков ----------
        if (Current != null && (Current.Name == "uniform" || Current.Name == "buffer"))
        {
            string blockType = Current.Name; // "uniform" или "buffer"
            _pos++; // пропускаем uniform/buffer
            if (!ExpectIdentifier(out string blockName)) return false;
            if (!Match(TokenKind.Separator, "{")) return false;
            if (!StructDeclarationList()) return false;
            if (!Match(TokenKind.Separator, "}")) return false;
            if (Current?.Kind == TokenKind.Identifier) // имя экземпляра
            {
                _pos++;
                ArraySpecifier();
            }
            if (!Match(TokenKind.Separator, ";")) return false;
            return true;
        }

        // Анонимный блок (uniform/buffer)
        if (Current?.Kind == TokenKind.Identifier && Peek(1)?.Name == "{")
        {
            string blockTag = Current.Name;
            _pos++; // пропускаем имя тега
            if (!Match(TokenKind.Separator, "{")) return false;
            if (!StructDeclarationList()) return false;
            if (!Match(TokenKind.Separator, "}")) return false;
            if (Current?.Kind == TokenKind.Identifier) // имя экземпляра
            {
                _pos++;
                ArraySpecifier();
            }
            if (!Match(TokenKind.Separator, ";")) return false;
            return true;
        }

        // Обычное объявление переменных
        if (!InitDeclaratorList()) return false;
        if (!Match(TokenKind.Separator, ";")) return false;
        return true;
    }

    // init_declarator_list -> single_declaration (',' typeless_declaration)*
    private bool InitDeclaratorList()
    {
        if (!SingleDeclaration()) return false;
        while (Match(TokenKind.Separator, ","))
        {
            if (!TypelessDeclaration()) return false;
        }
        return true;
    }

    // single_declaration -> fully_specified_type typeless_declaration?
    // Здесь тип уже разобран, разбираем только declarator
    private bool SingleDeclaration()
    {
        return TypelessDeclaration(); // тип уже съеден в FullySpecifiedType
    }

    // typeless_declaration -> IDENTIFIER array_specifier? ('=' initializer)?
    private bool TypelessDeclaration()
    {
        if (!ExpectIdentifier(out _)) return false;
        ArraySpecifier();
        if (Match(TokenKind.Operator, "="))
        {
            if (!Initializer()) return false;
        }
        return true;
    }

    // initializer -> assignment_expression | '{' initializer_list ','? '}'
    private bool Initializer()
    {
        if (AssignmentExpression()) return true;
        if (Match(TokenKind.Separator, "{"))
        {
            if (!InitializerList()) return false;
            if (Match(TokenKind.Separator, ",")) { } // висящая запятая
            if (!Match(TokenKind.Separator, "}")) return false;
            return true;
        }
        return false;
    }

    private bool InitializerList()
    {
        if (!Initializer()) return false;
        while (Match(TokenKind.Separator, ","))
        {
            if (!Initializer()) return false;
        }
        return true;
    }

    // ---------- Структуры ----------

    // struct_specifier -> 'struct' IDENTIFIER? '{' struct_declaration_list '}'
    private bool StructSpecifier()
    {
        if (!Match(TokenKind.Keyword, "struct")) return false;
        if (Current?.Kind == TokenKind.Identifier) _pos++; // имя структуры необязательно
        if (!Match(TokenKind.Separator, "{")) return false;
        if (!StructDeclarationList()) return false;
        if (!Match(TokenKind.Separator, "}")) return false;
        return true;
    }

    private bool StructDeclarationList()
    {
        while (Current?.Kind == TokenKind.Keyword || Current?.Kind == TokenKind.Identifier || IsTypeQualifier())
        {
            if (!StructDeclaration()) return false;
        }
        return true;
    }

    private bool StructDeclaration()
    {
        // type_qualifier? type_specifier struct_declarator_list ';'
        while (IsTypeQualifier()) _pos++;
        if (!TypeSpecifier()) return false;
        if (!StructDeclaratorList()) return false;
        if (!Match(TokenKind.Separator, ";")) return false;
        return true;
    }

    private bool StructDeclaratorList()
    {
        if (!StructDeclarator()) return false;
        while (Match(TokenKind.Separator, ","))
        {
            if (!StructDeclarator()) return false;
        }
        return true;
    }

    private bool StructDeclarator()
    {
        if (!ExpectIdentifier(out _)) return false;
        ArraySpecifier();
        return true;
    }

    // ---------- Типы и квалификаторы ----------

    private bool FullySpecifiedType()
    {
        while (true)
        {
            if (IsTypeQualifier())
            {
                if (Current.Name == "layout")
                {
                    if (!LayoutQualifier()) return false;
                }
                else
                {
                    _pos++; // простой квалификатор-ключевое слово
                }
            }
            else break;
        }
        return TypeSpecifier();
    }

    private bool TypeSpecifier()
    {
        if (IsBasicType())
        {
            _pos++;
            ArraySpecifier();
            return true;
        }
        if (Current?.Kind == TokenKind.Identifier) // имя структуры или пользовательский тип
        {
            _pos++;
            ArraySpecifier();
            return true;
        }
        if (StructSpecifier()) return true;
        return false;
    }

    private bool IsBasicType()
    {
        if (Current?.Kind != TokenKind.Keyword) return false;
        string[] basic = { "void", "int", "uint", "float", "double", "bool",
                           "vec2", "vec3", "vec4", "dvec2", "dvec3", "dvec4" };
        return basic.Contains(Current.Name);
    }

    private bool IsTypeQualifier()
    {
        if (Current == null) return false;
        if (Current.Kind == TokenKind.Keyword)
        {
            // Убрали uniform и buffer
            string[] quals = { "const", "readonly", "in", "out", "inout" };
            if (quals.Contains(Current.Name))
                return true;
        }
        return Current.Kind == TokenKind.Keyword && Current.Name == "layout";
    }

    private void ArraySpecifier()
    {
        while (Match(TokenKind.Separator, "["))
        {
            if (!Match(TokenKind.Separator, "]"))
            {
                ConstantExpression(); // размер массива
                Match(TokenKind.Separator, "]");
            }
        }
    }

    // ---------- Операторы (statements) ----------

    private bool CompoundStatement()
    {
        if (!Match(TokenKind.Separator, "{")) return false;
        while (HasTokens && !Match(TokenKind.Separator, "}"))
        {
            if (!Statement()) return false;
        }
        if (!Match(TokenKind.Separator, "}")) return false;
        return true;
    }

    private bool Statement()
    {
        if (Match(TokenKind.Separator, "{"))
            return CompoundStatement();
        return SimpleStatement();
    }

    private bool SimpleStatement()
    {
        if (IsDeclaration()) return Declaration();
        if (ExpressionStatement()) return true;
        if (SelectionStatement()) return true;
        if (SwitchStatement()) return true;
        if (CaseLabel()) return true;
        if (IterationStatement()) return true;
        if (JumpStatement()) return true;
        return false;
    }

    private bool ExpressionStatement()
    {
        if (Match(TokenKind.Separator, ";")) return true;
        if (Expression())
        {
            if (Match(TokenKind.Separator, ";")) return true;
        }
        return false;
    }

    private bool SelectionStatement()
    {
        if (!Match(TokenKind.Keyword, "if")) return false;
        if (!Match(TokenKind.Separator, "(")) return false;
        if (!Condition()) return false;
        if (!Match(TokenKind.Separator, ")")) return false;
        if (!Statement()) return false;
        if (Match(TokenKind.Keyword, "else"))
        {
            if (!Statement()) return false;
        }
        return true;
    }

    private bool Condition()
    {
        if (Expression()) return true;
        // вариант: fully_specified_type IDENTIFIER '=' initializer
        int save = _pos;
        if (FullySpecifiedType() && ExpectIdentifier(out _) && Match(TokenKind.Operator, "=") && Initializer())
            return true;
        _pos = save;
        return false;
    }

    private bool SwitchStatement()
    {
        if (!Match(TokenKind.Keyword, "switch")) return false;
        if (!Match(TokenKind.Separator, "(")) return false;
        if (!Expression()) return false;
        if (!Match(TokenKind.Separator, ")")) return false;
        if (!Match(TokenKind.Separator, "{")) return false;
        while (HasTokens && !Match(TokenKind.Separator, "}"))
        {
            if (!Statement()) return false;
        }
        return true;
    }

    private bool CaseLabel()
    {
        if (Match(TokenKind.Keyword, "case"))
        {
            if (!ConstantExpression()) return false;
            if (!Match(TokenKind.Separator, ":")) return false;
            return true;
        }
        if (Match(TokenKind.Keyword, "default"))
        {
            if (!Match(TokenKind.Separator, ":")) return false;
            return true;
        }
        return false;
    }

    private bool IterationStatement()
    {
        if (Match(TokenKind.Keyword, "while"))
        {
            if (!Match(TokenKind.Separator, "(")) return false;
            if (!Condition()) return false;
            if (!Match(TokenKind.Separator, ")")) return false;
            return Statement();
        }
        if (Match(TokenKind.Keyword, "do"))
        {
            if (!Statement()) return false;
            if (!Match(TokenKind.Keyword, "while")) return false;
            if (!Match(TokenKind.Separator, "(")) return false;
            if (!Expression()) return false;
            if (!Match(TokenKind.Separator, ")")) return false;
            return Match(TokenKind.Separator, ";");
        }
        if (Match(TokenKind.Keyword, "for"))
        {
            if (!Match(TokenKind.Separator, "(")) return false;
            if (!ForInitStatement()) return false;
            if (!ForRestStatement()) return false;
            if (!Match(TokenKind.Separator, ")")) return false;
            return Statement();
        }
        return false;
    }

    private bool ForInitStatement()
    {
        if (IsDeclaration()) return Declaration();
        return ExpressionStatement();
    }

    private bool ForRestStatement()
    {
        // condition? ';' expression?
        Condition(); // может быть пусто
        if (!Match(TokenKind.Separator, ";")) return false;
        Expression(); // может быть пусто
        return true;
    }

    private bool JumpStatement()
    {
        if (Match(TokenKind.Keyword, "break") ||
            Match(TokenKind.Keyword, "continue") ||
            Match(TokenKind.Keyword, "discard"))
        {
            return Match(TokenKind.Separator, ";");
        }
        if (Match(TokenKind.Keyword, "return"))
        {
            Expression(); // необязательно
            return Match(TokenKind.Separator, ";");
        }
        return false;
    }

    // ---------- Выражения (полностью переиспользуем ваш старый код, но с проверкой токенов) ----------

    private bool Expression()
    {
        if (!AssignmentExpression()) return false;
        while (Match(TokenKind.Separator, ","))
        {
            if (!AssignmentExpression()) return false;
        }
        return true;
    }

    private bool AssignmentExpression()
    {
        if (ConstantExpression()) return true;
        if (UnaryExpression() && IsAssignmentOperator() && AssignmentExpression())
            return true;
        return false;
    }

    private bool IsAssignmentOperator()
    {
        if (Current?.Kind != TokenKind.Operator) return false;
        string[] ops = { "=", "+=", "-=", "*=", "/=", "%=", "<<=", ">>=", "&=", "^=", "|=" };
        return ops.Contains(Current.Name);
    }

    private bool ConstantExpression()
    {
        if (!LogicalOrExpression()) return false;
        if (Match(TokenKind.Operator, "?"))
        {
            if (!Expression()) return false;
            if (!Match(TokenKind.Separator, ":")) return false;
            if (!AssignmentExpression()) return false;
        }
        return true;
    }

    // Приоритеты операторов (снизу вверх)
    private bool LogicalOrExpression()
    {
        if (!LogicalAndExpression()) return false;
        while (Match(TokenKind.Operator, "||"))
        {
            if (!LogicalAndExpression()) return false;
        }
        return true;
    }

    private bool LogicalAndExpression()
    {
        if (!InclusiveOrExpression()) return false;
        while (Match(TokenKind.Operator, "&&"))
        {
            if (!InclusiveOrExpression()) return false;
        }
        return true;
    }

    private bool InclusiveOrExpression()
    {
        if (!ExclusiveOrExpression()) return false;
        while (Match(TokenKind.Operator, "|"))
        {
            if (!ExclusiveOrExpression()) return false;
        }
        return true;
    }

    private bool ExclusiveOrExpression()
    {
        if (!AndExpression()) return false;
        while (Match(TokenKind.Operator, "^"))
        {
            if (!AndExpression()) return false;
        }
        return true;
    }

    private bool AndExpression()
    {
        if (!EqualityExpression()) return false;
        while (Match(TokenKind.Operator, "&"))
        {
            if (!EqualityExpression()) return false;
        }
        return true;
    }

    private bool EqualityExpression()
    {
        if (!RelationalExpression()) return false;
        while (Match(TokenKind.Operator, "==") || Match(TokenKind.Operator, "!="))
        {
            if (!RelationalExpression()) return false;
        }
        return true;
    }

    private bool RelationalExpression()
    {
        if (!ShiftExpression()) return false;
        while (Match(TokenKind.Operator, "<") || Match(TokenKind.Operator, ">") ||
               Match(TokenKind.Operator, "<=") || Match(TokenKind.Operator, ">="))
        {
            if (!ShiftExpression()) return false;
        }
        return true;
    }

    private bool ShiftExpression()
    {
        if (!AdditiveExpression()) return false;
        while (Match(TokenKind.Operator, "<<") || Match(TokenKind.Operator, ">>"))
        {
            if (!AdditiveExpression()) return false;
        }
        return true;
    }

    private bool AdditiveExpression()
    {
        if (!MultiplicativeExpression()) return false;
        while (Match(TokenKind.Operator, "+") || Match(TokenKind.Operator, "-"))
        {
            if (!MultiplicativeExpression()) return false;
        }
        return true;
    }

    private bool MultiplicativeExpression()
    {
        if (!UnaryExpression()) return false;
        while (Match(TokenKind.Operator, "*") || Match(TokenKind.Operator, "/") || Match(TokenKind.Operator, "%"))
        {
            if (!UnaryExpression()) return false;
        }
        return true;
    }

    private bool UnaryExpression()
    {
        if (PostfixExpression()) return true;
        if (Match(TokenKind.Operator, "++") || Match(TokenKind.Operator, "--"))
            return UnaryExpression();
        if (Match(TokenKind.Operator, "+") || Match(TokenKind.Operator, "-") ||
            Match(TokenKind.Operator, "!") || Match(TokenKind.Operator, "~"))
            return UnaryExpression();
        return false;
    }

    private bool PostfixExpression()
    {
        if (!PrimaryExpression()) return false;
        while (true)
        {
            if (Match(TokenKind.Separator, "["))
            {
                if (!Expression()) return false;
                if (!Match(TokenKind.Separator, "]")) return false;
            }
            else if (Match(TokenKind.Separator, "."))
            {
                if (Current?.Kind == TokenKind.Identifier)
                {
                    _pos++;
                    if (Match(TokenKind.Separator, "("))
                    {
                        // вызов метода .length() и т.п.
                        FunctionCallParameters();
                        if (!Match(TokenKind.Separator, ")")) return false;
                    }
                }
                else return false;
            }
            else if (Match(TokenKind.Separator, "("))
            {
                // вызов функции или конструктор
                FunctionCallParameters();
                if (!Match(TokenKind.Separator, ")")) return false;
            }
            else if (Match(TokenKind.Operator, "++") || Match(TokenKind.Operator, "--"))
            {
                // постфиксный инкремент
            }
            else break;
        }
        return true;
    }

    private bool PrimaryExpression()
    {
        if (Current?.Kind == TokenKind.Identifier)
        {
            _pos++;
            return true;
        }
        if (Current?.Kind == TokenKind.Keyword && (Current.Name == "true" || Current.Name == "false"))
        {
            _pos++;
            return true;
        }
        if (IsConstant())
        {
            _pos++;
            return true;
        }
        if (Match(TokenKind.Separator, "("))
        {
            if (!Expression()) return false;
            if (!Match(TokenKind.Separator, ")")) return false;
            return true;
        }
        return false;
    }

    private bool IsConstant()
    {
        if (Current == null) return false;
        return Current.Kind == TokenKind.IntConstant ||
               Current.Kind == TokenKind.UIntConstant ||
               Current.Kind == TokenKind.FloatConstant ||
               Current.Kind == TokenKind.DoubleConstant;
    }

    private bool FunctionCallParameters()
    {
        if (Current?.Name == ")") return true;
        if (AssignmentExpression())
        {
            while (Match(TokenKind.Separator, ","))
            {
                if (!AssignmentExpression()) return false;
            }
        }
        else if (Match(TokenKind.Keyword, "void")) { }
        return true;
    }

    // ---------- Вспомогательные методы ----------

    private bool Match(TokenKind kind, string name = null)
    {
        if (!HasTokens) return false;
        if (Current.Kind != kind) return false;
        if (name != null && Current.Name != name) return false;
        _pos++;
        return true;
    }

    private bool ExpectIdentifier(out string name)
    {
        name = null;
        if (Current?.Kind == TokenKind.Identifier)
        {
            name = Current.Name;
            _pos++;
            return true;
        }
        Console.WriteLine($"Ожидался идентификатор, найдено {Current?.Name ?? "EOF"}");
        return false;
    }

    private Term Peek(int offset)
    {
        int idx = _pos + offset;
        return idx < _tokens.Count ? (Term)_tokens[idx] : null;
    }

    private bool IsEof() => !HasTokens;
}