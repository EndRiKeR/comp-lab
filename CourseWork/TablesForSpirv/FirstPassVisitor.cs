using CompLab.CourseWork.SpirV;

namespace comp_lab.CourseWork;

public static class FirstPassVisitor
{
    public static void Visit(TranslationUnitNode node, FirstPassContext context)
    {
        context.RegisterBasicTypes();
        foreach (var decl in node.Declarations)
        {
            Visit(decl, context);
        }
    }

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

    private static void Visit(FunctionDefinitionNode node, FirstPassContext context)
    {
        // Обрабатываем прототип
        var proto = node.Prototype;
        if (proto == null) return;

        // Определяем возвращаемый тип
        var returnType = GetTypeFromTypeNode(proto.Type, context);
        // Определяем типы параметров
        var paramTypes = new List<SpirvType>();
        if (proto.Parameters != null)
        {
            foreach (var param in proto.Parameters.Parameters)
            {
                var paramType = GetTypeFromParameterDeclaration(param, context);
                paramTypes.Add(paramType);
            }
        }

        // Создаём символ функции
        var funcName = proto.Name.Name;
        var funcInfo = new SymbolInfo(SymbolKind.Function, returnType, paramTypes: paramTypes);
        context.Symbols.AddSymbol(funcName, funcInfo, local: false);
    }

    private static void Visit(DeclarationNode node, FirstPassContext context)
    {
        switch (node.DeclType)
        {
            case DeclarationType.InitDeclaratorList:
                if (node.InitDeclaratorList != null)
                {
                    foreach (var singleDecl in node.InitDeclaratorList.SingleDeclarations)
                    {
                        ProcessSingleDeclaration(singleDecl, context);
                    }
                    foreach (var typelessDecl in node.InitDeclaratorList.TypelessDeclarations)
                    {
                        ProcessTypelessDeclaration(typelessDecl, context);
                    }
                }
                break;
            case DeclarationType.StructBlock:
                ProcessStructBlock(node, context);
                break;
            case DeclarationType.TypeQualifierOnly:
                // Пока игнорируем (например, "layout(...) uniform;" без переменных)
                break;
            case DeclarationType.Precision:
                // Пока игнорируем (точность для float/int)
                break;
            case DeclarationType.FunctionPrototype:
                if (node.FunctionPrototype != null)
                {
                    // Регистрируем прототип функции (аналогично определению, но без тела)
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

    private static void ProcessSingleDeclaration(SingleDeclarationNode node, FirstPassContext context)
    {
        // Определяем полный тип переменной
        var fullType = GetTypeFromFullySpecifiedType(node.FullySpecifiedType, context);
        if (node.TypelessDeclaration != null)
        {
            ProcessTypelessDeclaration(node.TypelessDeclaration, context, fullType);
        }
    }

    private static void ProcessTypelessDeclaration(TypelessDeclarationNode node, FirstPassContext context, SpirvType? forcedType = null)
    {
        // Определяем тип (если не передан)
        var varType = forcedType ?? GetTypeFromTypelessDeclaration(node, context);
        // Определяем storage class (пока по умолчанию UniformConstant? Нужно из квалификаторов)
        // Для упрощения: извлекаем storage class из квалификаторов типа (если есть)
        var storageClass = StorageClass.Private; // по умолчанию
        // В реальности нужно анализировать TypeQualifier у переменной.
        // Пока оставим заглушку – потом доработаем.

        var varName = node.Identifier.Name;
        var varInfo = new SymbolInfo(SymbolKind.Variable, varType, storageClass);
        context.Symbols.AddSymbol(varName, varInfo, local: false);
        // Также нужно добавить тип переменной в кэш (если ещё не добавлен)
        context.Types.AddType(varType);
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
                "double" => new FloatType(64), // потребуется capability
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
        // Размеры идут от самого внешнего к внутреннему, но в SPIR-V массив вложенный: [10][20] -> array of array
        // В GLSL многомерные массивы являются массивами массивов.
        foreach (var dim in arraySpec.Dimensions)
        {
            uint? length = null;
            if (dim.ConstantExpression != null)
            {
                // Упрощённо: предполагаем, что константное выражение является целым числом
                // В реальности нужно вычислять константное выражение
                length = EvaluateConstantExpression(dim.ConstantExpression);
            }
            current = new ArrayType(current, length);
            context.Types.AddType(current);
        }
        return current;
    }

    private static uint? EvaluateConstantExpression(ConstantExpressionNode expr)
    {
        // Заглушка: нужно реализовать вычисление констант
        // Пока просто возвращаем 1 для теста
        return 1;
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
}