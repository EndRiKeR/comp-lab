using comp_lab.CourseWork._2._AstToTables;
using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._3._AstToBinary
{
    public class SpirVGenerator
    {
        private FirstPassContext _firstPass = null!;
        private SecondPassContext _secondPass = null!;
        private readonly Dictionary<SymbolInfo, uint> _functionSymbolIds = new();
        private readonly Dictionary<SymbolInfo, uint> _functionTypeIds = new();
        
        private uint _lastInstructionCountBeforeBlock;
        private readonly HashSet<SpirvType> _emittedTypes = new();
        private readonly Dictionary<SymbolInfo, (uint returnTypeId, uint[] paramTypeIds)> _pendingFunctionTypeInfo = new();

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
            
            // Переносим все литералы, собранные на первом проходе, во второй проход
            foreach (var (type, value) in _firstPass.RequiredConstants)
            {
                _secondPass.GetConstantId(type, value);
            }

            EmitHeader();                       // Заголовок
            
            var intType = new IntType(32, false);
            _secondPass.GetConstantId(intType, 0u);
            _secondPass.GetConstantId(intType, 1u);
            _secondPass.EmitPendingConstants();
            
            var mainSymbol = _firstPass.Symbols.Lookup(entryPointName);
            if (mainSymbol == null || mainSymbol.Kind != SymbolKind.Function)
                throw new Exception("Entry point not found");
            GetOrCreateFunctionId(mainSymbol);   // только ID, не OpFunction
            
            ReserveGlobalVariableIds();
            EmitEntryPoint(entryPointName);     // Точка входа (main)
            
            EnsureEntryPointTypes();            // Добавляем базу
            CollectGlobalPointerTypes();
            EmitTypeDeclarations();             // Типы
            
            EmitDecorations();

            EmitUniformVariables();             // In, out, uniform
            EmitGlobalVariables();              // Глобальные переменные
            
            // EmitDebugNames();                   // Дебаг переменные
            
            EmitFunctionTypes();
            
            _secondPass.EmitPendingConstants(); // Константы
            EmitFunctions(ast);                 // Функции
            
            _secondPass.Module.SetBound();
        }
        
        private void CollectGlobalPointerTypes()
        {
            foreach (var (_, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind == SymbolKind.Variable && symbol.StorageClass.HasValue)
                {
                    var ptrType = new PointerType(symbol.StorageClass.Value, symbol.Type);
                    _firstPass.Types.AddType(ptrType);   // рекурсивно добавит и базовый тип
                    AddNestedTypes(ptrType);
                }
            }
        }

        private void EmitHeader()
        {
            _secondPass.Module.Capability(1); // Shader
            var glslStdId = _secondPass.Module.GetNextId();
            _secondPass.Module.ExtInstImport(glslStdId, "GLSL.std.450");
            _secondPass.ImportedSetIds["GLSL.std.450"] = glslStdId;
            _secondPass.Module.MemoryModel(0, 1); // Logical, GLSL450
        }

        private void EmitEntryPoint(string entryPointName)
        {
            var mainSymbol = _firstPass.Symbols.Lookup(entryPointName);
            if (mainSymbol == null || mainSymbol.Kind != SymbolKind.Function)
                throw new Exception("Entry point not found");
            var mainId = GetOrCreateFunctionId(mainSymbol);

            // 7. OpEntryPoint и OpExecutionMode
            uint execModel = 4;
            bool isCompute = _firstPass.LocalSizeX.HasValue;
            if (isCompute)
                execModel = 5; // GLCompute
            
            var interfaceIds = CollectInterfaceVariables();
            
            var entryInst = new Instruction { Opcode = Opcode.OpEntryPoint, Operands = { execModel, mainId, entryPointName } };
            foreach (var id in interfaceIds)
                entryInst.Operands.Add(id);
            
            _secondPass.Module.AddInstruction(entryInst);
            
            if (isCompute)
            {
                var localSizeX = _firstPass.LocalSizeX ?? 1;
                var localSizeY = _firstPass.LocalSizeY ?? 1;
                var localSizeZ = _firstPass.LocalSizeZ ?? 1;
                _secondPass.Module.AddInstruction(new Instruction
                {
                    Opcode = Opcode.OpExecutionMode,
                    Operands = { mainId, 17u, localSizeX, localSizeY, localSizeZ }
                });
            }
            else
            {
                _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpExecutionMode, Operands = { mainId, 7u } });
            }
        }
        
        private void EnsureEntryPointTypes()
        {
            _firstPass.Types.AddType(new BoolType());

            foreach (var (_, sym) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (sym.Kind == SymbolKind.Variable && sym.Type != null)
                    _firstPass.Types.AddType(sym.Type);
                else if (sym.Kind == SymbolKind.Function && sym.Type != null)
                    _firstPass.Types.AddType(sym.Type);
            }

            // Рекурсивно добавить типы-компоненты (например, для PointerType, ArrayType)
            var allTypes = _firstPass.Types.AllTypes.ToList();
            foreach (var type in allTypes)
            {
                AddNestedTypes(type);
            }
        }

        private void AddNestedTypes(SpirvType type)
        {
            switch (type)
            {
                case VectorType vt:
                    _firstPass.Types.AddType(vt.ComponentType);
                    AddNestedTypes(vt.ComponentType);
                    break;
                case MatrixType mt:
                    _firstPass.Types.AddType(mt.ColumnType);
                    AddNestedTypes(mt.ColumnType);
                    break;
                case ArrayType at:
                    _firstPass.Types.AddType(at.ElementType);
                    AddNestedTypes(at.ElementType);
                    break;
                case PointerType pt:
                    _firstPass.Types.AddType(pt.PointeeType);
                    AddNestedTypes(pt.PointeeType);
                    break;
                case StructType st:
                    foreach (var member in st.MemberTypes)
                    {
                        _firstPass.Types.AddType(member);
                        AddNestedTypes(member);
                    }
                    break;
            }
        }

        private void EmitTypeDeclarations()
        {
            foreach (var type in _firstPass.Types.AllTypes)
            {
                if (!_emittedTypes.Contains(type))
                    EmitType(type);
            }
        }

        private void EmitFunctionTypes()
        {
            foreach (var (funcSymbol, typeInfo) in _pendingFunctionTypeInfo)
            {
                var (returnTypeId, paramTypeIds) = typeInfo;
                var funcTypeInst = new Instruction
                {
                    Opcode = Opcode.OpTypeFunction,
                    ResultId = _secondPass.Module.GetNextId(),
                    Operands = { returnTypeId }
                };
                foreach (var pt in paramTypeIds)
                    funcTypeInst.Operands.Add(pt);
                
                _secondPass.Module.AddInstruction(funcTypeInst);
                _functionTypeIds[funcSymbol] = funcTypeInst.ResultId.Value;
            }
        }

        private void EmitType(SpirvType type)
        {
            if (_emittedTypes.Contains(type))
                return;

            switch (type)
            {
                case VectorType vt:
                    EmitType(vt.ComponentType);
                    break;
                case MatrixType mt:
                    EmitType(mt.ColumnType);
                    break;
                case ArrayType at:
                    EmitType(at.ElementType);
                    break;
                case PointerType pt:
                    EmitType(pt.PointeeType);
                    break;
                case StructType st:
                    foreach (var member in st.MemberTypes)
                        EmitType(member);
                    break;
            }
            
            var id = _secondPass.MapType(type);
            switch (type)
            {
                case VoidType:
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeVoid, ResultId = id });
                    break;
                case BoolType:
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeBool, ResultId = id });
                    break;
                case IntType it:
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeInt, ResultId = id, Operands = { it.Width, it.Signed ? 1u : 0u } });
                    break;
                case FloatType ft:
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeFloat, ResultId = id, Operands = { ft.Width } });
                    break;
                case VectorType vt:
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeVector, ResultId = id, Operands = { _secondPass.MapType(vt.ComponentType), vt.ComponentCount } });
                    break;
                case MatrixType mt:
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeMatrix, ResultId = id, Operands = { _secondPass.MapType(mt.ColumnType), mt.ColumnCount } });
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
                    foreach (var mid in memberIds)
                        structInst.Operands.Add(mid);
                    _secondPass.Module.AddInstruction(structInst);
                    for (int i = 0; i < st.MemberNames.Count; i++)
                    {
                        _secondPass.Module.AddInstruction(new Instruction
                        {
                            Opcode = Opcode.OpMemberName,
                            Operands = { id, (uint)i, st.MemberNames[i] }
                        });
                    }
                    break;
                case PointerType pt:
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypePointer, ResultId = id, Operands = { (uint)pt.StorageClass, _secondPass.MapType(pt.PointeeType) } });
                    break;
                default: throw new NotSupportedException($"Type {type.GetType()}");
            }
            _emittedTypes.Add(type);
        }

        private uint CreateConstantInt(int width, bool signed, uint value)
        {
            var intType = new IntType(width, signed);
            if (!_firstPass.Types.Contains(intType))
            {
                _firstPass.Types.AddType(intType);
                EmitType(intType);
            }
            return _secondPass.GetConstantId(intType, value);
        }
        
        private void ReserveGlobalVariableIds()
        {
            foreach (var (_, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind == SymbolKind.Variable)
                {
                    symbol.Id = _secondPass.Module.GetNextId(); // резервируем ID
                }
            }
        }

        private void EmitGlobalVariables()
        {
            foreach (var (_, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind != SymbolKind.Variable ||
                    symbol.StorageClass == StorageClass.Input ||
                    symbol.StorageClass == StorageClass.Output ||
                    symbol.StorageClass == StorageClass.Uniform ||
                    symbol.StorageClass == StorageClass.StorageBuffer)
                    continue;
                
                var ptrType = new PointerType(symbol.StorageClass!.Value, symbol.Type);
                var ptrTypeId = _secondPass.MapType(ptrType);
                var varId = symbol.Id!.Value;
                _secondPass.Module.Variable(ptrTypeId, varId, (uint)symbol.StorageClass.Value, null);
            }
        }
        
        private void EmitUniformVariables()
        {
            uint bindingIndex = 0;
            foreach (var (_, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind == SymbolKind.Variable && 
                    (symbol.StorageClass == StorageClass.Input || 
                     symbol.StorageClass == StorageClass.Output ||
                     symbol.StorageClass == StorageClass.Uniform ||
                     symbol.StorageClass == StorageClass.StorageBuffer))
                {
                    var ptrType = new PointerType(symbol.StorageClass!.Value, symbol.Type);
                    var ptrTypeId = _secondPass.MapType(ptrType);
                    var varId = symbol.Id!.Value;
                    _secondPass.Module.Variable(ptrTypeId, varId, (uint)symbol.StorageClass.Value, null);
                }
            }
        }
        
        private void EmitDecorations()
        {
            uint bindingIndex = 0;
            foreach (var (_, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind == SymbolKind.Variable &&
                    (symbol.StorageClass == StorageClass.Uniform || symbol.StorageClass == StorageClass.StorageBuffer))
                {
                    var varId = symbol.Id!.Value;
                    _secondPass.Module.Decorate(varId, 34u, 0u); // DescriptorSet 0
                    _secondPass.Module.Decorate(varId, 33u, bindingIndex++);
            
                    // Добавить Block decoration для структуры, на которую указывает переменная
                    if (symbol.Type is PointerType ptrType && ptrType.PointeeType is StructType structType)
                    {
                        var structId = _secondPass.MapType(structType);
                        _secondPass.Module.Decorate(structId, 25u); // Block
                        // Добавить смещения для членов
                        uint offset = 0;
                        for (int i = 0; i < structType.MemberTypes.Count; i++)
                        {
                            uint size = GetTypeSize(structType.MemberTypes[i]);
                            _secondPass.Module.MemberDecorate(structId, (uint)i, 12u, offset); // Offset
                            offset += size;
                        }
                    }
                }
            }
        }

        private uint GetTypeSize(SpirvType type)
        {
            return type switch
            {
                FloatType => 4,
                IntType => 4,
                VectorType vt => 4 * (uint)vt.ComponentCount,
                StructType st => (uint)st.MemberTypes.Sum(m => GetTypeSize(m)),
                _ => 4
            };
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

        private List<uint> CollectInterfaceVariables()
        {
            var list = new List<uint>();
            foreach (var (_, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind == SymbolKind.Variable && 
                    (symbol.StorageClass == StorageClass.Input || 
                     symbol.StorageClass == StorageClass.Output ||
                     symbol.StorageClass == StorageClass.Uniform ||
                     symbol.StorageClass == StorageClass.StorageBuffer) &&
                    symbol.Id.HasValue)
                {
                    list.Add(symbol.Id.Value);
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

            _pendingFunctionTypeInfo[funcSymbol] = (returnTypeId, paramTypeIds);

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
            _secondPass.EmitPendingConstants();
            var funcName = funcDef.Prototype.Name.Name;
            var funcSymbol = _firstPass.Symbols.Lookup(funcName);
            
            if (funcSymbol == null)
                throw new Exception("Function symbol not found");
            
            var funcId = GetOrCreateFunctionId(funcSymbol);
            var returnTypeId = _secondPass.MapType(funcSymbol.Type);
            var funcTypeId = _functionTypeIds[funcSymbol];
            _secondPass.Module.Function(returnTypeId, funcId, 0, funcTypeId);

            _secondPass.EnterLocalScope();
            if (funcDef.Prototype.Parameters != null)
            {
                foreach (var param in funcDef.Prototype.Parameters.Parameters)
                {
                    var paramType = TypeResolver.GetTypeFromParameterDeclaration(param, _firstPass);
                    var paramPtrType = new PointerType(StorageClass.Function, paramType);
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

            var entryLabel = _secondPass.Module.GetNextId();
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
                        var initVal = GenerateExpression(single.TypelessDeclaration.Initializer.AssignmentExpression);
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
            
            Console.WriteLine($"Binary: left type = {leftType?.GetType().Name}, operator = {bin.Operator}");
            
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
            bool isComparison = bin.Operator is "==" or "!=" or "<" or ">" or "<=" or ">=";
            var resultType = isComparison ? new BoolType() : leftType;
            var resTypeId = _secondPass.MapType(resultType);
            var resultId = _secondPass.Module.GetNextId();
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
                _secondPass.Module.AddInstruction(new Instruction
                {
                    Opcode = Opcode.OpAccessChain,
                    ResultType = _secondPass.MapType(ptrType),
                    ResultId = ptrId,
                    Operands = { basePtr, index }
                });
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
                // Простой идентификатор без операций
                if (post.PrimaryExpression != null &&
                    post.ArrayIndexExpression == null &&
                    post.FieldSelection == null &&
                    post.PostfixExpression == null)
                {
                    return GetPointer(post.PrimaryExpression);
                }

                // Получаем базовый указатель
                uint basePtr;
                ExpressionNode baseExpr;
                if (post.PostfixExpression != null)
                {
                    basePtr = GetPointer(post.PostfixExpression);
                    baseExpr = post.PostfixExpression;
                }
                else if (post.PrimaryExpression != null)
                {
                    basePtr = GetPointer(post.PrimaryExpression);
                    baseExpr = post.PrimaryExpression;
                }
                else
                    throw new NotImplementedException("PostfixExpression without base");

                // Текущий тип после обработки базовой части
                var currentType = GetExpressionType(baseExpr);
                var indices = new List<uint>();

                // Индексация массива
                if (post.ArrayIndexExpression != null)
                {
                    // Для простоты – константный индекс. При необходимости доработайте для выражений.
                    if (uint.TryParse(post.ArrayIndexExpression, out uint constIndex))
                        indices.Add(_secondPass.GetConstantId(new IntType(32, false), constIndex));
                    else
                        throw new NotImplementedException("Non-constant array index in lvalue");

                    if (currentType is ArrayType arrType)
                        currentType = arrType.ElementType;
                    else if (currentType is PointerType ptrType && ptrType.PointeeType is ArrayType ptrArrType)
                        currentType = ptrArrType.ElementType;
                    else
                        throw new InvalidOperationException("Array index applied to non-array type");
                }

                // Доступ к полю
                if (post.FieldSelection?.Identifier != null)
                {
                    var fieldName = post.FieldSelection.Identifier.Name;
                    indices.Add(GetFieldIndex(currentType, fieldName));
                    currentType = GetFieldType(currentType, fieldName);
                }

                // Если нет индексов – возвращаем базовый указатель
                if (indices.Count == 0)
                    return basePtr;

                // Генерируем OpAccessChain
                var resultPtrId = _secondPass.Module.GetNextId();
                ExpressionNode baseExpressionNode;
                if (post.PostfixExpression != null)
                    baseExpressionNode = post.PostfixExpression;
                else if (post.PrimaryExpression != null)
                    baseExpressionNode = post.PrimaryExpression;
                else
                    throw new InvalidOperationException("PostfixExpression has no base expression");
                var resultPtrType = new PointerType(GetPointerStorageClass(baseExpressionNode), currentType);
                var accessInst = new Instruction
                {
                    Opcode = Opcode.OpAccessChain,
                    ResultType = _secondPass.MapType(resultPtrType),
                    ResultId = resultPtrId,
                    Operands = { basePtr }
                };
                foreach (var idx in indices)
                    accessInst.Operands.Add(idx);
                _secondPass.Module.AddInstruction(accessInst);
                return resultPtrId;
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
            // Постфиксный инкремент/декремент
            if (post.HasIncOp || post.HasDecOp)
            {
                var ptr = GetPointer(post.PostfixExpression!);
                var loaded = _secondPass.Module.GetNextId();
                var valType = GetExpressionType(post.PostfixExpression!);
                _secondPass.Module.Load(_secondPass.MapType(valType), loaded, ptr);
                var one = _secondPass.GetConstantId(valType, 1);
                var updated = _secondPass.Module.GetNextId();
                var op = valType is IntType
                    ? (post.HasIncOp ? Opcode.OpIAdd : Opcode.OpISub)
                    : (post.HasIncOp ? Opcode.OpFAdd : Opcode.OpFSub);
                _secondPass.Module.AddInstruction(new Instruction
                {
                    Opcode = op,
                    ResultType = _secondPass.MapType(valType),
                    ResultId = updated,
                    Operands = { loaded, one }
                });
                _secondPass.Module.Store(ptr, updated);
                return loaded; // возвращаем старое значение
            }

            // Вызов функции или конструктор
            if (post.FunctionCallParameters != null)
            {
                if (post.PrimaryExpression?.Identifier != null)
                {
                    // Обычный вызов функции
                    var funcName = post.PrimaryExpression.Identifier.Name;
                    var funcSym = _firstPass.Symbols.Lookup(funcName);
                    if (funcSym == null || funcSym.Kind != SymbolKind.Function)
                        throw new Exception($"Function {funcName} not found");
                    var funcId = GetOrCreateFunctionId(funcSym);
                    var args = post.FunctionCallParameters.AssignmentExpressions
                        .Select(a => GenerateExpression(a)).ToArray();
                    var callId = _secondPass.Module.GetNextId();
                    var retTypeId = _secondPass.MapType(funcSym.Type);
                    var callInst = new Instruction
                    {
                        Opcode = Opcode.OpFunctionCall,
                        ResultType = retTypeId,
                        ResultId = callId,
                        Operands = { funcId }
                    };
                    foreach (var arg in args) callInst.Operands.Add(arg);
                    _secondPass.Module.AddInstruction(callInst);
                    return callId;
                }
                else if (post.ConstructorType != null)
                {
                    // Конструктор типа (например, vec3(1,2,3))
                    var type = TypeResolver.GetTypeFromTypeSpecifier(post.ConstructorType, _firstPass);
                    var args = post.FunctionCallParameters.AssignmentExpressions
                        .Select(a => GenerateExpression(a)).ToArray();
                    var constrId = _secondPass.Module.GetNextId();
                    var inst = new Instruction
                    {
                        Opcode = Opcode.OpCompositeConstruct,
                        ResultType = _secondPass.MapType(type),
                        ResultId = constrId
                    };
                    foreach (var arg in args) inst.Operands.Add(arg);
                    _secondPass.Module.AddInstruction(inst);
                    return constrId;
                }
                else
                    throw new NotImplementedException("Function call without identifier or constructor type");
            }

            // Доступ к элементу массива
            if (post.ArrayIndexExpression != null)
            {
                var basePtr = GetPointer(post.PostfixExpression!);
                uint indexId;
                if (uint.TryParse(post.ArrayIndexExpression, out uint constIndex))
                    indexId = _secondPass.GetConstantId(new IntType(32, false), constIndex);
                else
                {
                    // Если индекс – выражение, вычислить его (упрощённо)
                    var indexExpr = new PrimaryExpressionNode { IntConstant = post.ArrayIndexExpression };
                    indexId = GenerateExpression(indexExpr);
                }
                var baseType = GetExpressionType(post.PostfixExpression!);
                SpirvType elemType;
                if (baseType is ArrayType arrType)
                    elemType = arrType.ElementType;
                else if (baseType is PointerType ptrType && ptrType.PointeeType is ArrayType ptrArrType)
                    elemType = ptrArrType.ElementType;
                else
                    throw new InvalidOperationException("Array index on non-array");

                var elemPtr = _secondPass.Module.GetNextId();
                var ptrTypeSpv = new PointerType(StorageClass.Function, elemType);
                _secondPass.Module.AddInstruction(new Instruction
                {
                    Opcode = Opcode.OpAccessChain,
                    ResultType = _secondPass.MapType(ptrTypeSpv),
                    ResultId = elemPtr,
                    Operands = { basePtr, indexId }
                });
                var loaded = _secondPass.Module.GetNextId();
                _secondPass.Module.Load(_secondPass.MapType(elemType), loaded, elemPtr);
                return loaded;
            }

            // Доступ к полю структуры (field selection)
            if (post.FieldSelection?.Identifier != null)
            {
                ExpressionNode baseExpr;
                if (post.PostfixExpression != null)
                    baseExpr = post.PostfixExpression;
                else if (post.PrimaryExpression != null)
                    baseExpr = post.PrimaryExpression;
                else
                    throw new NotImplementedException("Field selection without base expression");

                var basePtr = GetPointer(baseExpr);
                var baseType = GetExpressionType(baseExpr);
                var fieldName = post.FieldSelection.Identifier.Name;
                var index = GetFieldIndex(baseType, fieldName);
                var fieldType = GetFieldType(baseType, fieldName);
                var fieldPtr = _secondPass.Module.GetNextId();
                var ptrTypeSpv = new PointerType(GetPointerStorageClass(baseExpr), fieldType);
                _secondPass.Module.AddInstruction(new Instruction
                {
                    Opcode = Opcode.OpAccessChain,
                    ResultType = _secondPass.MapType(ptrTypeSpv),
                    ResultId = fieldPtr,
                    Operands = { basePtr, index }
                });
                var loaded = _secondPass.Module.GetNextId();
                _secondPass.Module.Load(_secondPass.MapType(fieldType), loaded, fieldPtr);
                return loaded;
            }

            // Просто primary_expression (идентификатор)
            if (post.PrimaryExpression != null)
                return GenerateExpression(post.PrimaryExpression);

            // Рекурсивная обработка цепочки без операций (на случай лишней вложенности)
            if (post.PostfixExpression != null && post.ArrayIndexExpression == null &&
                post.FieldSelection == null && !post.HasIncOp && !post.HasDecOp &&
                post.FunctionCallParameters == null)
            {
                return GenerateExpression(post.PostfixExpression);
            }

            throw new NotImplementedException("Unhandled PostfixExpression");
        }

        private void GenerateIfStatement(SelectionStatementNode ifStmt)
        {
            var cond = GenerateExpression(ifStmt.Condition);
            var mergeLabel = _secondPass.Module.GetNextId();
            var thenLabel = _secondPass.Module.GetNextId();
            var elseLabel = ifStmt.ElseStatement != null ? _secondPass.Module.GetNextId() : mergeLabel;

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
            if (loop.Type == IterationType.For && loop.ForInit != null)
            {
                if (loop.ForInit.ExpressionStatement != null)
                    GenerateStatement(loop.ForInit.ExpressionStatement);
                else if (loop.ForInit.DeclarationStatement != null)
                    GenerateDeclarationStatement(loop.ForInit.DeclarationStatement);
            }
            
            var headerLabel = _secondPass.Module.GetNextId();
            var mergeLabel = _secondPass.Module.GetNextId();
            var continueLabel = _secondPass.Module.GetNextId();

            var prevMergeLabel = _secondPass.CurrentMergeLabel;
            var prevContinueTarget = _secondPass.CurrentContinueTarget;
            _secondPass.CurrentMergeLabel = mergeLabel;
            _secondPass.CurrentContinueTarget = continueLabel;

            _secondPass.Module.Branch(headerLabel);
            _secondPass.Module.Label(headerLabel);
            _secondPass.CurrentBlock = headerLabel;
            BeginBlock();
            _secondPass.Module.LoopMerge(mergeLabel, continueLabel, 0);

            if (loop.Type == IterationType.While && loop.WhileCondition != null)
            {
                var cond = GenerateExpression(loop.WhileCondition.Expression!);
                var bodyLabel = _secondPass.Module.GetNextId();
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
                var bodyLabel = _secondPass.Module.GetNextId();
                _secondPass.Module.BranchConditional(cond, bodyLabel, mergeLabel);
                _secondPass.Module.Label(bodyLabel);
                _secondPass.CurrentBlock = bodyLabel;
                BeginBlock();
                GenerateStatement(loop.ForBody!);
                
                if (!IsCurrentBlockTerminated())
                    _secondPass.Module.Branch(continueLabel);
                
                _secondPass.Module.Label(continueLabel);
                _secondPass.CurrentBlock = continueLabel;
                
                if (loop.ForRest.Expression != null)
                    GenerateExpression(loop.ForRest.Expression);
                
                _secondPass.Module.Branch(headerLabel);
            }
            else throw new NotImplementedException();

            _secondPass.Module.Label(mergeLabel);
            _secondPass.CurrentBlock = mergeLabel;

            _secondPass.CurrentMergeLabel = prevMergeLabel;
            _secondPass.CurrentContinueTarget = prevContinueTarget;
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
                if (prim.Identifier != null)
                {
                    var sym = _secondPass.Lookup(prim.Identifier.Name);
                    if (sym != null)
                    {
                        var type = sym.Type;
                        if (type is PointerType ptrType)
                            type = ptrType.PointeeType;
                        return type;
                    }
                }
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
                // Определяем базовое выражение
                ExpressionNode baseNode;
                if (post.PostfixExpression != null)
                    baseNode = post.PostfixExpression;
                else if (post.PrimaryExpression != null)
                    baseNode = post.PrimaryExpression;
                else
                    throw new InvalidOperationException("PostfixExpression has no base expression");

                // Получаем тип базового узла и разыменовываем указатели
                SpirvType baseType = GetDereferencedType(GetExpressionType(baseNode));

                // 1. Индексация массива
                if (post.ArrayIndexExpression != null)
                {
                    if (baseType is ArrayType arrType)
                        return arrType.ElementType;
                    if (baseType is PointerType ptrType && ptrType.PointeeType is ArrayType arrType2)
                        return arrType2.ElementType;
                    throw new InvalidOperationException("Array index on non-array type");
                }

                // 2. Доступ к полю структуры
                if (post.FieldSelection?.Identifier != null)
                {
                    return GetFieldType(baseType, post.FieldSelection.Identifier.Name);
                }

                // 3. Вызов функции или конструктор
                if (post.FunctionCallParameters != null)
                {
                    if (post.PrimaryExpression?.Identifier != null)
                    {
                        var funcSym = _firstPass.Symbols.Lookup(post.PrimaryExpression.Identifier.Name);
                        if (funcSym?.Kind == SymbolKind.Function)
                            return funcSym.Type;
                    }
                    else if (post.ConstructorType != null)
                    {
                        return TypeResolver.GetTypeFromTypeSpecifier(post.ConstructorType, _firstPass);
                    }
                }

                // 4. Постфиксный инкремент/декремент – тип не меняется
                if (post.HasIncOp || post.HasDecOp)
                {
                    return baseType;
                }

                // 5. В противном случае – тип базового узла
                return baseType;
            }
            
            throw new NotImplementedException($"GetExpressionType for {expr.GetType()}");
        }

        private uint GetFieldIndex(SpirvType containerType, string fieldName)
        {
            while (containerType is PointerType ptrType)
                containerType = ptrType.PointeeType;
            
            if (containerType is StructType st)
            {
                int idx = st.MemberNames.ToList().IndexOf(fieldName);
                if (idx == -1) throw new Exception($"Field '{fieldName}' not found in struct");
                return _secondPass.GetConstantId(new IntType(32, false), (uint)idx);
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
            while (containerType is PointerType ptrType)
                containerType = ptrType.PointeeType;
            
            if (containerType is StructType st)
            {
                int idx = st.MemberNames.ToList().IndexOf(fieldName);
                if (idx == -1) throw new Exception($"Field '{fieldName}' not found in struct");
                return st.MemberTypes[idx];
            }
            if (containerType is VectorType vt && fieldName.Length == 1)
                return vt.ComponentType;
            if (containerType is MatrixType mt && fieldName.Length == 1)
                return mt.ColumnType;
            throw new NotImplementedException();
        }
        
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
        
        private StorageClass GetPointerStorageClass(ExpressionNode expr)
        {
            // Сначала пытаемся получить символ для идентификатора
            SymbolInfo? sym = null;
            if (expr is IdentifierExpressionNode id)
                sym = _secondPass.Lookup(id.Name);
            else if (expr is PrimaryExpressionNode prim && prim.Identifier != null)
                sym = _secondPass.Lookup(prim.Identifier.Name);
            else if (expr is PostfixExpressionNode post)
            {
                // Рекурсивно спускаемся к базовому идентификатору
                ExpressionNode baseExpr;
                if (post.PostfixExpression != null)
                    baseExpr = post.PostfixExpression;
                else if (post.PrimaryExpression != null)
                    baseExpr = post.PrimaryExpression;
                else
                    throw new InvalidOperationException("PostfixExpression has no base expression");
                
                if (baseExpr != null)
                    return GetPointerStorageClass(baseExpr);
            }
    
            if (sym != null && sym.StorageClass.HasValue)
                return sym.StorageClass.Value;
    
            // Если не идентификатор – предполагаем, что тип выражения – указатель
            var type = GetExpressionType(expr);
            if (type is PointerType ptrType)
                return ptrType.StorageClass;
    
            throw new InvalidOperationException($"Expression is not a pointer: {expr.GetType()} - {type?.GetType()}");
        }
        
        private SpirvType GetDereferencedType(SpirvType type)
        {
            while (type is PointerType ptrType)
                type = ptrType.PointeeType;
            return type;
        }
    }
}