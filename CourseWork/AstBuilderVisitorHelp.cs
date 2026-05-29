namespace comp_lab.CourseWork;

public partial class AstBuilderVisitor : GLSLParserBaseVisitor<AstNode>
{
    public IdentifierNode CreateIdentifierNode(GLSLParser.Function_definitionContext context)
    {
        return new IdentifierNode(context.IDENTIFIER().GetText());
    }
    
    public IdentifierNode CreateIdentifierNode(GLSLParser.Parameter_declarationContext context)
    {
        return new IdentifierNode(context.IDENTIFIER().GetText());
    }
    
    public IdentifierNode CreateIdentifierNode(GLSLParser.Type_specifierContext context)
    {
        return new IdentifierNode(context.IDENTIFIER().GetText());
    }
    
    public TypeNode GetTypeNodeFromFunctionDefinition(GLSLParser.Function_definitionContext context)
    {
        var typeSpecifier = (TypeSpecifierNode)Visit(context.fully_specified_type().type_specifier());

        if (context.fully_specified_type().type_qualifier() == null)
            return new TypeNode
            {
                TypeSpecifier = typeSpecifier,
            };
        
        var singleTypeQualifiers = new List<SingleTypeQualifierNode>();
        foreach (var qual in context.fully_specified_type().type_qualifier())
            singleTypeQualifiers.Add((SingleTypeQualifierNode)Visit(qual));

        var typeQualifier = new TypeQualifierNode
        {
            Qualifiers = singleTypeQualifiers,
        };
        
        return new TypeNode
        {
            TypeSpecifier = typeSpecifier,
            TypeQualifiers = typeQualifier
        };
    }
    
    public ParametersNode? GetParametersNodeFromFunctionDefinition(GLSLParser.Function_definitionContext context)
    {
        // без VOID
        var parameters = new List<ParameterDeclarationNode>();
        if (context.function_parameters() == null)
            return new ParametersNode { Parameters = parameters };
            
        foreach (var param in context.function_parameters().parameter_declaration())
            parameters.Add((ParameterDeclarationNode)Visit(param));

        return new ParametersNode { Parameters = parameters };
    }
    
    public List<StatementNode> GetListOfStatementNodeFromFunctionDefinition(GLSLParser.Function_definitionContext context)
    {
        var statements = new List<StatementNode>();
            
        foreach (var param in context.compound_statement().statement())
            statements.Add((StatementNode)Visit(param));

        return statements;
    }
    
    public void GetTypeNodeFromParameterDeclaration(GLSLParser.Parameter_declarationContext context, out TypeSpecifierNode? typeSpecifier, out TypeQualifierNode? typeQualifier)
    {
        typeQualifier = null;
        typeSpecifier = (TypeSpecifierNode)Visit(context.fully_specified_type().type_specifier());

        if (context.fully_specified_type().type_qualifier() == null)
            return;
        
        var singleTypeQualifiers = new List<SingleTypeQualifierNode>();
        foreach (var qual in context.fully_specified_type().type_qualifier())
            singleTypeQualifiers.Add((SingleTypeQualifierNode)Visit(qual));

        typeQualifier = new TypeQualifierNode
        {
            Qualifiers = singleTypeQualifiers,
        };
    }
}