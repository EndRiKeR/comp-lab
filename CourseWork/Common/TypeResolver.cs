using comp_lab.CourseWork._2._AstToTables;

namespace comp_lab.CourseWork.Common
{
    public static class TypeResolver
    {
        public static SpirvType GetTypeFromTypeSpecifier(TypeSpecifierNode node, FirstPassContext context)
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

        public static SpirvType GetTypeFromStructSpecifier(StructSpecifierNode node, FirstPassContext context)
        {
            var memberTypes = new List<SpirvType>();
            var memberNames = new List<string>();
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
                            memberNames.Add(decl.Identifier.Name);
                        }
                    }
                }
            }
            var structType = new StructType(memberTypes, memberNames);
            context.Types.AddType(structType);
            if (node.Name != null)
            {
                var typeInfo = new SymbolInfo(SymbolKind.Type, structType);
                context.Symbols.AddSymbol(node.Name.Name, typeInfo, false);
            }
            return structType;
        }

        public static SpirvType ApplyArraySpecifier(SpirvType baseType, ArraySpecifierNode? arraySpec, FirstPassContext context)
        {
            if (arraySpec == null) return baseType;
            SpirvType current = baseType;
            foreach (var dim in arraySpec.Dimensions)
            {
                uint? length = null;
                if (dim.ConstantExpression != null)
                {
                    length = EvaluateConstantExpression(dim.ConstantExpression, context);
                }
                current = new ArrayType(current, length);
                context.Types.AddType(current);
            }
            return current;
        }

        private static uint? EvaluateConstantExpression(ConstantExpressionNode expr, FirstPassContext context)
        {
            if (expr.BinaryExpression != null)
            {
                var bin = expr.BinaryExpression;
                var leftVal = EvaluateConstantExpressionFromNode(bin.Left, context);
                var rightVal = EvaluateConstantExpressionFromNode(bin.Right, context);
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
            else if (expr.Condition != null && expr.TrueExpression != null && expr.FalseExpression != null)
            {
                var condVal = EvaluateConstantExpressionFromNode(expr.Condition, context);
                if (condVal.HasValue && condVal.Value != 0)
                    return EvaluateConstantExpressionFromNode(expr.TrueExpression, context);
                else if (condVal.HasValue)
                    return EvaluateConstantExpressionFromNode(expr.FalseExpression, context);
            }
            return null;
        }

        private static uint? EvaluateConstantExpressionFromNode(ExpressionNode node, FirstPassContext context)
        {
            if (node is ConstantExpressionNode constExpr)
                return EvaluateConstantExpression(constExpr, context);
            if (node is PrimaryExpressionNode primary && TryGetIntConstant(primary, out int value))
                return (uint)value;
            if (node is UnaryExpressionNode unary && unary.PostfixExpression?.PrimaryExpression is PrimaryExpressionNode primary2)
                if (TryGetIntConstant(primary2, out int value2))
                    return (uint)value2;
            if (node is UnaryExpressionNode negUnary && negUnary.UnaryOperator?.Operator == "-")
                if (EvaluateConstantExpressionFromNode(negUnary.Operand, context) is uint negVal)
                    return (uint)(-(int)negVal);
            return null;
        }

        private static bool TryGetIntConstant(PrimaryExpressionNode node, out int value)
        {
            value = 0;
            if (node.IntConstant != null && int.TryParse(node.IntConstant, out value))
                return true;
            if (node.UintConstant != null && uint.TryParse(node.UintConstant, out var uval))
            {
                value = (int)uval;
                return true;
            }
            return false;
        }

        public static SpirvType GetTypeFromParameterDeclaration(ParameterDeclarationNode node, FirstPassContext context)
        {
            if (node.ParameterTypeSpecifier != null)
            {
                var baseType = GetTypeFromTypeSpecifier(node.ParameterTypeSpecifier, context);
                return ApplyArraySpecifier(baseType, node.ArraySpecifier, context);
            }
            throw new InvalidOperationException("Parameter without type specifier");
        }

        public static SpirvType GetTypeFromFullySpecifiedType(FullySpecifiedTypeNode node, FirstPassContext context)
        {
            return GetTypeFromTypeSpecifier(node.TypeSpecifier, context);
        }
    }
}