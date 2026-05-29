using System.Text;
using comp_lab.CourseWork;

namespace comp_lab.CourseWork.Visualization;

public class AstPrinter
{
    private const string IndentString = "  ";
    
    // ==================== ТЕКСТОВЫЙ ВЫВОД ====================
    
    public string PrintText(AstNode node)
    {
        var sb = new StringBuilder();
        PrintTextRecursive(sb, node, 0);
        return sb.ToString();
    }
    
    private void PrintTextRecursive(StringBuilder sb, AstNode node, int indent)
    {
        if (node == null)
        {
            sb.AppendLine($"{GetIndent(indent)}[null]");
            return;
        }
        
        string indentStr = GetIndent(indent);
        
        switch (node)
        {
            case SingleDeclarationNode single:
                sb.AppendLine($"{indentStr}SingleDeclaration");
                PrintTextRecursive(sb, single.FullySpecifiedType, indent + 1);
                if (single.TypelessDeclaration != null)
                    PrintTextRecursive(sb, single.TypelessDeclaration, indent + 1);
                break;

            case FullySpecifiedTypeNode fully:
                sb.AppendLine($"{indentStr}FullySpecifiedType");
                if (fully.TypeQualifier != null)
                    PrintTextRecursive(sb, fully.TypeQualifier, indent + 1);
                PrintTextRecursive(sb, fully.TypeSpecifier, indent + 1);
                break;

            case TypelessDeclarationNode typeless:
                sb.AppendLine($"{indentStr}TypelessDeclaration: {typeless.Identifier?.Name}");
                if (typeless.ArraySpecifier != null)
                    PrintTextRecursive(sb, typeless.ArraySpecifier, indent + 1);
                if (typeless.Initializer != null)
                    PrintTextRecursive(sb, typeless.Initializer, indent + 1);
                break;

            case StorageQualifierNode storage:
                sb.AppendLine($"{indentStr}StorageQualifier: {storage.Qualifier}");
                if (storage.SubroutineTypeNames != null)
                    PrintTextRecursive(sb, storage.SubroutineTypeNames, indent + 1);
                break;

            case LayoutQualifierNode layout:
                sb.AppendLine($"{indentStr}LayoutQualifier");
                foreach (var id in layout.Ids)
                    PrintTextRecursive(sb, id, indent + 1);
                break;

            case LayoutQualifierIdNode layoutId:
                if (layoutId.Identifier != null)
                    sb.AppendLine($"{indentStr}LayoutId: {layoutId.Identifier.Name}");
                else if (layoutId.IsShared)
                    sb.AppendLine($"{indentStr}LayoutId: shared");
                if (layoutId.ConstantExpression != null)
                    PrintTextRecursive(sb, layoutId.ConstantExpression, indent + 1);
                break;

            case TypeNode type:
                sb.AppendLine($"{indentStr}TypeNode");
                if (type.TypeQualifiers != null)
                    PrintTextRecursive(sb, type.TypeQualifiers, indent + 1);
                PrintTextRecursive(sb, type.TypeSpecifier, indent + 1);
                break;

            case AssignmentExpressionNode assign:
                sb.AppendLine($"{indentStr}AssignmentExpression");
                if (assign.Operator != null)
                    sb.AppendLine($"{GetIndent(indent + 1)}Operator: {assign.Operator.Operator}");
                if (assign.LeftUnary != null)
                    PrintTextRecursive(sb, assign.LeftUnary, indent + 1);
                if (assign.RightAssignment != null)
                    PrintTextRecursive(sb, assign.RightAssignment, indent + 1);
                if (assign.ConstantExpression != null)
                    PrintTextRecursive(sb, assign.ConstantExpression, indent + 1);
                break;

            case BinaryExpressionNode binary:
                sb.AppendLine($"{indentStr}BinaryExpression: {binary.Operator}");
                PrintTextRecursive(sb, binary.Left, indent + 1);
                PrintTextRecursive(sb, binary.Right, indent + 1);
                break;

            case UnaryExpressionNode unary:
                sb.AppendLine($"{indentStr}UnaryExpression");
                if (unary.UnaryOperator != null)
                    sb.AppendLine($"{GetIndent(indent + 1)}Operator: {unary.UnaryOperator.Operator}");
                if (unary.HasIncOp)
                    sb.AppendLine($"{GetIndent(indent + 1)}HasIncOp");
                if (unary.HasDecOp)
                    sb.AppendLine($"{GetIndent(indent + 1)}HasDecOp");
                if (unary.PostfixExpression != null)
                    PrintTextRecursive(sb, unary.PostfixExpression, indent + 1);
                if (unary.Operand != null)
                    PrintTextRecursive(sb, unary.Operand, indent + 1);
                break;

            case PostfixExpressionNode postfix:
                sb.AppendLine($"{indentStr}PostfixExpression");
                if (postfix.PrimaryExpression != null)
                    PrintTextRecursive(sb, postfix.PrimaryExpression, indent + 1);
                if (postfix.PostfixExpression != null)
                    PrintTextRecursive(sb, postfix.PostfixExpression, indent + 1);
                if (postfix.ConstructorType != null)
                    PrintTextRecursive(sb, postfix.ConstructorType, indent + 1);
                if (postfix.FieldSelection != null)
                    PrintTextRecursive(sb, postfix.FieldSelection, indent + 1);
                break;

            case PrimaryExpressionNode primary:
                sb.AppendLine($"{indentStr}PrimaryExpression");
                if (primary.Identifier != null)
                    sb.AppendLine($"{GetIndent(indent + 1)}Identifier: {primary.Identifier.Name}");
                if (primary.BooleanValue.HasValue)
                    sb.AppendLine($"{GetIndent(indent + 1)}Bool: {primary.BooleanValue}");
                if (primary.IntConstant != null)
                    sb.AppendLine($"{GetIndent(indent + 1)}Int: {primary.IntConstant}");
                if (primary.UintConstant != null)
                    sb.AppendLine($"{GetIndent(indent + 1)}UInt: {primary.UintConstant}");
                if (primary.FloatConstant != null)
                    sb.AppendLine($"{GetIndent(indent + 1)}Float: {primary.FloatConstant}");
                if (primary.DoubleConstant != null)
                    sb.AppendLine($"{GetIndent(indent + 1)}Double: {primary.DoubleConstant}");
                if (primary.ParenthesizedExpression != null)
                    PrintTextRecursive(sb, primary.ParenthesizedExpression, indent + 1);
                break;

            case IdentifierNode id:
                sb.AppendLine($"{indentStr}Identifier: {id.Name}");
                break;

            case ConstantExpressionNode constExpr:
                sb.AppendLine($"{indentStr}ConstantExpression");
                if (constExpr.BinaryExpression != null)
                    PrintTextRecursive(sb, constExpr.BinaryExpression, indent + 1);
                if (constExpr.Condition != null)
                    PrintTextRecursive(sb, constExpr.Condition, indent + 1);
                if (constExpr.TrueExpression != null)
                    PrintTextRecursive(sb, constExpr.TrueExpression, indent + 1);
                if (constExpr.FalseExpression != null)
                    PrintTextRecursive(sb, constExpr.FalseExpression, indent + 1);
                break;
            
            // Translation Unit
            case TranslationUnitNode tu:
                sb.AppendLine($"{indentStr}TranslationUnit");
                foreach (var decl in tu.Declarations)
                    PrintTextRecursive(sb, decl, indent + 1);
                break;
                
            // Functions
            case FunctionDefinitionNode func:
                sb.AppendLine($"{indentStr}FunctionDefinition:");
                PrintTextRecursive(sb, func.Prototype, indent + 1);
                PrintTextRecursive(sb, func.Body, indent + 1);
                break;
                
            case FunctionPrototypeNode proto:
                sb.AppendLine($"{indentStr}FunctionPrototype:");
                PrintTextRecursive(sb, proto.Type, indent + 1);
                sb.AppendLine($"{GetIndent(indent + 1)}Name: {proto.Name?.Name}");
                PrintTextRecursive(sb, proto.Parameters, indent + 1);
                break;
                
            case ParametersNode paramsNode:
                sb.AppendLine($"{indentStr}Parameters:");
                foreach (var param in paramsNode.Parameters)
                    PrintTextRecursive(sb, param, indent + 1);
                break;
                
            case ParameterDeclarationNode param:
                sb.AppendLine($"{indentStr}Parameter:");
                if (param.TypeQualifier != null)
                    PrintTextRecursive(sb, param.TypeQualifier, indent + 1);
                PrintTextRecursive(sb, param.ParameterTypeSpecifier, indent + 1);
                if (param.Identifier != null)
                    sb.AppendLine($"{GetIndent(indent + 1)}Name: {param.Identifier.Name}");
                break;
                
            // Declarations
            case DeclarationNode decl:
                sb.AppendLine($"{indentStr}Declaration [{decl.DeclType}]");
                if (decl.FunctionPrototype != null)
                    PrintTextRecursive(sb, decl.FunctionPrototype, indent + 1);
                if (decl.InitDeclaratorList != null)
                    PrintTextRecursive(sb, decl.InitDeclaratorList, indent + 1);
                if (decl.PrecisionQualifier != null)
                    PrintTextRecursive(sb, decl.PrecisionQualifier, indent + 1);
                if (decl.PrecisionType != null)
                    PrintTextRecursive(sb, decl.PrecisionType, indent + 1);
                if (decl.IdentifierList != null)
                    PrintTextRecursive(sb, decl.IdentifierList, indent + 1);
                break;
                
            case InitDeclaratorListNode initDecl:
                sb.AppendLine($"{indentStr}InitDeclaratorList:");
                foreach (var single in initDecl.SingleDeclarations)
                    PrintTextRecursive(sb, single, indent + 1);
                foreach (var typeless in initDecl.TypelessDeclarations)
                    PrintTextRecursive(sb, typeless, indent + 1);
                break;
                
            case TypeSpecifierNode typeSpec:
                if (typeSpec.NonArrayType?.BasicType != null)
                    sb.AppendLine($"{indentStr}TypeSpecifier: {typeSpec.NonArrayType.BasicType}");
                else if (typeSpec.NonArrayType?.StructSpecifier != null)
                {
                    sb.AppendLine($"{indentStr}TypeSpecifier: struct");
                    PrintTextRecursive(sb, typeSpec.NonArrayType.StructSpecifier, indent + 1);
                }
                else if (typeSpec.TypeName != null)
                    sb.AppendLine($"{indentStr}TypeSpecifier: {typeSpec.TypeName.Name}");
                    
                if (typeSpec.ArraySpecifier != null)
                    PrintTextRecursive(sb, typeSpec.ArraySpecifier, indent + 1);
                break;
                
            case ArraySpecifierNode array:
                sb.AppendLine($"{indentStr}ArraySpecifier [{array.Dimensions.Count}]");
                foreach (var dim in array.Dimensions)
                    PrintTextRecursive(sb, dim, indent + 1);
                break;
                
            // Qualifiers
            case TypeQualifierNode typeQual:
                sb.AppendLine($"{indentStr}TypeQualifier:");
                foreach (var qual in typeQual.Qualifiers)
                    PrintTextRecursive(sb, qual, indent + 1);
                break;
            
            case PrecisionQualifierNode precision:
                sb.AppendLine($"{indentStr}Precision: {precision.Precision}");
                break;
                
            // Structures
            case StructSpecifierNode structSpec:
                sb.AppendLine($"{indentStr}Struct: {structSpec.Name?.Name ?? "anonymous"}");
                PrintTextRecursive(sb, structSpec.Declarations, indent + 1);
                break;
                
            case StructDeclarationListNode structDecls:
                sb.AppendLine($"{indentStr}StructDeclarations:");
                foreach (var decl in structDecls.Declarations)
                    PrintTextRecursive(sb, decl, indent + 1);
                break;
                
            case StructDeclarationNode structDecl:
                sb.AppendLine($"{indentStr}StructDeclaration:");
                PrintTextRecursive(sb, structDecl.TypeSpecifier, indent + 1);
                PrintTextRecursive(sb, structDecl.DeclaratorList, indent + 1);
                break;
                
            case StructDeclaratorNode structDeclarator:
                sb.AppendLine($"{indentStr}Member: {structDeclarator.Identifier.Name}");
                if (structDeclarator.ArraySpecifier != null)
                    PrintTextRecursive(sb, structDeclarator.ArraySpecifier, indent + 1);
                break;
                
            // Statements
            case CompoundStatementNode compound:
                sb.AppendLine($"{indentStr}CompoundStatement:");
                foreach (var stmt in compound.Statements)
                    PrintTextRecursive(sb, stmt, indent + 1);
                break;
                
            case CompoundStatementNoNewScopeNode compoundNoScope:
                sb.AppendLine($"{indentStr}CompoundStatement (no new scope):");
                foreach (var stmt in compoundNoScope.Statements)
                    PrintTextRecursive(sb, stmt, indent + 1);
                break;
                
            case DeclarationStatementNode declStmt:
                sb.AppendLine($"{indentStr}DeclarationStatement:");
                PrintTextRecursive(sb, declStmt.Declaration, indent + 1);
                break;
                
            case ExpressionStatementNode exprStmt:
                sb.AppendLine($"{indentStr}ExpressionStatement:");
                PrintTextRecursive(sb, exprStmt.Expression, indent + 1);
                break;
                
            case SelectionStatementNode ifStmt:
                sb.AppendLine($"{indentStr}IfStatement:");
                sb.AppendLine($"{GetIndent(indent + 1)}Condition:");
                PrintTextRecursive(sb, ifStmt.Condition, indent + 2);
                sb.AppendLine($"{GetIndent(indent + 1)}Then:");
                PrintTextRecursive(sb, ifStmt.ThenStatement, indent + 2);
                if (ifStmt.ElseStatement != null)
                {
                    sb.AppendLine($"{GetIndent(indent + 1)}Else:");
                    PrintTextRecursive(sb, ifStmt.ElseStatement, indent + 2);
                }
                break;
                
            case IterationStatementNode loop:
                sb.AppendLine($"{indentStr}IterationStatement [{loop.Type}]");
                switch (loop.Type)
                {
                    case IterationType.While:
                        PrintTextRecursive(sb, loop.WhileCondition, indent + 1);
                        PrintTextRecursive(sb, loop.WhileBody, indent + 1);
                        break;
                    case IterationType.DoWhile:
                        PrintTextRecursive(sb, loop.DoBody, indent + 1);
                        PrintTextRecursive(sb, loop.DoWhileExpression, indent + 1);
                        break;
                    case IterationType.For:
                        PrintTextRecursive(sb, loop.ForInit, indent + 1);
                        PrintTextRecursive(sb, loop.ForRest, indent + 1);
                        PrintTextRecursive(sb, loop.ForBody, indent + 1);
                        break;
                }
                break;
                
            case JumpStatementNode jump:
                sb.AppendLine($"{indentStr}JumpStatement: {jump.Type}");
                if (jump.ReturnExpression != null)
                    PrintTextRecursive(sb, jump.ReturnExpression, indent + 1);
                break;
                
            default:
                sb.AppendLine($"{indentStr}[{node.GetType().Name}]");
                break;
        }
    }
    
    // ==================== GRAPHVIZ DOT ФОРМАТ ====================
    
    public string PrintDot(AstNode node)
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph AST {");
        sb.AppendLine("  node [shape=box, style=filled, fillcolor=lightyellow];");
        sb.AppendLine("  edge [color=gray];");
        
        int nodeId = 0;
        PrintDotRecursive(sb, node, ref nodeId);
        
        sb.AppendLine("}");
        return sb.ToString();
    }
    
    private int PrintDotRecursive(StringBuilder sb, AstNode node, ref int nodeId)
    {
        if (node == null) return -1;

        int currentId = nodeId++;
        string label = GetNodeLabel(node);
        sb.AppendLine($"  node{currentId} [label=\"{EscapeForDot(label)}\"];");

        switch (node)
        {
            case TranslationUnitNode tu:
                foreach (var child in tu.Declarations)
                    AddChild(sb, child, currentId, ref nodeId);
                break;

            case FunctionDefinitionNode func:
                AddChild(sb, func.Prototype, currentId, ref nodeId);
                AddChild(sb, func.Body, currentId, ref nodeId);
                break;

            case FunctionPrototypeNode proto:
                AddChild(sb, proto.Type, currentId, ref nodeId);
                AddChild(sb, proto.Name, currentId, ref nodeId);
                AddChild(sb, proto.Parameters, currentId, ref nodeId);
                break;

            case ParametersNode paramsNode:
                foreach (var p in paramsNode.Parameters)
                    AddChild(sb, p, currentId, ref nodeId);
                break;

            case ParameterDeclarationNode param:
                if (param.Identifier != null)
                    AddChild(sb, param.Identifier, currentId, ref nodeId);
                if (param.TypeQualifier != null)
                    AddChild(sb, param.TypeQualifier, currentId, ref nodeId);
                if (param.ParameterTypeSpecifier != null)
                    AddChild(sb, param.ParameterTypeSpecifier, currentId, ref nodeId);
                if (param.ArraySpecifier != null)
                    AddChild(sb, param.ArraySpecifier, currentId, ref nodeId);
                break;

            case ParameterDeclaratorNode paramDecl:
                AddChild(sb, paramDecl.TypeSpecifier, currentId, ref nodeId);
                AddChild(sb, paramDecl.Identifier, currentId, ref nodeId);
                if (paramDecl.ArraySpecifier != null)
                    AddChild(sb, paramDecl.ArraySpecifier, currentId, ref nodeId);
                break;

            case DeclarationNode decl:
                if (decl.FunctionPrototype != null)
                    AddChild(sb, decl.FunctionPrototype, currentId, ref nodeId);
                if (decl.InitDeclaratorList != null)
                    AddChild(sb, decl.InitDeclaratorList, currentId, ref nodeId);
                if (decl.PrecisionQualifier != null)
                    AddChild(sb, decl.PrecisionQualifier, currentId, ref nodeId);
                if (decl.PrecisionType != null)
                    AddChild(sb, decl.PrecisionType, currentId, ref nodeId);
                if (decl.BlockTypeQualifier != null)
                    AddChild(sb, decl.BlockTypeQualifier, currentId, ref nodeId);
                if (decl.BlockName != null)
                    AddChild(sb, decl.BlockName, currentId, ref nodeId);
                if (decl.StructDeclarationList != null)
                    AddChild(sb, decl.StructDeclarationList, currentId, ref nodeId);
                if (decl.BlockInstanceName != null)
                    AddChild(sb, decl.BlockInstanceName, currentId, ref nodeId);
                if (decl.BlockInstanceArraySpecifier != null)
                    AddChild(sb, decl.BlockInstanceArraySpecifier, currentId, ref nodeId);
                if (decl.StandaloneTypeQualifier != null)
                    AddChild(sb, decl.StandaloneTypeQualifier, currentId, ref nodeId);
                if (decl.IdentifierList != null)
                    AddChild(sb, decl.IdentifierList, currentId, ref nodeId);
                break;

            case InitDeclaratorListNode initDecl:
                foreach (var single in initDecl.SingleDeclarations)
                    AddChild(sb, single, currentId, ref nodeId);
                foreach (var typeless in initDecl.TypelessDeclarations)
                    AddChild(sb, typeless, currentId, ref nodeId);
                break;

            case SingleDeclarationNode single:
                AddChild(sb, single.FullySpecifiedType, currentId, ref nodeId);
                if (single.TypelessDeclaration != null)
                    AddChild(sb, single.TypelessDeclaration, currentId, ref nodeId);
                break;

            case FullySpecifiedTypeNode fully:
                if (fully.TypeQualifier != null)
                    AddChild(sb, fully.TypeQualifier, currentId, ref nodeId);
                AddChild(sb, fully.TypeSpecifier, currentId, ref nodeId);
                break;

            case TypelessDeclarationNode typeless:
                AddChild(sb, typeless.Identifier, currentId, ref nodeId);
                if (typeless.ArraySpecifier != null)
                    AddChild(sb, typeless.ArraySpecifier, currentId, ref nodeId);
                if (typeless.Initializer != null)
                    AddChild(sb, typeless.Initializer, currentId, ref nodeId);
                break;

            case IdentifierListNode idList:
                foreach (var id in idList.Identifiers)
                    AddChild(sb, id, currentId, ref nodeId);
                break;

            case TypeNode type:
                if (type.TypeQualifiers != null)
                    AddChild(sb, type.TypeQualifiers, currentId, ref nodeId);
                AddChild(sb, type.TypeSpecifier, currentId, ref nodeId);
                break;

            case TypeSpecifierNode typeSpec:
                if (typeSpec.NonArrayType != null)
                    AddChild(sb, typeSpec.NonArrayType, currentId, ref nodeId);
                if (typeSpec.ArraySpecifier != null)
                    AddChild(sb, typeSpec.ArraySpecifier, currentId, ref nodeId);
                break;

            case TypeSpecifierNonarrayNode nonArray:
                if (nonArray.StructSpecifier != null)
                    AddChild(sb, nonArray.StructSpecifier, currentId, ref nodeId);
                if (nonArray.TypeName != null)
                    AddChild(sb, nonArray.TypeName, currentId, ref nodeId);
                break;

            case ArraySpecifierNode array:
                foreach (var dim in array.Dimensions)
                    AddChild(sb, dim, currentId, ref nodeId);
                break;

            case DimensionNode dim:
                if (dim.ConstantExpression != null)
                    AddChild(sb, dim.ConstantExpression, currentId, ref nodeId);
                break;

            case TypeQualifierNode typeQual:
                foreach (var qual in typeQual.Qualifiers)
                    AddChild(sb, qual, currentId, ref nodeId);
                break;

            case StorageQualifierNode storage:
                // нет детей
                break;

            case LayoutQualifierNode layout:
                foreach (var id in layout.Ids)
                    AddChild(sb, id, currentId, ref nodeId);
                break;

            case LayoutQualifierIdNode layoutId:
                if (layoutId.Identifier != null)
                    AddChild(sb, layoutId.Identifier, currentId, ref nodeId);
                if (layoutId.ConstantExpression != null)
                    AddChild(sb, layoutId.ConstantExpression, currentId, ref nodeId);
                break;

            case LayoutQualifierIdListNode layoutIdList:
                foreach (var id in layoutIdList.Ids)
                    AddChild(sb, id, currentId, ref nodeId);
                break;

            case PrecisionQualifierNode precision:
                // нет детей
                break;

            case InterpolationQualifierNode interpolation:
                // нет детей
                break;

            case InvariantQualifierNode:
            case PreciseQualifierNode:
                // нет детей
                break;

            case StructSpecifierNode structSpec:
                if (structSpec.Name != null)
                    AddChild(sb, structSpec.Name, currentId, ref nodeId);
                if (structSpec.Declarations != null)
                    AddChild(sb, structSpec.Declarations, currentId, ref nodeId);
                break;

            case StructDeclarationListNode structDecls:
                foreach (var decl in structDecls.Declarations)
                    AddChild(sb, decl, currentId, ref nodeId);
                break;

            case StructDeclarationNode structDecl:
                if (structDecl.TypeSpecifier != null)
                    AddChild(sb, structDecl.TypeSpecifier, currentId, ref nodeId);
                if (structDecl.TypeQualifier != null)
                    AddChild(sb, structDecl.TypeQualifier, currentId, ref nodeId);
                if (structDecl.DeclaratorList != null)
                    AddChild(sb, structDecl.DeclaratorList, currentId, ref nodeId);
                break;

            case StructDeclaratorListNode structDeclaratorList:
                foreach (var decl in structDeclaratorList.Declarators)
                    AddChild(sb, decl, currentId, ref nodeId);
                break;

            case StructDeclaratorNode structDeclarator:
                AddChild(sb, structDeclarator.Identifier, currentId, ref nodeId);
                if (structDeclarator.ArraySpecifier != null)
                    AddChild(sb, structDeclarator.ArraySpecifier, currentId, ref nodeId);
                break;

            case InitializerNode init:
                if (init.AssignmentExpression != null)
                    AddChild(sb, init.AssignmentExpression, currentId, ref nodeId);
                if (init.InitializerList != null)
                    AddChild(sb, init.InitializerList, currentId, ref nodeId);
                break;

            case InitializerListNode initList:
                foreach (var i in initList.Initializers)
                    AddChild(sb, i, currentId, ref nodeId);
                break;

            case CompoundStatementNode compound:
                foreach (var stmt in compound.Statements)
                    AddChild(sb, stmt, currentId, ref nodeId);
                break;

            case CompoundStatementNoNewScopeNode compoundNoScope:
                foreach (var stmt in compoundNoScope.Statements)
                    AddChild(sb, stmt, currentId, ref nodeId);
                break;

            case StatementListNode stmtList:
                foreach (var stmt in stmtList.Statements)
                    AddChild(sb, stmt, currentId, ref nodeId);
                break;

            case DeclarationStatementNode declStmt:
                AddChild(sb, declStmt.Declaration, currentId, ref nodeId);
                break;

            case ExpressionStatementNode exprStmt:
                if (exprStmt.Expression != null)
                    AddChild(sb, exprStmt.Expression, currentId, ref nodeId);
                break;

            case SelectionStatementNode ifStmt:
                AddChild(sb, ifStmt.Condition, currentId, ref nodeId);
                AddChild(sb, ifStmt.ThenStatement, currentId, ref nodeId);
                if (ifStmt.ElseStatement != null)
                    AddChild(sb, ifStmt.ElseStatement, currentId, ref nodeId);
                break;

            case SwitchStatementNode switchStmt:
                AddChild(sb, switchStmt.Expression, currentId, ref nodeId);
                foreach (var stmt in switchStmt.Statements)
                    AddChild(sb, stmt, currentId, ref nodeId);
                break;

            case CaseLabelNode caseLabel:
                if (caseLabel.CaseExpression != null)
                    AddChild(sb, caseLabel.CaseExpression, currentId, ref nodeId);
                break;

            case IterationStatementNode loop:
                switch (loop.Type)
                {
                    case IterationType.While:
                        if (loop.WhileCondition != null)
                            AddChild(sb, loop.WhileCondition, currentId, ref nodeId);
                        if (loop.WhileBody != null)
                            AddChild(sb, loop.WhileBody, currentId, ref nodeId);
                        break;
                    case IterationType.DoWhile:
                        if (loop.DoBody != null)
                            AddChild(sb, loop.DoBody, currentId, ref nodeId);
                        if (loop.DoWhileExpression != null)
                            AddChild(sb, loop.DoWhileExpression, currentId, ref nodeId);
                        break;
                    case IterationType.For:
                        if (loop.ForInit != null)
                            AddChild(sb, loop.ForInit, currentId, ref nodeId);
                        if (loop.ForRest != null)
                            AddChild(sb, loop.ForRest, currentId, ref nodeId);
                        if (loop.ForBody != null)
                            AddChild(sb, loop.ForBody, currentId, ref nodeId);
                        break;
                }
                break;

            case ForInitStatementNode forInit:
                if (forInit.ExpressionStatement != null)
                    AddChild(sb, forInit.ExpressionStatement, currentId, ref nodeId);
                if (forInit.DeclarationStatement != null)
                    AddChild(sb, forInit.DeclarationStatement, currentId, ref nodeId);
                break;

            case ForRestStatementNode forRest:
                if (forRest.Condition != null)
                    AddChild(sb, forRest.Condition, currentId, ref nodeId);
                if (forRest.Expression != null)
                    AddChild(sb, forRest.Expression, currentId, ref nodeId);
                break;

            case ConditionNode condition:
                if (condition.Expression != null)
                    AddChild(sb, condition.Expression, currentId, ref nodeId);
                if (condition.FullySpecifiedType != null)
                    AddChild(sb, condition.FullySpecifiedType, currentId, ref nodeId);
                if (condition.Identifier != null)
                    AddChild(sb, condition.Identifier, currentId, ref nodeId);
                if (condition.Initializer != null)
                    AddChild(sb, condition.Initializer, currentId, ref nodeId);
                break;

            case JumpStatementNode jump:
                if (jump.ReturnExpression != null)
                    AddChild(sb, jump.ReturnExpression, currentId, ref nodeId);
                break;

            case StatementNoNewScopeNode stmtNoScope:
                if (stmtNoScope.CompoundStatement != null)
                    AddChild(sb, stmtNoScope.CompoundStatement, currentId, ref nodeId);
                if (stmtNoScope.SimpleStatement != null)
                    AddChild(sb, stmtNoScope.SimpleStatement, currentId, ref nodeId);
                break;

            // === Выражения ===
            case AssignmentExpressionNode assign:
                if (assign.ConstantExpression != null)
                    AddChild(sb, assign.ConstantExpression, currentId, ref nodeId);
                if (assign.LeftUnary != null)
                    AddChild(sb, assign.LeftUnary, currentId, ref nodeId);
                if (assign.RightAssignment != null)
                    AddChild(sb, assign.RightAssignment, currentId, ref nodeId);
                break;

            case ConstantExpressionNode constExpr:
                if (constExpr.BinaryExpression != null)
                    AddChild(sb, constExpr.BinaryExpression, currentId, ref nodeId);
                if (constExpr.Condition != null)
                    AddChild(sb, constExpr.Condition, currentId, ref nodeId);
                if (constExpr.TrueExpression != null)
                    AddChild(sb, constExpr.TrueExpression, currentId, ref nodeId);
                if (constExpr.FalseExpression != null)
                    AddChild(sb, constExpr.FalseExpression, currentId, ref nodeId);
                break;

            case BinaryExpressionNode binary:
                AddChild(sb, binary.Left, currentId, ref nodeId);
                AddChild(sb, binary.Right, currentId, ref nodeId);
                break;

            case UnaryExpressionNode unary:
                if (unary.PostfixExpression != null)
                    AddChild(sb, unary.PostfixExpression, currentId, ref nodeId);
                if (unary.Operand != null)
                    AddChild(sb, unary.Operand, currentId, ref nodeId);
                break;

            case PostfixExpressionNode postfix:
                if (postfix.PrimaryExpression != null)
                    AddChild(sb, postfix.PrimaryExpression, currentId, ref nodeId);
                if (postfix.PostfixExpression != null)
                    AddChild(sb, postfix.PostfixExpression, currentId, ref nodeId);
                if (postfix.ConstructorType != null)
                    AddChild(sb, postfix.ConstructorType, currentId, ref nodeId);
                if (postfix.FieldSelection != null)
                    AddChild(sb, postfix.FieldSelection, currentId, ref nodeId);
                break;

            case PrimaryExpressionNode primary:
                if (primary.ParenthesizedExpression != null)
                    AddChild(sb, primary.ParenthesizedExpression, currentId, ref nodeId);
                break;

            case FieldSelectionNode fieldSel:
                if (fieldSel.Identifier != null)
                    AddChild(sb, fieldSel.Identifier, currentId, ref nodeId);
                if (fieldSel.FunctionCall != null)
                    AddChild(sb, fieldSel.FunctionCall, currentId, ref nodeId);
                break;

            case FunctionCallNode funcCall:
                AddChild(sb, funcCall.FunctionIdentifier, currentId, ref nodeId);
                if (funcCall.Parameters != null)
                    AddChild(sb, funcCall.Parameters, currentId, ref nodeId);
                break;

            case FunctionIdentifierNode funcId:
                if (funcId.TypeSpecifier != null)
                    AddChild(sb, funcId.TypeSpecifier, currentId, ref nodeId);
                if (funcId.PostfixExpression != null)
                    AddChild(sb, funcId.PostfixExpression, currentId, ref nodeId);
                break;

            case FunctionCallParametersNode funcParams:
                foreach (var expr in funcParams.AssignmentExpressions)
                    AddChild(sb, expr, currentId, ref nodeId);
                break;

            case IdentifierNode id:
                // лист
                break;

            default:
                // Для любых других узлов (например, IntegerExpressionNode, ConstructorExpressionNode и т.д.)
                // можно добавить рекурсивный обход через reflection, но лучше явно добавить нужные
                break;
        }

        return currentId;
    }
    
    private void AddChild(StringBuilder sb, AstNode child, int parentId, ref int nodeId)
    {
        if (child != null)
        {
            int childId = PrintDotRecursive(sb, child, ref nodeId);
            if (childId != -1)
                sb.AppendLine($"  node{parentId} -> node{childId};");
        }
    }
    
    private string GetNodeLabel(AstNode node)
    {
        return node switch
        {
            TranslationUnitNode => "TranslationUnit",
            FunctionDefinitionNode => "FunctionDefinition",
            FunctionPrototypeNode => "FunctionPrototype",
            ParametersNode => "Parameters",
            ParameterDeclarationNode => "Parameter",
            DeclarationNode decl => $"Declaration [{decl.DeclType}]",
            InitDeclaratorListNode => "InitDeclaratorList",
            TypelessDeclarationNode var => $"Variable: {var.Identifier?.Name}",
            TypeNode => "Type",
            TypeSpecifierNode type => $"TypeSpecifier: {type.NonArrayType?.BasicType ?? type.TypeName?.Name ?? "unknown"}",
            StorageQualifierNode storage => $"Storage: {storage.Qualifier}",
            LayoutQualifierNode => "Layout",
            PrecisionQualifierNode precision => $"Precision: {precision.Precision}",
            StructSpecifierNode structSpec => $"Struct: {structSpec.Name?.Name ?? "anonymous"}",
            CompoundStatementNode => "CompoundStatement",
            CompoundStatementNoNewScopeNode => "CompoundStatement (no scope)",
            DeclarationStatementNode => "DeclarationStatement",
            ExpressionStatementNode => "ExpressionStatement",
            SelectionStatementNode => "If",
            IterationStatementNode iter => $"Loop: {iter.Type}",
            JumpStatementNode jump => $"Jump: {jump.Type}",
            BinaryExpressionNode binary => $"Binary: {binary.Operator}",
            UnaryExpressionNode unary => "Unary",
            AssignmentExpressionNode assign => $"Assignment: {assign.Operator?.Operator}",
            PrimaryExpressionNode primary => GetPrimaryLabel(primary),
            IdentifierNode id => $"Identifier: {id.Name}",
            SingleDeclarationNode => "SingleDeclaration",
            FullySpecifiedTypeNode => "FullySpecifiedType",
            ConstantExpressionNode => "ConstantExpression",
            PostfixExpressionNode => "PostfixExpression",
            TypeQualifierNode => "TypeQualifier",
            LayoutQualifierIdNode layoutId =>
                layoutId.Identifier != null ? $"LayoutId: {layoutId.Identifier.Name}" :
                layoutId.IsShared ? "LayoutId: shared" : "LayoutId",
            InterpolationQualifierNode interpolation => $"Interpolation: {interpolation.Qualifier}",
            TypeSpecifierNonarrayNode => "NonArrayType",
            ArraySpecifierNode array => "ArraySpecifier",
            DimensionNode => "Dimension",
            ConditionNode => "Condition",
            ForInitStatementNode => "ForInit",
            ForRestStatementNode => "ForRest",
            _ => node.GetType().Name.Replace("Node", "")
        };
    }
    
    private string GetPrimaryLabel(PrimaryExpressionNode primary)
    {
        if (primary.Identifier != null)
            return $"Identifier: {primary.Identifier.Name}";
        if (primary.BooleanValue.HasValue)
            return $"Boolean: {primary.BooleanValue.Value}";
        if (primary.IntConstant != null)
            return $"Int: {primary.IntConstant}";
        if (primary.UintConstant != null)
            return $"UInt: {primary.UintConstant}";
        if (primary.FloatConstant != null)
            return $"Float: {primary.FloatConstant}";
        if (primary.DoubleConstant != null)
            return $"Double: {primary.DoubleConstant}";
        return "PrimaryExpression";
    }
    
    private string EscapeForDot(string text)
    {
        return text.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\n", "\\n")
                   .Replace("[", "\\[")
                   .Replace("]", "\\]");
    }
    
    private string GetIndent(int level)
    {
        return new string(' ', level * 2);
    }
    
    // ==================== СОХРАНЕНИЕ В ФАЙЛ ====================
    
    public void SaveToFile(string filePath, string content)
    {
        File.WriteAllText(filePath, content);
        Console.WriteLine($"Saved to: {filePath}");
    }
}