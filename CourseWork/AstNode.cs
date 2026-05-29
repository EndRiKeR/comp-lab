using comp_lab.Labs.lab2.structs;

namespace comp_lab.CourseWork;

public abstract class AstNode { }

// ==================== TRANSLATION UNIT ====================
public class TranslationUnitNode : AstNode
{
    public List<ExternalDeclarationNode> Declarations { get; }

    public TranslationUnitNode(List<ExternalDeclarationNode> decls)
    {
        Declarations = decls;
    }
}

public abstract class ExternalDeclarationNode : AstNode { }

// ==================== FUNCTION ====================
public class FunctionDefinitionNode : ExternalDeclarationNode
{
    public FunctionPrototypeNode Prototype { get; set; }
    public CompoundStatementNoNewScopeNode Body { get; set; }
}

public class FunctionPrototypeNode : AstNode
{
    public TypeNode Type { get; set; }
    public IdentifierNode Name { get; set; }
    public ParametersNode? Parameters { get; set; }
}

public class ParametersNode : AstNode
{
    public List<ParameterDeclarationNode> Parameters { get; set; } = new();
}

public class ParameterDeclarationNode : AstNode
{
    public IdentifierNode? Identifier { get; set; }
    public TypeQualifierNode? TypeQualifier { get; set; }
    public TypeSpecifierNode? ParameterTypeSpecifier { get; set; }
    public ArraySpecifierNode? ArraySpecifier { get; set; }
}

public class ParameterDeclaratorNode : AstNode
{
    public TypeSpecifierNode TypeSpecifier { get; set; }
    public IdentifierNode Identifier { get; set; }
    public ArraySpecifierNode? ArraySpecifier { get; set; }
}

// ==================== DECLARATIONS ====================
public class DeclarationNode : ExternalDeclarationNode
{
    public DeclarationType DeclType { get; set; }
    
    // Для function_prototype
    public FunctionPrototypeNode? FunctionPrototype { get; set; }
    
    // Для init_declarator_list
    public InitDeclaratorListNode? InitDeclaratorList { get; set; }
    
    // Для PRECISION
    public PrecisionQualifierNode? PrecisionQualifier { get; set; }
    public TypeSpecifierNode? PrecisionType { get; set; }
    
    // Для type_qualifier IDENTIFIER { struct_declaration_list }
    public TypeQualifierNode? BlockTypeQualifier { get; set; }
    public IdentifierNode? BlockName { get; set; }
    public StructDeclarationListNode? StructDeclarationList { get; set; }
    public IdentifierNode? BlockInstanceName { get; set; }
    public ArraySpecifierNode? BlockInstanceArraySpecifier { get; set; }
    
    // Для type_qualifier identifier_list?
    public TypeQualifierNode? StandaloneTypeQualifier { get; set; }
    public IdentifierListNode? IdentifierList { get; set; }
    
    public DeclarationNode(FunctionPrototypeNode functionPrototype)
    {
        DeclType = DeclarationType.FunctionPrototype;
        FunctionPrototype = functionPrototype;
    }
    
    public DeclarationNode(InitDeclaratorListNode initDeclaratorList)
    {
        DeclType = DeclarationType.InitDeclaratorList;
        InitDeclaratorList = initDeclaratorList;
    }
    
    public DeclarationNode(PrecisionQualifierNode precisionQualifier, TypeSpecifierNode precisionType)
    {
        DeclType = DeclarationType.Precision;
        PrecisionQualifier = precisionQualifier;
        PrecisionType = precisionType;
    }
    
    public DeclarationNode(TypeQualifierNode typeQualifier, IdentifierNode blockName, 
                           StructDeclarationListNode structDeclarationList, 
                           IdentifierNode? instanceName = null, 
                           ArraySpecifierNode? arraySpecifier = null)
    {
        DeclType = DeclarationType.StructBlock;
        BlockTypeQualifier = typeQualifier;
        BlockName = blockName;
        StructDeclarationList = structDeclarationList;
        BlockInstanceName = instanceName;
        BlockInstanceArraySpecifier = arraySpecifier;
    }
    
    public DeclarationNode(TypeQualifierNode typeQualifier, IdentifierListNode? identifierList = null)
    {
        DeclType = DeclarationType.TypeQualifierOnly;
        StandaloneTypeQualifier = typeQualifier;
        IdentifierList = identifierList;
    }
}

public enum DeclarationType
{
    FunctionPrototype,
    InitDeclaratorList,
    Precision,
    StructBlock,
    TypeQualifierOnly
}

public class InitDeclaratorListNode : AstNode
{
    public List<SingleDeclarationNode> SingleDeclarations { get; set; } = new();
    public List<TypelessDeclarationNode> TypelessDeclarations { get; set; } = new();
}

public class SingleDeclarationNode : AstNode
{
    public FullySpecifiedTypeNode FullySpecifiedType { get; set; }
    public TypelessDeclarationNode? TypelessDeclaration { get; set; }
}

public class TypelessDeclarationNode : AstNode
{
    public IdentifierNode Identifier { get; set; }
    public ArraySpecifierNode? ArraySpecifier { get; set; }
    public InitializerNode? Initializer { get; set; }
}

public class IdentifierListNode : AstNode
{
    public List<IdentifierNode> Identifiers { get; set; } = new();
}

// ==================== TYPES ====================
public class TypeNode : AstNode
{
    public TypeQualifierNode? TypeQualifiers { get; set; }
    public TypeSpecifierNode TypeSpecifier { get; set; }
}

public class FullySpecifiedTypeNode : AstNode
{
    public TypeSpecifierNode? TypeSpecifier { get; set; }
    public TypeQualifierNode? TypeQualifier { get; set; }
}

public class TypeSpecifierNode : AstNode
{
    public IdentifierNode? TypeName { get; set; }
    public TypeSpecifierNonarrayNode NonArrayType { get; set; }
    public ArraySpecifierNode? ArraySpecifier { get; set; }
}

public class TypeSpecifierNonarrayNode : AstNode
{
    public string? BasicType { get; set; }
    public StructSpecifierNode? StructSpecifier { get; set; }
    public IdentifierNode? TypeName { get; set; }
}

public class ArraySpecifierNode : AstNode
{
    public List<DimensionNode> Dimensions { get; set; } = new();
}

public class DimensionNode : AstNode
{
    public ConstantExpressionNode? ConstantExpression { get; set; }
}

// ==================== QUALIFIERS ====================
public class TypeQualifierNode : AstNode
{
    public List<SingleTypeQualifierNode> Qualifiers { get; set; } = new();
}

public abstract class SingleTypeQualifierNode : AstNode { }

public class StorageQualifierNode : SingleTypeQualifierNode
{
    public string Qualifier { get; set; }
    public TypeNameListNode? SubroutineTypeNames { get; set; }
    
    public StorageQualifierNode()
    {
        Qualifier = string.Empty;
    }
}

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

public class LayoutQualifierIdListNode : AstNode
{
    public List<LayoutQualifierIdNode> Ids { get; set; } = new();
}

public class PrecisionQualifierNode : SingleTypeQualifierNode
{
    public string Precision { get; set; }
    
    public PrecisionQualifierNode()
    {
        Precision = string.Empty;
    }
}

public class InterpolationQualifierNode : SingleTypeQualifierNode
{
    public string Qualifier { get; set; }
    
    public InterpolationQualifierNode()
    {
        Qualifier = string.Empty;
    }
}

public class InvariantQualifierNode : SingleTypeQualifierNode { }

public class PreciseQualifierNode : SingleTypeQualifierNode { }

// ==================== STRUCTURES ====================
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

// ==================== INITIALIZER ====================
public class InitializerNode : AstNode
{
    public AssignmentExpressionNode? AssignmentExpression { get; set; }
    public InitializerListNode? InitializerList { get; set; }
}

public class InitializerListNode : AstNode
{
    public List<InitializerNode> Initializers { get; set; } = new();
}

// ==================== STATEMENTS ====================
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

public class CompoundStatementNoNewScopeNode : StatementNode
{
    public List<StatementNode> Statements { get; set; } = new();
}

public class StatementListNode : AstNode
{
    public List<StatementNode> Statements { get; set; } = new();
}

public class SelectionStatementNode : StatementNode
{
    public ExpressionNode Condition { get; set; }
    public StatementNode ThenStatement { get; set; }
    public StatementNode? ElseStatement { get; set; }
}

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

public class SwitchStatementNode : StatementNode
{
    public ExpressionNode Expression { get; set; }
    public List<StatementNode> Statements { get; set; } = new();
}

public class CaseLabelNode : StatementNode
{
    public ExpressionNode? CaseExpression { get; set; }
    public bool IsDefault { get; set; }
}

public class IterationStatementNode : StatementNode
{
    public IterationType Type { get; set; }
    public ConditionNode? WhileCondition { get; set; }
    public StatementNode? WhileBody { get; set; }
    public StatementNode? DoBody { get; set; }
    public ExpressionNode? DoWhileExpression { get; set; }
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

public class ForInitStatementNode : AstNode
{
    public ExpressionStatementNode? ExpressionStatement { get; set; }
    public DeclarationStatementNode? DeclarationStatement { get; set; }
}

public class ForRestStatementNode : AstNode
{
    public ConditionNode? Condition { get; set; }
    public ExpressionNode? Expression { get; set; }
}

public class ConditionNode : AstNode
{
    public ExpressionNode? Expression { get; set; }
    public FullySpecifiedTypeNode? FullySpecifiedType { get; set; }
    public IdentifierNode? Identifier { get; set; }
    public InitializerNode? Initializer { get; set; }
}

public class JumpStatementNode : StatementNode
{
    public JumpType Type { get; set; }
    public ExpressionNode? ReturnExpression { get; set; }
}

public enum JumpType
{
    Break,
    Continue,
    Return,
    Discard
}

public class StatementNoNewScopeNode : StatementNode
{
    public CompoundStatementNoNewScopeNode? CompoundStatement { get; set; }
    public StatementNode? SimpleStatement { get; set; }
}

// ==================== EXPRESSIONS ====================
public abstract class ExpressionNode : AstNode { }

public class PrimaryExpressionNode : ExpressionNode
{
    public IdentifierNode? Identifier { get; set; }
    public bool? BooleanValue { get; set; }
    public string? IntConstant { get; set; }
    public string? UintConstant { get; set; }
    public string? FloatConstant { get; set; }
    public string? DoubleConstant { get; set; }
    public ExpressionNode? ParenthesizedExpression { get; set; }
}

public class PostfixExpressionNode : ExpressionNode
{
    public ExpressionNode? PrimaryExpression { get; set; }
    public PostfixExpressionNode? PostfixExpression { get; set; }
    public string? ArrayIndexExpression { get; set; }
    public FunctionCallParametersNode? FunctionCallParameters { get; set; }
    public TypeSpecifierNode? ConstructorType { get; set; }
    public FieldSelectionNode? FieldSelection { get; set; }
    public bool HasIncOp { get; set; }
    public bool HasDecOp { get; set; }
}

public class FieldSelectionNode : AstNode
{
    public IdentifierNode? Identifier { get; set; }
    public FunctionCallNode? FunctionCall { get; set; }
}

public class FunctionCallNode : AstNode
{
    public FunctionIdentifierNode FunctionIdentifier { get; set; }
    public FunctionCallParametersNode? Parameters { get; set; }
}

public class FunctionIdentifierNode : AstNode
{
    public TypeSpecifierNode? TypeSpecifier { get; set; }
    public PostfixExpressionNode? PostfixExpression { get; set; }
}

public class FunctionCallParametersNode : AstNode
{
    public List<AssignmentExpressionNode> AssignmentExpressions { get; set; } = new();
    public bool IsVoid { get; set; }
}

public class UnaryExpressionNode : ExpressionNode
{
    public PostfixExpressionNode? PostfixExpression { get; set; }
    public bool HasIncOp { get; set; }
    public bool HasDecOp { get; set; }
    public UnaryOperatorNode? UnaryOperator { get; set; }
    public UnaryExpressionNode? Operand { get; set; }
}

public class UnaryOperatorNode : AstNode
{
    public string Operator { get; set; }
    
    public UnaryOperatorNode()
    {
        Operator = string.Empty;
    }
}

public class AssignmentExpressionNode : ExpressionNode
{
    public ConstantExpressionNode? ConstantExpression { get; set; }
    public UnaryExpressionNode? LeftUnary { get; set; }
    public AssignmentOperatorNode? Operator { get; set; }
    public AssignmentExpressionNode? RightAssignment { get; set; }
}

public class AssignmentOperatorNode : AstNode
{
    public string Operator { get; set; }
    
    public AssignmentOperatorNode()
    {
        Operator = string.Empty;
    }
}

public class BinaryExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; set; }
    public string Operator { get; set; }
    public ExpressionNode Right { get; set; }
    
    public BinaryExpressionNode(ExpressionNode left, string op, ExpressionNode right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}

public class ConstantExpressionNode : ExpressionNode
{
    public BinaryExpressionNode? BinaryExpression { get; set; }
    public BinaryExpressionNode? Condition { get; set; }
    public ExpressionNode? TrueExpression { get; set; }
    public AssignmentExpressionNode? FalseExpression { get; set; }
}

public class IdentifierExpressionNode : ExpressionNode
{
    public string Name { get; set; }
    
    public IdentifierExpressionNode(string name)
    {
        Name = name;
    }
}

public class IntegerExpressionNode : AstNode
{
    public ExpressionNode Expression { get; set; }
    
    public IntegerExpressionNode(ExpressionNode expression)
    {
        Expression = expression;
    }
}

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

public class FieldAccessExpressionNode : ExpressionNode
{
    public ExpressionNode Base { get; set; }
    public string FieldName { get; set; }
    public FunctionCallNode? MethodCall { get; set; }
    
    public FieldAccessExpressionNode(ExpressionNode baseExpr, string fieldName)
    {
        Base = baseExpr;
        FieldName = fieldName;
        MethodCall = null;
    }
    
    public FieldAccessExpressionNode(ExpressionNode baseExpr, FunctionCallNode methodCall)
    {
        Base = baseExpr;
        FieldName = methodCall.FunctionIdentifier.TypeSpecifier?.TypeName?.Name ?? "";
        MethodCall = methodCall;
    }
}

// ==================== TYPE NAME LIST ====================
public class TypeNameListNode : AstNode
{
    public List<IdentifierNode> TypeNames { get; set; } = new();
}

// ==================== BASE IDENTIFIER ====================
public class IdentifierNode : AstNode
{
    public string Name { get; set; }
    
    public IdentifierNode(string name)
    {
        Name = name;
    }
}