using comp_lab.Labs.lab2.structs;
using comp_lab.Labs.glsl.Ast; // создадим пространство для узлов

namespace comp_lab.Labs.glsl;

public class GlslParser
{
    private List<GrammarPart> _tokens;
    private int _pos;
    private Term Current => (_pos < _tokens.Count) ? (Term)_tokens[_pos] : null;
    private bool HasTokens => _pos < _tokens.Count;

    public TranslationUnitNode Parse(List<GrammarPart> tokens)
    {
        _tokens = tokens;
        _pos = 0;
        var node = TranslationUnit();
        if (HasTokens)
            throw new Exception($"Лишние токены после разбора: {Current?.Name}");
        return node;
    }

    private TranslationUnitNode TranslationUnit()
    {
        var declarations = new List<ExternalDeclarationNode>();
        while (HasTokens && !IsEof())
        {
            declarations.Add(ExternalDeclaration());
        }
        return new TranslationUnitNode(declarations);
    }

    private ExternalDeclarationNode ExternalDeclaration()
    {
        if (IsFunctionPrototype())
            return FunctionDefinition();
        if (IsDeclaration())
            return Declaration();
        if (Match(TokenKind.Separator, ";"))
            return new EmptyDeclarationNode();
        throw new Exception($"Ожидалось объявление или функция, найдено {Current?.Name}");
    }

    private FunctionDefinitionNode FunctionDefinition()
    {
        var type = FullySpecifiedType();
        var name = ExpectIdentifier();
        Expect(TokenKind.Separator, "(");
        var parameters = FunctionParameters();
        Expect(TokenKind.Separator, ")");
        var body = CompoundStatement();
        return new FunctionDefinitionNode(type, name, parameters, body);
    }

    private bool IsFunctionPrototype()
    {
        int save = _pos;
        bool ok = false;
        try { ok = FullySpecifiedType() != null && Current?.Kind == TokenKind.Identifier && Peek(1)?.Name == "("; }
        catch { ok = false; }
        _pos = save;
        return ok;
    }

    private List<ParameterNode> FunctionParameters()
    {
        var list = new List<ParameterNode>();
        if (Current?.Name == ")") return list;
        list.Add(ParameterDeclaration());
        while (Match(TokenKind.Separator, ","))
            list.Add(ParameterDeclaration());
        return list;
    }

    private ParameterNode ParameterDeclaration()
    {
        var quals = new List<string>();
        while (IsTypeQualifier()) 
        {
            if (Current.Name == "layout")
                LayoutQualifier(); // пропускаем, но можно и сохранить
            else
                quals.Add(Current.Name);
            _pos++;
        }
        var type = FullySpecifiedType();
        string name = null;
        if (Current?.Kind == TokenKind.Identifier)
        {
            name = Current.Name;
            _pos++;
            ArraySpecifier(); // размерность параметра-массива
        }
        return new ParameterNode(quals, type, name);
    }

    private bool IsDeclaration()
    {
        int save = _pos;
        bool ok = false;
        try { ok = FullySpecifiedType() != null && (Current?.Kind == TokenKind.Identifier || Current?.Name == "{"); }
        catch { ok = false; }
        _pos = save;
        return ok;
    }

    private ExternalDeclarationNode Declaration()
    {
        var type = FullySpecifiedType();

        // uniform / buffer блок
        if (Current != null && (Current.Name == "uniform" || Current.Name == "buffer"))
        {
            string storage = Current.Name;
            _pos++;
            string blockName = ExpectIdentifier();
            Expect(TokenKind.Separator, "{");
            var members = StructDeclarationList();
            Expect(TokenKind.Separator, "}");
            string instanceName = null;
            if (Current?.Kind == TokenKind.Identifier)
            {
                instanceName = Current.Name;
                _pos++;
                ArraySpecifier();
            }
            Expect(TokenKind.Separator, ";");
            return new BufferBlockNode(storage, blockName, members, instanceName);
        }

        // анонимный блок (например, просто struct без uniform? Или legacy)
        if (Current?.Kind == TokenKind.Identifier && Peek(1)?.Name == "{")
        {
            string tag = Current.Name;
            _pos++;
            Expect(TokenKind.Separator, "{");
            var members = StructDeclarationList();
            Expect(TokenKind.Separator, "}");
            string instanceName = null;
            if (Current?.Kind == TokenKind.Identifier)
            {
                instanceName = Current.Name;
                _pos++;
                ArraySpecifier();
            }
            Expect(TokenKind.Separator, ";");
            return new AnonymousBlockNode(tag, members, instanceName);
        }

        // обычные переменные
        var decls = InitDeclaratorList();
        Expect(TokenKind.Separator, ";");
        return new VariableDeclarationNode(type, decls);
    }

    private List<InitDeclaratorNode> InitDeclaratorList()
    {
        var list = new List<InitDeclaratorNode>();
        list.Add(SingleDeclaration());
        while (Match(TokenKind.Separator, ","))
            list.Add(TypelessDeclaration());
        return list;
    }

    private InitDeclaratorNode SingleDeclaration()
    {
        // тип уже съеден, здесь только declarator
        return TypelessDeclaration();
    }

    private InitDeclaratorNode TypelessDeclaration()
    {
        string name = ExpectIdentifier();
        var dims = ArraySpecifier();
        ExpressionNode init = null;
        if (Match(TokenKind.Operator, "="))
            init = Initializer();
        return new InitDeclaratorNode(name, dims, init);
    }

    private ExpressionNode Initializer()
    {
        if (Match(TokenKind.Separator, "{"))
        {
            var list = new List<ExpressionNode>();
            list.Add(Initializer());
            while (Match(TokenKind.Separator, ","))
                list.Add(Initializer());
            if (Match(TokenKind.Separator, ",")) { }
            Expect(TokenKind.Separator, "}");
            return new InitializerListNode(list);
        }
        return AssignmentExpression();
    }

    private List<StructMemberNode> StructDeclarationList()
    {
        var members = new List<StructMemberNode>();
        while (HasTokens && (Current.Kind == TokenKind.Keyword || Current.Kind == TokenKind.Identifier || IsTypeQualifier()))
        {
            members.Add(StructDeclaration());
        }
        return members;
    }

    private StructMemberNode StructDeclaration()
    {
        var quals = new List<string>();
        while (IsTypeQualifier())
        {
            quals.Add(Current.Name);
            _pos++;
        }
        var type = TypeSpecifier();
        var decls = StructDeclaratorList();
        Expect(TokenKind.Separator, ";");
        return new StructMemberNode(quals, type, decls);
    }

    private List<StructDeclaratorNode> StructDeclaratorList()
    {
        var list = new List<StructDeclaratorNode>();
        list.Add(StructDeclarator());
        while (Match(TokenKind.Separator, ","))
            list.Add(StructDeclarator());
        return list;
    }

    private StructDeclaratorNode StructDeclarator()
    {
        string name = ExpectIdentifier();
        var dims = ArraySpecifier();
        return new StructDeclaratorNode(name, dims);
    }

    private TypeNode FullySpecifiedType()
    {
        var quals = new List<string>();
        while (true)
        {
            if (IsTypeQualifier())
            {
                if (Current.Name == "layout")
                {
                    LayoutQualifier(); // пропускаем, можно сохранить при необходимости
                }
                else
                {
                    quals.Add(Current.Name);
                    _pos++;
                }
            }
            else break;
        }
        var type = TypeSpecifier();
        return new TypeNode(quals, type);
    }

    private TypeSpecifierNode TypeSpecifier()
    {
        if (IsBasicType())
        {
            string name = Current.Name;
            _pos++;
            var dims = ArraySpecifier();
            return new BasicTypeNode(name, dims);
        }
        if (Current?.Kind == TokenKind.Identifier)
        {
            string name = Current.Name;
            _pos++;
            var dims = ArraySpecifier();
            return new UserTypeNode(name, dims);
        }
        if (Current?.Name == "struct")
        {
            return StructSpecifier();
        }
        throw new Exception($"Ожидался тип, найдено {Current?.Name}");
    }

    private StructTypeNode StructSpecifier()
    {
        Match(TokenKind.Keyword, "struct");
        string name = null;
        if (Current?.Kind == TokenKind.Identifier)
        {
            name = Current.Name;
            _pos++;
        }
        Expect(TokenKind.Separator, "{");
        var members = StructDeclarationList();
        Expect(TokenKind.Separator, "}");
        return new StructTypeNode(name, members);
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
            string[] quals = { "const", "readonly", "in", "out", "inout" };
            if (quals.Contains(Current.Name)) return true;
        }
        return Current.Kind == TokenKind.Keyword && Current.Name == "layout";
    }

    private List<ArrayDimensionNode> ArraySpecifier()
    {
        var dims = new List<ArrayDimensionNode>();
        while (Match(TokenKind.Separator, "["))
        {
            ExpressionNode size = null;
            if (!Match(TokenKind.Separator, "]"))
            {
                size = ConstantExpression();
                Expect(TokenKind.Separator, "]");
            }
            dims.Add(new ArrayDimensionNode(size));
        }
        return dims;
    }

    // ---- Statements ----
    private BlockNode CompoundStatement()
    {
        Expect(TokenKind.Separator, "{");
        var statements = new List<StatementNode>();
        while (HasTokens && !Match(TokenKind.Separator, "}"))
        {
            statements.Add(Statement());
        }
        return new BlockNode(statements);
    }

    private StatementNode Statement()
    {
        if (Current?.Name == "{")
            return CompoundStatement();
        return SimpleStatement();
    }

    private StatementNode SimpleStatement()
    {
        if (IsDeclaration())
            return Declaration();
        if (Match(TokenKind.Separator, ";"))
            return new EmptyStatementNode();
        if (Current?.Name == "if")
            return SelectionStatement();
        if (Current?.Name == "switch")
            return SwitchStatement();
        if (Current?.Name == "case" || Current?.Name == "default")
            return CaseLabel();
        if (Current?.Name == "while" || Current?.Name == "do" || Current?.Name == "for")
            return IterationStatement();
        if (Current?.Name == "break" || Current?.Name == "continue" || Current?.Name == "return" || Current?.Name == "discard")
            return JumpStatement();
        // expression statement
        var expr = Expression();
        Expect(TokenKind.Separator, ";");
        return new ExpressionStatementNode(expr);
    }

    private IfStatementNode SelectionStatement()
    {
        Expect(TokenKind.Keyword, "if");
        Expect(TokenKind.Separator, "(");
        var cond = Condition();
        Expect(TokenKind.Separator, ")");
        var thenStmt = Statement();
        StatementNode elseStmt = null;
        if (Match(TokenKind.Keyword, "else"))
            elseStmt = Statement();
        return new IfStatementNode(cond, thenStmt, elseStmt);
    }

    private ExpressionNode Condition()
    {
        int save = _pos;
        try
        {
            // попробуем как объявление переменной
            var type = FullySpecifiedType();
            string name = ExpectIdentifier();
            Expect(TokenKind.Operator, "=");
            var init = Initializer();
            return new DeclarationExpressionNode(type, name, init);
        }
        catch
        {
            _pos = save;
            return Expression();
        }
    }

    private SwitchStatementNode SwitchStatement()
    {
        Expect(TokenKind.Keyword, "switch");
        Expect(TokenKind.Separator, "(");
        var expr = Expression();
        Expect(TokenKind.Separator, ")");
        Expect(TokenKind.Separator, "{");
        var cases = new List<StatementNode>();
        while (HasTokens && !Match(TokenKind.Separator, "}"))
            cases.Add(Statement());
        return new SwitchStatementNode(expr, cases);
    }

    private CaseLabelNode CaseLabel()
    {
        if (Match(TokenKind.Keyword, "case"))
        {
            var expr = ConstantExpression();
            Expect(TokenKind.Separator, ":");
            return new CaseLabelNode(expr);
        }
        if (Match(TokenKind.Keyword, "default"))
        {
            Expect(TokenKind.Separator, ":");
            return new DefaultLabelNode();
        }
        throw new Exception("Ожидалось case или default");
    }

    private StatementNode IterationStatement()
    {
        if (Match(TokenKind.Keyword, "while"))
        {
            Expect(TokenKind.Separator, "(");
            var cond = Condition();
            Expect(TokenKind.Separator, ")");
            var body = Statement();
            return new WhileStatementNode(cond, body);
        }
        if (Match(TokenKind.Keyword, "do"))
        {
            var body = Statement();
            Expect(TokenKind.Keyword, "while");
            Expect(TokenKind.Separator, "(");
            var expr = Expression();
            Expect(TokenKind.Separator, ")");
            Expect(TokenKind.Separator, ";");
            return new DoWhileStatementNode(body, expr);
        }
        if (Match(TokenKind.Keyword, "for"))
        {
            Expect(TokenKind.Separator, "(");
            var init = ForInitStatement();
            var cond = ForCondition();
            Expect(TokenKind.Separator, ";");
            var iter = ForIteration();
            Expect(TokenKind.Separator, ")");
            var body = Statement();
            return new ForStatementNode(init, cond, iter, body);
        }
        throw new Exception("Ожидался цикл");
    }

    private StatementNode ForInitStatement()
    {
        if (IsDeclaration())
            return Declaration();
        // expression statement
        var expr = Expression();
        Expect(TokenKind.Separator, ";");
        return new ExpressionStatementNode(expr);
    }

    private ExpressionNode ForCondition()
    {
        if (Current?.Name == ";") return null;
        return Condition();
    }

    private ExpressionNode ForIteration()
    {
        if (Current?.Name == ")") return null;
        return Expression();
    }

    private JumpStatementNode JumpStatement()
    {
        if (Match(TokenKind.Keyword, "break"))
        {
            Expect(TokenKind.Separator, ";");
            return new BreakNode();
        }
        if (Match(TokenKind.Keyword, "continue"))
        {
            Expect(TokenKind.Separator, ";");
            return new ContinueNode();
        }
        if (Match(TokenKind.Keyword, "return"))
        {
            var expr = Expression();
            Expect(TokenKind.Separator, ";");
            return new ReturnNode(expr);
        }
        if (Match(TokenKind.Keyword, "discard"))
        {
            Expect(TokenKind.Separator, ";");
            return new DiscardNode();
        }
        throw new Exception("Ожидался jump statement");
    }

    // ---- Expressions (возвращают ExpressionNode) ----
    private ExpressionNode Expression()
    {
        var left = AssignmentExpression();
        while (Match(TokenKind.Separator, ","))
        {
            var right = AssignmentExpression();
            left = new BinaryExpressionNode(left, ",", right);
        }
        return left;
    }

    private ExpressionNode AssignmentExpression()
    {
        // ternary first
        var cond = LogicalOrExpression();
        if (Match(TokenKind.Operator, "?"))
        {
            var trueExpr = Expression();
            Expect(TokenKind.Separator, ":");
            var falseExpr = AssignmentExpression();
            return new ConditionalExpressionNode(cond, trueExpr, falseExpr);
        }
        // assignment
        if (UnaryExpression() && IsAssignmentOperator())
        {
            string op = Current.Name;
            _pos++;
            var right = AssignmentExpression();
            return new AssignmentExpressionNode(cond, op, right); // cond - левая часть
        }
        return cond;
    }

    private bool IsAssignmentOperator()
    {
        if (Current?.Kind != TokenKind.Operator) return false;
        string[] ops = { "=", "+=", "-=", "*=", "/=", "%=", "<<=", ">>=", "&=", "^=", "|=" };
        return ops.Contains(Current.Name);
    }

    private ExpressionNode LogicalOrExpression()
    {
        var left = LogicalAndExpression();
        while (Match(TokenKind.Operator, "||"))
        {
            var right = LogicalAndExpression();
            left = new BinaryExpressionNode(left, "||", right);
        }
        return left;
    }

    private ExpressionNode LogicalAndExpression()
    {
        var left = InclusiveOrExpression();
        while (Match(TokenKind.Operator, "&&"))
        {
            var right = InclusiveOrExpression();
            left = new BinaryExpressionNode(left, "&&", right);
        }
        return left;
    }

    private ExpressionNode InclusiveOrExpression()
    {
        var left = ExclusiveOrExpression();
        while (Match(TokenKind.Operator, "|"))
        {
            var right = ExclusiveOrExpression();
            left = new BinaryExpressionNode(left, "|", right);
        }
        return left;
    }

    private ExpressionNode ExclusiveOrExpression()
    {
        var left = AndExpression();
        while (Match(TokenKind.Operator, "^"))
        {
            var right = AndExpression();
            left = new BinaryExpressionNode(left, "^", right);
        }
        return left;
    }

    private ExpressionNode AndExpression()
    {
        var left = EqualityExpression();
        while (Match(TokenKind.Operator, "&"))
        {
            var right = EqualityExpression();
            left = new BinaryExpressionNode(left, "&", right);
        }
        return left;
    }

    private ExpressionNode EqualityExpression()
    {
        var left = RelationalExpression();
        while (true)
        {
            if (Match(TokenKind.Operator, "=="))
                left = new BinaryExpressionNode(left, "==", RelationalExpression());
            else if (Match(TokenKind.Operator, "!="))
                left = new BinaryExpressionNode(left, "!=", RelationalExpression());
            else break;
        }
        return left;
    }

    private ExpressionNode RelationalExpression()
    {
        var left = ShiftExpression();
        while (true)
        {
            if (Match(TokenKind.Operator, "<"))
                left = new BinaryExpressionNode(left, "<", ShiftExpression());
            else if (Match(TokenKind.Operator, ">"))
                left = new BinaryExpressionNode(left, ">", ShiftExpression());
            else if (Match(TokenKind.Operator, "<="))
                left = new BinaryExpressionNode(left, "<=", ShiftExpression());
            else if (Match(TokenKind.Operator, ">="))
                left = new BinaryExpressionNode(left, ">=", ShiftExpression());
            else break;
        }
        return left;
    }

    private ExpressionNode ShiftExpression()
    {
        var left = AdditiveExpression();
        while (true)
        {
            if (Match(TokenKind.Operator, "<<"))
                left = new BinaryExpressionNode(left, "<<", AdditiveExpression());
            else if (Match(TokenKind.Operator, ">>"))
                left = new BinaryExpressionNode(left, ">>", AdditiveExpression());
            else break;
        }
        return left;
    }

    private ExpressionNode AdditiveExpression()
    {
        var left = MultiplicativeExpression();
        while (true)
        {
            if (Match(TokenKind.Operator, "+"))
                left = new BinaryExpressionNode(left, "+", MultiplicativeExpression());
            else if (Match(TokenKind.Operator, "-"))
                left = new BinaryExpressionNode(left, "-", MultiplicativeExpression());
            else break;
        }
        return left;
    }

    private ExpressionNode MultiplicativeExpression()
    {
        var left = UnaryExpression();
        while (true)
        {
            if (Match(TokenKind.Operator, "*"))
                left = new BinaryExpressionNode(left, "*", UnaryExpression());
            else if (Match(TokenKind.Operator, "/"))
                left = new BinaryExpressionNode(left, "/", UnaryExpression());
            else if (Match(TokenKind.Operator, "%"))
                left = new BinaryExpressionNode(left, "%", UnaryExpression());
            else break;
        }
        return left;
    }

    private ExpressionNode UnaryExpression()
    {
        if (Match(TokenKind.Operator, "++") || Match(TokenKind.Operator, "--"))
        {
            string op = Current.Name; // но мы уже съели, сохранить
            // проще: отдельно
        }
        // более простая реализация:
        if (Current != null && (Current.Name == "++" || Current.Name == "--" || Current.Name == "+" || Current.Name == "-" || Current.Name == "!" || Current.Name == "~"))
        {
            string op = Current.Name;
            _pos++;
            var expr = UnaryExpression();
            return new UnaryExpressionNode(op, expr);
        }
        return PostfixExpression();
    }

    private ExpressionNode PostfixExpression()
    {
        var primary = PrimaryExpression();
        while (true)
        {
            if (Match(TokenKind.Separator, "["))
            {
                var index = Expression();
                Expect(TokenKind.Separator, "]");
                primary = new ArrayAccessNode(primary, index);
            }
            else if (Match(TokenKind.Separator, "."))
            {
                if (Current?.Kind == TokenKind.Identifier)
                {
                    string field = Current.Name;
                    _pos++;
                    if (Match(TokenKind.Separator, "("))
                    {
                        var args = FunctionCallParameters();
                        Expect(TokenKind.Separator, ")");
                        primary = new MethodCallNode(primary, field, args);
                    }
                    else
                    {
                        primary = new FieldAccessNode(primary, field);
                    }
                }
                else throw new Exception("Ожидалось имя поля");
            }
            else if (Match(TokenKind.Separator, "("))
            {
                var args = FunctionCallParameters();
                Expect(TokenKind.Separator, ")");
                primary = new FunctionCallNode(primary as IdentifierExpressionNode, args);
            }
            else if (Match(TokenKind.Operator, "++") || Match(TokenKind.Operator, "--"))
            {
                primary = new PostfixIncDecNode(primary, Current.Name);
                // токен уже съеден в Match
            }
            else break;
        }
        return primary;
    }

    private ExpressionNode PrimaryExpression()
    {
        if (Current?.Kind == TokenKind.Identifier)
        {
            string name = Current.Name;
            _pos++;
            return new IdentifierExpressionNode(name);
        }
        if (Current?.Kind == TokenKind.Keyword && (Current.Name == "true" || Current.Name == "false"))
        {
            bool val = Current.Name == "true";
            _pos++;
            return new BoolLiteralNode(val);
        }
        if (IsConstant())
        {
            string text = Current.Name;
            TokenKind kind = Current.Kind;
            _pos++;
            return new LiteralNode(text, kind);
        }
        if (Match(TokenKind.Separator, "("))
        {
            var expr = Expression();
            Expect(TokenKind.Separator, ")");
            return expr;
        }
        // конструктор типа: vec4(0)
        if (IsBasicType() && Peek(0)?.Name != null && Peek(1)?.Name == "(")
        {
            var type = TypeSpecifier(); // basic type with possible arrays
            Expect(TokenKind.Separator, "(");
            var args = FunctionCallParameters();
            Expect(TokenKind.Separator, ")");
            return new ConstructorNode(type, args);
        }
        throw new Exception($"Неожиданный токен в выражении: {Current?.Name}");
    }

    private List<ExpressionNode> FunctionCallParameters()
    {
        var list = new List<ExpressionNode>();
        if (Current?.Name == ")") return list;
        if (Match(TokenKind.Keyword, "void")) return list;
        list.Add(AssignmentExpression());
        while (Match(TokenKind.Separator, ","))
            list.Add(AssignmentExpression());
        return list;
    }

    private ExpressionNode ConstantExpression() => AssignmentExpression(); // упрощённо

    // ---- Вспомогательные методы с пробросом исключений ----
    private void Expect(TokenKind kind, string name = null)
    {
        if (!Match(kind, name))
            throw new Exception($"Ожидался {kind} '{name}', найдено {Current?.Name}");
    }

    private bool Match(TokenKind kind, string name = null)
    {
        if (!HasTokens) return false;
        if (Current.Kind != kind) return false;
        if (name != null && Current.Name != name) return false;
        _pos++;
        return true;
    }

    private string ExpectIdentifier()
    {
        if (Current?.Kind == TokenKind.Identifier)
        {
            string name = Current.Name;
            _pos++;
            return name;
        }
        throw new Exception($"Ожидался идентификатор, найдено {Current?.Name}");
    }

    private void LayoutQualifier()
    {
        if (!Match(TokenKind.Keyword, "layout")) throw new Exception("Ожидался layout");
        if (!Match(TokenKind.Separator, "(")) throw new Exception("Ожидалась (");
        int depth = 1;
        while (HasTokens && depth > 0)
        {
            if (Current.Kind == TokenKind.Separator && Current.Name == "(") depth++;
            else if (Current.Kind == TokenKind.Separator && Current.Name == ")") depth--;
            _pos++;
        }
        if (depth != 0) throw new Exception("Незакрытая скобка в layout");
    }

    private bool IsConstant()
    {
        if (Current == null) return false;
        return Current.Kind == TokenKind.IntConstant ||
               Current.Kind == TokenKind.UIntConstant ||
               Current.Kind == TokenKind.FloatConstant ||
               Current.Kind == TokenKind.DoubleConstant;
    }

    private bool IsEof() => !HasTokens;

    private Term Peek(int offset)
    {
        int idx = _pos + offset;
        return idx < _tokens.Count ? (Term)_tokens[idx] : null;
    }
}