using comp_lab.Labs.lab2.structs;

namespace comp_lab.CourseWork;

public abstract class AstNode { }

// TranslationUnitNode
public class TranslationUnitNode : AstNode
{
    public List<ExternalDeclarationNode> Declarations { get; }

    public TranslationUnitNode(List<ExternalDeclarationNode> decls)
    {
        Declarations = decls;   
    }
}

public abstract class ExternalDeclarationNode : AstNode { }

public class FunctionDefinitionNode : ExternalDeclarationNode
{
    public FunctionPrototypeNode Prototype { get; set; }
    public CompoundStatementNode CompoundStatement { get; set; }
}

public class FunctionPrototypeNode : AstNode
{
    public TypeNode Type { get; set; }
    public IdentifierNode Name { get; set; }
    public ParametersNode? Parameters { get; set; }
}

public class TypeNode : AstNode
{
    public TypeQualifierNode? TypeQualifiers { get; set; }
    public TypeSpecifierNode TypeSpecifier { get; set; }
}

public abstract class SingleTypeQualifier : AstNode { }
public class ArraySpecifier : AstNode
{
    public List<DimensionNode> Dimensions { get; set; }
}

public class IdentifierNode(string name) : AstNode
{
    public string Name { get; set; } = name;
}

// ParametersNode - список параметров функции
public class ParametersNode : AstNode
{
    public List<ParameterDeclarationNode> Parameters { get; set; } = new();
}

// ParameterDeclarationNode - один параметр
public class ParameterDeclarationNode : AstNode
{
    public IdentifierNode Identifier { get; set; }
    
    public TypeQualifierNode? TypeQualifier { get; set; }
    public TypeSpecifierNode? ParameterTypeSpecifier { get; set; }
}

// InitializerNode - инициализатор переменной
public class InitializerNode : AstNode
{
    public AssignmentExpressionNode? AssignmentExpression { get; set; }
    public InitializerListNode? InitializerList { get; set; }
}

// InitializerListNode - список инициализаторов в {}
public class InitializerListNode : AstNode
{
    public List<InitializerNode> Initializers { get; set; } = new();
}

// ---------- Выражения (базовые классы) ----------
public abstract class ExpressionNode : AstNode { }

// PrimaryExpressionNode - первичное выражение
public class PrimaryExpressionNode : ExpressionNode
{
    public IdentifierNode? Identifier { get; set; }
    public bool? BooleanValue { get; set; } // true/false
    public string? IntConstant { get; set; }
    public string? UintConstant { get; set; }
    public string? FloatConstant { get; set; }
    public string? DoubleConstant { get; set; }
    public ExpressionNode? ParenthesizedExpression { get; set; }
}

// PostfixExpressionNode - постфиксное выражение
public class PostfixExpressionNode : ExpressionNode
{
    public ExpressionNode? PrimaryExpression { get; set; }
    public PostfixExpressionNode? PostfixExpression { get; set; } // для цепочек
    public string? ArrayIndexExpression { get; set; } // для [expression]
    public FunctionCallParametersNode? FunctionCallParameters { get; set; }
    public TypeSpecifierNode? ConstructorType { get; set; } // для конструкторов типа vec4(0)
    public FieldSelectionNode? FieldSelection { get; set; }
    public bool HasIncOp { get; set; } // ++
    public bool HasDecOp { get; set; } // --
}

// FieldSelectionNode - доступ к полю структуры или вызов метода
public class FieldSelectionNode : AstNode
{
    public IdentifierNode? Identifier { get; set; }
    public FunctionCallNode? FunctionCall { get; set; }
}

// FunctionCallNode - вызов функции
public class FunctionCallNode : AstNode
{
    public FunctionIdentifierNode FunctionIdentifier { get; set; }
    public FunctionCallParametersNode? Parameters { get; set; }
}

// FunctionIdentifierNode - идентификатор функции (тип или постфикс выражение)
public class FunctionIdentifierNode : AstNode
{
    public TypeSpecifierNode? TypeSpecifier { get; set; }
    public PostfixExpressionNode? PostfixExpression { get; set; }
}

// FunctionCallParametersNode - параметры вызова функции
public class FunctionCallParametersNode : AstNode
{
    public List<AssignmentExpressionNode> AssignmentExpressions { get; set; } = new();
    public bool IsVoid { get; set; }
}

// UnaryExpressionNode - унарное выражение
public class UnaryExpressionNode : ExpressionNode
{
    public PostfixExpressionNode? PostfixExpression { get; set; }
    public bool HasIncOp { get; set; } // ++ перед выражением
    public bool HasDecOp { get; set; } // -- перед выражением
    public UnaryOperatorNode? UnaryOperator { get; set; }
    public UnaryExpressionNode? Operand { get; set; }
}

// UnaryOperatorNode - унарный оператор
public class UnaryOperatorNode : AstNode
{
    public string Operator { get; set; } // "+", "-", "!", "~"
}

// AssignmentExpressionNode - выражение присваивания
public class AssignmentExpressionNode : ExpressionNode
{
    public ConstantExpressionNode? ConstantExpression { get; set; }
    public UnaryExpressionNode? LeftUnary { get; set; }
    public AssignmentOperatorNode? Operator { get; set; }
    public AssignmentExpressionNode? RightAssignment { get; set; }
}

// AssignmentOperatorNode - оператор присваивания
public class AssignmentOperatorNode : AstNode
{
    public string Operator { get; set; } // "=", "+=", "-=", "*=", "/=" и т.д.
}

// ConstantExpressionNode - константное выражение
public class ConstantExpressionNode : ExpressionNode
{
    public BinaryExpressionNode? BinaryExpression { get; set; }
    // для тернарного оператора ? :
    public BinaryExpressionNode? Condition { get; set; }
    public ExpressionNode? TrueExpression { get; set; }
    public AssignmentExpressionNode? FalseExpression { get; set; }
}

// ---------- Объявления ----------
public class DeclarationNode : ExternalDeclarationNode
{
    public FunctionPrototypeNode? FunctionPrototype { get; set; }
    public InitDeclaratorListNode? InitDeclaratorList { get; set; }
    public PrecisionQualifierNode? PrecisionQualifier { get; set; }
    public TypeSpecifierNode? PrecisionType { get; set; }
    public TypeQualifierNode? TypeQualifier { get; set; }
    public IdentifierNode? StructIdentifier { get; set; }
    public StructDeclarationListNode? StructDeclarationList { get; set; }
    public IdentifierNode? StructInstanceIdentifier { get; set; }
    public ArraySpecifierNode? StructInstanceArraySpecifier { get; set; }
    public IdentifierListNode? IdentifierList { get; set; }
}

// InitDeclaratorListNode - список инициализаторов объявлений
public class InitDeclaratorListNode : AstNode
{
    public List<SingleDeclarationNode> SingleDeclarations { get; set; } = new();
    public List<TypelessDeclarationNode> TypelessDeclarations { get; set; } = new();
}

// SingleDeclarationNode - объявление с типом
public class SingleDeclarationNode : AstNode
{
    public FullySpecifiedTypeNode FullySpecifiedType { get; set; }
    public TypelessDeclarationNode? TypelessDeclaration { get; set; }
}

// TypelessDeclarationNode - объявление без типа (только имя, массив, инициализатор)
public class TypelessDeclarationNode : AstNode
{
    public IdentifierNode Identifier { get; set; }
    public ArraySpecifierNode? ArraySpecifier { get; set; }
    public InitializerNode? Initializer { get; set; }
}

// FullySpecifiedTypeNode - тип с квалификаторами
public class FullySpecifiedTypeNode : AstNode
{
    public TypeSpecifierNode? TypeSpecifier { get; set; }
    public TypeQualifierNode? TypeQualifier { get; set; }
}

// TypeQualifierNode - контейнер квалификаторов
public class TypeQualifierNode : AstNode
{
    public List<SingleTypeQualifierNode> Qualifiers { get; set; } = new();
}

public abstract class SingleTypeQualifierNode : AstNode { }

// StorageQualifierNode - квалификатор хранения
public class StorageQualifierNode : SingleTypeQualifierNode
{
    public string Qualifier { get; set; } // "const", "in", "out", "inout", "uniform", "buffer", etc.
    public TypeNameListNode? SubroutineTypeNames { get; set; }
}

// LayoutQualifierNode - layout квалификатор
public class LayoutQualifierNode : SingleTypeQualifierNode
{
    public List<LayoutQualifierIdNode> Ids { get; set; } = new();
}

public class LayoutQualifierIdNode : AstNode
{
    public IdentifierNode? Identifier { get; set; }
    public ConstantExpressionNode? ConstantExpression { get; set; }
    public bool IsShared { get; set; }
}

// PrecisionQualifierNode - квалификатор точности
public class PrecisionQualifierNode : SingleTypeQualifierNode
{
    public string Precision { get; set; } // "highp", "mediump", "lowp"
}

// InterpolationQualifierNode - интерполяционный квалификатор
public class InterpolationQualifierNode : SingleTypeQualifierNode
{
    public string Qualifier { get; set; } // "smooth", "flat", "noperspective"
}

// InvariantQualifierNode - invariant квалификатор
public class InvariantQualifierNode : SingleTypeQualifierNode { }

// PreciseQualifierNode - precise квалификатор
public class PreciseQualifierNode : SingleTypeQualifierNode { }

// TypeSpecifierNode - спецификатор типа (с массивами)
public class TypeSpecifierNode : AstNode
{
    public IdentifierNode? TypeName { get; set; }
    public TypeSpecifierNonarrayNode NonArrayType { get; set; }
    public ArraySpecifierNode? ArraySpecifier { get; set; }
}

// TypeSpecifierNonarrayNode - тип без массива
public class TypeSpecifierNonarrayNode : AstNode
{
    public string? BasicType { get; set; } // "void", "int", "float", "vec2", и т.д.
    public StructSpecifierNode? StructSpecifier { get; set; }
    public IdentifierNode? TypeName { get; set; }
}

// StructSpecifierNode - определение структуры
public class StructSpecifierNode : AstNode
{
    public IdentifierNode? Name { get; set; }
    public StructDeclarationListNode? Declarations { get; set; }
}

public class StructDeclarationListNode : AstNode
{
    public List<StructDeclarationNode> Declarations { get; set; } = new();
}

public class StructDeclarationNode : AstNode
{
    public TypeSpecifierNode? TypeSpecifier { get; set; }
    public TypeQualifierNode? TypeQualifier { get; set; }
    public StructDeclaratorListNode? DeclaratorList { get; set; }
}

public class StructDeclaratorListNode : AstNode
{
    public List<StructDeclaratorNode> Declarators { get; set; } = new();
}

public class StructDeclaratorNode : AstNode
{
    public IdentifierNode Identifier { get; set; }
    public ArraySpecifierNode? ArraySpecifier { get; set; }
}

// ---------- Statements (операторы) ----------
public abstract class StatementNode : AstNode { }

public class DeclarationStatementNode : StatementNode
{
    public DeclarationNode Declaration { get; set; }
}

public class ExpressionStatementNode : StatementNode
{
    public ExpressionNode? Expression { get; set; }
}

public class CompoundStatementNode : StatementNode
{
    public List<StatementNode> Statements { get; set; } = new();
}

// Для statement_no_new_scope и compound_statement_no_new_scope - по сути то же самое
public class StatementNoNewScopeNode : StatementNode
{
    public CompoundStatementNoNewScopeNode? CompoundStatement { get; set; }
    public StatementNode? SimpleStatement { get; set; }
}

public class CompoundStatementNoNewScopeNode : StatementNode
{
    public List<StatementNode> Statements { get; set; } = new();
}

// SelectionStatementNode - if/else
public class SelectionStatementNode : StatementNode
{
    public ExpressionNode Condition { get; set; }
    public StatementNode ThenStatement { get; set; }
    public StatementNode? ElseStatement { get; set; }
}

// SwitchStatementNode - switch
public class SwitchStatementNode : StatementNode
{
    public ExpressionNode Expression { get; set; }
    public List<StatementNode> Statements { get; set; } = new();
}

// CaseLabelNode - case/default метка
public class CaseLabelNode : StatementNode
{
    public ExpressionNode? CaseExpression { get; set; } // null для default
    public bool IsDefault { get; set; }
}

// ConditionNode - условие (в if, while, for)
public class ConditionNode : AstNode
{
    public ExpressionNode? Expression { get; set; }
    public FullySpecifiedTypeNode? FullySpecifiedType { get; set; }
    public IdentifierNode? Identifier { get; set; }
    public InitializerNode? Initializer { get; set; }
}

// IterationStatementNode - циклы while, do-while, for
public class IterationStatementNode : StatementNode
{
    public IterationType Type { get; set; } // While, DoWhile, For
    public ConditionNode? WhileCondition { get; set; }
    public StatementNode? WhileBody { get; set; }
    
    // для do-while
    public StatementNode? DoBody { get; set; }
    public ExpressionNode? DoWhileExpression { get; set; }
    
    // для for
    public ForInitStatementNode? ForInit { get; set; }
    public ForRestStatementNode? ForRest { get; set; }
    public StatementNode? ForBody { get; set; }
}

public enum IterationType
{
    While,
    DoWhile,
    For
}

// ForInitStatementNode - инициализация цикла for
public class ForInitStatementNode : AstNode
{
    public ExpressionStatementNode? ExpressionStatement { get; set; }
    public DeclarationStatementNode? DeclarationStatement { get; set; }
}

// ForRestStatementNode - условие и инкремент в for
public class ForRestStatementNode : AstNode
{
    public ConditionNode? Condition { get; set; }
    public ExpressionNode? Expression { get; set; }
}

// JumpStatementNode - break, continue, return, discard
public class JumpStatementNode : StatementNode
{
    public JumpType Type { get; set; }
    public ExpressionNode? ReturnExpression { get; set; } // только для return
}

public enum JumpType
{
    Break,
    Continue,
    Return,
    Discard
}

// IdentifierListNode - список идентификаторов
public class IdentifierListNode : AstNode
{
    public List<IdentifierNode> Identifiers { get; set; } = new();
}

// TypeNameListNode - список имён типов
public class TypeNameListNode : AstNode
{
    public List<IdentifierNode> TypeNames { get; set; } = new();
}

// ArraySpecifierNode - спецификатор массива (измерения)
public class ArraySpecifierNode : AstNode
{
    public List<DimensionNode> Dimensions { get; set; } = new();
}

public class DimensionNode : AstNode
{
    public ConstantExpressionNode? ConstantExpression { get; set; } // null = пустой []
}

// IdentifierExpressionNode - идентификатор как выражение
public class IdentifierExpressionNode : ExpressionNode
{
    public string Name { get; set; }
    
    public IdentifierExpressionNode(string name)
    {
        Name = name;
    }
}

// IntegerExpressionNode - обёртка выражения для индекса массива
public class IntegerExpressionNode : AstNode
{
    public ExpressionNode Expression { get; set; }
    
    public IntegerExpressionNode(ExpressionNode expression)
    {
        Expression = expression;
    }
}

// BinaryExpressionNode - бинарное выражение (общий класс для всех операторов)
public class BinaryExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; set; }
    public string Operator { get; set; } // "+", "-", "*", "/", "%", "<<", ">>", "<", ">", "<=", ">=", "==", "!=", "&", "|", "^", "&&", "||", "^^"
    public ExpressionNode Right { get; set; }
    
    public BinaryExpressionNode(ExpressionNode left, string op, ExpressionNode right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}

// ConstructorExpressionNode - вызов конструктора типа (vec4(0), mat3(1.0))
public class ConstructorExpressionNode : ExpressionNode
{
    public TypeSpecifierNode TypeSpecifier { get; set; }
    public FunctionCallParametersNode Parameters { get; set; }
    
    public ConstructorExpressionNode(TypeSpecifierNode typeSpecifier, FunctionCallParametersNode parameters)
    {
        TypeSpecifier = typeSpecifier;
        Parameters = parameters;
    }
}

// FieldAccessExpressionNode - доступ к полю структуры или вызов метода (.length())
public class FieldAccessExpressionNode : ExpressionNode
{
    public ExpressionNode Base { get; set; } // выражение до точки
    public string FieldName { get; set; } // имя поля
    public FunctionCallNode? MethodCall { get; set; } // если это вызов метода, например .length()
    
    public FieldAccessExpressionNode(ExpressionNode baseExpr, string fieldName)
    {
        Base = baseExpr;
        FieldName = fieldName;
        MethodCall = null;
    }
    
    public FieldAccessExpressionNode(ExpressionNode baseExpr, FunctionCallNode methodCall)
    {
        Base = baseExpr;
        FieldName = methodCall.FunctionIdentifier.TypeSpecifier.TypeName.Name;
        MethodCall = methodCall;
    }
}

// PrecisionDeclarationNode - объявление точности
// PRECISION precision_qualifier type_specifier SEMICOLON
public class PrecisionDeclarationNode : DeclarationNode
{
    public PrecisionQualifierNode PrecisionQualifier { get; set; }
    public TypeSpecifierNode TypeSpecifier { get; set; }
    
    public PrecisionDeclarationNode(PrecisionQualifierNode precisionQualifier, TypeSpecifierNode typeSpecifier)
    {
        PrecisionQualifier = precisionQualifier;
        TypeSpecifier = typeSpecifier;
    }
}

// TypeQualifierDeclarationNode - объявление с type_qualifier и identifier_list
// type_qualifier identifier_list? SEMICOLON
public class TypeQualifierDeclarationNode : DeclarationNode
{
    public TypeQualifierNode TypeQualifier { get; set; }
    public IdentifierListNode? IdentifierList { get; set; }
    
    public TypeQualifierDeclarationNode(TypeQualifierNode typeQualifier, IdentifierListNode? identifierList = null)
    {
        TypeQualifier = typeQualifier;
        IdentifierList = identifierList;
    }
}

// StructDeclarationNode - уже есть, но добавлю недостающие поля
// Нужно дополнить существующий StructDeclarationNode:
public class StructDeclarationNodeComplete : AstNode
{
    public TypeSpecifierNode? TypeSpecifier { get; set; }
    public TypeQualifierNode? TypeQualifier { get; set; }
    public StructDeclaratorListNode DeclaratorList { get; set; }
    
    public StructDeclarationNodeComplete(StructDeclaratorListNode declaratorList)
    {
        DeclaratorList = declaratorList;
    }
    
    public StructDeclarationNodeComplete(TypeSpecifierNode typeSpecifier, StructDeclaratorListNode declaratorList)
    {
        TypeSpecifier = typeSpecifier;
        DeclaratorList = declaratorList;
    }
    
    public StructDeclarationNodeComplete(TypeQualifierNode typeQualifier, TypeSpecifierNode typeSpecifier, StructDeclaratorListNode declaratorList)
    {
        TypeQualifier = typeQualifier;
        TypeSpecifier = typeSpecifier;
        DeclaratorList = declaratorList;
    }
}

// Дополнение для FunctionIdentifierNode - нужно уметь получать имя
public class FunctionIdentifierNodeComplete : AstNode
{
    public TypeSpecifierNode? TypeSpecifier { get; set; }
    public PostfixExpressionNode? PostfixExpression { get; set; }
    
    public FunctionIdentifierNodeComplete(TypeSpecifierNode typeSpecifier)
    {
        TypeSpecifier = typeSpecifier;
    }
    
    public FunctionIdentifierNodeComplete(PostfixExpressionNode postfixExpression)
    {
        PostfixExpression = postfixExpression;
    }
    
    public string GetIdentifierName()
    {
        if (TypeSpecifier != null)
            return TypeSpecifier.TypeName.Name;
        if (PostfixExpression != null)
            return PostfixExpression.ToString();
        return "";
    }
}

// VariableIdentifierNode - уже есть IdentifierNode, но для ясности добавим обёртку
public class VariableIdentifierNode : AstNode
{
    public IdentifierNode Identifier { get; set; }
    
    public VariableIdentifierNode(IdentifierNode identifier)
    {
        Identifier = identifier;
    }
}

// FieldSelectionNode - дополнить для поддержки function_call
public class FieldSelectionNodeComplete : AstNode
{
    public IdentifierNode? Identifier { get; set; }
    public FunctionCallNode? FunctionCall { get; set; }
    
    public FieldSelectionNodeComplete(IdentifierNode identifier)
    {
        Identifier = identifier;
    }
    
    public FieldSelectionNodeComplete(FunctionCallNode functionCall)
    {
        FunctionCall = functionCall;
    }
}

// SelectionRestStatementNode - для обработки else
// Не обязательно создавать отдельный класс, можно в SelectionStatementNode хранить else напрямую
// Но для полноты грамматики добавлю:
public class SelectionRestStatementNode : AstNode
{
    public StatementNode ThenStatement { get; set; }
    public StatementNode? ElseStatement { get; set; }
    
    public SelectionRestStatementNode(StatementNode thenStatement, StatementNode? elseStatement = null)
    {
        ThenStatement = thenStatement;
        ElseStatement = elseStatement;
    }
}

// Полный SelectionStatementNode с использованием SelectionRestStatementNode
public class SelectionStatementNodeComplete : StatementNode
{
    public ExpressionNode Condition { get; set; }
    public SelectionRestStatementNode RestStatement { get; set; }
    
    public SelectionStatementNodeComplete(ExpressionNode condition, SelectionRestStatementNode restStatement)
    {
        Condition = condition;
        RestStatement = restStatement;
    }
}

// StatementNoNewScopeNode - уже есть, но уточню
// В грамматике: statement_no_new_scope : compound_statement_no_new_scope | simple_statement
public class StatementNoNewScopeNodeComplete : StatementNode
{
    public CompoundStatementNoNewScopeNode? CompoundStatement { get; set; }
    public StatementNode? SimpleStatement { get; set; }
    
    public StatementNoNewScopeNodeComplete(CompoundStatementNoNewScopeNode compoundStatement)
    {
        CompoundStatement = compoundStatement;
    }
    
    public StatementNoNewScopeNodeComplete(StatementNode simpleStatement)
    {
        SimpleStatement = simpleStatement;
    }
}

// CompoundStatementNoNewScopeNode - уже есть, но добавлю полную версию
public class CompoundStatementNoNewScopeNodeComplete : StatementNode
{
    public List<StatementNode> Statements { get; set; }
    
    public CompoundStatementNoNewScopeNodeComplete()
    {
        Statements = new List<StatementNode>();
    }
    
    public CompoundStatementNoNewScopeNodeComplete(List<StatementNode> statements)
    {
        Statements = statements;
    }
}

// TypeNameNode - имя типа (для subroutine и других мест)
public class TypeNameNode : AstNode
{
    public IdentifierNode Identifier { get; set; }
    
    public TypeNameNode(IdentifierNode identifier)
    {
        Identifier = identifier;
    }
}

// SubroutineQualifierNode - специальный квалификатор subroutine
public class SubroutineQualifierNode : StorageQualifierNode
{
    public TypeNameListNode? TypeNames { get; set; }
    
    public SubroutineQualifierNode(TypeNameListNode? typeNames = null)
    {
        Qualifier = "subroutine";
        TypeNames = typeNames;
    }
}

