using comp_lab.CourseWork;
using CompLab.CourseWork.SpirV;

public class SpirVGenerator
{
    private FirstPassContext _firstPass = null!;
    private SecondPassContext _secondPass = null!;

    public void Generate(TranslationUnitNode ast, string entryPointName)
    {
        // Первый проход
        _firstPass = new FirstPassContext();
        FirstPassVisitor.Visit(ast, _firstPass);
        
        // Второй проход
        _secondPass = new SecondPassContext(_firstPass.Symbols, _firstPass.Types);
        
        // 1. Добавляем необходимые Capabilities (упрощённо – Shader)
        _secondPass.Module.Capability(1); // Shader
        
        // 2. Импорт GLSL.std.450
        var glslStdId = _secondPass.Module.GetNextId();
        _secondPass.Module.ExtInstImport(glslStdId, "GLSL.std.450");
        _secondPass.ImportedSetIds["GLSL.std.450"] = glslStdId;
        
        // 3. Memory model
        _secondPass.Module.MemoryModel(0, 1); // Logical, GLSL450
        
        // 4. Генерация деклараций типов, констант, глобальных переменных
        EmitTypeDeclarations();
        EmitGlobalVariables();
        
        // 5. Регистрируем entry point (пока только одну функцию main)
        var mainSymbol = _firstPass.Symbols.Lookup(entryPointName);
        if (mainSymbol == null || mainSymbol.Kind != SymbolKind.Function)
            throw new Exception("Entry point not found");
        var mainId = GetOrCreateFunctionId(mainSymbol);
        // Собираем интерфейс (глобальные переменные в Input/Output)
        var interfaceIds = CollectInterfaceVariables();
        var execModel = 4; // Fragment – для примера, нужно определить из шейдера
        var nameLiteral = entryPointName;
        var entryInst = new Instruction { Opcode = Opcode.OpEntryPoint, Operands = { (uint)execModel, mainId, nameLiteral } };
        foreach (var id in interfaceIds)
            entryInst.Operands.Add(id);
        _secondPass.Module.AddInstruction(entryInst);
        
        // Можно добавить OpExecutionMode (например, OriginLowerLeft для фрагментного)
        _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpExecutionMode, Operands = { mainId, 7u } }); // OriginLowerLeft = 7
        
        // 6. Генерация функций
        EmitFunctions();
        
        // 7. Завершаем модуль
        _secondPass.Module.SetBound();
    }
    
    private void EmitTypeDeclarations()
    {
        foreach (var type in _firstPass.Types.AllTypes)
        {
            if (!_secondPass.TypeIdMap.ContainsKey(type))
                EmitType(type);
        }
    }
    
    private void EmitType(SpirvType type)
    {
        var id = _secondPass.MapType(type); // создаст запись в TypeIdMap и выделит id
        switch (type)
        {
            case VoidType:
                _secondPass.Module.TypeVoid(id);
                break;
            case BoolType:
                _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeBool, ResultId = id });
                break;
            case IntType intType:
                _secondPass.Module.TypeInt(id, (uint)intType.Width, intType.Signed ? 1u : 0u);
                break;
            case FloatType floatType:
                _secondPass.Module.TypeFloat(id, (uint)floatType.Width);
                break;
            case VectorType vecType:
                var compId = _secondPass.MapType(vecType.ComponentType);
                _secondPass.Module.TypeVector(id, compId, (uint)vecType.ComponentCount);
                break;
            case MatrixType matType:
                var colId = _secondPass.MapType(matType.ColumnType);
                _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeMatrix, ResultId = id, Operands = { colId, (uint)matType.ColumnCount } });
                break;
            case ArrayType arrType:
                var elemId = _secondPass.MapType(arrType.ElementType);
                if (arrType.Length.HasValue)
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeArray, ResultId = id, Operands = { elemId, CreateConstantInt(32, false, arrType.Length.Value) } });
                else
                    _secondPass.Module.AddInstruction(new Instruction { Opcode = Opcode.OpTypeRuntimeArray, ResultId = id, Operands = { elemId } });
                break;
            case StructType structType:
                var memberIds = structType.MemberTypes.Select(m => _secondPass.MapType(m)).ToArray();
                var structInst = new Instruction { Opcode = Opcode.OpTypeStruct, ResultId = id };
                foreach (var mid in memberIds) structInst.Operands.Add(mid);
                _secondPass.Module.AddInstruction(structInst);
                break;
            case PointerType ptrType:
                var pointeeId = _secondPass.MapType(ptrType.PointeeType);
                _secondPass.Module.TypePointer(id, (uint)ptrType.StorageClass, pointeeId);
                break;
            default:
                throw new NotSupportedException($"Type {type.GetType()} not supported");
        }
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
                var varType = symbol.Type;
                var ptrType = new PointerType(symbol.StorageClass!.Value, varType);
                var ptrTypeId = _secondPass.MapType(ptrType);
                var varId = _secondPass.Module.GetNextId();
                symbol.Id = varId;
                _secondPass.Module.Variable(ptrTypeId, varId, (uint)symbol.StorageClass.Value, null);
                _secondPass.Module.Name(varId, name);
                // Добавить декорации: Location, Binding, DescriptorSet и т.д. – нужно из AST
                // Пока пропускаем
            }
        }
    }
    
    private List<uint> CollectInterfaceVariables()
    {
        // Находим все глобальные переменные с StorageClass Input или Output
        var list = new List<uint>();
        foreach (var (_, sym) in _firstPass.Symbols.GetGlobalSymbols())
        {
            if (sym.Kind == SymbolKind.Variable && (sym.StorageClass == StorageClass.Input || sym.StorageClass == StorageClass.Output))
            {
                if (sym.Id.HasValue)
                    list.Add(sym.Id.Value);
            }
        }
        return list;
    }
    
    private Dictionary<SymbolInfo, uint> _functionIds = new();
    private uint GetOrCreateFunctionId(SymbolInfo funcSymbol)
    {
        if (_functionIds.TryGetValue(funcSymbol, out var id))
            return id;
        // Создаём OpTypeFunction
        var returnTypeId = _secondPass.MapType(funcSymbol.Type);
        var paramTypeIds = funcSymbol.ParameterTypes?.Select(t => _secondPass.MapType(t)).ToArray() ?? Array.Empty<uint>();
        var funcTypeInst = new Instruction { Opcode = Opcode.OpTypeFunction, ResultId = _secondPass.Module.GetNextId(), Operands = { returnTypeId } };
        foreach (var pt in paramTypeIds)
            funcTypeInst.Operands.Add(pt);
        var funcTypeId = funcTypeInst.ResultId.Value;
        _secondPass.Module.AddInstruction(funcTypeInst);
        
        // Id самой функции (будет использован в OpFunction)
        var funcId = _secondPass.Module.GetNextId();
        _functionIds[funcSymbol] = funcId;
        funcSymbol.Id = funcId;
        return funcId;
    }
    
    private void EmitFunctions()
    {
        // Найти все определения функций в AST (у нас нет прямой связи символ -> определение, придётся повторно обойти AST)
        // Для простоты: отдельно передаём AST в метод
    }
    
    // Метод для генерации тела функции, вызываемый из обхода AST
    public void GenerateFunction(FunctionDefinitionNode funcDef, string entryPointName)
    {
        var funcName = funcDef.Prototype.Name.Name;
        var funcSymbol = _firstPass.Symbols.Lookup(funcName);
        if (funcSymbol == null) throw new Exception("Function symbol not found");
        
        var funcId = GetOrCreateFunctionId(funcSymbol);
        var returnTypeId = _secondPass.MapType(funcSymbol.Type);
        var funcTypeId = _functionIds[funcSymbol]; // это id OpTypeFunction
        
        _secondPass.Module.Function(returnTypeId, funcId, 0, funcTypeId);
        
        // Параметры
        _secondPass.EnterLocalScope();
        if (funcDef.Prototype.Parameters != null)
        {
            foreach (var param in funcDef.Prototype.Parameters.Parameters)
            {
                var paramType = new VoidType(); //GetTypeFromParameterDeclaration(param, _firstPass); // TODO: нужно реализовать
                var paramPtrType = new PointerType(StorageClass.Function, paramType);
                var paramPtrTypeId = _secondPass.MapType(paramPtrType);
                var paramId = _secondPass.Module.GetNextId();
                _secondPass.Module.FunctionParameter(paramPtrTypeId, paramId);
                // Сохраняем символ параметра в локальной таблице
                if (param.Identifier != null)
                {
                    var paramSym = new SymbolInfo(SymbolKind.Variable, paramType, StorageClass.Function);
                    paramSym.Id = paramId;
                    _secondPass.AddLocalSymbol(param.Identifier.Name, paramSym);
                }
            }
        }
        
        // Генерация тела функции
        var entryLabel = _secondPass.Module.GetNextId();
        _secondPass.Module.Label(entryLabel);
        _secondPass.CurrentBlock = entryLabel;
        
        // Генерируем код тела (CompoundStatementNoNewScopeNode)
        GenerateStatement(funcDef.Body, _secondPass);
        
        // Если в конце функции нет терминирующей инструкции, добавляем OpReturn (для void)
        // Проверка последней инструкции блока – упрощённо:
        _secondPass.Module.Return(); // или ReturnValue если не void
        
        _secondPass.Module.FunctionEnd();
        _secondPass.ExitLocalScope();
    }
    
    private void GenerateStatement(StatementNode stmt, SecondPassContext ctx)
    {
        switch (stmt)
        {
            case CompoundStatementNoNewScopeNode compound:
                foreach (var s in compound.Statements)
                    GenerateStatement(s, ctx);
                break;
            case ExpressionStatementNode exprStmt:
                if (exprStmt.Expression != null)
                    GenerateExpression(exprStmt.Expression, ctx);
                break;
            case DeclarationStatementNode declStmt:
                GenerateDeclarationStatement(declStmt, ctx);
                break;
            case SelectionStatementNode ifStmt:
                GenerateIfStatement(ifStmt, ctx);
                break;
            case IterationStatementNode loopStmt:
                GenerateLoop(loopStmt, ctx);
                break;
            case JumpStatementNode jump:
                GenerateJump(jump, ctx);
                break;
            // другие узлы...
            default:
                throw new NotImplementedException($"Statement {stmt.GetType()} not implemented");
        }
    }
    
    private void GenerateExpression(ExpressionNode expr, SecondPassContext ctx) { /* */ }
    private void GenerateDeclarationStatement(DeclarationStatementNode declStmt, SecondPassContext ctx)
    {
        if (declStmt.Declaration.DeclType == DeclarationType.InitDeclaratorList && declStmt.Declaration.InitDeclaratorList != null)
        {
            foreach (var singleDecl in declStmt.Declaration.InitDeclaratorList.SingleDeclarations)
            {
                var varType = new VoidType(); // GetTypeFromFullySpecifiedType(singleDecl.FullySpecifiedType, _firstPass); // TODO: нужно реализовать
                var ptrType = new PointerType(StorageClass.Function, varType);
                var ptrTypeId = ctx.Module.MapType(ptrType);
                var varId = ctx.Module.GetNextId();
                ctx.Module.Variable(ptrTypeId, varId, (uint)StorageClass.Function, null);
            
                if (singleDecl.TypelessDeclaration != null)
                {
                    var varName = singleDecl.TypelessDeclaration.Identifier.Name;
                    var sym = new SymbolInfo(SymbolKind.Variable, varType, StorageClass.Function) { Id = varId };
                    ctx.AddLocalSymbol(varName, sym);
                
                    // Если есть инициализатор
                    if (singleDecl.TypelessDeclaration.Initializer != null)
                    {
                        // Вычисляем значение инициализатора
                        var initValue = GenerateInitializer(singleDecl.TypelessDeclaration.Initializer, varType, ctx);
                        // Сохраняем через OpStore
                        ctx.Module.Store(varId, initValue);
                    }
                }
            }
        }
    }
    
    private uint GenerateBinaryExpression(BinaryExpressionNode bin, SecondPassContext ctx)
    {
        var left = GenerateExpression(bin.Left, ctx);
        var right = GenerateExpression(bin.Right, ctx);
        var resultType = InferType(bin); // нужно определить тип выражения
    
        Opcode op;
        if (resultType is IntType intType)
        {
            op = bin.Operator switch
            {
                "+" => Opcode.OpIAdd,
                "-" => Opcode.OpISub,
                "*" => Opcode.OpIMul,
                "/" => intType.Signed ? Opcode.OpSDiv : Opcode.OpUDiv,
                "%" => intType.Signed ? Opcode.OpSMod : Opcode.OpUMod,
                "&" => Opcode.OpBitwiseAnd,
                "|" => Opcode.OpBitwiseOr,
                "^" => Opcode.OpBitwiseXor,
                "<<" => Opcode.OpShiftLeftLogical,
                ">>" => intType.Signed ? Opcode.OpShiftRightArithmetic : Opcode.OpShiftRightLogical,
                _ => throw new NotImplementedException()
            };
        }
        else if (resultType is FloatType)
        {
            op = bin.Operator switch
            {
                "+" => Opcode.OpFAdd,
                "-" => Opcode.OpFSub,
                "*" => Opcode.OpFMul,
                "/" => Opcode.OpFDiv,
                _ => throw new NotImplementedException()
            };
        }
        else throw new NotSupportedException();
    
        var resultId = ctx.Module.GetNextId();
        var resultTypeId = ctx.MapType(resultType);
        ctx.Module.AddInstruction(new Instruction { Opcode = op, ResultType = resultTypeId, ResultId = resultId, Operands = { left, right } });
        return resultId;
    }
    
    private void GenerateIfStatement(SelectionStatementNode ifStmt, SecondPassContext ctx) { /* */ }
    private void GenerateLoop(IterationStatementNode loop, SecondPassContext ctx) { /* */ }
    private void GenerateJump(JumpStatementNode jump, SecondPassContext ctx) { /* */ }
}