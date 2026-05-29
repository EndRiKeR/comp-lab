using comp_lab.Labs.lab2.structs;

namespace comp_lab.CourseWork;

public abstract class AstNode { }

// Корень
public class TranslationUnitNode : AstNode
{
    public List<ExternalDeclarationNode> Declarations { get; }
    public TranslationUnitNode(List<ExternalDeclarationNode> decls) => Declarations = decls;
}

public abstract class ExternalDeclarationNode : AstNode { }
public class EmptyDeclarationNode : ExternalDeclarationNode { }
public class VariableDeclarationNode : ExternalDeclarationNode
{
    public TypeNode Type { get; }
    public List<InitDeclaratorNode> Declarators { get; }
    public VariableDeclarationNode(TypeNode type, List<InitDeclaratorNode> decls) => (Type, Declarators) = (type, decls);
}

public class BufferBlockNode : ExternalDeclarationNode
{
    public string Storage { get; } // uniform/buffer
    public string BlockName { get; }
    public List<StructMemberNode> Members { get; }
    public string InstanceName { get; }
    public BufferBlockNode(string storage, string blockName, List<StructMemberNode> members, string instanceName) =>
        (Storage, BlockName, Members, InstanceName) = (storage, blockName, members, instanceName);
}

public class AnonymousBlockNode : ExternalDeclarationNode
{
    public string Tag { get; }
    public List<StructMemberNode> Members { get; }
    public string InstanceName { get; }
    public AnonymousBlockNode(string tag, List<StructMemberNode> members, string instanceName) =>
        (Tag, Members, InstanceName) = (tag, members, instanceName);
}

public class FunctionDefinitionNode : ExternalDeclarationNode
{
    public TypeNode ReturnType { get; }
    public string Name { get; }
    public List<ParameterNode> Parameters { get; }
    public BlockNode Body { get; }
    public FunctionDefinitionNode(TypeNode ret, string name, List<ParameterNode> pars, BlockNode body) =>
        (ReturnType, Name, Parameters, Body) = (ret, name, pars, body);
}

public class ParameterNode : AstNode
{
    public List<string> Qualifiers { get; }
    public TypeNode Type { get; }
    public string Name { get; }
    public ParameterNode(List<string> quals, TypeNode type, string name) => (Qualifiers, Type, Name) = (quals, type, name);
}

public class InitDeclaratorNode : AstNode
{
    public string Name { get; }
    public List<ArrayDimensionNode> Dimensions { get; }
    public ExpressionNode Initializer { get; }
    public InitDeclaratorNode(string name, List<ArrayDimensionNode> dims, ExpressionNode init) =>
        (Name, Dimensions, Initializer) = (name, dims, init);
}

public class StructMemberNode : AstNode
{
    public List<string> Qualifiers { get; }
    public TypeSpecifierNode Type { get; }
    public List<StructDeclaratorNode> Declarators { get; }
    public StructMemberNode(List<string> quals, TypeSpecifierNode type, List<StructDeclaratorNode> decls) =>
        (Qualifiers, Type, Declarators) = (quals, type, decls);
}

public class StructDeclaratorNode : AstNode
{
    public string Name { get; }
    public List<ArrayDimensionNode> Dimensions { get; }
    public StructDeclaratorNode(string name, List<ArrayDimensionNode> dims) => (Name, Dimensions) = (name, dims);
}

public class TypeNode : AstNode
{
    public List<string> Qualifiers { get; }
    public TypeSpecifierNode Specifier { get; }
    public TypeNode(List<string> quals, TypeSpecifierNode spec) => (Qualifiers, Specifier) = (quals, spec);
}

public abstract class TypeSpecifierNode : AstNode { }
public class BasicTypeNode : TypeSpecifierNode
{
    public string Name { get; }
    public List<ArrayDimensionNode> Dimensions { get; }
    public BasicTypeNode(string name, List<ArrayDimensionNode> dims) => (Name, Dimensions) = (name, dims);
}
public class UserTypeNode : TypeSpecifierNode
{
    public string Name { get; }
    public List<ArrayDimensionNode> Dimensions { get; }
    public UserTypeNode(string name, List<ArrayDimensionNode> dims) => (Name, Dimensions) = (name, dims);
}
public class StructTypeNode : TypeSpecifierNode
{
    public string Name { get; }
    public List<StructMemberNode> Members { get; }
    public StructTypeNode(string name, List<StructMemberNode> members) => (Name, Members) = (name, members);
}

public class ArrayDimensionNode : AstNode
{
    public ExpressionNode Size { get; }
    public ArrayDimensionNode(ExpressionNode size) => Size = size;
}

// Statements
public abstract class StatementNode : AstNode { }
public class BlockNode : StatementNode
{
    public List<StatementNode> Statements { get; }
    public BlockNode(List<StatementNode> stmts) => Statements = stmts;
}
public class EmptyStatementNode : StatementNode { }
public class ExpressionStatementNode : StatementNode
{
    public ExpressionNode Expression { get; }
    public ExpressionStatementNode(ExpressionNode expr) => Expression = expr;
}
public class IfStatementNode : StatementNode
{
    public ExpressionNode Condition { get; }
    public StatementNode Then { get; }
    public StatementNode Else { get; }
    public IfStatementNode(ExpressionNode cond, StatementNode then, StatementNode @else) =>
        (Condition, Then, Else) = (cond, then, @else);
}
public class SwitchStatementNode : StatementNode
{
    public ExpressionNode Expression { get; }
    public List<StatementNode> Cases { get; }
    public SwitchStatementNode(ExpressionNode expr, List<StatementNode> cases) => (Expression, Cases) = (expr, cases);
}
public abstract class AbstractCaseLabelNode : StatementNode { }
public class CaseLabelNode : AbstractCaseLabelNode
{
    public ExpressionNode Expression { get; }
    public CaseLabelNode(ExpressionNode expr) => Expression = expr;
}
public class DefaultLabelNode : AbstractCaseLabelNode { }
public class WhileStatementNode : StatementNode
{
    public ExpressionNode Condition { get; }
    public StatementNode Body { get; }
    public WhileStatementNode(ExpressionNode cond, StatementNode body) => (Condition, Body) = (cond, body);
}
public class DoWhileStatementNode : StatementNode
{
    public StatementNode Body { get; }
    public ExpressionNode Condition { get; }
    public DoWhileStatementNode(StatementNode body, ExpressionNode cond) => (Body, Condition) = (body, cond);
}
public class ForStatementNode : StatementNode
{
    public StatementNode Init { get; }
    public ExpressionNode Condition { get; }
    public ExpressionNode Iteration { get; }
    public StatementNode Body { get; }
    public ForStatementNode(StatementNode init, ExpressionNode cond, ExpressionNode iter, StatementNode body) =>
        (Init, Condition, Iteration, Body) = (init, cond, iter, body);
}
public abstract class JumpStatementNode : StatementNode { }
public class BreakNode : JumpStatementNode { }
public class ContinueNode : JumpStatementNode { }
public class ReturnNode : JumpStatementNode
{
    public ExpressionNode Expression { get; }
    public ReturnNode(ExpressionNode expr) => Expression = expr;
}
public class DiscardNode : JumpStatementNode { }

// Expressions
public abstract class ExpressionNode : AstNode { }
public class IdentifierExpressionNode : ExpressionNode
{
    public string Name { get; }
    public IdentifierExpressionNode(string name) => Name = name;
}
public class LiteralNode : ExpressionNode
{
    public string Text { get; }
    public TokenKind Kind { get; }
    public LiteralNode(string text, TokenKind kind) => (Text, Kind) = (text, kind);
}
public class BoolLiteralNode : ExpressionNode
{
    public bool Value { get; }
    public BoolLiteralNode(bool val) => Value = val;
}
public class BinaryExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public string Operator { get; }
    public ExpressionNode Right { get; }
    public BinaryExpressionNode(ExpressionNode left, string op, ExpressionNode right) =>
        (Left, Operator, Right) = (left, op, right);
}
public class UnaryExpressionNode : ExpressionNode
{
    public string Operator { get; }
    public ExpressionNode Operand { get; }
    public UnaryExpressionNode(string op, ExpressionNode operand) => (Operator, Operand) = (op, operand);
}
public class AssignmentExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public string Operator { get; }
    public ExpressionNode Right { get; }
    public AssignmentExpressionNode(ExpressionNode left, string op, ExpressionNode right) =>
        (Left, Operator, Right) = (left, op, right);
}
public class ConditionalExpressionNode : ExpressionNode
{
    public ExpressionNode Condition { get; }
    public ExpressionNode TrueExpr { get; }
    public ExpressionNode FalseExpr { get; }
    public ConditionalExpressionNode(ExpressionNode cond, ExpressionNode trueExpr, ExpressionNode falseExpr) =>
        (Condition, TrueExpr, FalseExpr) = (cond, trueExpr, falseExpr);
}
public class ArrayAccessNode : ExpressionNode
{
    public ExpressionNode Array { get; }
    public ExpressionNode Index { get; }
    public ArrayAccessNode(ExpressionNode array, ExpressionNode index) => (Array, Index) = (array, index);
}
public class FieldAccessNode : ExpressionNode
{
    public ExpressionNode Object { get; }
    public string Field { get; }
    public FieldAccessNode(ExpressionNode obj, string field) => (Object, Field) = (obj, field);
}
public class MethodCallNode : ExpressionNode
{
    public ExpressionNode Object { get; }
    public string Method { get; }
    public List<ExpressionNode> Arguments { get; }
    public MethodCallNode(ExpressionNode obj, string method, List<ExpressionNode> args) =>
        (Object, Method, Arguments) = (obj, method, args);
}
public class FunctionCallNode : ExpressionNode
{
    public IdentifierExpressionNode Function { get; }
    public List<ExpressionNode> Arguments { get; }
    public FunctionCallNode(IdentifierExpressionNode func, List<ExpressionNode> args) =>
        (Function, Arguments) = (func, args);
}
public class ConstructorNode : ExpressionNode
{
    public TypeSpecifierNode Type { get; }
    public List<ExpressionNode> Arguments { get; }
    public ConstructorNode(TypeSpecifierNode type, List<ExpressionNode> args) => (Type, Arguments) = (type, args);
}
public class PostfixIncDecNode : ExpressionNode
{
    public ExpressionNode Operand { get; }
    public string Operator { get; } // "++" или "--"
    public PostfixIncDecNode(ExpressionNode operand, string op) => (Operand, Operator) = (operand, op);
}
public class DeclarationExpressionNode : ExpressionNode
{
    public TypeNode Type { get; }
    public string Name { get; }
    public ExpressionNode Initializer { get; }
    public DeclarationExpressionNode(TypeNode type, string name, ExpressionNode init) =>
        (Type, Name, Initializer) = (type, name, init);
}
public class InitializerListNode : ExpressionNode
{
    public List<ExpressionNode> Elements { get; }
    public InitializerListNode(List<ExpressionNode> elements) => Elements = elements;
}