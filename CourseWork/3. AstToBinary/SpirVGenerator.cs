using comp_lab.CourseWork._2._AstToTables;
using comp_lab.CourseWork.Common;

namespace comp_lab.CourseWork._3._AstToBinary
{
    public class SpirVGenerator
    {
        private FirstPassContext _firstPass = null!;
        private SecondPassContext _secondPass = null!;
        
        private SpirvModule _spirvModule = null!;
        private SpirvModuleWorker _spirvModuleWorker = null!;
        
        private readonly Dictionary<SymbolInfo, uint> _functionSymbolIds = new();
        private readonly Dictionary<SymbolInfo, uint> _functionTypeIds = new();
        
        private uint _lastInstructionCountBeforeBlock;
        private readonly HashSet<SpirvType> _emittedTypes = new();
        private readonly Dictionary<SymbolInfo, (uint returnTypeId, uint[] paramTypeIds)> _pendingFunctionTypeInfo = new();

        public void GenerateAndSave(TranslationUnitNode ast, string entryPointName, string outputPath)
        {
            Generate(ast, entryPointName);
            var binary = _spirvModuleWorker.Serialize();
            File.WriteAllBytes(outputPath, binary);
            Console.WriteLine($"SPIR-V written to {outputPath}");
        }
        
        public void Generate(TranslationUnitNode ast, string entryPointName)
        {
            _firstPass = new FirstPassContext();
            FirstPassVisitor.Visit(ast, _firstPass);
            _spirvModule = new SpirvModule();
            _spirvModuleWorker = new SpirvModuleWorker(_spirvModule);
            _secondPass = new SecondPassContext(_firstPass, _spirvModuleWorker);
            
            AddGlobalPointerTypesToTypeCache();

            uint extInstId = _spirvModuleWorker.GetNextId();
            _secondPass.ImportedSetIds["GLSL.std.450"] = extInstId;

            var voidType = new VoidType();
            _firstPass.Types.AddType(voidType);
            uint voidTypeId = _secondPass.MapType(voidType);

            var mainSymbol = _firstPass.Symbols.Lookup(entryPointName);
            if (mainSymbol == null || mainSymbol.Kind != SymbolKind.Function)
                throw new Exception("Entry point not found");
            uint mainFuncTypeId = _spirvModuleWorker.GetNextId();
            _functionTypeIds[mainSymbol] = mainFuncTypeId;

            var returnTypeId = _secondPass.MapType(mainSymbol.Type);
            var paramTypeIds = mainSymbol.ParameterTypes?.Select(t => _secondPass.MapType(t)).ToArray() ?? Array.Empty<uint>();
            _pendingFunctionTypeInfo[mainSymbol] = (returnTypeId, paramTypeIds);

            uint mainId = _spirvModuleWorker.GetNextId();
            _functionSymbolIds[mainSymbol] = mainId;
            mainSymbol.Id = mainId;

            _secondPass.MapType(new BoolType());

            foreach (var type in _firstPass.Types.AllTypes)
                _secondPass.MapType(type);

            ReserveGlobalVariableIds();

            foreach (var (type, value) in _firstPass.RequiredConstants)
                _secondPass.GetConstantId(type, value);

            foreach (var decl in ast.Declarations)
                if (decl is FunctionDefinitionNode funcDef && funcDef.Prototype.Name.Name != entryPointName)
                    GetOrCreateFunctionId(_firstPass.Symbols.Lookup(funcDef.Prototype.Name.Name));

            _spirvModuleWorker.AddCapability(1);
            _spirvModuleWorker.AddExtInstImport(extInstId, "GLSL.std.450");
            _spirvModuleWorker.AddMemoryModel(0, 1);

            EmitEntryPoint(entryPointName);

            EmitPendingTypes();

            EmitFunctionTypes();

            EmitUniformVariables();
            EmitGlobalVariables();

            _secondPass.EmitPendingConstants();
            
            EmitDecorations();

            EmitFunctions(ast);
        }
        
        private void AddGlobalPointerTypesToTypeCache()
        {
            foreach (var (_, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind == SymbolKind.Variable && symbol.StorageClass.HasValue)
                {
                    var ptrType = new PointerType(symbol.StorageClass.Value, symbol.Type);
                    _firstPass.Types.AddType(ptrType);
                    AddNestedTypes(ptrType);
                }
                else if (symbol.Kind == SymbolKind.Function)
                {
                    _firstPass.Types.AddType(symbol.Type);
                    AddNestedTypes(symbol.Type);
                    if (symbol.ParameterTypes != null)
                    {
                        foreach (var paramType in symbol.ParameterTypes)
                        {
                            _firstPass.Types.AddType(paramType);
                            AddNestedTypes(paramType);
                        }
                    }
                }
            }
        }
        
        public void EmitPendingTypes()
        {
            foreach (var type in _secondPass.PendingTypes)
                EmitTypeInstruction(type);
            _secondPass.PendingTypes.Clear();
        }
        
        private void EmitTypeInstruction(SpirvType type)
        {
            if (_emittedTypes.Contains(type)) return;

            switch (type)
            {
                case VectorType vt:
                    EmitTypeInstruction(vt.ComponentType);
                    break;
                case MatrixType mt:
                    EmitTypeInstruction(mt.ColumnType);
                    break;
                case ArrayType at:
                    EmitTypeInstruction(at.ElementType);
                    break;
                case PointerType pt:
                    EmitTypeInstruction(pt.PointeeType);
                    break;
                case StructType st:
                    foreach (var m in st.MemberTypes)
                        EmitTypeInstruction(m);
                    break;
            }

            var id = _secondPass.TypeIdMap[type]; 
            switch (type)
            {
                case VoidType:
                    _spirvModuleWorker.AddTypeVoid(id);
                    break;
                case BoolType:
                    _spirvModuleWorker.AddTypeBool(id);
                    break;
                case IntType it:
                    _spirvModuleWorker.AddTypeInt(id, (uint)it.Width, it.Signed ? 1u : 0u);
                    break;
                case FloatType ft:
                    _spirvModuleWorker.AddTypeFloat(id, (uint)ft.Width);
                    break;
                case VectorType vt:
                    _spirvModuleWorker.AddTypeVector(id, _secondPass.TypeIdMap[vt.ComponentType], (uint)vt.ComponentCount);
                    break;
                case MatrixType mt:
                    _spirvModuleWorker.AddTypeMatrix(id, _secondPass.TypeIdMap[mt.ColumnType], (uint)mt.ColumnCount);
                    break;
                case ArrayType at:
                    var elemId = _secondPass.TypeIdMap[at.ElementType];
                    if (at.Length.HasValue)
                        _spirvModuleWorker.AddTypeArray(id, elemId, CreateConstantInt(32, false, at.Length.Value));
                    else
                        _spirvModuleWorker.AddTypeRuntimeArray(id, elemId);
                    break;
                case StructType st:
                    var memberIds = st.MemberTypes.Select(m => _secondPass.TypeIdMap[m]).ToArray();
                    _spirvModuleWorker.AddTypeStruct(id, memberIds);
                    for (int i = 0; i < st.MemberNames.Count; i++)
                        _spirvModuleWorker.AddMemberName(id, (uint)i, st.MemberNames[i]);
                    break;
                case PointerType pt:
                    _spirvModuleWorker.AddTypePointer(id, (uint)pt.StorageClass, _secondPass.TypeIdMap[pt.PointeeType]);
                    break;
                default:
                    throw new NotSupportedException($"Type {type.GetType()}");
            }
            _emittedTypes.Add(type);
        }

        private void EmitEntryPoint(string entryPointName)
        {
            var mainSymbol = _firstPass.Symbols.Lookup(entryPointName); 
            if (mainSymbol == null || mainSymbol.Kind != SymbolKind.Function)
                throw new Exception("Entry point not found");
            
            var mainId = GetOrCreateFunctionId(mainSymbol);

            uint execModel = 4;
            bool isCompute = _firstPass.LocalSizeX.HasValue;
            if (isCompute)
                execModel = 5;
            
            var interfaceIds = CollectInterfaceVariables();
            
            _spirvModuleWorker.AddEntryPoint(execModel, mainId, entryPointName, interfaceIds.ToArray());
            
            if (isCompute)
            {
                var localSizeX = _firstPass.LocalSizeX ?? 1;
                var localSizeY = _firstPass.LocalSizeY ?? 1;
                var localSizeZ = _firstPass.LocalSizeZ ?? 1;
                _spirvModuleWorker.AddExecutionMode(mainId, localSizeX, localSizeY, localSizeZ);
            }
            else
            {
                _spirvModuleWorker.AddExecutionMode(mainId);
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
            foreach (var (funcSymbol, (returnTypeId, paramTypeIds)) in _pendingFunctionTypeInfo)
            {
                var typeId = _functionTypeIds[funcSymbol];
                _spirvModuleWorker.AddTypeFunction(typeId, returnTypeId, paramTypeIds);
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
            _spirvModuleWorker.AddTypeVoid(id);
            break;
        case BoolType:
            _spirvModuleWorker.AddTypeBool(id);
            break;
        case IntType it:
            _spirvModuleWorker.AddTypeInt(id, (uint)it.Width, it.Signed ? 1u : 0u);
            break;
        case FloatType ft:
            _spirvModuleWorker.AddTypeFloat(id, (uint)ft.Width);
            break;
        case VectorType vt:
            _spirvModuleWorker.AddTypeVector(id, _secondPass.MapType(vt.ComponentType), (uint)vt.ComponentCount);
            break;
        case MatrixType mt:
            _spirvModuleWorker.AddTypeMatrix(id, _secondPass.MapType(mt.ColumnType), (uint)mt.ColumnCount);
            break;
        case ArrayType at:
            var elemId = _secondPass.MapType(at.ElementType);
            if (at.Length.HasValue)
                _spirvModuleWorker.AddTypeArray(id, elemId, CreateConstantInt(32, false, at.Length.Value));
            else
                _spirvModuleWorker.AddTypeRuntimeArray(id, elemId);
            break;
        case StructType st:
            var memberIds = st.MemberTypes.Select(m => _secondPass.MapType(m)).ToArray();
            _spirvModuleWorker.AddTypeStruct(id, memberIds);
            for (int i = 0; i < st.MemberNames.Count; i++)
            {
                _spirvModuleWorker.AddMemberName(id, (uint)i, st.MemberNames[i]);
            }
            break;
        case PointerType pt:
            _spirvModuleWorker.AddTypePointer(id, (uint)pt.StorageClass, _secondPass.MapType(pt.PointeeType));
            break;
        default: 
            throw new NotSupportedException($"Type {type.GetType()}");
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
                    symbol.Id = _spirvModuleWorker.GetNextId();
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
                _spirvModuleWorker.AddNonFunctionVariable(ptrTypeId, varId, (uint)symbol.StorageClass.Value, null);
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
                    _spirvModuleWorker.AddNonFunctionVariable(ptrTypeId, varId, (uint)symbol.StorageClass.Value, null);
                }
            }
        }
        
        private void EmitDecorations()
        {
            uint bindingIndex = 0;
            foreach (var (_, symbol) in _firstPass.Symbols.GetGlobalSymbols())
            {
                if (symbol.Kind == SymbolKind.Variable &&
                    (symbol.StorageClass == StorageClass.Uniform ||
                     symbol.StorageClass == StorageClass.StorageBuffer))
                {
                    var varId = symbol.Id!.Value;
                    _spirvModuleWorker.AddDecorate(varId, (uint)Decoration.DescriptorSet, 0u);
                    _spirvModuleWorker.AddDecorate(varId, (uint)Decoration.Binding, bindingIndex++);

                    if (symbol.Type is PointerType ptrType && ptrType.PointeeType is StructType structType)
                    {
                        var structId = _secondPass.MapType(structType);
                        _spirvModuleWorker.AddDecorate(structId, (uint)Decoration.Block);
                        uint offset = 0;
                        for (int i = 0; i < structType.MemberTypes.Count; i++)
                        {
                            uint size = GetTypeSize(structType.MemberTypes[i]);
                            _spirvModuleWorker.AddMemberDecorate(structId, (uint)i, (uint)Decoration.Offset, offset);
                            offset += size;
                        }
                    }
                    else if (symbol.Type is StructType structType2)
                    {
                        var structId = _secondPass.MapType(structType2);
                        _spirvModuleWorker.AddDecorate(structId, (uint)Decoration.Block);
                        uint offset = 0;
                        for (int i = 0; i < structType2.MemberTypes.Count; i++)
                        {
                            uint size = GetTypeSize(structType2.MemberTypes[i]);
                            _spirvModuleWorker.AddMemberDecorate(structId, (uint)i, (uint)Decoration.Offset, offset);
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

            id = _spirvModuleWorker.GetNextId();
            _functionSymbolIds[funcSymbol] = id;
            funcSymbol.Id = id;
            return id;
        }

        private void EmitFunctions(TranslationUnitNode ast)
        {
            foreach (var decl in ast.Declarations)
            {
                if (decl is FunctionDefinitionNode funcDef)
                    GenerateFunction(funcDef);
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
            _spirvModuleWorker.AddFunction(returnTypeId, funcId, 0, funcTypeId);

            _secondPass.EnterLocalScope();
            if (funcDef.Prototype.Parameters != null)
            {
                foreach (var param in funcDef.Prototype.Parameters.Parameters)
                {
                    var paramType = TypeResolver.GetTypeFromParameterDeclaration(param, _firstPass);
                    var paramPtrType = new PointerType(StorageClass.Function, paramType);
                    var paramPtrTypeId = _secondPass.MapType(paramPtrType);
                    var paramId = _spirvModuleWorker.GetNextId();
                    _spirvModuleWorker.AddFunctionParameter(paramPtrTypeId, paramId);
                    if (param.Identifier != null)
                    {
                        var paramSym = new SymbolInfo(SymbolKind.Variable, paramType, StorageClass.Function) { Id = paramId };
                        _secondPass.AddLocalSymbol(param.Identifier.Name, paramSym);
                    }
                }
            }

            var entryLabel = _spirvModuleWorker.GetNextId();
            _spirvModuleWorker.AddLabel(entryLabel);
            _secondPass.CurrentBlock = entryLabel;
            BeginBlock();
            GenerateStatement(funcDef.Body);
            
            if (!IsCurrentBlockTerminated())
                _spirvModuleWorker.AddReturn();
            
            _spirvModuleWorker.AddFunctionEnd();
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
                var varId = _spirvModuleWorker.GetNextId();
                _spirvModuleWorker.AddFunctionVariable(ptrTypeId, varId, (uint)StorageClass.Function, null);
                if (single.TypelessDeclaration != null)
                {
                    var varName = single.TypelessDeclaration.Identifier.Name;
                    var sym = new SymbolInfo(SymbolKind.Variable, varType, StorageClass.Function) { Id = varId };
                    _secondPass.AddLocalSymbol(varName, sym);
                    if (single.TypelessDeclaration.Initializer != null)
                    {
                        var initVal = GenerateExpression(single.TypelessDeclaration.Initializer.AssignmentExpression);
                        _spirvModuleWorker.AddStore(varId, initVal);
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
                var compositeId = _spirvModuleWorker.GetNextId();
                var resultTypeId = _secondPass.MapType(targetType);
                _spirvModuleWorker.AddConstantComposite(resultTypeId, compositeId, constituents);
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
            if (node.Identifier == null && !node.BooleanValue.HasValue && 
                node.IntConstant == null && node.UintConstant == null &&
                node.FloatConstant == null && node.DoubleConstant == null && 
                node.ParenthesizedExpression == null)
            {
                return _secondPass.GetConstantId(new BoolType(), false);
            }
            
            if (node.Identifier != null)
            {
                string name = node.Identifier.Name;
                if (name == "true")
                    return _secondPass.GetConstantId(new BoolType(), true);
                if (name == "false")
                    return _secondPass.GetConstantId(new BoolType(), false);
                return LoadVariable(name);
            }

            if (node.BooleanValue.HasValue)
                return _secondPass.GetConstantId(new BoolType(), node.BooleanValue.Value);

            if (node.IntConstant != null)
            {
                if (int.TryParse(node.IntConstant, out var ival))
                    return _secondPass.GetConstantId(new IntType(32, true), ival);
                if (uint.TryParse(node.IntConstant, out var uval))
                    return _secondPass.GetConstantId(new IntType(32, false), uval);
            }

            if (node.UintConstant != null && uint.TryParse(node.UintConstant, out var uval2))
                return _secondPass.GetConstantId(new IntType(32, false), uval2);

            if (node.FloatConstant != null && float.TryParse(node.FloatConstant,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var fval))
                return _secondPass.GetConstantId(new FloatType(32), fval);

            if (node.DoubleConstant != null && double.TryParse(node.DoubleConstant,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var dval))
                return _secondPass.GetConstantId(new FloatType(64), dval);

            if (node.ParenthesizedExpression != null)
                return GenerateExpression(node.ParenthesizedExpression);

            throw new NotImplementedException(
                $"Unhandled PrimaryExpression: Id={node.Identifier?.Name}, " +
                $"Bool={node.BooleanValue}, Int={node.IntConstant}, Uint={node.UintConstant}, " +
                $"Float={node.FloatConstant}, Double={node.DoubleConstant}");
        }

        private uint LoadVariable(string name)
        {
            var sym = _secondPass.Lookup(name);
            if (sym == null) throw new Exception($"Unknown variable {name}");
            var ptrId = sym.Id!.Value;
            var loaded = _spirvModuleWorker.GetNextId();
            _spirvModuleWorker.AddLoad(_secondPass.MapType(sym.Type), loaded, ptrId);
            return loaded;
        }

        private uint GenerateBinaryExpression(BinaryExpressionNode bin)
        {
            var left = GenerateExpression(bin.Left);
            var right = GenerateExpression(bin.Right);
            var leftType = GetExpressionType(bin.Left);
            
            if (string.IsNullOrEmpty(bin.Operator))
            {
                return left;
            }
            
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
                    _ => throw new NotSupportedException($"Binary operator '{bin.Operator}' not supported for FloatType")
                };
            }
            else if (leftType is BoolType)
            {
                op = bin.Operator switch
                {
                    "==" => Opcode.OpLogicalEqual,
                    "!=" => Opcode.OpLogicalNotEqual,
                    "&&" => Opcode.OpLogicalAnd,
                    "||" => Opcode.OpLogicalOr,
                    _ => throw new NotSupportedException($"Binary operator '{bin.Operator}' not supported for BoolType")
                };
            }
            else
            {
                throw new NotSupportedException($"Binary operator '{bin.Operator}' on type {leftType?.GetType().Name}");
            }
            
            bool isComparison = bin.Operator is "==" or "!=" or "<" or ">" or "<=" or ">=";
            var resultType = isComparison ? new BoolType() : leftType;
            var resTypeId = _secondPass.MapType(resultType);
            var resultId = _spirvModuleWorker.GetNextId();
            _spirvModuleWorker.AddFunctionInstruction(new Instruction { Opcode = op, ResultType = resTypeId, ResultId = resultId, Operands = { left, right } });
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
                            var subId = _spirvModuleWorker.GetNextId();
                            var op = operandType is IntType ? Opcode.OpISub : Opcode.OpFSub;
                            _spirvModuleWorker.AddFunctionInstruction(new Instruction { Opcode = op, ResultType = _secondPass.MapType(operandType), ResultId = subId, Operands = { zero, operand } });
                            return subId;
                        case "!":
                            var boolType = new BoolType();
                            var notId = _spirvModuleWorker.GetNextId();
                            _spirvModuleWorker.AddFunctionInstruction(new Instruction { Opcode = Opcode.OpLogicalNot, ResultType = _secondPass.MapType(boolType), ResultId = notId, Operands = { operand } });
                            return notId;
                        case "~":
                            var notBitId = _spirvModuleWorker.GetNextId();
                            _spirvModuleWorker.AddFunctionInstruction(new Instruction { Opcode = Opcode.OpNot, ResultType = _secondPass.MapType(operandType), ResultId = notBitId, Operands = { operand } });
                            return notBitId;
                        default: throw new NotImplementedException();
                    }
                }
                else if (unary.HasIncOp)
                {
                    var ptr = GetPointer(unary.Operand);
                    var loaded = _spirvModuleWorker.GetNextId();
                    _spirvModuleWorker.AddLoad(_secondPass.MapType(operandType), loaded, ptr);
                    var one = _secondPass.GetConstantId(operandType, 1);
                    var added = _spirvModuleWorker.GetNextId();
                    var addOp = operandType is IntType ? Opcode.OpIAdd : Opcode.OpFAdd;
                    _spirvModuleWorker.AddFunctionInstruction(new Instruction { Opcode = addOp, ResultType = _secondPass.MapType(operandType), ResultId = added, Operands = { loaded, one } });
                    _spirvModuleWorker.AddStore(ptr, added);
                    return added;
                }
                else if (unary.HasDecOp) {
                }
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
                var ptrId = _spirvModuleWorker.GetNextId();
                var ptrType = new PointerType(StorageClass.Function, GetFieldType(baseType, field.FieldName));
                uint resultType = _secondPass.MapType(ptrType);
                
                _spirvModuleWorker.AddAccessChain(resultType, ptrId, basePtr, [index]);
                
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
                if (post.PrimaryExpression != null &&
                    post.ArrayIndexExpression == null &&
                    post.FieldSelection == null &&
                    post.PostfixExpression == null)
                {
                    return GetPointer(post.PrimaryExpression);
                }

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

                var currentType = GetExpressionType(baseExpr);
                var indices = new List<uint>();

                if (post.ArrayIndexExpression != null)
                {
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

                if (post.FieldSelection?.Identifier != null)
                {
                    var fieldName = post.FieldSelection.Identifier.Name;
                    indices.Add(GetFieldIndex(currentType, fieldName));
                    currentType = GetFieldType(currentType, fieldName);
                }

                if (indices.Count == 0)
                    return basePtr;

                var resultPtrId = _spirvModuleWorker.GetNextId();
                ExpressionNode baseExpressionNode;
                if (post.PostfixExpression != null)
                    baseExpressionNode = post.PostfixExpression;
                else if (post.PrimaryExpression != null)
                    baseExpressionNode = post.PrimaryExpression;
                else
                    throw new InvalidOperationException("PostfixExpression has no base expression");
                var resultPtrType = new PointerType(GetPointerStorageClass(baseExpressionNode), currentType);
                
                _spirvModuleWorker.AddAccessChain(_secondPass.MapType(resultPtrType), resultPtrId, basePtr, indices.ToArray());
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
            _spirvModuleWorker.AddStore(ptr, right);
            return right;
        }
        
        private uint GenerateConstantExpression(ConstantExpressionNode constExpr)
        {
            if (constExpr.BinaryExpression != null)
            {
                if (string.IsNullOrEmpty(constExpr.BinaryExpression.Operator))
                    return GenerateExpression(constExpr.BinaryExpression.Left);
                return GenerateBinaryExpression(constExpr.BinaryExpression);
            }
            if (constExpr.Condition != null && constExpr.TrueExpression != null && constExpr.FalseExpression != null)
            {
                var condVal = GenerateExpression(constExpr.Condition);
                var trueVal = GenerateExpression(constExpr.TrueExpression);
                var falseVal = GenerateExpression(constExpr.FalseExpression);
                var resultId = _spirvModuleWorker.GetNextId();
                var resultType = GetExpressionType(constExpr.TrueExpression);
                _spirvModuleWorker.AddSelect(_secondPass.MapType(resultType), resultId, condVal, trueVal, falseVal);
                return resultId;
            }
            throw new NotImplementedException("Unsupported constant expression");
        }

        private uint GenerateConstructorExpression(ConstructorExpressionNode ctor)
        {
            var type = TypeResolver.GetTypeFromTypeSpecifier(ctor.TypeSpecifier, _firstPass);
            var args = ctor.Parameters.AssignmentExpressions.Select(a => GenerateExpression(a)).ToArray();
            var resultId = _spirvModuleWorker.GetNextId();
            
            _spirvModuleWorker.AddCompositeConstruct(_secondPass.MapType(type), resultId, args);
            
            return resultId;
        }

        private uint GenerateFieldAccess(FieldAccessExpressionNode field)
        {
            var baseVal = GenerateExpression(field.Base);
            var baseType = GetExpressionType(field.Base);
            var index = GetFieldIndex(baseType, field.FieldName);
            var extracted = _spirvModuleWorker.GetNextId();
            var fieldType = GetFieldType(baseType, field.FieldName);

            _spirvModuleWorker.AddCompositeExtract(_secondPass.MapType(fieldType), extracted, baseVal, index);
            
            return extracted;
        }

        private uint GeneratePostfixExpression(PostfixExpressionNode post)
        {
            if (post.HasIncOp || post.HasDecOp)
            {
                var ptr = GetPointer(post.PostfixExpression!);
                var loaded = _spirvModuleWorker.GetNextId();
                var valType = GetExpressionType(post.PostfixExpression!);
                _spirvModuleWorker.AddLoad(_secondPass.MapType(valType), loaded, ptr);
                var one = _secondPass.GetConstantId(valType, 1);
                var updated = _spirvModuleWorker.GetNextId();
                var op = valType is IntType
                    ? (post.HasIncOp ? Opcode.OpIAdd : Opcode.OpISub)
                    : (post.HasIncOp ? Opcode.OpFAdd : Opcode.OpFSub);
                
                _spirvModuleWorker.AddFunctionInstruction(new Instruction
                {
                    Opcode = op,
                    ResultType = _secondPass.MapType(valType),
                    ResultId = updated,
                    Operands = { loaded, one }
                });
                
                _spirvModuleWorker.AddStore(ptr, updated);
                
                return loaded;
            }

            if (post.FunctionCallParameters != null)
            {
                if (post.PrimaryExpression?.Identifier != null)
                {
                    var funcName = post.PrimaryExpression.Identifier.Name;
                    var funcSym = _firstPass.Symbols.Lookup(funcName);
                    if (funcSym == null || funcSym.Kind != SymbolKind.Function)
                        throw new Exception($"Function {funcName} not found");
                    var funcId = GetOrCreateFunctionId(funcSym);
                    var args = post.FunctionCallParameters.AssignmentExpressions
                        .Select(a => GenerateExpression(a)).ToArray();
                    var callId = _spirvModuleWorker.GetNextId();
                    var retTypeId = _secondPass.MapType(funcSym.Type);
                    _spirvModuleWorker.AddFunctionCall(retTypeId, callId, funcId, args);
                    return callId;
                }

                if (post.ConstructorType != null)
                {
                    var targetType = TypeResolver.GetTypeFromTypeSpecifier(post.ConstructorType, _firstPass);
                    var args = post.FunctionCallParameters.AssignmentExpressions
                        .Select(a => GenerateExpression(a)).ToArray();

                    if (args.Length == 1)
                    {
                        var argType = GetExpressionType(post.FunctionCallParameters.AssignmentExpressions[0]);
                        if (targetType is FloatType && argType is IntType)
                        {
                            var converted = _spirvModuleWorker.GetNextId();
                            var op = ((IntType)argType).Signed ? Opcode.OpConvertSToF : Opcode.OpConvertUToF;
                            _spirvModuleWorker.AddFunctionInstruction(new Instruction
                            {
                                Opcode = op,
                                ResultType = _secondPass.MapType(targetType),
                                ResultId = converted,
                                Operands = { args[0] }
                            });
                            return converted;
                        }
                        if (targetType is IntType && argType is FloatType)
                        {
                            var converted = _spirvModuleWorker.GetNextId();
                            var op = ((IntType)targetType).Signed ? Opcode.OpConvertFToS : Opcode.OpConvertFToU;
                            _spirvModuleWorker.AddFunctionInstruction(new Instruction
                            {
                                Opcode = op,
                                ResultType = _secondPass.MapType(targetType),
                                ResultId = converted,
                                Operands = { args[0] }
                            });
                            return converted;
                        }
                        if (targetType is IntType && argType is IntType && ((IntType)targetType).Width != ((IntType)argType).Width)
                        {
                            var converted = _spirvModuleWorker.GetNextId();
                            var op = ((IntType)targetType).Signed ? Opcode.OpSConvert : Opcode.OpUConvert;
                            _spirvModuleWorker.AddFunctionInstruction(new Instruction
                            {
                                Opcode = op,
                                ResultType = _secondPass.MapType(targetType),
                                ResultId = converted,
                                Operands = { args[0] }
                            });
                            return converted;
                        }
                        if (targetType is FloatType && argType is FloatType && ((FloatType)targetType).Width != ((FloatType)argType).Width)
                        {
                            var converted = _spirvModuleWorker.GetNextId();
                            _spirvModuleWorker.AddFunctionInstruction(new Instruction
                            {
                                Opcode = Opcode.OpFConvert,
                                ResultType = _secondPass.MapType(targetType),
                                ResultId = converted,
                                Operands = { args[0] }
                            });
                            return converted;
                        }
                    }

                    var constrId = _spirvModuleWorker.GetNextId();
                    _spirvModuleWorker.AddCompositeConstruct(_secondPass.MapType(targetType), constrId, args);
                    return constrId;
                }

                throw new NotImplementedException("Function call without identifier or constructor type");
            }

            if (post.ArrayIndexExpression != null)
            {
                var basePtr = GetPointer(post.PostfixExpression!);
                uint indexId;
                if (uint.TryParse(post.ArrayIndexExpression, out uint constIndex))
                    indexId = _secondPass.GetConstantId(new IntType(32, false), constIndex);
                else
                {
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

                var elemPtr = _spirvModuleWorker.GetNextId();
                var ptrTypeSpv = new PointerType(StorageClass.Function, elemType);
                _spirvModuleWorker.AddAccessChain(_secondPass.MapType(ptrTypeSpv), elemPtr, basePtr, [indexId]);
                var loaded = _spirvModuleWorker.GetNextId();
                _spirvModuleWorker.AddLoad(_secondPass.MapType(elemType), loaded, elemPtr);
                return loaded;
            }

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
                var fieldPtr = _spirvModuleWorker.GetNextId();
                var ptrTypeSpv = new PointerType(GetPointerStorageClass(baseExpr), fieldType);
                _spirvModuleWorker.AddAccessChain(_secondPass.MapType(ptrTypeSpv), fieldPtr, basePtr, [index]);
                var loaded = _spirvModuleWorker.GetNextId();
                _spirvModuleWorker.AddLoad(_secondPass.MapType(fieldType), loaded, fieldPtr);
                return loaded;
            }

            if (post.PrimaryExpression != null)
                return GenerateExpression(post.PrimaryExpression);

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
            var mergeLabel = _spirvModuleWorker.GetNextId();
            var thenLabel = _spirvModuleWorker.GetNextId();
            var elseLabel = ifStmt.ElseStatement != null ? _spirvModuleWorker.GetNextId() : mergeLabel;

            _spirvModuleWorker.AddSelectionMerge(mergeLabel, 0);
            _spirvModuleWorker.AddBranchConditional(cond, thenLabel, elseLabel);

            _spirvModuleWorker.AddLabel(thenLabel);
            _secondPass.CurrentBlock = thenLabel;
            BeginBlock();
            GenerateStatement(ifStmt.ThenStatement);
            if (!IsCurrentBlockTerminated()) _spirvModuleWorker.AddBranch(mergeLabel);

            if (ifStmt.ElseStatement != null)
            {
                _spirvModuleWorker.AddLabel(elseLabel);
                _secondPass.CurrentBlock = elseLabel;
                BeginBlock();
                GenerateStatement(ifStmt.ElseStatement);
                if (!IsCurrentBlockTerminated()) _spirvModuleWorker.AddBranch(mergeLabel);
            }

            _spirvModuleWorker.AddLabel(mergeLabel);
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
            
            var headerLabel = _spirvModuleWorker.GetNextId();
            var mergeLabel = _spirvModuleWorker.GetNextId();
            var continueLabel = _spirvModuleWorker.GetNextId();

            var prevMergeLabel = _secondPass.CurrentMergeLabel;
            var prevContinueTarget = _secondPass.CurrentContinueTarget;
            _secondPass.CurrentMergeLabel = mergeLabel;
            _secondPass.CurrentContinueTarget = continueLabel;

            _spirvModuleWorker.AddBranch(headerLabel);
            _spirvModuleWorker.AddLabel(headerLabel);
            _secondPass.CurrentBlock = headerLabel;
            BeginBlock();
            _spirvModuleWorker.AddLoopMerge(mergeLabel, continueLabel, 0);

            if (loop.Type == IterationType.While && loop.WhileCondition != null)
            {
                var cond = GenerateExpression(loop.WhileCondition.Expression!);
                var bodyLabel = _spirvModuleWorker.GetNextId();
                _spirvModuleWorker.AddBranchConditional(cond, bodyLabel, mergeLabel);
                _spirvModuleWorker.AddLabel(bodyLabel);
                _secondPass.CurrentBlock = bodyLabel;
                BeginBlock();
                GenerateStatement(loop.WhileBody!);
                if (!IsCurrentBlockTerminated()) _spirvModuleWorker.AddBranch(continueLabel);
                _spirvModuleWorker.AddLabel(continueLabel);
                _secondPass.CurrentBlock = continueLabel;
                _spirvModuleWorker.AddBranch(headerLabel);
            }
            else if (loop.Type == IterationType.For && loop.ForRest != null)
            {
                var cond = loop.ForRest.Condition != null ? GenerateExpression(loop.ForRest.Condition.Expression!) : _secondPass.GetConstantId(new BoolType(), true);
                var bodyLabel = _spirvModuleWorker.GetNextId();
                _spirvModuleWorker.AddBranchConditional(cond, bodyLabel, mergeLabel);
                _spirvModuleWorker.AddLabel(bodyLabel);
                _secondPass.CurrentBlock = bodyLabel;
                BeginBlock();
                GenerateStatement(loop.ForBody!);
                
                if (!IsCurrentBlockTerminated())
                    _spirvModuleWorker.AddBranch(continueLabel);
                
                _spirvModuleWorker.AddLabel(continueLabel);
                _secondPass.CurrentBlock = continueLabel;
                
                if (loop.ForRest.Expression != null)
                    GenerateExpression(loop.ForRest.Expression);
                
                _spirvModuleWorker.AddBranch(headerLabel);
            }
            else throw new NotImplementedException();

            _spirvModuleWorker.AddLabel(mergeLabel);
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
                        _spirvModuleWorker.AddReturnValue(val);
                    }
                    else _spirvModuleWorker.AddReturn();
                    break;
                case JumpType.Break:
                    if (_secondPass.CurrentMergeLabel.HasValue)
                        _spirvModuleWorker.AddBranch(_secondPass.CurrentMergeLabel.Value);
                    else throw new Exception("Break outside loop");
                    break;
                case JumpType.Continue:
                    if (_secondPass.CurrentContinueTarget.HasValue)
                        _spirvModuleWorker.AddBranch(_secondPass.CurrentContinueTarget.Value);
                    else throw new Exception("Continue outside loop");
                    break;
                case JumpType.Discard:
                    _spirvModuleWorker.AddTerminateInvocation();
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
                throw new NotImplementedException($"UnaryExpression without operand or postfix expression");
            }
            if (expr is ConstructorExpressionNode ctor) 
                return TypeResolver.GetTypeFromTypeSpecifier(ctor.TypeSpecifier, _firstPass);
            if (expr is FieldAccessExpressionNode field) 
                return GetFieldType(GetExpressionType(field.Base), field.FieldName);
            if (expr is PostfixExpressionNode post)
            {
                ExpressionNode? baseNode = null;
                if (post.PostfixExpression != null)
                    baseNode = post.PostfixExpression;
                else if (post.PrimaryExpression != null)
                    baseNode = post.PrimaryExpression;

                if (baseNode != null)
                {
                    SpirvType baseType = GetDereferencedType(GetExpressionType(baseNode));

                    if (post.ArrayIndexExpression != null)
                    {
                        if (baseType is ArrayType arrType)
                            return arrType.ElementType;
                        if (baseType is PointerType ptrType && ptrType.PointeeType is ArrayType arrType2)
                            return arrType2.ElementType;
                        throw new InvalidOperationException("Array index on non-array type");
                    }

                    if (post.FieldSelection?.Identifier != null)
                    {
                        return GetFieldType(baseType, post.FieldSelection.Identifier.Name);
                    }

                    if (post.FunctionCallParameters != null)
                    {
                        if (post.PrimaryExpression?.Identifier != null)
                        {
                            string name = post.PrimaryExpression.Identifier.Name;
                            var typeSym = _firstPass.Symbols.Lookup(name);
                            if (typeSym != null && typeSym.Kind == SymbolKind.Type)
                                return typeSym.Type;
                            var basicType = GetBasicTypeByName(name);
                            if (basicType != null)
                                return basicType;
                        }
    
                        if (post.ConstructorType != null)
                            return TypeResolver.GetTypeFromTypeSpecifier(post.ConstructorType, _firstPass);
    
                        if (post.FunctionCallParameters.AssignmentExpressions.Count == 1)
                        {
                            var argType = GetExpressionType(post.FunctionCallParameters.AssignmentExpressions[0]);
                            return argType;
                        }
    
                        return new VoidType();
                    }

                    if (post.HasIncOp || post.HasDecOp)
                        return baseType;

                    return baseType;
                }

                if (post.ArrayIndexExpression != null)
                {
                    return new IntType(32, true);
                }
                if (post.FieldSelection != null)
                {
                    return new FloatType(32);
                }
                if (post.FunctionCallParameters != null)
                {
                    return new VoidType();
                }

                return new BoolType();
            }
            
            if (expr is AssignmentExpressionNode assign)
            {
                if (assign.ConstantExpression != null)
                    return GetExpressionType(assign.ConstantExpression);
                if (assign.RightAssignment != null)
                    return GetExpressionType(assign.RightAssignment);
                throw new NotImplementedException("AssignmentExpressionNode without right side");
            }
            
            if (expr is ConstantExpressionNode constExpr)
            {
                if (constExpr.BinaryExpression != null)
                    return GetExpressionType(constExpr.BinaryExpression);
                if (constExpr.Condition != null && constExpr.TrueExpression != null)
                    return GetExpressionType(constExpr.TrueExpression);
                throw new NotImplementedException("ConstantExpressionNode without binary or ternary");
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
                return _secondPass.GetConstantId(new IntType(32, true), idx);
            }
            if (containerType is VectorType vt && fieldName.Length == 1)
            {
                int idx = fieldName[0] switch { 'x' => 0, 'y' => 1, 'z' => 2, 'w' => 3, _ => 0 };
                return _secondPass.GetConstantId(new IntType(32, true), idx);
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
            _lastInstructionCountBeforeBlock = (uint)_spirvModuleWorker.FunctionInstructionCount();
        }

        private bool IsCurrentBlockTerminated()
        {
            if (_spirvModuleWorker.FunctionInstructionCount() <= _lastInstructionCountBeforeBlock)
                return false;
            
            var last = _spirvModuleWorker.LastFunctionInstruction();
            var termOps = new[] { Opcode.OpReturn, Opcode.OpReturnValue, Opcode.OpBranch, Opcode.OpBranchConditional, Opcode.OpKill, Opcode.OpUnreachable, Opcode.OpTerminateInvocation };
            return termOps.Contains(last.Opcode);
        }
        
        private StorageClass GetPointerStorageClass(ExpressionNode expr)
        {
            SymbolInfo? sym = null;
            if (expr is IdentifierExpressionNode id)
                sym = _secondPass.Lookup(id.Name);
            else if (expr is PrimaryExpressionNode prim && prim.Identifier != null)
                sym = _secondPass.Lookup(prim.Identifier.Name);
            else if (expr is PostfixExpressionNode post)
            {
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

            var type = GetExpressionType(expr);
            if (type is PointerType ptrType)
                return ptrType.StorageClass;
    
            throw new InvalidOperationException($"Expression is not a pointer: {expr.GetType()} - {type?.GetType()}");
        }
        
        private SpirvType? GetBasicTypeByName(string name)
        {
            return name switch
            {
                "void" => new VoidType(),
                "bool" => new BoolType(),
                "int" => new IntType(32, true),
                "uint" => new IntType(32, false),
                "float" => new FloatType(32),
                "double" => new FloatType(64),
                _ => null
            };
        }
        
        private SpirvType GetDereferencedType(SpirvType type)
        {
            while (type is PointerType ptrType)
                type = ptrType.PointeeType;
            return type;
        }
    }
}