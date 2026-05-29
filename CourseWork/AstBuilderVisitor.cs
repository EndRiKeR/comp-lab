namespace comp_lab.CourseWork;

public partial class AstBuilderVisitor : GLSLParserFullBaseVisitor<AstNode>
{
    // ==================== TRANSLATION UNIT ====================
    public override AstNode VisitTranslation_unit(GLSLParserFull.Translation_unitContext context)
    {
        var decls = new List<ExternalDeclarationNode>();
        foreach (var extDecl in context.external_declaration())
        {
            var node = Visit(extDecl);
            if (node is ExternalDeclarationNode decl)
                decls.Add(decl);
        }
        return new TranslationUnitNode(decls);
    }

    public override AstNode VisitExternal_declaration(GLSLParserFull.External_declarationContext context)
    {
        if (context.function_definition() != null)
            return Visit(context.function_definition());
        if (context.declaration() != null)
            return Visit(context.declaration());
        // пустое объявление (просто ;) — игнорируем
        return null;
    }

    // ==================== FUNCTION ====================
    public override AstNode VisitFunction_definition(GLSLParserFull.Function_definitionContext context)
    {
        var prototype = (FunctionPrototypeNode)Visit(context.function_prototype());
        var body = (CompoundStatementNoNewScopeNode)Visit(context.compound_statement_no_new_scope());
        
        return new FunctionDefinitionNode
        {
            Prototype = prototype,
            Body = body
        };
    }

    public override AstNode VisitFunction_prototype(GLSLParserFull.Function_prototypeContext context)
    {
        // Получаем FullySpecifiedTypeNode
        var fullySpecifiedType = (FullySpecifiedTypeNode)Visit(context.fully_specified_type());
    
        // Преобразуем FullySpecifiedTypeNode в TypeNode
        var typeNode = new TypeNode
        {
            TypeSpecifier = fullySpecifiedType.TypeSpecifier,
            TypeQualifiers = fullySpecifiedType.TypeQualifier
        };
    
        var name = new IdentifierNode(context.IDENTIFIER().GetText());
        var parameters = context.function_parameters() != null 
            ? (ParametersNode)Visit(context.function_parameters()) 
            : null;
    
        return new FunctionPrototypeNode
        {
            Type = typeNode,
            Name = name,
            Parameters = parameters
        };
    }

    public override AstNode VisitFunction_parameters(GLSLParserFull.Function_parametersContext context)
    {
        var parameters = new List<ParameterDeclarationNode>();
        foreach (var param in context.parameter_declaration())
        {
            parameters.Add((ParameterDeclarationNode)Visit(param));
        }
        return new ParametersNode { Parameters = parameters };
    }

    public override AstNode VisitParameter_declaration(GLSLParserFull.Parameter_declarationContext context)
    {
        var result = new ParameterDeclarationNode();
        
        // Проверяем различные варианты параметра
        if (context.type_qualifier() != null)
        {
            result.TypeQualifier = (TypeQualifierNode)Visit(context.type_qualifier());
        }
        
        if (context.parameter_declarator() != null)
        {
            var declarator = (ParameterDeclaratorNode)Visit(context.parameter_declarator());
            result.Identifier = declarator.Identifier;
            result.ParameterTypeSpecifier = declarator.TypeSpecifier;
            result.ArraySpecifier = declarator.ArraySpecifier;
        }
        else if (context.parameter_type_specifier() != null)
        {
            result.ParameterTypeSpecifier = (TypeSpecifierNode)Visit(context.parameter_type_specifier());
        }
        
        return result;
    }

    public override AstNode VisitParameter_declarator(GLSLParserFull.Parameter_declaratorContext context)
    {
        return new ParameterDeclaratorNode
        {
            TypeSpecifier = (TypeSpecifierNode)Visit(context.type_specifier()),
            Identifier = new IdentifierNode(context.IDENTIFIER().GetText()),
            ArraySpecifier = context.array_specifier() != null 
                ? (ArraySpecifierNode)Visit(context.array_specifier()) 
                : null
        };
    }

    public override AstNode VisitParameter_type_specifier(GLSLParserFull.Parameter_type_specifierContext context)
    {
        return Visit(context.type_specifier());
    }

    // ==================== DECLARATIONS ====================
    public override AstNode VisitDeclaration(GLSLParserFull.DeclarationContext context)
    {
        // 1. function_prototype;
        if (context.function_prototype() != null)
        {
            return new DeclarationNode((FunctionPrototypeNode)Visit(context.function_prototype()));
        }
        
        // 2. init_declarator_list;
        if (context.init_declarator_list() != null)
        {
            return new DeclarationNode((InitDeclaratorListNode)Visit(context.init_declarator_list()));
        }
        
        // 3. PRECISION precision_qualifier type_specifier;
        if (context.PRECISION() != null)
        {
            var precisionQualifier = (PrecisionQualifierNode)Visit(context.precision_qualifier());
            var typeSpecifier = (TypeSpecifierNode)Visit(context.type_specifier());
            return new DeclarationNode(precisionQualifier, typeSpecifier);
        }
        
        // 4. type_qualifier IDENTIFIER { struct_declaration_list } (IDENTIFIER array_specifier?)?
        if (context.type_qualifier() != null && context.IDENTIFIER().Length > 0 && context.LEFT_BRACE() != null)
        {
            var typeQualifier = (TypeQualifierNode)Visit(context.type_qualifier());
            var blockName = new IdentifierNode(context.IDENTIFIER(0).GetText());
            var structDeclList = (StructDeclarationListNode)Visit(context.struct_declaration_list());
            
            IdentifierNode? instanceName = null;
            ArraySpecifierNode? arraySpecifier = null;
            
            if (context.IDENTIFIER().Length > 1)
            {
                instanceName = new IdentifierNode(context.IDENTIFIER(1).GetText());
                if (context.array_specifier() != null)
                {
                    arraySpecifier = (ArraySpecifierNode)Visit(context.array_specifier());
                }
            }
            
            return new DeclarationNode(typeQualifier, blockName, structDeclList, instanceName, arraySpecifier);
        }
        
        // 5. type_qualifier identifier_list?;
        if (context.type_qualifier() != null)
        {
            var typeQualifier = (TypeQualifierNode)Visit(context.type_qualifier());
            var identifierList = context.identifier_list() != null 
                ? (IdentifierListNode)Visit(context.identifier_list()) 
                : null;
            return new DeclarationNode(typeQualifier, identifierList);
        }
        
        return null;
    }

    public override AstNode VisitInit_declarator_list(GLSLParserFull.Init_declarator_listContext context)
    {
        var result = new InitDeclaratorListNode();
        
        // single_declaration
        result.SingleDeclarations.Add((SingleDeclarationNode)Visit(context.single_declaration()));
        
        // typeless_declaration*
        foreach (var typeless in context.typeless_declaration())
        {
            result.TypelessDeclarations.Add((TypelessDeclarationNode)Visit(typeless));
        }
        
        return result;
    }

    public override AstNode VisitSingle_declaration(GLSLParserFull.Single_declarationContext context)
    {
        var result = new SingleDeclarationNode
        {
            FullySpecifiedType = (FullySpecifiedTypeNode)Visit(context.fully_specified_type())
        };
        
        if (context.typeless_declaration() != null)
        {
            result.TypelessDeclaration = (TypelessDeclarationNode)Visit(context.typeless_declaration());
        }
        
        return result;
    }

    public override AstNode VisitTypeless_declaration(GLSLParserFull.Typeless_declarationContext context)
    {
        var result = new TypelessDeclarationNode
        {
            Identifier = new IdentifierNode(context.IDENTIFIER().GetText())
        };
        
        if (context.array_specifier() != null)
        {
            result.ArraySpecifier = (ArraySpecifierNode)Visit(context.array_specifier());
        }
        
        if (context.initializer() != null)
        {
            result.Initializer = (InitializerNode)Visit(context.initializer());
        }
        
        return result;
    }

    // ==================== IDENTIFIER LIST ====================
    public override AstNode VisitIdentifier_list(GLSLParserFull.Identifier_listContext context)
    {
        var result = new IdentifierListNode();
        foreach (var id in context.IDENTIFIER())
        {
            result.Identifiers.Add(new IdentifierNode(id.GetText()));
        }
        return result;
    }

    // ==================== TYPES ====================
    public override AstNode VisitFully_specified_type(GLSLParserFull.Fully_specified_typeContext context)
    {
        var result = new FullySpecifiedTypeNode();
        
        if (context.type_qualifier() != null)
        {
            result.TypeQualifier = (TypeQualifierNode)Visit(context.type_qualifier());
        }
        
        result.TypeSpecifier = (TypeSpecifierNode)Visit(context.type_specifier());
        
        return result;
    }

    public override AstNode VisitType_specifier(GLSLParserFull.Type_specifierContext context)
    {
        var result = new TypeSpecifierNode
        {
            NonArrayType = (TypeSpecifierNonarrayNode)Visit(context.type_specifier_nonarray())
        };
        
        if (context.array_specifier() != null)
        {
            result.ArraySpecifier = (ArraySpecifierNode)Visit(context.array_specifier());
        }
        
        return result;
    }

    public override AstNode VisitType_specifier_nonarray(GLSLParserFull.Type_specifier_nonarrayContext context)
    {
        var result = new TypeSpecifierNonarrayNode();
        
        // Проверяем все возможные варианты
        if (context.VOID() != null) result.BasicType = "void";
        else if (context.FLOAT() != null) result.BasicType = "float";
        else if (context.DOUBLE() != null) result.BasicType = "double";
        else if (context.INT() != null) result.BasicType = "int";
        else if (context.UINT() != null) result.BasicType = "uint";
        else if (context.BOOL() != null) result.BasicType = "bool";
        else if (context.VEC2() != null) result.BasicType = "vec2";
        else if (context.VEC3() != null) result.BasicType = "vec3";
        else if (context.VEC4() != null) result.BasicType = "vec4";
        else if (context.DVEC2() != null) result.BasicType = "dvec2";
        else if (context.DVEC3() != null) result.BasicType = "dvec3";
        else if (context.DVEC4() != null) result.BasicType = "dvec4";
        else if (context.IVEC2() != null) result.BasicType = "ivec2";
        else if (context.IVEC3() != null) result.BasicType = "ivec3";
        else if (context.IVEC4() != null) result.BasicType = "ivec4";
        else if (context.UVEC2() != null) result.BasicType = "uvec2";
        else if (context.UVEC3() != null) result.BasicType = "uvec3";
        else if (context.UVEC4() != null) result.BasicType = "uvec4";
        else if (context.MAT2() != null) result.BasicType = "mat2";
        else if (context.MAT3() != null) result.BasicType = "mat3";
        else if (context.MAT4() != null) result.BasicType = "mat4";
        else if (context.MAT2X2() != null) result.BasicType = "mat2x2";
        else if (context.MAT2X3() != null) result.BasicType = "mat2x3";
        else if (context.MAT2X4() != null) result.BasicType = "mat2x4";
        else if (context.MAT3X2() != null) result.BasicType = "mat3x2";
        else if (context.MAT3X3() != null) result.BasicType = "mat3x3";
        else if (context.MAT3X4() != null) result.BasicType = "mat3x4";
        else if (context.MAT4X2() != null) result.BasicType = "mat4x2";
        else if (context.MAT4X3() != null) result.BasicType = "mat4x3";
        else if (context.MAT4X4() != null) result.BasicType = "mat4x4";
        else if (context.DMAT2() != null) result.BasicType = "dmat2";
        else if (context.DMAT3() != null) result.BasicType = "dmat3";
        else if (context.DMAT4() != null) result.BasicType = "dmat4";
        else if (context.DMAT4() != null)
        {
            result.StructSpecifier = (StructSpecifierNode)Visit(context.struct_specifier());
        }
        else if (context.type_name() != null)
        {
            result.TypeName = (IdentifierNode)Visit(context.type_name());
        }
        // Для простоты остальные типы (sampler, image и т.д.) можно хранить как строку
        else
        {
            // Берём текст первого токена как имя типа
            result.BasicType = context.GetText();
        }
        
        return result;
    }

    public override AstNode VisitType_name(GLSLParserFull.Type_nameContext context)
    {
        return new IdentifierNode(context.IDENTIFIER().GetText());
    }

    // ==================== ARRAY SPECIFIER ====================
    public override AstNode VisitArray_specifier(GLSLParserFull.Array_specifierContext context)
    {
        var result = new ArraySpecifierNode();
        foreach (var dim in context.dimension())
        {
            result.Dimensions.Add((DimensionNode)Visit(dim));
        }
        return result;
    }

    public override AstNode VisitDimension(GLSLParserFull.DimensionContext context)
    {
        var result = new DimensionNode();
        if (context.constant_expression() != null)
        {
            result.ConstantExpression = (ConstantExpressionNode)Visit(context.constant_expression());
        }
        return result;
    }

    // ==================== QUALIFIERS ====================
    public override AstNode VisitType_qualifier(GLSLParserFull.Type_qualifierContext context)
    {
        var result = new TypeQualifierNode();
        foreach (var qual in context.single_type_qualifier())
        {
            result.Qualifiers.Add((SingleTypeQualifierNode)Visit(qual));
        }
        return result;
    }

    public override AstNode VisitSingle_type_qualifier(GLSLParserFull.Single_type_qualifierContext context)
    {
        if (context.storage_qualifier() != null)
            return Visit(context.storage_qualifier());
        if (context.layout_qualifier() != null)
            return Visit(context.layout_qualifier());
        if (context.precision_qualifier() != null)
            return Visit(context.precision_qualifier());
        if (context.interpolation_qualifier() != null)
            return Visit(context.interpolation_qualifier());
        if (context.invariant_qualifier() != null)
            return new InvariantQualifierNode();
        if (context.precise_qualifier() != null)
            return new PreciseQualifierNode();
        
        return null;
    }

    public override AstNode VisitStorage_qualifier(GLSLParserFull.Storage_qualifierContext context)
    {
        var result = new StorageQualifierNode();
        
        if (context.CONST() != null) result.Qualifier = "const";
        else if (context.IN() != null) result.Qualifier = "in";
        else if (context.OUT() != null) result.Qualifier = "out";
        else if (context.INOUT() != null) result.Qualifier = "inout";
        else if (context.CENTROID() != null) result.Qualifier = "centroid";
        else if (context.PATCH() != null) result.Qualifier = "patch";
        else if (context.SAMPLE() != null) result.Qualifier = "sample";
        else if (context.UNIFORM() != null) result.Qualifier = "uniform";
        else if (context.BUFFER() != null) result.Qualifier = "buffer";
        else if (context.SHARED() != null) result.Qualifier = "shared";
        else if (context.COHERENT() != null) result.Qualifier = "coherent";
        else if (context.VOLATILE() != null) result.Qualifier = "volatile";
        else if (context.RESTRICT() != null) result.Qualifier = "restrict";
        else if (context.READONLY() != null) result.Qualifier = "readonly";
        else if (context.WRITEONLY() != null) result.Qualifier = "writeonly";
        else if (context.ATTRIBUTE() != null) result.Qualifier = "attribute";
        else if (context.VARYING() != null) result.Qualifier = "varying";
        else if (context.SUBROUTINE() != null)
        {
            result.Qualifier = "subroutine";
            if (context.type_name_list() != null)
            {
                result.SubroutineTypeNames = (TypeNameListNode)Visit(context.type_name_list());
            }
        }
        
        return result;
    }

    public override AstNode VisitPrecision_qualifier(GLSLParserFull.Precision_qualifierContext context)
    {
        var result = new PrecisionQualifierNode();
        if (context.HIGHP() != null) result.Precision = "highp";
        else if (context.MEDIUMP() != null) result.Precision = "mediump";
        else if (context.LOWP() != null) result.Precision = "lowp";
        return result;
    }

    public override AstNode VisitInterpolation_qualifier(GLSLParserFull.Interpolation_qualifierContext context)
    {
        var result = new InterpolationQualifierNode();
        if (context.SMOOTH() != null) result.Qualifier = "smooth";
        else if (context.FLAT() != null) result.Qualifier = "flat";
        else if (context.NOPERSPECTIVE() != null) result.Qualifier = "noperspective";
        return result;
    }

    public override AstNode VisitLayout_qualifier(GLSLParserFull.Layout_qualifierContext context)
    {
        var result = new LayoutQualifierNode();
        result.Ids = ((LayoutQualifierIdListNode)Visit(context.layout_qualifier_id_list())).Ids;
        return result;
    }

    public override AstNode VisitLayout_qualifier_id_list(GLSLParserFull.Layout_qualifier_id_listContext context)
    {
        var result = new LayoutQualifierIdListNode();
        foreach (var id in context.layout_qualifier_id())
        {
            result.Ids.Add((LayoutQualifierIdNode)Visit(id));
        }
        return result;
    }

    public override AstNode VisitLayout_qualifier_id(GLSLParserFull.Layout_qualifier_idContext context)
    {
        var result = new LayoutQualifierIdNode();
    
        if (context.IDENTIFIER() != null)
        {
            result.Identifier = new IdentifierNode(context.IDENTIFIER().GetText());
            if (context.constant_expression() != null)
            {
                var constNode = Visit(context.constant_expression());
                if (constNode is ConstantExpressionNode constExpr)
                    result.ConstantExpression = constExpr;
            }
        }
        else if (context.SHARED() != null)
        {
            result.IsShared = true;
        }
    
        return result;
    }

    // ==================== STRUCTURES ====================
    public override AstNode VisitStruct_specifier(GLSLParserFull.Struct_specifierContext context)
    {
        var result = new StructSpecifierNode();
        
        if (context.IDENTIFIER() != null)
        {
            result.Name = new IdentifierNode(context.IDENTIFIER().GetText());
        }
        
        result.Declarations = (StructDeclarationListNode)Visit(context.struct_declaration_list());
        
        return result;
    }

    public override AstNode VisitStruct_declaration_list(GLSLParserFull.Struct_declaration_listContext context)
    {
        var result = new StructDeclarationListNode();
        foreach (var decl in context.struct_declaration())
        {
            result.Declarations.Add((StructDeclarationNode)Visit(decl));
        }
        return result;
    }

    public override AstNode VisitStruct_declaration(GLSLParserFull.Struct_declarationContext context)
    {
        var result = new StructDeclarationNode();
        
        if (context.type_qualifier() != null)
        {
            result.TypeQualifier = (TypeQualifierNode)Visit(context.type_qualifier());
        }
        
        result.TypeSpecifier = (TypeSpecifierNode)Visit(context.type_specifier());
        result.DeclaratorList = (StructDeclaratorListNode)Visit(context.struct_declarator_list());
        
        return result;
    }

    public override AstNode VisitStruct_declarator_list(GLSLParserFull.Struct_declarator_listContext context)
    {
        var result = new StructDeclaratorListNode();
        foreach (var decl in context.struct_declarator())
        {
            result.Declarators.Add((StructDeclaratorNode)Visit(decl));
        }
        return result;
    }

    public override AstNode VisitStruct_declarator(GLSLParserFull.Struct_declaratorContext context)
    {
        var result = new StructDeclaratorNode
        {
            Identifier = new IdentifierNode(context.IDENTIFIER().GetText())
        };
        
        if (context.array_specifier() != null)
        {
            result.ArraySpecifier = (ArraySpecifierNode)Visit(context.array_specifier());
        }
        
        return result;
    }

    // ==================== INITIALIZER ====================
    public override AstNode VisitInitializer(GLSLParserFull.InitializerContext context)
    {
        var result = new InitializerNode();
        
        if (context.assignment_expression() != null)
        {
            result.AssignmentExpression = (AssignmentExpressionNode)Visit(context.assignment_expression());
        }
        else if (context.initializer_list() != null)
        {
            result.InitializerList = (InitializerListNode)Visit(context.initializer_list());
        }
        
        return result;
    }

    public override AstNode VisitInitializer_list(GLSLParserFull.Initializer_listContext context)
    {
        var result = new InitializerListNode();
        foreach (var init in context.initializer())
        {
            result.Initializers.Add((InitializerNode)Visit(init));
        }
        return result;
    }

    // ==================== STATEMENTS ====================
    public override AstNode VisitCompound_statement(GLSLParserFull.Compound_statementContext context)
    {
        var result = new CompoundStatementNode();
        if (context.statement_list() != null)
        {
            var statementList = (StatementListNode)Visit(context.statement_list());
            result.Statements = statementList.Statements;
        }
        return result;
    }

    public override AstNode VisitCompound_statement_no_new_scope(GLSLParserFull.Compound_statement_no_new_scopeContext context)
    {
        var result = new CompoundStatementNoNewScopeNode();
        if (context.statement_list() != null)
        {
            var statementList = (StatementListNode)Visit(context.statement_list());
            result.Statements = statementList.Statements;
        }
        return result;
    }

    public override AstNode VisitStatement_list(GLSLParserFull.Statement_listContext context)
    {
        var result = new StatementListNode();
        foreach (var stmt in context.statement())
        {
            result.Statements.Add((StatementNode)Visit(stmt));
        }
        return result;
    }

    public override AstNode VisitStatement(GLSLParserFull.StatementContext context)
    {
        if (context.compound_statement() != null)
            return Visit(context.compound_statement());
        return Visit(context.simple_statement());
    }

    public override AstNode VisitSimple_statement(GLSLParserFull.Simple_statementContext context)
    {
        if (context.declaration_statement() != null)
            return Visit(context.declaration_statement());
        if (context.expression_statement() != null)
            return Visit(context.expression_statement());
        if (context.selection_statement() != null)
            return Visit(context.selection_statement());
        if (context.switch_statement() != null)
            return Visit(context.switch_statement());
        if (context.case_label() != null)
            return Visit(context.case_label());
        if (context.iteration_statement() != null)
            return Visit(context.iteration_statement());
        if (context.jump_statement() != null)
            return Visit(context.jump_statement());
        
        return null;
    }

    public override AstNode VisitDeclaration_statement(GLSLParserFull.Declaration_statementContext context)
    {
        return new DeclarationStatementNode
        {
            Declaration = (DeclarationNode)Visit(context.declaration())
        };
    }

    public override AstNode VisitExpression_statement(GLSLParserFull.Expression_statementContext context)
    {
        var result = new ExpressionStatementNode();
        if (context.expression() != null)
        {
            result.Expression = (ExpressionNode)Visit(context.expression());
        }
        return result;
    }

    public override AstNode VisitSelection_statement(GLSLParserFull.Selection_statementContext context)
    {
        var condition = (ExpressionNode)Visit(context.expression());
        var rest = (SelectionRestStatementNode)Visit(context.selection_rest_statement());
        
        return new SelectionStatementNode
        {
            Condition = condition,
            ThenStatement = rest.ThenStatement,
            ElseStatement = rest.ElseStatement
        };
    }

    public override AstNode VisitSelection_rest_statement(GLSLParserFull.Selection_rest_statementContext context)
    {
        var thenStmt = (StatementNode)Visit(context.statement(0));
        StatementNode? elseStmt = null;
        
        if (context.statement().Length > 1)
        {
            elseStmt = (StatementNode)Visit(context.statement(1));
        }
        
        return new SelectionRestStatementNode(thenStmt, elseStmt);
    }

    public override AstNode VisitSwitch_statement(GLSLParserFull.Switch_statementContext context)
    {
        var result = new SwitchStatementNode
        {
            Expression = (ExpressionNode)Visit(context.expression())
        };
        
        if (context.statement_list() != null)
        {
            var statementList = (StatementListNode)Visit(context.statement_list());
            result.Statements = statementList.Statements;
        }
        
        return result;
    }

    public override AstNode VisitCase_label(GLSLParserFull.Case_labelContext context)
    {
        var result = new CaseLabelNode();
        
        if (context.CASE() != null)
        {
            result.CaseExpression = (ExpressionNode)Visit(context.expression());
            result.IsDefault = false;
        }
        else if (context.DEFAULT() != null)
        {
            result.IsDefault = true;
        }
        
        return result;
    }

    public override AstNode VisitIteration_statement(GLSLParserFull.Iteration_statementContext context)
    {
        var result = new IterationStatementNode();
        
        if (context.WHILE() != null)
        {
            result.Type = IterationType.While;
            result.WhileCondition = (ConditionNode)Visit(context.condition());
            result.WhileBody = (StatementNoNewScopeNode)Visit(context.statement_no_new_scope());
        }
        else if (context.DO() != null)
        {
            result.Type = IterationType.DoWhile;
            result.DoBody = (StatementNode)Visit(context.statement());
            result.DoWhileExpression = (ExpressionNode)Visit(context.expression());
        }
        else if (context.FOR() != null)
        {
            result.Type = IterationType.For;
            result.ForInit = (ForInitStatementNode)Visit(context.for_init_statement());
            result.ForRest = (ForRestStatementNode)Visit(context.for_rest_statement());
            result.ForBody = (StatementNoNewScopeNode)Visit(context.statement_no_new_scope());
        }
        
        return result;
    }

    public override AstNode VisitFor_init_statement(GLSLParserFull.For_init_statementContext context)
    {
        var result = new ForInitStatementNode();
        
        if (context.expression_statement() != null)
        {
            result.ExpressionStatement = (ExpressionStatementNode)Visit(context.expression_statement());
        }
        else if (context.declaration_statement() != null)
        {
            result.DeclarationStatement = (DeclarationStatementNode)Visit(context.declaration_statement());
        }
        
        return result;
    }

    public override AstNode VisitFor_rest_statement(GLSLParserFull.For_rest_statementContext context)
    {
        var result = new ForRestStatementNode();
        
        if (context.condition() != null)
        {
            result.Condition = (ConditionNode)Visit(context.condition());
        }
        
        if (context.expression() != null)
        {
            result.Expression = (ExpressionNode)Visit(context.expression());
        }
        
        return result;
    }

    public override AstNode VisitCondition(GLSLParserFull.ConditionContext context)
    {
        var result = new ConditionNode();
        
        if (context.expression() != null)
        {
            result.Expression = (ExpressionNode)Visit(context.expression());
        }
        else if (context.fully_specified_type() != null)
        {
            result.FullySpecifiedType = (FullySpecifiedTypeNode)Visit(context.fully_specified_type());
            result.Identifier = new IdentifierNode(context.IDENTIFIER().GetText());
            result.Initializer = (InitializerNode)Visit(context.initializer());
        }
        
        return result;
    }

    public override AstNode VisitJump_statement(GLSLParserFull.Jump_statementContext context)
    {
        var result = new JumpStatementNode();
        
        if (context.CONTINUE() != null)
            result.Type = JumpType.Continue;
        else if (context.BREAK() != null)
            result.Type = JumpType.Break;
        else if (context.RETURN() != null)
        {
            result.Type = JumpType.Return;
            if (context.expression() != null)
            {
                result.ReturnExpression = (ExpressionNode)Visit(context.expression());
            }
        }
        else if (context.DISCARD() != null)
            result.Type = JumpType.Discard;
        
        return result;
    }

    public override AstNode VisitStatement_no_new_scope(GLSLParserFull.Statement_no_new_scopeContext context)
    {
        if (context.compound_statement_no_new_scope() != null)
            return Visit(context.compound_statement_no_new_scope());
        return Visit(context.simple_statement());
    }

    // ==================== EXPRESSIONS ====================
    public override AstNode VisitPrimary_expression(GLSLParserFull.Primary_expressionContext context)
    {
        var result = new PrimaryExpressionNode();
        
        if (context.variable_identifier() != null)
        {
            result.Identifier = new IdentifierNode(context.variable_identifier().GetText());
        }
        else if (context.TRUE() != null)
        {
            result.BooleanValue = true;
        }
        else if (context.FALSE() != null)
        {
            result.BooleanValue = false;
        }
        else if (context.INTCONSTANT() != null)
        {
            result.IntConstant = context.INTCONSTANT().GetText();
        }
        else if (context.UINTCONSTANT() != null)
        {
            result.UintConstant = context.UINTCONSTANT().GetText();
        }
        else if (context.FLOATCONSTANT() != null)
        {
            result.FloatConstant = context.FLOATCONSTANT().GetText();
        }
        else if (context.DOUBLECONSTANT() != null)
        {
            result.DoubleConstant = context.DOUBLECONSTANT().GetText();
        }
        else if (context.expression() != null)
        {
            result.ParenthesizedExpression = (ExpressionNode)Visit(context.expression());
        }
        
        return result;
    }

    public override AstNode VisitPostfix_expression(GLSLParserFull.Postfix_expressionContext context)
    {
        // Для простоты возвращаем PrimaryExpression или строим более сложную структуру
        if (context.primary_expression() != null)
        {
            return Visit(context.primary_expression());
        }
        
        var result = new PostfixExpressionNode();
        
        if (context.postfix_expression() != null)
        {
            result.PostfixExpression = (PostfixExpressionNode)Visit(context.postfix_expression());
        }
        
        if (context.type_specifier() != null && context.LEFT_PAREN() != null)
        {
            result.ConstructorType = (TypeSpecifierNode)Visit(context.type_specifier());
            if (context.function_call_parameters() != null)
            {
                result.FunctionCallParameters = (FunctionCallParametersNode)Visit(context.function_call_parameters());
            }
        }
        else if (context.function_call_parameters() != null)
        {
            result.FunctionCallParameters = (FunctionCallParametersNode)Visit(context.function_call_parameters());
        }
        
        if (context.LEFT_BRACKET() != null)
        {
            result.ArrayIndexExpression = context.integer_expression().GetText();
        }
        
        if (context.field_selection() != null)
        {
            result.FieldSelection = (FieldSelectionNode)Visit(context.field_selection());
        }
        
        result.HasIncOp = context.INC_OP() != null;
        result.HasDecOp = context.DEC_OP() != null;
        
        return result;
    }

    public override AstNode VisitField_selection(GLSLParserFull.Field_selectionContext context)
    {
        var result = new FieldSelectionNode();
        
        if (context.variable_identifier() != null)
        {
            result.Identifier = new IdentifierNode(context.variable_identifier().GetText());
        }
        else if (context.function_call() != null)
        {
            result.FunctionCall = (FunctionCallNode)Visit(context.function_call());
        }
        
        return result;
    }

    public override AstNode VisitFunction_call(GLSLParserFull.Function_callContext context)
    {
        var result = new FunctionCallNode
        {
            FunctionIdentifier = (FunctionIdentifierNode)Visit(context.function_identifier())
        };
        
        if (context.function_call_parameters() != null)
        {
            result.Parameters = (FunctionCallParametersNode)Visit(context.function_call_parameters());
        }
        
        return result;
    }

    public override AstNode VisitFunction_identifier(GLSLParserFull.Function_identifierContext context)
    {
        var result = new FunctionIdentifierNode();
        
        if (context.type_specifier() != null)
        {
            result.TypeSpecifier = (TypeSpecifierNode)Visit(context.type_specifier());
        }
        else if (context.postfix_expression() != null)
        {
            result.PostfixExpression = (PostfixExpressionNode)Visit(context.postfix_expression());
        }
        
        return result;
    }

    public override AstNode VisitFunction_call_parameters(GLSLParserFull.Function_call_parametersContext context)
    {
        var result = new FunctionCallParametersNode();
        
        if (context.VOID() != null)
        {
            result.IsVoid = true;
        }
        else
        {
            foreach (var expr in context.assignment_expression())
            {
                result.AssignmentExpressions.Add((AssignmentExpressionNode)Visit(expr));
            }
        }
        
        return result;
    }

    public override AstNode VisitUnary_expression(GLSLParserFull.Unary_expressionContext context)
    {
        var result = new UnaryExpressionNode();
    
        if (context.postfix_expression() != null)
        {
            // Исправление: получаем узел и проверяем его тип
            var node = Visit(context.postfix_expression());
        
            if (node is PostfixExpressionNode postfixNode)
                result.PostfixExpression = postfixNode;
            else if (node is PrimaryExpressionNode primaryNode)
            {
                // Оборачиваем PrimaryExpression в PostfixExpression
                result.PostfixExpression = new PostfixExpressionNode
                {
                    PrimaryExpression = primaryNode
                };
            }
        }
    
        result.HasIncOp = context.INC_OP() != null;
        result.HasDecOp = context.DEC_OP() != null;
    
        if (context.unary_operator() != null)
        {
            result.UnaryOperator = (UnaryOperatorNode)Visit(context.unary_operator());
        }
    
        if (context.unary_expression() != null && (context.INC_OP() != null || context.DEC_OP() != null || context.unary_operator() != null))
        {
            result.Operand = (UnaryExpressionNode)Visit(context.unary_expression());
        }
    
        return result;
    }

    public override AstNode VisitUnary_operator(GLSLParserFull.Unary_operatorContext context)
    {
        var result = new UnaryOperatorNode();
        
        if (context.PLUS() != null) result.Operator = "+";
        else if (context.DASH() != null) result.Operator = "-";
        else if (context.BANG() != null) result.Operator = "!";
        else if (context.TILDE() != null) result.Operator = "~";
        
        return result;
    }

    public override AstNode VisitAssignment_expression(GLSLParserFull.Assignment_expressionContext context)
    {
        var result = new AssignmentExpressionNode();
        
        if (context.constant_expression() != null)
        {
            result.ConstantExpression = (ConstantExpressionNode)Visit(context.constant_expression());
        }
        else if (context.unary_expression() != null && context.assignment_operator() != null)
        {
            result.LeftUnary = (UnaryExpressionNode)Visit(context.unary_expression());
            result.Operator = (AssignmentOperatorNode)Visit(context.assignment_operator());
            result.RightAssignment = (AssignmentExpressionNode)Visit(context.assignment_expression());
        }
        
        return result;
    }

    public override AstNode VisitAssignment_operator(GLSLParserFull.Assignment_operatorContext context)
    {
        var result = new AssignmentOperatorNode();
        
        if (context.EQUAL() != null) result.Operator = "=";
        else if (context.MUL_ASSIGN() != null) result.Operator = "*=";
        else if (context.DIV_ASSIGN() != null) result.Operator = "/=";
        else if (context.MOD_ASSIGN() != null) result.Operator = "%=";
        else if (context.ADD_ASSIGN() != null) result.Operator = "+=";
        else if (context.SUB_ASSIGN() != null) result.Operator = "-=";
        else if (context.LEFT_ASSIGN() != null) result.Operator = "<<=";
        else if (context.RIGHT_ASSIGN() != null) result.Operator = ">>=";
        else if (context.AND_ASSIGN() != null) result.Operator = "&=";
        else if (context.XOR_ASSIGN() != null) result.Operator = "^=";
        else if (context.OR_ASSIGN() != null) result.Operator = "|=";
        
        return result;
    }

    public override AstNode VisitBinary_expression(GLSLParserFull.Binary_expressionContext context)
    {
        // Если это просто unary_expression
        if (context.unary_expression() != null && context.binary_expression().Length == 0)
        {
            return Visit(context.unary_expression());
        }
        
        // Бинарная операция
        if (context.binary_expression().Length >= 2)
        {
            var left = Visit(context.binary_expression(0));
            var right = Visit(context.binary_expression(1));
            
            // Приводим к ExpressionNode (если нужно, оборачиваем)
            ExpressionNode leftExpr = left as ExpressionNode ?? WrapToExpression(left);
            ExpressionNode rightExpr = right as ExpressionNode ?? WrapToExpression(right);
            
            string op = GetBinaryOperator(context);
            
            return new BinaryExpressionNode(leftExpr, op, rightExpr);
        }
        
        return Visit(context.unary_expression());
    }

    private ExpressionNode WrapToExpression(AstNode node)
    {
        if (node is ExpressionNode expr)
            return expr;
        
        // Если узел не ExpressionNode, оборачиваем его
        return new PrimaryExpressionNode { Identifier = node as IdentifierNode };
    }

    private string GetBinaryOperator(GLSLParserFull.Binary_expressionContext context)
    {
        if (context.STAR() != null) return "*";
        if (context.SLASH() != null) return "/";
        if (context.PERCENT() != null) return "%";
        if (context.PLUS() != null) return "+";
        if (context.DASH() != null) return "-";
        if (context.LEFT_OP() != null) return "<<";
        if (context.RIGHT_OP() != null) return ">>";
        if (context.LEFT_ANGLE() != null) return "<";
        if (context.RIGHT_ANGLE() != null) return ">";
        if (context.LE_OP() != null) return "<=";
        if (context.GE_OP() != null) return ">=";
        if (context.EQ_OP() != null) return "==";
        if (context.NE_OP() != null) return "!=";
        if (context.AMPERSAND() != null) return "&";
        if (context.CARET() != null) return "^";
        if (context.VERTICAL_BAR() != null) return "|";
        if (context.AND_OP() != null) return "&&";
        if (context.XOR_OP() != null) return "^^";
        if (context.OR_OP() != null) return "||";
        return "?";
    }

    public override AstNode VisitConstant_expression(GLSLParserFull.Constant_expressionContext context)
    {
        var result = new ConstantExpressionNode();
    
        if (context.binary_expression() != null)
        {
            var node = Visit(context.binary_expression());
            if (node is BinaryExpressionNode binaryNode)
                result.BinaryExpression = binaryNode;
            else if (node is ExpressionNode exprNode)
            {
                // Оборачиваем в бинарное выражение (как одиночный операнд)
                // Или просто сохраняем как есть, но нужно расширить класс
                result.BinaryExpression = new BinaryExpressionNode(exprNode, "", new PrimaryExpressionNode());
            }
        }
    
        if (context.QUESTION() != null && context.binary_expression() != null)
        {
            var conditionNode = Visit(context.binary_expression());
            result.Condition = conditionNode as BinaryExpressionNode;
            result.TrueExpression = Visit(context.expression()) as ExpressionNode;
            result.FalseExpression = Visit(context.assignment_expression()) as AssignmentExpressionNode;
        }
    
        return result;
    }

    public override AstNode VisitExpression(GLSLParserFull.ExpressionContext context)
    {
        // Упрощённо: возвращаем первое assignment_expression
        return Visit(context.assignment_expression());
    }

    // ==================== HELPER NODES ====================
    public override AstNode VisitVariable_identifier(GLSLParserFull.Variable_identifierContext context)
    {
        return new IdentifierNode(context.IDENTIFIER().GetText());
    }

    public override AstNode VisitInteger_expression(GLSLParserFull.Integer_expressionContext context)
    {
        return new IntegerExpressionNode((ExpressionNode)Visit(context.expression()));
    }

    public override AstNode VisitType_name_list(GLSLParserFull.Type_name_listContext context)
    {
        var result = new TypeNameListNode();
        foreach (var name in context.type_name())
        {
            result.TypeNames.Add((IdentifierNode)Visit(name));
        }
        return result;
    }

    public override AstNode VisitInvariant_qualifier(GLSLParserFull.Invariant_qualifierContext context)
    {
        return new InvariantQualifierNode();
    }

    public override AstNode VisitPrecise_qualifier(GLSLParserFull.Precise_qualifierContext context)
    {
        return new PreciseQualifierNode();
    }
}