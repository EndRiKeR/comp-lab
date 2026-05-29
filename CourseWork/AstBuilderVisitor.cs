namespace comp_lab.CourseWork;

public class AstBuilderVisitor : GLSLParserBaseVisitor<AstNode>
{
    // -----------------------------------------------------------------
    // Программа
    public override AstNode VisitTranslation_unit(GLSLParser.Translation_unitContext context)
    {
        var decls = new List<ExternalDeclaration>();
        foreach (var extDecl in context.external_declaration())
        {
            decls.Add((ExternalDeclaration)Visit(extDecl));
        }
        return new TranslationUnit(decls);
    }

    public override AstNode VisitExternal_declaration(GLSLParser.External_declarationContext context)
    {
        if (context.function_definition() != null)
            return Visit(context.function_definition());
        if (context.declaration() != null)
            return Visit(context.declaration());
        // пустое ';' — пропускаем, возвращаем null (можно игнорировать)
        return null;
    }

    // Функция
    public override AstNode VisitFunction_definition(GLSLParser.Function_definitionContext context)
    {
        var returnType = (TypeSpecifier)Visit(context.fully_specified_type());
        string name = context.IDENTIFIER().GetText();
        var parameters = new List<ParameterDeclaration>();
        if (context.function_parameters() != null)
        {
            foreach (var param in context.function_parameters().parameter_declaration())
            {
                parameters.Add((ParameterDeclaration)Visit(param));
            }
        }
        var body = (BlockStatement)Visit(context.compound_statement());
        return new FunctionDefinition(returnType, name, parameters, body);
    }

    // Параметр функции
    public override AstNode VisitParameter_declaration(GLSLParser.Parameter_declarationContext context)
    {
        // Упрощённо: тип, имя (если есть)
        var type = (TypeSpecifier)Visit(context.fully_specified_type());
        string name = context.IDENTIFIER()?.GetText() ?? "";
        return new ParameterDeclaration(type, name);
    }

    // Объявление переменной (глобальное или локальное)
    public override AstNode VisitDeclaration(GLSLParser.DeclarationContext context)
    {
        var type = (TypeSpecifier)Visit(context.fully_specified_type());
        var declarators = new List<VariableDeclarator>();
        var initDecl = context.init_declarator_list();
        if (initDecl != null)
        {
            foreach (var singleDecl in initDecl.single_declaration())
            {
                // single_declaration -> fully_specified_type? typeless_declaration
                // тип уже съеден, разбираем только typeless_declaration
                var typeless = singleDecl.typeless_declaration();
                if (typeless != null)
                {
                    var name = typeless.IDENTIFIER().GetText();
                    Expression init = null;
                    if (typeless.initializer() != null)
                        init = (Expression)Visit(typeless.initializer());
                    declarators.Add(new VariableDeclarator(name, init));
                }
            }
            // аналогично для typeless_declaration после запятых
        }
        return new VariableDeclaration(type, declarators);
    }

    // Выражения
    public override AstNode VisitAssignment_expression(GLSLParser.Assignment_expressionContext context)
    {
        if (context.assignment_operator() != null)
        {
            var left = (Expression)Visit(context.unary_expression());
            string op = context.assignment_operator().GetText();
            var right = (Expression)Visit(context.assignment_expression());
            return new AssignmentExpression(left, op, right);
        }
        else
            return Visit(context.constant_expression());
    }

    public override AstNode VisitAdditive_expression(GLSLParser.Additive_expressionContext context)
    {
        var left = (Expression)Visit(context.additive_expression(0));
        var op = context.PLUS() != null ? "+" : "-";
        var right = (Expression)Visit(context.multiplicative_expression(0));
        return new BinaryExpression(left, op, right);
    }

    // ... аналогично для других бинарных операций (multiplicative, equality, logical и т.д.)

    public override AstNode VisitPrimary_expression(GLSLParser.Primary_expressionContext context)
    {
        if (context.IDENTIFIER() != null)
            return new IdentifierExpression(context.IDENTIFIER().GetText());
        if (context.INT_CONST() != null)
        {
            int val = int.Parse(context.INT_CONST().GetText());
            return new IntConstant(val);
        }
        if (context.FLOAT_CONST() != null)
        {
            float val = float.Parse(context.FLOAT_CONST().GetText(), System.Globalization.CultureInfo.InvariantCulture);
            return new FloatConstant(val);
        }
        if (context.expression() != null)
            return Visit(context.expression());
        // true/false
        return base.VisitPrimary_expression(context);
    }

    // Блок операторов
    public override AstNode VisitCompound_statement(GLSLParser.Compound_statementContext context)
    {
        var statements = new List<Statement>();
        foreach (var stmtCtx in context.statement())
        {
            statements.Add((Statement)Visit(stmtCtx));
        }
        return new BlockStatement(statements);
    }

    // Условный оператор
    public override AstNode VisitSelection_statement(GLSLParser.Selection_statementContext context)
    {
        var cond = (Expression)Visit(context.condition());
        var thenStmt = (Statement)Visit(context.statement(0));
        Statement elseStmt = null;
        if (context.statement().Length > 1)
            elseStmt = (Statement)Visit(context.statement(1));
        return new IfStatement(cond, thenStmt, elseStmt);
    }

    // ... и так далее для всех правил
}