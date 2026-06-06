using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._2._AstToTables;

public static class FirstPassVisitor
{
    // ----------------------- Visit Translation Unit Node
    public static void Visit(TranslationUnitNode node, FirstPassContext context)
    {
        foreach (var decl in node.Declarations)
        {
            Visit(decl, context);
        }
    }

    // ----------------------- External Declaration Node
    private static void Visit(ExternalDeclarationNode node, FirstPassContext context)
    {
        switch (node)
        {
            case FunctionDefinitionNode funcDef:
                Visit(funcDef, context);
                break;
            case DeclarationNode decl:
                Visit(decl, context);
                break;
        }
    }

    // ----------------------- Function Definition Node
    private static void Visit(FunctionDefinitionNode node, FirstPassContext context)
    {
        context.Symbols.EnterScope();
        
        var proto = node.Prototype;
        var returnType = GetTypeFromTypeNode(proto.Type, context);
        var paramTypes = new List<SpirvType>();
        if (proto.Parameters != null)
        {
            foreach (var param in proto.Parameters.Parameters)
            {
                var paramType = GetTypeFromParameterDeclaration(param, context);
                paramTypes.Add(paramType);
                var paramPtrType = new PointerType(StorageClass.Function, paramType);
                context.Types.AddType(paramPtrType);
            }
        }
        
        var funcName = proto.Name.Name;
        var funcInfo = new SymbolInfo(SymbolKind.Function, returnType, paramTypes: paramTypes);
        context.Symbols.AddSymbol(funcName, funcInfo, local: false);
        
        Visit(node.Body, context);
        
        context.Symbols.ExitScope();
    }
    
    // ----------------------- Declaration Node
    private static void Visit(DeclarationNode node, FirstPassContext context)
    {
        switch (node.DeclType)
        {
            case DeclarationType.InitDeclaratorList:
                if (node.InitDeclaratorList != null)
                {
                    foreach (var singleDecl in node.InitDeclaratorList.SingleDeclarations)
                    {
                        ProcessSingleDeclaration(singleDecl, context, isLocal: false);
                    }
                }
                break;
            case DeclarationType.StructBlock:
                ProcessStructBlock(node, context);
                break;
            case DeclarationType.TypeQualifierOnly:
                if (node.StandaloneTypeQualifier != null)
                {
                    context.CurrentTypeQualifiers = node.StandaloneTypeQualifier;
                    foreach (var qual in node.StandaloneTypeQualifier.Qualifiers)
                    {
                        if (qual is LayoutQualifierNode layout)
                        {
                            foreach (var id in layout.Ids)
                            {
                                if (id.Identifier?.Name == "local_size_x" && id.ConstantExpression != null)
                                {
                                    var val = EvaluateConstantExpression(id.ConstantExpression);
                                    if (val.HasValue) context.LocalSizeX = val.Value;
                                }
                                if (id.Identifier?.Name == "local_size_y" && id.ConstantExpression != null)
                                {
                                    var val = EvaluateConstantExpression(id.ConstantExpression);
                                    if (val.HasValue) context.LocalSizeY = val.Value;
                                }
                                if (id.Identifier?.Name == "local_size_z" && id.ConstantExpression != null)
                                {
                                    var val = EvaluateConstantExpression(id.ConstantExpression);
                                    if (val.HasValue) context.LocalSizeZ = val.Value;
                                }
                            }
                        }
                    }
                }
                break;

            case DeclarationType.Precision:
                if (node.PrecisionQualifier != null && node.PrecisionType != null)
                {
                    var precisionType = GetTypeFromTypeSpecifier(node.PrecisionType, context);
                    context.PrecisionInfo[precisionType] = node.PrecisionQualifier.Precision;
                }
                break;
            case DeclarationType.FunctionPrototype:
                if (node.FunctionPrototype != null)
                {
                    var returnType = GetTypeFromTypeNode(node.FunctionPrototype.Type, context);
                    var paramTypes = new List<SpirvType>();
                    if (node.FunctionPrototype.Parameters != null)
                    {
                        foreach (var param in node.FunctionPrototype.Parameters.Parameters)
                        {
                            paramTypes.Add(GetTypeFromParameterDeclaration(param, context));
                        }
                    }
                    var funcName = node.FunctionPrototype.Name.Name;
                    var funcInfo = new SymbolInfo(SymbolKind.Function, returnType, paramTypes: paramTypes);
                    context.Symbols.AddSymbol(funcName, funcInfo, local: false);
                }
                break;
        }
    }

    // --------------------------------------------------- Statement block
    // ----------------------- Statement Node
    private static void Visit(StatementNode node, FirstPassContext context)
    {
        switch (node)
        {
            case CompoundStatementNoNewScopeNode decl:
                Visit(decl, context);
                break;
            case CompoundStatementNode decl:
                Visit(decl, context);
                break;
            case DeclarationStatementNode decl:
                Visit(decl, context);
                break;
            case ExpressionStatementNode decl:
                Visit(decl, context);
                break;
            case IterationStatementNode decl:
                Visit(decl, context);
                break;
            case JumpStatementNode decl:
                Visit(decl, context);
                break;
            case SelectionStatementNode decl:
                Visit(decl, context);
                break;
            case StatementNoNewScopeNode decl:
                Visit(decl, context);
                break;
        }
    }

    // ----------------------- Compound Statement No New Scope Node
    private static void Visit(CompoundStatementNoNewScopeNode node, FirstPassContext context)
    {
        foreach (var statement in node.Statements)
        {
            Visit(statement, context);
        }
    }
    
    // ----------------------- Compound Statement Node
    private static void Visit(CompoundStatementNode node, FirstPassContext context)
    {
        context.Symbols.EnterScope();
        
        foreach (var statement in node.Statements)
        {
            Visit(statement, context);
        }
        
        context.Symbols.ExitScope();
    }
    
    // ----------------------- Declaration Statement Node
    private static void Visit(DeclarationStatementNode node, FirstPassContext context)
    {
        var declaratorList = node.Declaration.InitDeclaratorList;
        if (declaratorList == null) return;

        foreach (var single in declaratorList.SingleDeclarations)
        {
            var varType = TypeResolver.GetTypeFromFullySpecifiedType(single.FullySpecifiedType, context);
            var ptrType = new PointerType(StorageClass.Function, varType);
        
            context.Types.AddType(varType);
            context.Types.AddType(ptrType);
        
            if (single.TypelessDeclaration?.Initializer != null)
                VisitInitializer(single.TypelessDeclaration.Initializer, context);
        }
    
        foreach (var typeless in declaratorList.TypelessDeclarations)
        {
            if (typeless.Initializer != null)
                VisitInitializer(typeless.Initializer, context);
        }
    }
    
    // ----------------------- Expression Statement Node
    private static void Visit(ExpressionStatementNode node, FirstPassContext context)
    {
        if (node.Expression != null)
            Visit(node.Expression, context);
    }
    
    // ----------------------- Iteration Statement Node
    private static void Visit(IterationStatementNode node, FirstPassContext context)
    {
        if (node.ForInit?.DeclarationStatement != null)
            Visit(node.ForInit.DeclarationStatement, context);
        else if (node.ForInit?.ExpressionStatement != null)
            Visit(node.ForInit.ExpressionStatement, context);
    
        if (node.ForRest?.Condition?.FullySpecifiedType != null && node.ForRest.Condition.Identifier != null && node.ForRest.Condition.Initializer != null)
        {
            VisitInitializer(node.ForRest.Condition.Initializer, context);
        }
        else if (node.ForRest?.Condition?.Expression != null)
        {
            Visit(node.ForRest.Condition.Expression, context);
        }
    
        if (node.WhileCondition?.FullySpecifiedType != null && node.WhileCondition.Identifier != null && node.WhileCondition.Initializer != null)
        {
            VisitInitializer(node.WhileCondition.Initializer, context);
        }
        else if (node.WhileCondition?.Expression != null)
        {
            Visit(node.WhileCondition.Expression, context);
        }
    
        if (node.DoBody != null)
            Visit(node.DoBody, context);
        else if (node.ForBody != null)
            Visit(node.ForBody, context);
        else if (node.WhileBody != null)
            Visit(node.WhileBody, context);
    }
    
    // ----------------------- Jump Statement Node
    private static void Visit(JumpStatementNode node, FirstPassContext context)
    {
        // нет важных данных для таблиц
    }
    
    // ----------------------- Selection Statement Node
    private static void Visit(SelectionStatementNode node, FirstPassContext context)
    {
        Visit(node.Condition, context);
        Visit(node.ThenStatement, context);
        
        if (node.ElseStatement != null)
            Visit(node.ElseStatement, context);
    }
    
    // ----------------------- Statement No New Scope Node
    private static void Visit(StatementNoNewScopeNode node, FirstPassContext context)
    {
        if (node.CompoundStatement != null)
            Visit(node.CompoundStatement, context);
        else if (node.SimpleStatement != null)
            Visit(node.SimpleStatement, context);
    }
    
    // --------------------------------------------------------------------- Expression Node
    // ----------------------- Expression Node
    private static void Visit(ExpressionNode node, FirstPassContext context)
    {
        switch (node)
        {
            case AssignmentExpressionNode decl:
                Visit(decl, context);
                break;
            case BinaryExpressionNode decl:
                Visit(decl, context);
                break;
            case ConstantExpressionNode decl:
                Visit(decl, context);
                break;
            case ConstructorExpressionNode decl:
                Visit(decl, context);
                break;
            case FieldAccessExpressionNode decl:
                Visit(decl, context);
                break;
            case IdentifierExpressionNode decl:
                Visit(decl, context);
                break;
            case PostfixExpressionNode decl:
                Visit(decl, context);
                break;
            case PrimaryExpressionNode decl:
                Visit(decl, context);
                break;
            case UnaryExpressionNode decl:
                Visit(decl, context);
                break;
        }
    }

    // ----------------------- Assignment Expression Node
    private static void Visit(AssignmentExpressionNode node, FirstPassContext context)
    {
        if (node.ConstantExpression != null)
        {
            Visit(node.ConstantExpression, context);
        }
        else
        {
            Visit(node.LeftUnary, context);
            Visit(node.Operator, context);
            if (node.RightAssignment != null)
                Visit(node.RightAssignment, context);
        }
    }
    
    // ----------------------- Assignment Operator Node
    private static void Visit(AssignmentOperatorNode node, FirstPassContext context)
    {
        // игнорируем опреатор присваивания
    }

    // ----------------------- Binary Expression Node
    private static void Visit(BinaryExpressionNode node, FirstPassContext context)
    {
        Visit(node.Left, context);
        Visit(node.Right, context);
    }

    // ----------------------- Constant Expression Node
    private static void Visit(ConstantExpressionNode node, FirstPassContext context)
    {
        if (node.BinaryExpression != null)
        {
            Visit(node.BinaryExpression, context);
        }
        else
        {
            Visit(node.Condition, context);
            Visit(node.TrueExpression, context);
            Visit(node.FalseExpression, context);
        }
    }
    
    // ----------------------- Constructor Expression Node
    private static void Visit(ConstructorExpressionNode node, FirstPassContext context)
    {
        var type = GetTypeFromTypeSpecifier(node.TypeSpecifier, context);
        context.Types.AddType(type);

        foreach (var assignment in node.Parameters.AssignmentExpressions)
        {
            Visit(assignment, context);
        }
    }

    // ----------------------- Field Access Expression Node
    private static void Visit(FieldAccessExpressionNode node, FirstPassContext context)
    {
        Visit(node.Base, context);
        
        var baseType = context.GetLastExpressionType();
        if (baseType != null)
        {
            if (baseType is StructType structType)
            {
                var fieldType = structType.MemberTypes.FirstOrDefault();
                context.SetLastExpressionType(fieldType);
            }
        }
    }

    // ----------------------- Identifier Expression Node
    private static void Visit(IdentifierExpressionNode node, FirstPassContext context)
    {
        var sym = context.Symbols.Lookup(node.Name);
        if (sym != null && sym.Kind == SymbolKind.Variable)
        {
            context.SetLastExpressionType(sym.Type);
        }
    }

    // ----------------------- Postfix Expression Node
    private static void Visit(PostfixExpressionNode node, FirstPassContext context)
    {
        if (node.PrimaryExpression != null)
        {
            Visit(node.PrimaryExpression, context);
        }
        
        if (node.PostfixExpression != null)
        {
            Visit(node.PostfixExpression, context);
        }
        
        if (node.FieldSelection?.Identifier != null)
        {
            var baseType = context.GetLastExpressionType();
            if (baseType == null) return;

            StorageClass? storageClass = null;
            SpirvType actualType = baseType;
            if (baseType is PointerType ptrType)
            {
                storageClass = ptrType.StorageClass;
                actualType = ptrType.PointeeType;
            }

            string fieldName = node.FieldSelection.Identifier.Name;
            SpirvType fieldType = null;
            int fieldIndex = -1;

            if (actualType is StructType structType)
            {
                for (int i = 0; i < structType.MemberNames.Count; i++)
                {
                    if (structType.MemberNames[i] == fieldName)
                    {
                        fieldIndex = i;
                        fieldType = structType.MemberTypes[i];
                        break;
                    }
                }
                if (fieldIndex == -1)
                    throw new InvalidOperationException($"Field '{fieldName}' not found in struct");
            }
            else if (actualType is VectorType vecType && fieldName.Length == 1)
            {
                fieldIndex = fieldName[0] switch { 'x' => 0, 'y' => 1, 'z' => 2, 'w' => 3, _ => -1 };
                if (fieldIndex >= 0 && fieldIndex < vecType.ComponentCount)
                    fieldType = vecType.ComponentType;
                else
                    throw new InvalidOperationException($"Invalid swizzle '{fieldName}' for vector");
            }
            else if (actualType is MatrixType matType && fieldName.Length == 1)
            {
                fieldIndex = fieldName[0] switch { 'x' => 0, 'y' => 1, 'z' => 2, 'w' => 3, _ => -1 };
                if (fieldIndex >= 0 && fieldIndex < matType.ColumnCount)
                    fieldType = matType.ColumnType;
                else
                    throw new InvalidOperationException($"Invalid column index '{fieldName}' for matrix");
            }
            else
            {
                throw new NotSupportedException($"Field access on type {actualType.GetType().Name}");
            }

            var indexConstType = new IntType(32, true);
            context.AddRequiredConstant(indexConstType, fieldIndex);

            if (storageClass.HasValue)
            {
                var fieldPtrType = new PointerType(storageClass.Value, fieldType);
                context.Types.AddType(fieldType);
                context.Types.AddType(fieldPtrType);
            }

            context.SetLastExpressionType(fieldType);
        }
        
        if (node.ArrayIndexExpression != null)
        {
            var baseType = context.GetLastExpressionType();
            if (baseType == null) return;

            StorageClass? storageClass = null;
            SpirvType actualType = baseType;
            if (baseType is PointerType ptrType)
            {
                storageClass = ptrType.StorageClass;
                actualType = ptrType.PointeeType;
            }

            SpirvType elemType = null;
            if (actualType is ArrayType arrType)
                elemType = arrType.ElementType;
            else if (actualType is PointerType ptrArrType && ptrArrType.PointeeType is ArrayType)
                elemType = ((ArrayType)ptrArrType.PointeeType).ElementType;
            else
                throw new InvalidOperationException("Array index on non-array type");

            if (uint.TryParse(node.ArrayIndexExpression, out uint constIndex))
            {
                var indexConstType = new IntType(32, true);
                context.AddRequiredConstant(indexConstType, (int)constIndex);
            }

            if (storageClass.HasValue)
            {
                var elemPtrType = new PointerType(storageClass.Value, elemType);
                context.Types.AddType(elemType);
                context.Types.AddType(elemPtrType);
            }

            context.SetLastExpressionType(elemType);
        }
        
        // Обработка вызова функции или конструктора
        if (node.FunctionCallParameters != null)
        {
            if (node.PrimaryExpression is PrimaryExpressionNode primary && TryGetIdentifier(primary, out var funcName))
            {
                var funcSymbol = context.Symbols.Lookup(funcName);
                if (funcSymbol != null && funcSymbol.Kind == SymbolKind.Function)
                {
                    context.SetLastExpressionType(funcSymbol.Type);
                }
            }
            else if (node.ConstructorType != null)
            {
                var constructorType = GetTypeFromTypeSpecifier(node.ConstructorType, context);
                context.SetLastExpressionType(constructorType);
            }
        }
        
        if (node.HasIncOp || node.HasDecOp)
        {
            var baseType = context.GetLastExpressionType();
            context.SetLastExpressionType(baseType);
        }
    }
    
    // ----------------------- Primary Expression Node
    private static void Visit(PrimaryExpressionNode node, FirstPassContext context)
    {
        if (node.Identifier != null)
        {
            var symbol = context.Symbols.Lookup(node.Identifier.Name);
            if (symbol != null)
            {
                if (symbol.Kind == SymbolKind.Variable || symbol.Kind == SymbolKind.Function)
                {
                    context.SetLastExpressionType(symbol.Type);
                }
            }
        }
        else if (node.BooleanValue.HasValue)
        {
            var boolType = new BoolType();
            context.AddRequiredConstant(boolType, node.BooleanValue.Value);
        }
        else if (node.IntConstant != null)
        {
            string clean = node.IntConstant.TrimEnd('u', 'U', 'l', 'L');
            if (int.TryParse(clean, out int ival))
            {
                var intType = new IntType(32, true);
                context.AddRequiredConstant(intType, ival);
            }
            else if (uint.TryParse(clean, out uint uval))
            {
                var uintType = new IntType(32, false);
                context.AddRequiredConstant(uintType, uval);
            }
        }
        else if (node.UintConstant != null)
        {
            string clean = node.UintConstant.TrimEnd('u', 'U');
            if (uint.TryParse(clean, out uint uval))
            {
                var uintType = new IntType(32, false);
                context.AddRequiredConstant(uintType, uval);
            }
            else if (int.TryParse(clean, out int ival))
            {
                var intType = new IntType(32, true);
                context.AddRequiredConstant(intType, ival);
            }
        }
        else if (node.FloatConstant != null)
        {
            string clean = node.FloatConstant.TrimEnd('f', 'F');
            if (float.TryParse(clean, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float fval))
            {
                var floatType = new FloatType(32);
                context.AddRequiredConstant(floatType, fval);
            }
        }
        else if (node.DoubleConstant != null)
        {
            string clean = node.DoubleConstant.TrimEnd('f', 'F', 'd', 'D');
            if (double.TryParse(clean, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double dval))
            {
                var doubleType = new FloatType(64);
                context.AddRequiredConstant(doubleType, dval);
            }
        }
        else if (node.ParenthesizedExpression != null)
        {
            Visit(node.ParenthesizedExpression, context);
        }
    }

    // ----------------------- Unary Expression Node
    private static void Visit(UnaryExpressionNode node, FirstPassContext context)
    {
        if (node.Operand != null)
        {
            Visit(node.Operand, context);
            
            var operandType = context.GetLastExpressionType();
            if (operandType == null) return;
            
            if (node.UnaryOperator != null)
            {
                switch (node.UnaryOperator.Operator)
                {
                    case "+":
                    case "-":
                    case "~":
                        context.SetLastExpressionType(operandType);
                        break;
                        
                    case "!":
                        var boolType = new BoolType();
                        context.Types.AddType(boolType);
                        context.SetLastExpressionType(boolType);
                        break;
                        
                    case "*":
                        if (operandType is PointerType ptrType)
                        {
                            context.SetLastExpressionType(ptrType.PointeeType);
                        }
                        break;
                        
                    case "&":
                        var ptrType2 = new PointerType(StorageClass.Function, operandType);
                        context.Types.AddType(ptrType2);
                        context.SetLastExpressionType(ptrType2);
                        break;
                }
            }
            else if (node.HasIncOp || node.HasDecOp)
            {
                context.SetLastExpressionType(operandType);
            }
        }
        else if (node.PostfixExpression != null)
        {
            Visit(node.PostfixExpression, context);
        }
    }
    
    private static void VisitInitializer(InitializerNode init, FirstPassContext context)
    {
        if (init.AssignmentExpression != null)
        {
            Visit(init.AssignmentExpression, context);
        }
        else if (init.InitializerList != null)
        {
            foreach (var subInit in init.InitializerList.Initializers)
                VisitInitializer(subInit, context);
        }
    }
    
    private static void ProcessTypelessDeclaration(TypelessDeclarationNode node, FirstPassContext context, SpirvType forcedType, StorageClass storageClass)
    {
        var varName = node.Identifier.Name;
        var varInfo = new SymbolInfo(SymbolKind.Variable, forcedType, storageClass);
        context.Symbols.AddSymbol(varName, varInfo, local: storageClass == StorageClass.Function);
        context.Types.AddType(forcedType);
    
        if (node.Initializer != null)
            VisitInitializer(node.Initializer, context);
    }
    
    private static void ProcessSingleDeclaration(SingleDeclarationNode node, FirstPassContext context, bool isLocal = false)
    {
        var fullType = GetTypeFromFullySpecifiedType(node.FullySpecifiedType, context);
        var storageClass = GetStorageClassFromFullySpecifiedType(node.FullySpecifiedType, isLocal);
        if (node.TypelessDeclaration != null)
        {
            ProcessTypelessDeclaration(node.TypelessDeclaration, context, fullType, storageClass);
            if (node.TypelessDeclaration.Initializer != null)
                VisitInitializer(node.TypelessDeclaration.Initializer, context);
        }
    }

    private static void ProcessStructBlock(DeclarationNode node, FirstPassContext context)
    {
        if (node.StructDeclarationList == null) return;

        var memberTypes = new List<SpirvType>();
        var memberNames = new List<string>();

        foreach (var structDecl in node.StructDeclarationList.Declarations)
        {
            if (structDecl.TypeSpecifier != null && structDecl.DeclaratorList != null)
            {
                var baseType = GetTypeFromTypeSpecifier(structDecl.TypeSpecifier, context);
                foreach (var decl in structDecl.DeclaratorList.Declarators)
                {
                    var memberType = ApplyArraySpecifier(baseType, decl.ArraySpecifier, context);
                    memberTypes.Add(memberType);
                    memberNames.Add(decl.Identifier.Name);
                }
            }
        }

        var structType = new StructType(memberTypes, memberNames);
        context.Types.AddType(structType);

        // Если у блока есть имя, регистрируем его как тип
        if (node.BlockName != null)
        {
            var typeInfo = new SymbolInfo(SymbolKind.Type, structType);
            context.Symbols.AddSymbol(node.BlockName.Name, typeInfo, false);
        }

        var storageClass = GetStorageClassFromTypeQualifier(node.BlockTypeQualifier);

        if (node.BlockInstanceName != null)
        {
            // Именованный экземпляр блока
            var varType = ApplyArraySpecifier(structType, node.BlockInstanceArraySpecifier, context);
            var varInfo = new SymbolInfo(SymbolKind.Variable, varType, storageClass);
            context.Symbols.AddSymbol(node.BlockInstanceName.Name, varInfo, false);
            context.Types.AddType(varType);
        }
        else
        {
            // Анонимный блок: каждое поле становится глобальной переменной
            for (int i = 0; i < memberTypes.Count; i++)
            {
                var fieldType = memberTypes[i];
                var fieldInfo = new SymbolInfo(SymbolKind.Variable, fieldType, storageClass);
                context.Symbols.AddSymbol(memberNames[i], fieldInfo, false);
                context.Types.AddType(fieldType);
            }
        }
    }

    // Вспомогательные методы для получения типа из различных узлов AST

    private static SpirvType GetTypeFromTypeNode(TypeNode node, FirstPassContext context)
    {
        var type = GetTypeFromTypeSpecifier(node.TypeSpecifier, context);
        return type;
    }

    private static SpirvType GetTypeFromFullySpecifiedType(FullySpecifiedTypeNode node, FirstPassContext context)
    {
        var type = GetTypeFromTypeSpecifier(node.TypeSpecifier, context);
        return type;
    }

    private static SpirvType GetTypeFromTypelessDeclaration(TypelessDeclarationNode node, FirstPassContext context)
    {
        throw new NotImplementedException("TypelessDeclaration should have forced type");
    }

    private static SpirvType GetTypeFromParameterDeclaration(ParameterDeclarationNode node, FirstPassContext context)
    {
        if (node.ParameterTypeSpecifier != null)
        {
            var baseType = GetTypeFromTypeSpecifier(node.ParameterTypeSpecifier, context);
            return ApplyArraySpecifier(baseType, node.ArraySpecifier, context);
        }
        throw new InvalidOperationException("Parameter without type specifier");
    }

    private static SpirvType GetTypeFromTypeSpecifier(TypeSpecifierNode node, FirstPassContext context)
    {
        SpirvType baseType;
        if (node.NonArrayType.BasicType != null)
        {
            baseType = node.NonArrayType.BasicType switch
            {
                "void" => new VoidType(),
                "bool" => new BoolType(),
                "int" => new IntType(32, true),
                "uint" => new IntType(32, false),
                "float" => new FloatType(32),
                "double" => new FloatType(64),
                _ => throw new NotSupportedException($"Basic type {node.NonArrayType.BasicType} not supported")
            };
        }
        else if (node.NonArrayType.StructSpecifier != null)
        {
            baseType = GetTypeFromStructSpecifier(node.NonArrayType.StructSpecifier, context);
        }
        else if (node.NonArrayType.TypeName != null)
        {
            var typeName = node.NonArrayType.TypeName.Name;
            var sym = context.Symbols.Lookup(typeName);
            if (sym == null || sym.Kind != SymbolKind.Type)
                throw new InvalidOperationException($"Unknown type name '{typeName}'");
            baseType = sym.Type;
        }
        else
        {
            throw new InvalidOperationException("Unknown type specifier");
        }
        
        context.Types.AddType(baseType);

        return ApplyArraySpecifier(baseType, node.ArraySpecifier, context);
    }

    private static SpirvType GetTypeFromStructSpecifier(StructSpecifierNode node, FirstPassContext context)
    {
        var memberTypes = new List<SpirvType>();
        if (node.Declarations != null)
        {
            foreach (var structDecl in node.Declarations.Declarations)
            {
                if (structDecl.TypeSpecifier != null && structDecl.DeclaratorList != null)
                {
                    var baseType = GetTypeFromTypeSpecifier(structDecl.TypeSpecifier, context);
                    foreach (var decl in structDecl.DeclaratorList.Declarators)
                    {
                        var memberType = ApplyArraySpecifier(baseType, decl.ArraySpecifier, context);
                        memberTypes.Add(memberType);
                    }
                }
            }
        }
        var structType = new StructType(memberTypes);
        context.Types.AddType(structType);

        if (node.Name != null)
        {
            var typeInfo = new SymbolInfo(SymbolKind.Type, structType);
            context.Symbols.AddSymbol(node.Name.Name, typeInfo, local: false);
        }

        return structType;
    }

    private static SpirvType ApplyArraySpecifier(SpirvType baseType, ArraySpecifierNode? arraySpec, FirstPassContext context)
    {
        if (arraySpec == null) return baseType;
        SpirvType current = baseType;
        
        foreach (var dim in arraySpec.Dimensions)
        {
            uint? length = null;
            if (dim.ConstantExpression != null)
            {
                length = EvaluateConstantExpression(dim.ConstantExpression);
            }
            current = new ArrayType(current, length);
            context.Types.AddType(current);
        }
        
        return current;
    }

    private static uint? EvaluateConstantExpression(ConstantExpressionNode expr)
    {
        if (expr.BinaryExpression != null)
        {
            var bin = expr.BinaryExpression;
            
            if (bin.Left is UnaryExpressionNode unary && 
                unary.PostfixExpression?.PrimaryExpression is PrimaryExpressionNode primary)
            {
                if (TryGetConstantValue(primary, out int value))
                    return (uint)value;
            }

            var leftVal = EvaluateConstantExpressionFromNode(bin.Left);
            var rightVal = EvaluateConstantExpressionFromNode(bin.Right);
            
            if (leftVal.HasValue && rightVal.HasValue)
            {
                return bin.Operator switch
                {
                    "+" => leftVal.Value + rightVal.Value,
                    "-" => leftVal.Value - rightVal.Value,
                    "*" => leftVal.Value * rightVal.Value,
                    "/" => rightVal.Value != 0 ? leftVal.Value / rightVal.Value : null,
                    _ => null
                };
            }
        }
        
        if (expr.Condition != null && expr.TrueExpression != null && expr.FalseExpression != null)
        {
            var condVal = EvaluateConstantExpressionFromNode(expr.Condition);
            if (condVal.HasValue && condVal.Value != 0)
                return EvaluateConstantExpressionFromNode(expr.TrueExpression);
            else if (condVal.HasValue)
                return EvaluateConstantExpressionFromNode(expr.FalseExpression);
        }
        
        return null;
    }

    private static uint? EvaluateConstantExpressionFromNode(ExpressionNode node)
    {
        if (node is ConstantExpressionNode constExpr)
            return EvaluateConstantExpression(constExpr);
            
        if (node is PrimaryExpressionNode primary && TryGetConstantValue(primary, out int value))
            return (uint)value;
            
        if (node is UnaryExpressionNode unary && unary.PostfixExpression?.PrimaryExpression is PrimaryExpressionNode primary2)
            if (TryGetConstantValue(primary2, out int value2))
                return (uint)value2;
        
        if (node is UnaryExpressionNode negUnary && negUnary.UnaryOperator?.Operator == "-")
            if (EvaluateConstantExpressionFromNode(negUnary.Operand) is uint negVal)
                return (uint)(-(int)negVal);
        
        return null;
    }

    private static StorageClass GetStorageClassFromTypeQualifier(TypeQualifierNode? qualifier)
    {
        if (qualifier == null) return StorageClass.Private;
        foreach (var q in qualifier.Qualifiers)
        {
            if (q is StorageQualifierNode storage)
            {
                return storage.Qualifier switch
                {
                    "uniform" => StorageClass.Uniform,
                    "buffer" => StorageClass.StorageBuffer,
                    "in" => StorageClass.Input,
                    "out" => StorageClass.Output,
                    "const" => StorageClass.UniformConstant,
                    "shared" => StorageClass.Workgroup,
                    _ => StorageClass.Private
                };
            }
        }
        return StorageClass.Private;
    }

    private static bool TryGetIdentifier(PrimaryExpressionNode node, out string identifier)
    {
        identifier = "";
        if (node.Identifier == null)
            return false;

        identifier = node.Identifier.Name;
        return true;
    }

    private static bool TryGetConstantValue<T>(PrimaryExpressionNode node, out T value)
    {
        value = default!;
        
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Boolean when node.BooleanValue.HasValue:
                value = (T)(object)node.BooleanValue.Value;
                return true;
                
            case TypeCode.Int32 when node.IntConstant != null:
                if (int.TryParse(node.IntConstant, out int intVal))
                {
                    value = (T)(object)intVal;
                    return true;
                }
                break;
                
            case TypeCode.UInt32 when node.UintConstant != null:
                if (uint.TryParse(node.UintConstant, out uint uintVal))
                {
                    value = (T)(object)uintVal;
                    return true;
                }
                break;
                
            case TypeCode.Single when node.FloatConstant != null:
                if (float.TryParse(node.FloatConstant, System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                {
                    value = (T)(object)floatVal;
                    return true;
                }
                break;
                
            case TypeCode.Double when node.DoubleConstant != null:
                if (double.TryParse(node.DoubleConstant, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double doubleVal))
                {
                    value = (T)(object)doubleVal;
                    return true;
                }
                break;
        }
        
        return false;
    }
    
    private static StorageClass GetStorageClassFromFullySpecifiedType(FullySpecifiedTypeNode node, bool isLocal)
    {
        if (isLocal) return StorageClass.Function;
        if (node.TypeQualifier == null) return StorageClass.Private;
        return GetStorageClassFromTypeQualifier(node.TypeQualifier);
    }
}