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
                
                if (param.Identifier != null)
                {
                    var paramInfo = new SymbolInfo(SymbolKind.Variable, paramType, StorageClass.Function);
                    context.Symbols.AddSymbol(param.Identifier.Name, paramInfo, local: true);
                }
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
                    // Это глобальное объявление (не внутри функции)
                    foreach (var singleDecl in node.InitDeclaratorList.SingleDeclarations)
                    {
                        ProcessSingleDeclaration(singleDecl, context, isLocal: false);
                    }
                    foreach (var typelessDecl in node.InitDeclaratorList.TypelessDeclarations)
                    {
                        ProcessTypelessDeclaration(typelessDecl, context, isLocal: false);
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
    
        bool isInsideFunction = context.Symbols.IsInsideFunction();
    
        foreach (var single in declaratorList?.SingleDeclarations)
        {
            var type = GetTypeFromFullySpecifiedType(single.FullySpecifiedType, context);
            context.Types.AddType(type);

            var ident = single.TypelessDeclaration?.Identifier;
            var declaratorInfo = new SymbolInfo(SymbolKind.Variable, type);

            context.Symbols.AddSymbol(ident?.Name, declaratorInfo, local: isInsideFunction);
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
        if (node.DoBody != null)
        {
            Visit(node.DoBody, context);
        }
        else if (node.ForBody != null)
        {
            Visit(node.ForBody, context);
        }
        else if (node.WhileBody != null)
        {
            Visit(node.WhileBody, context);
        }
    }
    
    // ----------------------- Jump Statement Node
    private static void Visit(JumpStatementNode node, FirstPassContext context)
    {
        // нет важных данных для таблиц
    }
    
    // ----------------------- Selection Statement Node
    private static void Visit(SelectionStatementNode node, FirstPassContext context)
    {
        Visit(node.ThenStatement, context);
        
        if (node.ElseStatement != null)
            Visit(node.ElseStatement, context);
    }
    
    // ----------------------- Statement No New Scope Node
    private static void Visit(StatementNoNewScopeNode node, FirstPassContext context)
    {
        if (node.SimpleStatement != null)
            Visit(node.SimpleStatement, context);
        
        if (node.CompoundStatement != null)
            Visit(node.CompoundStatement, context);
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
            // РЕКУРСИЯ
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
        // field access: vec.x, matrix[0][1], struct.field
        // Для первого прохода нужно проверить, что поле существует в базовом типе
    
        // Обрабатываем базовое выражение
        Visit(node.Base, context);
    
        // Проверяем существование поля в типе (необязательно, парсер уже проверил)
        // Получаем тип базового выражения из символа или из предыдущего вычисления
        var baseType = context.GetLastExpressionType();
        if (baseType != null)
        {
            // Для структур нужно убедиться, что поле существует
            if (baseType is StructType structType)
            {
                // Ищем поле по имени (нужна была бы дополнительная информация)
                // Для первого прохода достаточно того, что парсер уже проверил синтаксис
                // Сохраняем тип поля для последующих выражений
                var fieldType = structType.MemberTypes.FirstOrDefault();
                context.SetLastExpressionType(fieldType);
            }
        }
    }

    // ----------------------- Identifier Expression Node
    private static void Visit(IdentifierExpressionNode node, FirstPassContext context)
    {
        Console.WriteLine($"Get to identifier {node.Name}");
    }

    // ----------------------- Postfix Expression Node
    private static void Visit(PostfixExpressionNode node, FirstPassContext context)
    {
        // Обработка: a.b, a->b, a++, a--, a[b], a(b), type(b)
        // И рекурсивно: (a.b).c, arr[i].field
        
        // Продолжаем рекурсивно обходить левую часть, если есть
        if (node.PostfixExpression != null)
        {
            Visit(node.PostfixExpression, context);
        }
        
        // Обработка доступа к полю структуры
        if (node.FieldSelection?.Identifier != null)
        {
            // Получаем тип предыдущего выражения
            var baseType = context.GetLastExpressionType();
            if (baseType is StructType structType)
            {
                // Ищем индекс поля по имени (упрощённо, в реальности нужно отображение имени -> индекс)
                // Для первого прохода просто сохраняем тип предположительного поля
                var fieldType = structType.MemberTypes.FirstOrDefault();
                context.SetLastExpressionType(fieldType);
            }
            else if (baseType is VectorType vecType && node.FieldSelection.Identifier.Name.Length == 1)
            {
                // swizzle: .x, .y, .z, .w
                // Тип остаётся scalar (компонент вектора)
                context.SetLastExpressionType(vecType.ComponentType);
            }
            else if (baseType is MatrixType matType)
            {
                // Доступ к колонке матрицы: m[0] или m[1]
                // Тип - вектор (колонка)
                context.SetLastExpressionType(matType.ColumnType);
            }
        }
        
        // Обработка индексации массива: arr[expr]
        if (node.ArrayIndexExpression != null)
        {
            // arr[5] -> массив, результат - тип элемента
            var baseType = context.GetLastExpressionType();
            if (baseType is ArrayType arrType)
            {
                context.SetLastExpressionType(arrType.ElementType);
            }
            else if (baseType is PointerType ptrType && ptrType.PointeeType is ArrayType ptrArrType)
            {
                context.SetLastExpressionType(ptrArrType.ElementType);
            }
        }
        
        // Обработка вызова функции или конструктора
        if (node.FunctionCallParameters != null)
        {
            if (node.PrimaryExpression is PrimaryExpressionNode primary && TryGetIdentifier(primary, out var funcName))
            {
                // Вызов функции: func(a, b, c)
                var funcSymbol = context.Symbols.Lookup(funcName);
                if (funcSymbol != null && funcSymbol.Kind == SymbolKind.Function)
                {
                    // Сохраняем возвращаемый тип функции
                    context.SetLastExpressionType(funcSymbol.Type);
                }
            }
            else if (node.ConstructorType != null)
            {
                // Конструктор типа: vec3(1.0, 2.0, 3.0)
                var constructorType = GetTypeFromTypeSpecifier(node.ConstructorType, context);
                context.SetLastExpressionType(constructorType);
            }
        }
        
        // Обработка инкремента/декремента: a++, a--
        if (node.HasIncOp || node.HasDecOp)
        {
            // Тип не меняется
            var baseType = context.GetLastExpressionType();
            context.SetLastExpressionType(baseType);
        }
    }
    
    // ----------------------- Primary Expression Node
    private static void Visit(PrimaryExpressionNode node, FirstPassContext context)
    {
        // Обработка первичных выражений: литералы, идентификаторы, выражения в скобках
        
        if (node.Identifier != null)
        {
            // Идентификатор: имя переменной, функции, константы
            var symbol = context.Symbols.Lookup(node.Identifier.Name);
            if (symbol != null)
            {
                // Сохраняем тип для последующего использования
                if (symbol.Kind == SymbolKind.Variable || symbol.Kind == SymbolKind.Function)
                {
                    context.SetLastExpressionType(symbol.Type);
                }
            }
            // TODO: также могут быть именованные константы (#define PI 3.14)
        }
        else if (node.BooleanValue.HasValue)
        {
            // Булевский литерал: true, false
            var boolType = new BoolType();
            context.Types.AddType(boolType);
            context.SetLastExpressionType(boolType);
        }
        else if (node.IntConstant != null)
        {
            // Целочисленный литерал со знаком: 5, -10
            var intType = new IntType(32, true);
            context.Types.AddType(intType);
            context.SetLastExpressionType(intType);
        }
        else if (node.UintConstant != null)
        {
            // Беззнаковый литерал: 5u, 10U
            var uintType = new IntType(32, false);
            context.Types.AddType(uintType);
            context.SetLastExpressionType(uintType);
        }
        else if (node.FloatConstant != null)
        {
            // Литерал с плавающей точкой: 3.14, 1.0f
            var floatType = new FloatType(32);
            context.Types.AddType(floatType);
            context.SetLastExpressionType(floatType);
        }
        else if (node.DoubleConstant != null)
        {
            // Литерал double: 3.14lf
            var doubleType = new FloatType(64);
            context.Types.AddType(doubleType);
            context.SetLastExpressionType(doubleType);
        }
        else if (node.ParenthesizedExpression != null)
        {
            // Выражение в скобках: (a + b)
            Visit(node.ParenthesizedExpression, context);
            // Тип такой же, как у внутреннего выражения
        }
    }

    // ----------------------- Unary Expression Node
    private static void Visit(UnaryExpressionNode node, FirstPassContext context)
    {
        // Унарные операции: -a, +a, !a, ~a, ++a, --a
        // А также: *a (разыменование), &a (взятие адреса)
        
        if (node.Operand != null)
        {
            // Рекурсивно обходим операнд
            Visit(node.Operand, context);
            
            var operandType = context.GetLastExpressionType();
            if (operandType == null) return;
            
            // Определяем тип результата в зависимости от операции
            if (node.UnaryOperator != null)
            {
                switch (node.UnaryOperator.Operator)
                {
                    case "+":
                    case "-":
                    case "~":
                        // Унарный плюс/минус/битовое НЕ сохраняют тип
                        context.SetLastExpressionType(operandType);
                        break;
                        
                    case "!":
                        // Логическое НЕ -> результат bool
                        var boolType = new BoolType();
                        context.Types.AddType(boolType);
                        context.SetLastExpressionType(boolType);
                        break;
                        
                    case "*":
                        // Разыменование указателя: *ptr
                        if (operandType is PointerType ptrType)
                        {
                            context.SetLastExpressionType(ptrType.PointeeType);
                        }
                        break;
                        
                    case "&":
                        // Взятие адреса: &var -> указатель
                        var ptrType2 = new PointerType(StorageClass.Function, operandType);
                        context.Types.AddType(ptrType2);
                        context.SetLastExpressionType(ptrType2);
                        break;
                }
            }
            else if (node.HasIncOp || node.HasDecOp)
            {
                // Префиксный/постфиксный инкремент/декремент сохраняют тип
                context.SetLastExpressionType(operandType);
            }
        }
        else if (node.PostfixExpression != null)
        {
            // Постфиксная форма уже обработана в Visit(PostfixExpressionNode)
            Visit(node.PostfixExpression, context);
        }
    }
    
    private static void ProcessSingleDeclaration(SingleDeclarationNode node, FirstPassContext context)
    {
        // Определяем полный тип переменной
        var fullType = GetTypeFromFullySpecifiedType(node.FullySpecifiedType, context);
        if (node.TypelessDeclaration != null)
        {
            ProcessTypelessDeclaration(node.TypelessDeclaration, context, fullType);
        }
    }

    private static void ProcessTypelessDeclaration(TypelessDeclarationNode node, FirstPassContext context, SpirvType? forcedType = null, bool isLocal = false)
    {
        var varType = forcedType ?? GetTypeFromTypelessDeclaration(node, context);
        var storageClass = StorageClass.Private;
    
        if (isLocal)
            storageClass = StorageClass.Function;
    
        var varName = node.Identifier.Name;
        var varInfo = new SymbolInfo(SymbolKind.Variable, varType, storageClass);
        context.Symbols.AddSymbol(varName, varInfo, local: isLocal);
        context.Types.AddType(varType);
    }
    
    private static void ProcessSingleDeclaration(SingleDeclarationNode node, FirstPassContext context, bool isLocal = false)
    {
        var fullType = GetTypeFromFullySpecifiedType(node.FullySpecifiedType, context);
        if (node.TypelessDeclaration != null)
        {
            ProcessTypelessDeclaration(node.TypelessDeclaration, context, fullType, isLocal);
        }
    }

    private static void ProcessStructBlock(DeclarationNode node, FirstPassContext context)
    {
        // Обрабатываем объявление блока (uniform block, buffer block)
        // Сначала создаём тип структуры из struct_declaration_list
        if (node.StructDeclarationList != null)
        {
            var memberTypes = new List<SpirvType>();
            foreach (var structDecl in node.StructDeclarationList.Declarations)
            {
                // Для каждого structDecl (может содержать несколько declarator'ов)
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
            var structType = new StructType(memberTypes);
            context.Types.AddType(structType);

            // Если у блока есть имя (BlockName), регистрируем его как тип
            if (node.BlockName != null)
            {
                var typeInfo = new SymbolInfo(SymbolKind.Type, structType);
                context.Symbols.AddSymbol(node.BlockName.Name, typeInfo, local: false);
            }

            // Если есть instance name – регистрируем переменную этого типа
            if (node.BlockInstanceName != null)
            {
                // Storage class извлекаем из typeQualifier (node.BlockTypeQualifier)
                var storageClass = GetStorageClassFromTypeQualifier(node.BlockTypeQualifier);
                var varType = ApplyArraySpecifier(structType, node.BlockInstanceArraySpecifier, context);
                var varInfo = new SymbolInfo(SymbolKind.Variable, varType, storageClass);
                context.Symbols.AddSymbol(node.BlockInstanceName.Name, varInfo, local: false);
                context.Types.AddType(varType);
            }
        }
    }

    // Вспомогательные методы для получения типа из различных узлов AST

    private static SpirvType GetTypeFromTypeNode(TypeNode node, FirstPassContext context)
    {
        // Сначала получаем спецификатор типа
        var type = GetTypeFromTypeSpecifier(node.TypeSpecifier, context);
        // Затем применяем квалификаторы (если нужно) – для первого прохода тип без учёта storage class
        return type;
    }

    private static SpirvType GetTypeFromFullySpecifiedType(FullySpecifiedTypeNode node, FirstPassContext context)
    {
        // Получаем тип из спецификатора
        var type = GetTypeFromTypeSpecifier(node.TypeSpecifier, context);
        // Квалификаторы (storage, layout) пока игнорируем – они влияют на storage class, а не на тип
        return type;
    }

    private static SpirvType GetTypeFromTypelessDeclaration(TypelessDeclarationNode node, FirstPassContext context)
    {
        // В typelessDeclaration нет явного типа, он приходит из SingleDeclarationNode
        throw new NotImplementedException("TypelessDeclaration should have forced type");
    }

    private static SpirvType GetTypeFromParameterDeclaration(ParameterDeclarationNode node, FirstPassContext context)
    {
        // У параметра может быть TypeSpecifier и ArraySpecifier
        if (node.ParameterTypeSpecifier != null)
        {
            var baseType = GetTypeFromTypeSpecifier(node.ParameterTypeSpecifier, context);
            return ApplyArraySpecifier(baseType, node.ArraySpecifier, context);
        }
        // Если параметр имеет только identifier (встречается в прототипах без типа?), но по грамматике такого не должно быть
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
            // Имя типа (должно быть зарегистрировано ранее)
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

        return ApplyArraySpecifier(baseType, node.ArraySpecifier, context);
    }

    private static SpirvType GetTypeFromStructSpecifier(StructSpecifierNode node, FirstPassContext context)
    {
        // Собираем типы членов
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

        // Если у структуры есть имя, регистрируем его как тип
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
        // Более полная обработка константных выражений
        
        // Случай 1: простое целое число в бинарном выражении
        if (expr.BinaryExpression != null)
        {
            var bin = expr.BinaryExpression;
            
            // Пробуем получить константное значение слева
            if (bin.Left is UnaryExpressionNode unary && 
                unary.PostfixExpression?.PrimaryExpression is PrimaryExpressionNode primary)
            {
                if (TryGetConstantValue(primary, out int value))
                    return (uint)value;
            }
            
            // Рекурсивно вычисляем бинарное выражение (можно расширить)
            // Например: 5 + 3, 2 * 4 и т.д.
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
        
        // Случай 2: тернарный оператор condition ? true : false
        if (expr.Condition != null && expr.TrueExpression != null && expr.FalseExpression != null)
        {
            // Для константных выражений в GLSL условие тоже должно быть константным
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
        // Рекурсивно обходим узел для вычисления константы
        if (node is ConstantExpressionNode constExpr)
            return EvaluateConstantExpression(constExpr);
            
        if (node is PrimaryExpressionNode primary && TryGetConstantValue(primary, out int value))
            return (uint)value;
            
        if (node is UnaryExpressionNode unary && unary.PostfixExpression?.PrimaryExpression is PrimaryExpressionNode primary2)
            if (TryGetConstantValue(primary2, out int value2))
                return (uint)value2;
        
        // Для отрицательных чисел: -5
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
}