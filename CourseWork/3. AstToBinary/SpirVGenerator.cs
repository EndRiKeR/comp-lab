using comp_lab.CourseWork._2._AstToTables;
using comp_lab.CourseWork.Common;
using comp_lab.CourseWork.GLSLGrammar;

namespace comp_lab.CourseWork._3._AstToBinary
{
    public class SpirVGenerator
    {
        private FirstPassContext _firstPass = null!;
        private SecondPassContext _secondPass = null!;
        private readonly Dictionary<FunctionDefinitionNode, uint> _functionIds = new();
        private readonly Dictionary<SymbolInfo, uint> _functionSymbolIds = new();   // ID функции
        private readonly Dictionary<SymbolInfo, uint> _functionTypeIds = new();     // ID OpTypeFunction
        private readonly Dictionary<string, uint> _labelIds = new();
        
        private uint _lastInstructionCountBeforeBlock;
        private readonly HashSet<SpirvType> _emittedTypes = new();
        private readonly List<Instruction> _pendingFunctionTypes = new();

        public void GenerateAndSave(TranslationUnitNode ast, string entryPointName, string outputPath)
        {
            Generate(ast, entryPointName);
            var binary = _secondPass.Module.Serialize();
            File.WriteAllBytes(outputPath, binary);
            Console.WriteLine($"SPIR-V written to {outputPath}");
        }
        
        public void Generate(TranslationUnitNode ast, string entryPointName)
        {
            _firstPass = new FirstPassContext();
            FirstPassVisitor.Visit(ast, _firstPass);
            _secondPass = new SecondPassContext(_firstPass);

            _secondPass.Module.Capability(1); // Shader
            var glslStdId = _secondPass.Module.GetNextId();
            _secondPass.Module.ExtInstImport(glslStdId, "GLSL.std.450");
            _secondPass.ImportedSetIds["GLSL.std.450"] = glslStdId;
            _secondPass.Module.MemoryModel(0, 1); // Logical, GLSL450

            // Добавляем VoidType в кэш (но ещё не эмитируем)
            var voidType = new VoidType();
            _firstPass.Types.AddType(voidType);

            // Получаем ID функции (создаёт отложенные OpTypeFunction)
            var mainSymbol = _firstPass.Symbols.Lookup(entryPointName);
            if (mainSymbol == null || mainSymbol.Kind != SymbolKind.Function)
                throw new Exception("Entry point not found");
            var mainId = GetOrCreateFunctionId(mainSymbol);
            var interfaceIds = CollectInterfaceVariables();

            // Определяем модель выполнения
            uint execModel = 4; // Fragment по умолчанию
            bool isCompute = false;
            uint localSizeX = 0, localSizeY = 1, localSizeZ = 1;
            foreach (var (_, sym) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (sym.Kind == SymbolKind.Variable && sym.Decorations.TryGetValue(DecorationKind.LocalSizeX, out var x))
                {
                    isCompute = true;
                    localSizeX = x;
                    sym.Decorations.TryGetValue(DecorationKind.LocalSizeY, out localSizeY);
                    sym.Decorations.TryGetValue(DecorationKind.LocalSizeZ, out localSizeZ);
                    break;
                }
            }
            if (isCompute) execModel = 5;

            // OpEntryPoint и OpExecutionMode – сразу после MemoryModel
            var entryInst = new Instruction { Opcode = Opcode.OpEntryPoint, Operands = { execModel, mainId, entryPointName } };
            foreach (var id in interfaceIds) entryInst.Operands.Add(id);
            _secondPass.Module.AddInstruction(entryInst);

            if (isCompute)
            {
                var modeInst = new Instruction { Opcode = Opcode.OpExecutionMode, Operands = { mainId, 17u, localSizeX, localSizeY, localSizeZ } };
                _secondPass.Module.AddInstruction(modeInst);
            }
            else
            {
                _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpExecutionMode, Operands = { mainId, 7u } });
            }

            // Теперь эмитируем все типы (включая отложенные OpTypeFunction)
            EmitTypeDeclarations();

            // Debug-имена (после EntryPoint, перед переменными)
            EmitDebugNames();

            // Глобальные переменные
            EmitGlobalVariables();

            // Генерация функций
            EmitFunctions(ast);
            _secondPass.Module.SetBound();
        }

        private void EmitPendingFunctionTypes()
        {
            foreach (var inst in _pendingFunctionTypes)
            {
                _secondPass.Module.AddInstruction(inst);
            }
            _pendingFunctionTypes.Clear();
        }
        
        private void EmitDebugNames()
        {
            foreach (var (name, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind == SymbolKind.Variable && symbol.Id.HasValue)
                {
                    _secondPass.Module.Name(symbol.Id.Value, name);
                }
            }
        }

        private void EmitTypeDeclarations()
        {
            // Сначала эмитируем отложенные типы функций
            foreach (var inst in _pendingFunctionTypes)
                _secondPass.Module.AddInstruction(inst);
            _pendingFunctionTypes.Clear();

            // Затем все остальные типы из TypeCache
            foreach (var type in _firstPass.Types.AllTypes)
            {
                if (!_emittedTypes.Contains(type))
                    EmitType(type);
            }
        }
        
        private void EmitType(SpirvType type)
        {
            if (_emittedTypes.Contains(type))
                return;

            var id = _secondPass.MapType(type);
            switch (type)
            {
                case VoidType: _secondPass.Module.TypeVoid(id); break;
                case BoolType: _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeBool, ResultId = id }); break;
                case IntType it: _secondPass.Module.TypeInt(id, (uint)it.Width, it.Signed ? 1u : 0u); break;
                case FloatType ft: _secondPass.Module.TypeFloat(id, (uint)ft.Width); break;
                case VectorType vt:
                    _secondPass.Module.TypeVector(id, _secondPass.MapType(vt.ComponentType), (uint)vt.ComponentCount);
                    break;
                case MatrixType mt:
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeMatrix, ResultId = id, Operands = { _secondPass.MapType(mt.ColumnType), (uint)mt.ColumnCount } });
                    break;
                case ArrayType at:
                    var elemId = _secondPass.MapType(at.ElementType);
                    if (at.Length.HasValue)
                        _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeArray, ResultId = id, Operands = { elemId, CreateConstantInt(32, false, at.Length.Value) } });
                    else
                        _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeRuntimeArray, ResultId = id, Operands = { elemId } });
                    break;
                case StructType st:
                    var memberIds = st.MemberTypes.Select(m => _secondPass.MapType(m)).ToArray();
                    var structInst = new Instruction { Opcode = Opcode.OpTypeStruct, ResultId = id };
                    foreach (var mid in memberIds) structInst.Operands.Add(mid);
                    _secondPass.Module.AddInstruction(structInst);
                    break;
                case PointerType pt:
                    _secondPass.Module.TypePointer(id, (uint)pt.StorageClass, _secondPass.MapType(pt.PointeeType));
                    break;
                default: throw new NotSupportedException($"Type {type.GetType()}");
            }
            _emittedTypes.Add(type);
        }

        private uint CreateConstantInt(int width, bool signed, uint value)
        {
            var intType = new IntType(width, signed);
            var typeId = _secondPass.MapType(intType);
            var key = (typeId, value);
            if (_secondPass.ConstantIdMap.TryGetValue(key, out var existingId))
                return existingId;
            var constId = _secondPass.Module.GetNextId();
            _secondPass.ConstantIdMap[key] = constId;
            _secondPass.Module.Constant(typeId, constId, value);
            return constId;
        }

        private void EmitGlobalVariables()
        {
            foreach (var (name, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind == SymbolKind.Variable)
                {
                    var ptrType = new PointerType(symbol.StorageClass!.Value, symbol.Type);
                    if (!_emittedTypes.Contains(ptrType))
                        EmitType(ptrType);
                    var ptrTypeId = _secondPass.MapType(ptrType);
                    var varId = _secondPass.Module.GetNextId();
                    symbol.Id = varId;
                    _secondPass.Module.Variable(ptrTypeId, varId, (uint)symbol.StorageClass.Value, null);
                }
            }
        }

        private List<uint> CollectInterfaceVariables()
        {
            var list = new List<uint>();
            foreach (var (_, sym) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (sym.Kind == SymbolKind.Variable && (sym.StorageClass == StorageClass.Input || sym.StorageClass == StorageClass.Output))
                {
                    if (sym.Id.HasValue) list.Add(sym.Id.Value);
                }
            }
            return list;
        }

        private uint GetOrCreateFunctionId(SymbolInfo funcSymbol)
        {
            if (_functionSymbolIds.TryGetValue(funcSymbol, out var id))
                return id;

            var returnTypeId = _secondPass.MapType(funcSymbol.Type);
            var paramTypeIds = funcSymbol.ParameterTypes?.Select(t => _secondPass.MapType(t)).ToArray() ?? Array.Empty<uint>();
    
            // Создаём OpTypeFunction
            var funcTypeInst = new Instruction
            {
                Opcode = Opcode.OpTypeFunction,
                ResultId = _secondPass.Module.GetNextId(),
                Operands = { returnTypeId }
            };
            foreach (var pt in paramTypeIds) funcTypeInst.Operands.Add(pt);
    
            _pendingFunctionTypes.Add(funcTypeInst); // откладываем
            var funcTypeId = funcTypeInst.ResultId.Value;
            _functionTypeIds[funcSymbol] = funcTypeId;

            // ID самой функции
            id = _secondPass.Module.GetNextId();
            _functionSymbolIds[funcSymbol] = id;
            funcSymbol.Id = id;
            return id;
        }

        private void EmitFunctions(TranslationUnitNode ast)
        {
            foreach (var decl in ast.Declarations)
            {
                if (decl is FunctionDefinitionNode funcDef)
                {
                    GenerateFunction(funcDef);
                }
            }
        }

        private void GenerateFunction(FunctionDefinitionNode funcDef)
        {
            var funcName = funcDef.Prototype.Name.Name;
            var funcSymbol = _firstPass.Symbols.Lookup(funcName);
            if (funcSymbol == null) throw new Exception("Function symbol not found");
            var funcId = GetOrCreateFunctionId(funcSymbol);
            var returnTypeId = _secondPass.MapType(funcSymbol.Type);
            var funcTypeId = _functionTypeIds[funcSymbol];
            _secondPass.Module.Function(returnTypeId, funcId, 0, funcTypeId);

            _secondPass.EnterLocalScope();
            // Генерируем параметры, только если они есть в AST
            if (funcDef.Prototype.Parameters != null)
            {
                foreach (var param in funcDef.Prototype.Parameters.Parameters)
                {
                    var paramType = TypeResolver.GetTypeFromParameterDeclaration(param, _firstPass);
                    var paramPtrType = new PointerType(StorageClass.Function, paramType);
                    if (!_emittedTypes.Contains(paramPtrType))
                        EmitType(paramPtrType);
                    var paramPtrTypeId = _secondPass.MapType(paramPtrType);
                    var paramId = _secondPass.Module.GetNextId();
                    _secondPass.Module.FunctionParameter(paramPtrTypeId, paramId);
                    if (param.Identifier != null)
                    {
                        var paramSym = new SymbolInfo(SymbolKind.Variable, paramType, StorageClass.Function) { Id = paramId };
                        _secondPass.AddLocalSymbol(param.Identifier.Name, paramSym);
                    }
                }
            }

            var entryLabel = NewLabel("entry");
            _secondPass.Module.Label(entryLabel);
            _secondPass.CurrentBlock = entryLabel;
            BeginBlock();
            GenerateStatement(funcDef.Body);
            if (!IsCurrentBlockTerminated()) _secondPass.Module.Return();
            _secondPass.Module.FunctionEnd();
            _secondPass.ExitLocalScope();
        }

        private void GenerateStatement(StatementNode stmt)
        {
            switch (stmt)
            {
                case CompoundStatementNoNewScopeNode compound:
                    foreach (var s in compound.Statements) GenerateStatement(s);
                    break;
                case CompoundStatementNode compound:
                    _secondPass.EnterLocalScope();
                    foreach (var s in compound.Statements) GenerateStatement(s);
                    _secondPass.ExitLocalScope();
                    break;
                case StatementNoNewScopeNode stmtNoNewScope:
                    if (stmtNoNewScope.CompoundStatement != null)
                        GenerateStatement(stmtNoNewScope.CompoundStatement);
                    else if (stmtNoNewScope.SimpleStatement != null)
                        GenerateStatement(stmtNoNewScope.SimpleStatement);
                    break;
                case ExpressionStatementNode exprStmt:
                    if (exprStmt.Expression != null) GenerateExpression(exprStmt.Expression);
                    break;
                case DeclarationStatementNode declStmt:
                    GenerateDeclarationStatement(declStmt);
                    break;
                case SelectionStatementNode ifStmt:
                    GenerateIfStatement(ifStmt);
                    break;
                case IterationStatementNode loopStmt:
                    GenerateLoop(loopStmt);
                    break;
                case JumpStatementNode jump:
                    GenerateJump(jump);
                    break;
                default: throw new NotImplementedException($"Statement {stmt.GetType()}");
            }
        }

        private void GenerateDeclarationStatement(DeclarationStatementNode declStmt)
        {
            if (declStmt.Declaration.DeclType != DeclarationType.InitDeclaratorList || declStmt.Declaration.InitDeclaratorList == null)
                return;
            foreach (var single in declStmt.Declaration.InitDeclaratorList.SingleDeclarations)
            {
                var varType = TypeResolver.GetTypeFromFullySpecifiedType(single.FullySpecifiedType, _firstPass);
                var ptrType = new PointerType(StorageClass.Function, varType);
                if (!_emittedTypes.Contains(ptrType))
                    EmitType(ptrType);
                var ptrTypeId = _secondPass.MapType(ptrType);
                var varId = _secondPass.Module.GetNextId();
                _secondPass.Module.Variable(ptrTypeId, varId, (uint)StorageClass.Function, null);
                if (single.TypelessDeclaration != null)
                {
                    var varName = single.TypelessDeclaration.Identifier.Name;
                    var sym = new SymbolInfo(SymbolKind.Variable, varType, StorageClass.Function) { Id = varId };
                    _secondPass.AddLocalSymbol(varName, sym);
                    if (single.TypelessDeclaration.Initializer != null)
                    {
                        var initVal = GenerateInitializer(single.TypelessDeclaration.Initializer, varType);
                        _secondPass.Module.Store(varId, initVal);
                    }
                }
            }
        }

        private uint GenerateInitializer(InitializerNode init, SpirvType targetType)
        {
            if (init.AssignmentExpression != null)
                return GenerateAssignmentExpression(init.AssignmentExpression);
            if (init.InitializerList != null)
            {
                var constituents = new List<uint>();
                foreach (var subInit in init.InitializerList.Initializers)
                    constituents.Add(GenerateInitializer(subInit, targetType));
                var compositeId = _secondPass.Module.GetNextId();
                var resultTypeId = _secondPass.MapType(targetType);
                var inst = new Instruction { Opcode = Opcode.OpConstantComposite, ResultType = resultTypeId, ResultId = compositeId };
                foreach (var c in constituents) inst.Operands.Add(c);
                _secondPass.Module.AddInstruction(inst);
                return compositeId;
            }
            throw new NotImplementedException();
        }

        private uint GenerateExpression(ExpressionNode expr)
        {
            return expr switch
            {
                IdentifierExpressionNode id => LoadVariable(id.Name),
                PrimaryExpressionNode prim => GeneratePrimaryExpression(prim),
                BinaryExpressionNode bin => GenerateBinaryExpression(bin),
                UnaryExpressionNode unary => GenerateUnaryExpression(unary),
                AssignmentExpressionNode assign => GenerateAssignmentExpression(assign),
                ConstructorExpressionNode ctor => GenerateConstructorExpression(ctor),
                FieldAccessExpressionNode field => GenerateFieldAccess(field),
                PostfixExpressionNode post => GeneratePostfixExpression(post),
                _ => throw new NotImplementedException($"Expression {expr.GetType()}")
            };
        }

        private uint GeneratePrimaryExpression(PrimaryExpressionNode node)
        {
            if (node.Identifier != null)
                return LoadVariable(node.Identifier.Name);
            if (node.BooleanValue.HasValue)
                return _secondPass.GetConstantId(new BoolType(), node.BooleanValue.Value);
            if (node.IntConstant != null && int.TryParse(node.IntConstant, out var ival))
                return _secondPass.GetConstantId(new IntType(32, true), ival);
            if (node.UintConstant != null && uint.TryParse(node.UintConstant, out var uval))
                return _secondPass.GetConstantId(new IntType(32, false), uval);
            if (node.FloatConstant != null && float.TryParse(node.FloatConstant, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fval))
                return _secondPass.GetConstantId(new FloatType(32), fval);
            if (node.DoubleConstant != null && double.TryParse(node.DoubleConstant, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dval))
                return _secondPass.GetConstantId(new FloatType(64), dval);
            if (node.ParenthesizedExpression != null)
                return GenerateExpression(node.ParenthesizedExpression);
            throw new NotImplementedException();
        }

        private uint LoadVariable(string name)
        {
            var sym = _secondPass.Lookup(name);
            if (sym == null) throw new Exception($"Unknown variable {name}");
            var ptrId = sym.Id!.Value;
            var loaded = _secondPass.Module.GetNextId();
            _secondPass.Module.Load(_secondPass.MapType(sym.Type), loaded, ptrId);
            return loaded;
        }

        private uint GenerateBinaryExpression(BinaryExpressionNode bin)
        {
            var left = GenerateExpression(bin.Left);
            var right = GenerateExpression(bin.Right);
            var leftType = GetExpressionType(bin.Left);
            var resultType = leftType; // упрощённо
            Opcode op = Opcode.OpNop;
            if (leftType is IntType it)
            {
                op = bin.Operator switch
                {
                    "+" => Opcode.OpIAdd,
                    "-" => Opcode.OpISub,
                    "*" => Opcode.OpIMul,
                    "/" => it.Signed ? Opcode.OpSDiv : Opcode.OpUDiv,
                    "%" => it.Signed ? Opcode.OpSMod : Opcode.OpUMod,
                    "&" => Opcode.OpBitwiseAnd,
                    "|" => Opcode.OpBitwiseOr,
                    "^" => Opcode.OpBitwiseXor,
                    "<<" => Opcode.OpShiftLeftLogical,
                    ">>" => it.Signed ? Opcode.OpShiftRightArithmetic : Opcode.OpShiftRightLogical,
                    "==" => Opcode.OpIEqual,
                    "!=" => Opcode.OpINotEqual,
                    "<" => it.Signed ? Opcode.OpSLessThan : Opcode.OpULessThan,
                    ">" => it.Signed ? Opcode.OpSGreaterThan : Opcode.OpUGreaterThan,
                    "<=" => it.Signed ? Opcode.OpSLessThanEqual : Opcode.OpULessThanEqual,
                    ">=" => it.Signed ? Opcode.OpSGreaterThanEqual : Opcode.OpUGreaterThanEqual,
                    _ => throw new NotImplementedException()
                };
            }
            else if (leftType is FloatType)
            {
                op = bin.Operator switch
                {
                    "+" => Opcode.OpFAdd,
                    "-" => Opcode.OpFSub,
                    "*" => Opcode.OpFMul,
                    "/" => Opcode.OpFDiv,
                    "==" => Opcode.OpFOrdEqual,
                    "!=" => Opcode.OpFOrdNotEqual,
                    "<" => Opcode.OpFOrdLessThan,
                    ">" => Opcode.OpFOrdGreaterThan,
                    "<=" => Opcode.OpFOrdLessThanEqual,
                    ">=" => Opcode.OpFOrdGreaterThanEqual,
                    _ => throw new NotImplementedException()
                };
            }
            else throw new NotSupportedException();
            var resultId = _secondPass.Module.GetNextId();
            var resTypeId = _secondPass.MapType(resultType);
            _secondPass.Module.AddInstruction(new Instruction { Opcode = op, ResultType = resTypeId, ResultId = resultId, Operands = { left, right } });
            return resultId;
        }

        private uint GenerateUnaryExpression(UnaryExpressionNode unary)
        {
            if (unary.Operand != null)
            {
                var operand = GenerateExpression(unary.Operand);
                var operandType = GetExpressionType(unary.Operand);
                if (unary.UnaryOperator != null)
                {
                    switch (unary.UnaryOperator.Operator)
                    {
                        case "-":
                            var zero = _secondPass.GetConstantId(operandType, 0);
                            var subId = _secondPass.Module.GetNextId();
                            var op = operandType is IntType ? Opcode.OpISub : Opcode.OpFSub;
                            _secondPass.Module.AddInstruction(new Instruction { Opcode = op, ResultType = _secondPass.MapType(operandType), ResultId = subId, Operands = { zero, operand } });
                            return subId;
                        case "!":
                            var boolType = new BoolType();
                            var notId = _secondPass.Module.GetNextId();
                            _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpLogicalNot, ResultType = _secondPass.MapType(boolType), ResultId = notId, Operands = { operand } });
                            return notId;
                        case "~":
                            var notBitId = _secondPass.Module.GetNextId();
                            _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpNot, ResultType = _secondPass.MapType(operandType), ResultId = notBitId, Operands = { operand } });
                            return notBitId;
                        default: throw new NotImplementedException();
                    }
                }
                else if (unary.HasIncOp)
                {
                    var ptr = GetPointer(unary.Operand);
                    var loaded = _secondPass.Module.GetNextId();
                    _secondPass.Module.Load(_secondPass.MapType(operandType), loaded, ptr);
                    var one = _secondPass.GetConstantId(operandType, 1);
                    var added = _secondPass.Module.GetNextId();
                    var addOp = operandType is IntType ? Opcode.OpIAdd : Opcode.OpFAdd;
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = addOp, ResultType = _secondPass.MapType(operandType), ResultId = added, Operands = { loaded, one } });
                    _secondPass.Module.Store(ptr, added);
                    return added;
                }
                else if (unary.HasDecOp) { /* аналогично */ }
            }
            else if (unary.PostfixExpression != null)
                return GenerateExpression(unary.PostfixExpression);
            throw new NotImplementedException();
        }

        private uint GetPointer(ExpressionNode expr)
        {
            if (expr is IdentifierExpressionNode id)
            {
                var sym = _secondPass.Lookup(id.Name);
                return sym!.Id!.Value;
            }
            if (expr is PrimaryExpressionNode prim)
            {
                if (prim.Identifier != null)
                {
                    var sym = _secondPass.Lookup(prim.Identifier.Name);
                    return sym!.Id!.Value;
                }
                throw new NotImplementedException($"PrimaryExpression without identifier in GetPointer");
            }
            if (expr is FieldAccessExpressionNode field)
            {
                var basePtr = GetPointer(field.Base);
                var baseType = GetExpressionType(field.Base);
                var index = GetFieldIndex(baseType, field.FieldName);
                var ptrId = _secondPass.Module.GetNextId();
                var ptrType = new PointerType(StorageClass.Function, GetFieldType(baseType, field.FieldName));
                _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpAccessChain, ResultType = _secondPass.MapType(ptrType), ResultId = ptrId, Operands = { basePtr, index } });
                return ptrId;
            }
            if (expr is UnaryExpressionNode unary)
            {
                if (unary.PostfixExpression != null)
                    return GetPointer(unary.PostfixExpression);
                if (unary.UnaryOperator?.Operator == "*" && unary.Operand != null)
                    return GetPointer(unary.Operand);
                throw new NotImplementedException($"UnaryExpression cannot be lvalue: operator {unary.UnaryOperator?.Operator ?? "none"}");
            }
            if (expr is PostfixExpressionNode post)
            {
                if (post.PrimaryExpression != null && post.ArrayIndexExpression == null && post.FieldSelection == null && post.PostfixExpression == null)
                    return GetPointer(post.PrimaryExpression);
                throw new NotImplementedException($"PostfixExpression lvalue not fully handled: {post}");
            }
            throw new NotImplementedException($"GetPointer for {expr.GetType()}");
        }

        private uint GenerateAssignmentExpression(AssignmentExpressionNode assign)
        {
            if (assign.LeftUnary == null)
            {
                if (assign.ConstantExpression == null)
                    throw new Exception("Assignment without left operand and without constant expression");
                return GenerateConstantExpression(assign.ConstantExpression);
            }
            var ptr = GetPointer(assign.LeftUnary);
            var right = GenerateExpression(assign.RightAssignment!);
            _secondPass.Module.Store(ptr, right);
            return right;
        }
        
        private uint GenerateConstantExpression(ConstantExpressionNode constExpr)
        {
            if (constExpr.BinaryExpression != null)
                return GenerateBinaryExpression(constExpr.BinaryExpression);
            if (constExpr.Condition != null && constExpr.TrueExpression != null && constExpr.FalseExpression != null)
            {
                var condVal = GenerateExpression(constExpr.Condition);
                var trueVal = GenerateExpression(constExpr.TrueExpression);
                var falseVal = GenerateExpression(constExpr.FalseExpression);
                var resultId = _secondPass.Module.GetNextId();
                var resultType = GetExpressionType(constExpr.TrueExpression);
                _secondPass.Module.AddInstruction(new Instruction
                {
                    Opcode = Opcode.OpSelect,
                    ResultType = _secondPass.MapType(resultType),
                    ResultId = resultId,
                    Operands = { condVal, trueVal, falseVal }
                });
                return resultId;
            }
            throw new NotImplementedException("Unsupported constant expression");
        }

        private uint GenerateConstructorExpression(ConstructorExpressionNode ctor)
        {
            var type = TypeResolver.GetTypeFromTypeSpecifier(ctor.TypeSpecifier, _firstPass);
            var args = ctor.Parameters.AssignmentExpressions.Select(a => GenerateExpression(a)).ToArray();
            var resultId = _secondPass.Module.GetNextId();
            var inst = new Instruction { Opcode = Opcode.OpCompositeConstruct, ResultType = _secondPass.MapType(type), ResultId = resultId };
            foreach (var arg in args) inst.Operands.Add(arg);
            _secondPass.Module.AddInstruction(inst);
            return resultId;
        }

        private uint GenerateFieldAccess(FieldAccessExpressionNode field)
        {
            var baseVal = GenerateExpression(field.Base);
            var baseType = GetExpressionType(field.Base);
            var index = GetFieldIndex(baseType, field.FieldName);
            var extracted = _secondPass.Module.GetNextId();
            var fieldType = GetFieldType(baseType, field.FieldName);
            _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpCompositeExtract, ResultType = _secondPass.MapType(fieldType), ResultId = extracted, Operands = { baseVal, index } });
            return extracted;
        }

        private uint GeneratePostfixExpression(PostfixExpressionNode post)
        {
            if (post.HasIncOp || post.HasDecOp)
            {
                var ptr = GetPointer(post.PostfixExpression!);
                var loaded = _secondPass.Module.GetNextId();
                var valType = GetExpressionType(post.PostfixExpression!);
                _secondPass.Module.Load(_secondPass.MapType(valType), loaded, ptr);
                var one = _secondPass.GetConstantId(valType, 1);
                var updated = _secondPass.Module.GetNextId();
                var op = valType is IntType ? (post.HasIncOp ? Opcode.OpIAdd : Opcode.OpISub) : (post.HasIncOp ? Opcode.OpFAdd : Opcode.OpFSub);
                _secondPass.Module.AddInstruction(new Instruction { Opcode = op, ResultType = _secondPass.MapType(valType), ResultId = updated, Operands = { loaded, one } });
                _secondPass.Module.Store(ptr, updated);
                return loaded;
            }
            if (post.FunctionCallParameters != null)
            {
                var funcName = (post.PrimaryExpression?.Identifier?.Name) ?? throw new Exception();
                var funcSym = _firstPass.Symbols.Lookup(funcName);
                if (funcSym == null || funcSym.Kind != SymbolKind.Function) throw new Exception();
                var funcId = GetOrCreateFunctionId(funcSym);
                var args = post.FunctionCallParameters.AssignmentExpressions.Select(a => GenerateExpression(a)).ToArray();
                var callId = _secondPass.Module.GetNextId();
                var retTypeId = _secondPass.MapType(funcSym.Type);
                var callInst = new Instruction { Opcode = Opcode.OpFunctionCall, ResultType = retTypeId, ResultId = callId, Operands = { funcId } };
                foreach (var arg in args) callInst.Operands.Add(arg);
                _secondPass.Module.AddInstruction(callInst);
                return callId;
            }
            if (post.ArrayIndexExpression != null)
            {
                var basePtr = GetPointer(post.PostfixExpression!);
                var indexExpr = GenerateExpression(new PrimaryExpressionNode { IntConstant = post.ArrayIndexExpression });
                var elemPtr = _secondPass.Module.GetNextId();
                var elemType = (GetExpressionType(post.PostfixExpression!) as ArrayType)?.ElementType ?? throw new Exception();
                var ptrType = new PointerType(StorageClass.Function, elemType);
                _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpAccessChain, ResultType = _secondPass.MapType(ptrType), ResultId = elemPtr, Operands = { basePtr, indexExpr } });
                var loaded = _secondPass.Module.GetNextId();
                _secondPass.Module.Load(_secondPass.MapType(elemType), loaded, elemPtr);
                return loaded;
            }
            if (post.PrimaryExpression != null)
                return GenerateExpression(post.PrimaryExpression);
            throw new NotImplementedException();
        }

        private void GenerateIfStatement(SelectionStatementNode ifStmt)
        {
            var cond = GenerateExpression(ifStmt.Condition);
            var mergeLabel = NewLabel("merge");
            var thenLabel = NewLabel("then");
            var elseLabel = ifStmt.ElseStatement != null ? NewLabel("else") : mergeLabel;

            _secondPass.Module.SelectionMerge(mergeLabel, 0);
            _secondPass.Module.BranchConditional(cond, thenLabel, elseLabel);

            _secondPass.Module.Label(thenLabel);
            _secondPass.CurrentBlock = thenLabel;
            BeginBlock();
            GenerateStatement(ifStmt.ThenStatement);
            if (!IsCurrentBlockTerminated()) _secondPass.Module.Branch(mergeLabel);

            if (ifStmt.ElseStatement != null)
            {
                _secondPass.Module.Label(elseLabel);
                _secondPass.CurrentBlock = elseLabel;
                BeginBlock();
                GenerateStatement(ifStmt.ElseStatement);
                if (!IsCurrentBlockTerminated()) _secondPass.Module.Branch(mergeLabel);
            }

            _secondPass.Module.Label(mergeLabel);
            _secondPass.CurrentBlock = mergeLabel;
        }

        private void GenerateLoop(IterationStatementNode loop)
        {
            var headerLabel = NewLabel("loop_header");
            var mergeLabel = NewLabel("loop_merge");
            var continueLabel = NewLabel("loop_continue");

            _secondPass.Module.Branch(headerLabel);
            _secondPass.Module.Label(headerLabel);
            _secondPass.CurrentBlock = headerLabel;
            BeginBlock();
            _secondPass.Module.LoopMerge(mergeLabel, continueLabel, 0);

            if (loop.Type == IterationType.While && loop.WhileCondition != null)
            {
                var cond = GenerateExpression(loop.WhileCondition.Expression!);
                var bodyLabel = NewLabel("loop_body");
                _secondPass.Module.BranchConditional(cond, bodyLabel, mergeLabel);
                _secondPass.Module.Label(bodyLabel);
                _secondPass.CurrentBlock = bodyLabel;
                BeginBlock();
                GenerateStatement(loop.WhileBody!);
                if (!IsCurrentBlockTerminated()) _secondPass.Module.Branch(continueLabel);
                _secondPass.Module.Label(continueLabel);
                _secondPass.CurrentBlock = continueLabel;
                _secondPass.Module.Branch(headerLabel);
            }
            else if (loop.Type == IterationType.For && loop.ForRest != null)
            {
                var cond = loop.ForRest.Condition != null ? GenerateExpression(loop.ForRest.Condition.Expression!) : _secondPass.GetConstantId(new BoolType(), true);
                var bodyLabel = NewLabel("for_body");
                _secondPass.Module.BranchConditional(cond, bodyLabel, mergeLabel);
                _secondPass.Module.Label(bodyLabel);
                _secondPass.CurrentBlock = bodyLabel;
                BeginBlock();
                GenerateStatement(loop.ForBody!);
                if (!IsCurrentBlockTerminated()) _secondPass.Module.Branch(continueLabel);
                _secondPass.Module.Label(continueLabel);
                _secondPass.CurrentBlock = continueLabel;
                if (loop.ForRest.Expression != null) GenerateExpression(loop.ForRest.Expression);
                _secondPass.Module.Branch(headerLabel);
            }
            else throw new NotImplementedException();

            _secondPass.Module.Label(mergeLabel);
            _secondPass.CurrentBlock = mergeLabel;
        }

        private void GenerateJump(JumpStatementNode jump)
        {
            switch (jump.Type)
            {
                case JumpType.Return:
                    if (jump.ReturnExpression != null)
                    {
                        var val = GenerateExpression(jump.ReturnExpression);
                        _secondPass.Module.ReturnValue(val);
                    }
                    else _secondPass.Module.Return();
                    break;
                case JumpType.Break:
                    if (_secondPass.CurrentMergeLabel.HasValue)
                        _secondPass.Module.Branch(_secondPass.CurrentMergeLabel.Value);
                    else throw new Exception("Break outside loop");
                    break;
                case JumpType.Continue:
                    if (_secondPass.CurrentContinueTarget.HasValue)
                        _secondPass.Module.Branch(_secondPass.CurrentContinueTarget.Value);
                    else throw new Exception("Continue outside loop");
                    break;
                case JumpType.Discard:
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTerminateInvocation });
                    break;
            }
        }

        private SpirvType GetExpressionType(ExpressionNode expr)
        {
            if (expr is IdentifierExpressionNode id)
                return _secondPass.Lookup(id.Name)!.Type;
            if (expr is PrimaryExpressionNode prim)
            {
                if (prim.Identifier != null) return _secondPass.Lookup(prim.Identifier.Name)!.Type;
                if (prim.BooleanValue.HasValue) return new BoolType();
                if (prim.IntConstant != null) return new IntType(32, true);
                if (prim.UintConstant != null) return new IntType(32, false);
                if (prim.FloatConstant != null) return new FloatType(32);
                if (prim.DoubleConstant != null) return new FloatType(64);
                if (prim.ParenthesizedExpression != null) return GetExpressionType(prim.ParenthesizedExpression);
            }
            if (expr is BinaryExpressionNode bin) 
                return GetExpressionType(bin.Left);
            if (expr is UnaryExpressionNode unary)
            {
                if (unary.Operand != null)
                    return GetExpressionType(unary.Operand);
                if (unary.PostfixExpression != null)
                    return GetExpressionType(unary.PostfixExpression);
            }
            if (expr is ConstructorExpressionNode ctor) 
                return TypeResolver.GetTypeFromTypeSpecifier(ctor.TypeSpecifier, _firstPass);
            if (expr is FieldAccessExpressionNode field) 
                return GetFieldType(GetExpressionType(field.Base), field.FieldName);
            if (expr is PostfixExpressionNode post)
            {
                if (post.PostfixExpression != null)
                {
                    var baseType = GetExpressionType(post.PostfixExpression);
                    if (post.ArrayIndexExpression != null)
                    {
                        if (baseType is ArrayType arrType)
                            return arrType.ElementType;
                        if (baseType is PointerType ptrType && ptrType.PointeeType is ArrayType ptrArrType)
                            return ptrArrType.ElementType;
                    }
                    if (post.FieldSelection?.Identifier != null)
                        return GetFieldType(baseType, post.FieldSelection.Identifier.Name);
                    return baseType;
                }
                if (post.PrimaryExpression != null)
                {
                    if (post.FunctionCallParameters != null)
                    {
                        if (post.PrimaryExpression.Identifier != null)
                        {
                            var funcSym = _firstPass.Symbols.Lookup(post.PrimaryExpression.Identifier.Name);
                            if (funcSym?.Kind == SymbolKind.Function)
                                return funcSym.Type;
                        }
                        if (post.ConstructorType != null)
                            return TypeResolver.GetTypeFromTypeSpecifier(post.ConstructorType, _firstPass);
                    }
                    return GetExpressionType(post.PrimaryExpression);
                }
                if (post.ArrayIndexExpression != null && post.PostfixExpression == null)
                {
                    throw new NotImplementedException();
                }
            }
            if (expr is AssignmentExpressionNode assign)
            {
                if (assign.RightAssignment != null)
                    return GetExpressionType(assign.RightAssignment);
                if (assign.ConstantExpression != null)
                    return GetExpressionType(assign.ConstantExpression);
                if (assign.LeftUnary != null)
                    return GetExpressionType(assign.LeftUnary);
            }
            if (expr is ConstantExpressionNode constExpr)
            {
                if (constExpr.BinaryExpression != null)
                    return GetExpressionType(constExpr.BinaryExpression);
                if (constExpr.TrueExpression != null)
                    return GetExpressionType(constExpr.TrueExpression);
                if (constExpr.FalseExpression != null)
                    return GetExpressionType(constExpr.FalseExpression);
            }
            throw new NotImplementedException($"GetExpressionType for {expr.GetType()}");
        }

        private uint GetFieldIndex(SpirvType containerType, string fieldName)
        {
            if (containerType is StructType st)
            {
                return _secondPass.GetConstantId(new IntType(32, false), 0u);
            }
            if (containerType is VectorType vt && fieldName.Length == 1)
            {
                var idx = fieldName[0] switch { 'x' => 0, 'y' => 1, 'z' => 2, 'w' => 3, _ => 0 };
                return _secondPass.GetConstantId(new IntType(32, false), (uint)idx);
            }
            throw new NotImplementedException();
        }

        private SpirvType GetFieldType(SpirvType containerType, string fieldName)
        {
            if (containerType is StructType st)
            {
                return st.MemberTypes.FirstOrDefault() ?? throw new Exception();
            }
            if (containerType is VectorType vt && fieldName.Length == 1)
                return vt.ComponentType;
            if (containerType is MatrixType mt && fieldName.Length == 1)
                return mt.ColumnType;
            throw new NotImplementedException();
        }

        private uint NewLabel(string prefix) => _secondPass.Module.GetNextId();
        
        private void BeginBlock()
        {
            _lastInstructionCountBeforeBlock = (uint)_secondPass.Module.Instructions.Count;
        }

        private bool IsCurrentBlockTerminated()
        {
            if (_secondPass.Module.Instructions.Count <= _lastInstructionCountBeforeBlock)
                return false;
            var last = _secondPass.Module.Instructions.Last();
            var termOps = new[] { Opcode.OpReturn, Opcode.OpReturnValue, Opcode.OpBranch, Opcode.OpBranchConditional, Opcode.OpKill, Opcode.OpUnreachable, Opcode.OpTerminateInvocation };
            return termOps.Contains(last.Opcode);
        }
    }
}