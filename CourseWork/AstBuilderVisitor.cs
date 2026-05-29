namespace comp_lab.CourseWork;

public partial class AstBuilderVisitor : GLSLParserBaseVisitor<AstNode>
{
    public override AstNode VisitTranslation_unit(GLSLParser.Translation_unitContext context)
    {
        var decls = new List<ExternalDeclarationNode>();
        
        foreach (var extDecl in context.external_declaration())
            decls.Add((ExternalDeclarationNode)Visit(extDecl));
        
        return new TranslationUnitNode(decls);
    }

    public override AstNode VisitExternal_declaration(GLSLParser.External_declarationContext context)
    {
        if (context.function_definition() != null)
            return Visit(context.function_definition());
        if (context.declaration() != null)
            return Visit(context.declaration());
        return null;
    }
    
    public override AstNode VisitFunction_definition(GLSLParser.Function_definitionContext context)
    {
        FunctionPrototypeNode prototype = new FunctionPrototypeNode
        {
            Type = GetTypeNodeFromFunctionDefinition(context),
            Name = CreateIdentifierNode(context),
            Parameters = GetParametersNodeFromFunctionDefinition(context),
        };

        CompoundStatementNode compoundStatement = new CompoundStatementNode
        {
            Statements = GetListOfStatementNodeFromFunctionDefinition(context)
        };
        
        return new FunctionDefinitionNode
        {
            Prototype = prototype,
            CompoundStatement = compoundStatement,
        };
    }

    public override AstNode VisitParameter_declaration(GLSLParser.Parameter_declarationContext context)
    {
        GetTypeNodeFromParameterDeclaration(context, out var spec, out var qual);
        

        return new ParameterDeclarationNode
        {
            Identifier = CreateIdentifierNode(context),
            TypeQualifier = qual,
            ParameterTypeSpecifier = spec,
        };
    }

    public override AstNode VisitType_specifier(GLSLParser.Type_specifierContext context)
    {
        
        
        
        return new TypeSpecifierNode
        {
            TypeName = CreateIdentifierNode(context),
        };
    }
}