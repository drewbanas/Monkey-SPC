namespace compiler
{
    using parse_rule;

    using System.Collections.Generic;

    using lexer;
    using token;

    using code;

    using Instructions = System.Collections.Generic.List<System.Byte>;
    using error = System.String;

    struct Compiler_t
    {
        public List<Object.Object> constants;
        public symbol_table.SymbolTable symbolTable;
        public List<CompilationScope> scopes;
        public int scopeIndex;
    }

    struct Bytecode
    {
        public Instructions Instructions;
        public List<Object.Object> Constants;
    }

    struct EmittedInstruction
    {
        public Opcode Opcode;
        public int Position;
    }

    struct CompilationScope
    {
        public Instructions instructions;
        public EmittedInstruction lastInstruction;
        public EmittedInstruction previousInstruction;
    }

    struct Compiler
    {
        /*parser fields*/
        delegate error parseFn();

        static Lexer lexer;
        static List<string> errors;

        static token.Token curToken;
        static token.Token peekToken;

        static parseFn[] prefixParseFns;
        static parseFn[] infixParseFns;


        /* compiler fields*/
        static Compiler_t compiler;

        static string functionName; // saves identifier for the function literal

        public static void InitParser(Lexer l)
        {
            Compiler.lexer = l;
            Compiler.errors = new List<string>();

            prefixParseFns = new parseFn[11];
            prefixParseFns[(int)PrefixParser.Identifier] = identifier;
            prefixParseFns[(int)PrefixParser.IntegerLiteral] = integerLiteral;
            prefixParseFns[(int)PrefixParser.StringLiteral] = stringLiteral;
            prefixParseFns[(int)PrefixParser.PrefixExpression] = prefixExpression;
            prefixParseFns[(int)PrefixParser.Boolean] = boolean;
            prefixParseFns[(int)PrefixParser.GroupedExpression] = groupedExpression;
            prefixParseFns[(int)PrefixParser.IfExpression] = ifExpression;
            prefixParseFns[(int)PrefixParser.FunctionLiteral] = functionLiteral;
            prefixParseFns[(int)PrefixParser.ArrayLiteral] = arrayLiteral;
            prefixParseFns[(int)PrefixParser.HashLiteral] = hashLiteral;

            infixParseFns = new parseFn[4];
            infixParseFns[(int)InfixParser.InfixExpression] = infixExpression;
            infixParseFns[(int)InfixParser.CallExpression] = callExpression;
            infixParseFns[(int)InfixParser.IndexExpression] = indexExpression;

            // Read two tokens, so curToken and peekToken are both set
            nextToken();
            nextToken();
        }

        public static void New()
        {
            CompilationScope mainScope = new CompilationScope
            {
                instructions = new Instructions { },
                lastInstruction = new EmittedInstruction { },
                previousInstruction = new EmittedInstruction { },
            };

            symbol_table.SymbolTable symbolTable = symbol_table.NewSymbolTable();

            for (int i = 0; i < Object.builtins.Builtins.Length; i++)
            {
                Object._BuiltinDefinition v = Object.builtins.Builtins[i];
                symbol_table.DefineBuiltin(ref symbolTable, i, v.Name);
            }

            compiler = new Compiler_t
            {
                constants = new List<Object.Object>(),
                symbolTable = symbolTable,
                scopes = new List<CompilationScope> { mainScope },
                scopeIndex = 0,
            };
        }

        public static void NewWithState(ref symbol_table.SymbolTable s, List<Object.Object> constants)
        {
            New();
            compiler.symbolTable = s;
            compiler.constants = constants;
        }

        static void nextToken()
        {
            curToken = peekToken;
            peekToken = lexer.NextToken();
        }

        static bool curTokenIs(TokenType t)
        {
            return curToken.Type == t;
        }

        static bool peekTokenIs(TokenType t)
        {
            return peekToken.Type == t;
        }

        static bool expectPeek(TokenType t)
        {
            if (peekTokenIs(t))
            {
                nextToken();
                return true;
            }
            else
            {
                peekError(t);
                return false;
            }
        }

        public static List<string> ParseErrors()
        {
            return errors;
        }

        static void peekError(TokenType t)
        {
            string msg = string.Format("expected next token to be {0}, got {1} instead", t, peekToken.Type);
            errors.Add(msg);
        }

        static void noPrefixFnError(TokenType t)
        {
            string msg = string.Format("no prefix function for {0} found", t);
            errors.Add(msg);
        }

        public static error ParseProgram()
        {

            while (!curTokenIs(TokenType.EOF))
            {
                error err = statement();
                if (err != null)
                    Compiler.errors.Add(err);

                nextToken();
            }

            return null;
        }

        /*** merged parse and compile functions ***/

        static error statement()
        {
            switch (curToken.Type)
            {
                case TokenType.LET:
                    return letStatement();
                case TokenType.RETURN:
                    return returnStatement();
                case TokenType.EXIT: // NEW
                    return exitStatement();
                default:
                    return expressionStatement();
            }
        }

        static error exitStatement()
        {
            if (peekTokenIs(TokenType.SEMICOLON))
            {
                nextToken();
            }
            emit(Opcode.OpExit);
            return null;
        }

        static error letStatement()
        {
            if (!expectPeek(TokenType.IDENT))
            {
                return "expected identifier";
            }

            symbol_table.Symbol symbol = symbol_table.Define(ref compiler.symbolTable, curToken.Literal());
            string identName = curToken.Literal();

            if (!expectPeek(TokenType.ASSIGN))
            {
                return "expected '=' ";
            }

            nextToken();

            // capture function name, it get's defined a bit later
            if (curTokenIs(TokenType.FUNCTION))
            {
                functionName = identName;
            }
            else
            {
                functionName = null;
            }

            error err = expression(Precedence.LOWEST);
            if (err != null)
                return err;


            if (peekTokenIs(TokenType.SEMICOLON))
            {
                nextToken();
            }

            if (symbol.Scope == SymbolScope.GlobalScope)
            {
                emit(Opcode.OpSetGlobal, symbol.Index);
            }
            else
            {
                emit(Opcode.OpSetLocal, symbol.Index);
            }

            return null;
        }


        static error returnStatement()
        {
            if (compiler.scopeIndex == 0)
                return "can only return from functions";

            nextToken();

            error err = expression(Precedence.LOWEST); // return value
            if (err != null)
                return err;

            if (peekTokenIs(TokenType.SEMICOLON))
            {
                nextToken();
            }

            emit(Opcode.OpReturnValue);

            return null;
        }

        static error expressionStatement()
        {
            error err = expression(Precedence.LOWEST);
            if (err != null)
                return err;

            if (peekTokenIs(TokenType.SEMICOLON))
            {
                nextToken();
            }

            emit(Opcode.OpPop);

            return null;
        }

        static error expression(Precedence precedence)
        {
            PrefixParser prefixParser = parse_rule.Rules[(int)curToken.Type].Prefix;
            if (prefixParser == PrefixParser.NONE)
            {
                noPrefixFnError(curToken.Type);
                return null;
            }
            parseFn prefix = prefixParseFns[(int)prefixParser];

            error err = prefix(); // left
            if (err != null)
                return err;

            while (!peekTokenIs(TokenType.SEMICOLON) && precedence < peekPrecedence())
            {

                InfixParser infixParser = parse_rule.Rules[(int)peekToken.Type].Infix;
                if (infixParser == InfixParser.NONE)
                {
                    return null;
                }
                parseFn infix = infixParseFns[(int)infixParser];

                nextToken();

                err = infix();
                if (err != null)
                    return err;
            }

            return null;
        }

        static Precedence peekPrecedence()
        {
            return parse_rule.Rules[(int)peekToken.Type].Precedence;
        }

        static Precedence curPrecedence()
        {
            return parse_rule.Rules[(int)curToken.Type].Precedence;
        }

        static error identifier()
        {
            symbol_table.Symbol symbol = symbol_table.Resolve(ref compiler.symbolTable, curToken.Literal());
            if (symbol == null)
            {
                return string.Format("undefined variable {0}", curToken.Literal());
            }

            loadSymbols(symbol);

            return null;
        }

        static error integerLiteral()
        {

            long value;
            if (!long.TryParse(curToken.Literal(), out value))
            {
                string msg = string.Format("could not parse {0} as integer", curToken.Literal());
                errors.Add(msg);
                return msg;
            }

            Object.Integer integer = new Object.Integer { Value = value };
            emit(Opcode.OpConstant, addConstant(integer));

            return null;
        }

        static error stringLiteral()
        {
            Object.String str = new Object.String { Value = curToken.Literal() };
            emit(Opcode.OpConstant, addConstant(str));

            return null;
        }

        static error prefixExpression()
        {
            TokenType operator_ = curToken.Type;
            nextToken();

            error err = expression(Precedence.PREFIX); // right
            if (err != null)
            {
                return err;
            }

            switch (operator_)
            {
                case TokenType.BANG:
                    emit(Opcode.OpBang);
                    break;
                case TokenType.MINUS:
                    emit(Opcode.OpMinus);
                    break;
                default:
                    return string.Format("unknown operator {0}", operator_);
            }

            return null;
        }

        static error infixExpression()
        {
            TokenType operator_ = curToken.Type;

            Precedence precedence = curPrecedence();
            nextToken();
            error err = expression(precedence); // right
            if (err != null)
            {
                return err;
            }

            switch (operator_)
            {
                case TokenType.PLUS:
                    emit(Opcode.OpAdd);
                    break;
                case TokenType.MINUS:
                    emit(Opcode.OpSub);
                    break;
                case TokenType.ASTERISK:
                    emit(Opcode.OpMul);
                    break;
                case TokenType.SLASH:
                    emit(Opcode.OpDiv);
                    break;
                case TokenType.GT:
                    emit(Opcode.OpGreaterThan);
                    break;
                case TokenType.GT_EQ:
                    emit(Opcode.OpGreaterThanEqual);
                    break;

                /* temporary work arounds */
                /*
                case TokenType.LT: // a < b == !(a >= b)
                    emit(Opcode.OpGreaterThanEqual);
                    emit(Opcode.OpBang); // this is better caled NOT
                    break;

                case TokenType.LT_EQ: // a <= b == !(a  > b)
                    emit(Opcode.OpGreaterThan);
                    emit(Opcode.OpBang);
                    break;
                 */

                case TokenType.LT:
                    emit(Opcode.OpLessThan);
                    break;

                case TokenType.LT_EQ:
                    emit(Opcode.OpLessThanEqual);
                    break;


                case TokenType.EQ:
                    emit(Opcode.OpEqual);
                    break;
                case TokenType.NOT_EQ:
                    emit(Opcode.OpNotEqual);
                    break;

                case TokenType.CARET:
                    emit(Opcode.OpRaise);
                    break;
                case TokenType.PERCENT:
                    emit(Opcode.OpModulo);
                    break;
                case TokenType.SHL:
                    emit(Opcode.OpShiftLeft);
                    break;
                case TokenType.SHR:
                    emit(Opcode.OpShiftRight);
                    break;
                default:
                    return string.Format("unknown operator {0}", operator_);
            }

            return null;
        }

        static error boolean()
        {

            if (curTokenIs(TokenType.TRUE))
            {
                emit(Opcode.OpTrue);
            }
            else
            {
                emit(Opcode.OpFalse);
            }

            return null;
        }

        static error groupedExpression()
        {

            nextToken();

            error err = expression(Precedence.LOWEST);
            if (err != null)
                return err;

            if (!expectPeek(TokenType.RPAREN))
            {
                return "expect ')'";
            }

            return null;
        }

        static error ifExpression()
        {
            if (!expectPeek(TokenType.LPAREN))
            {
                return "expect '('";
            }

            nextToken();
            error err = expression(Precedence.LOWEST); // condition
            if (err != null)
                return err;

            int jumpNotTruthyPos = emit(Opcode.OpJumpNotTruthy, 9999);


            if (!expectPeek(TokenType.RPAREN))
            {
                return "expect ')'";
            }

            if (!expectPeek(TokenType.LBRACE))
            {
                return "expect '{'";
            }

            err = blockStatement(); // consequence 
            if (err != null)
                return err;

            if (lastInstructionIs(Opcode.OpPop))
            {
                removeLastPop();
            }

            // Emit an 'OpJump' with a bogus value
            int jumpPos = emit(Opcode.OpJump, 9999);

            int afterConsequencePos = currentInstructions().Count;
            changeOperand(jumpNotTruthyPos, afterConsequencePos);

            if (peekTokenIs(TokenType.ELSE))
            {
                nextToken();

                if (!expectPeek(TokenType.LBRACE))
                {
                    return "expect '{'";
                }

                err = blockStatement(); // altrenative
                if (err != null)
                    return err;

                if (lastInstructionIs(Opcode.OpPop))
                {
                    removeLastPop();
                }
            }
            else // no else alternative
            {
                emit(Opcode.OpNull);
            }

            int afterAlternativePos = currentInstructions().Count;
            changeOperand(jumpPos, afterAlternativePos);

            return null;
        }


        static error blockStatement()
        {
            nextToken();

            error err;

            while (!curTokenIs(TokenType.RBRACE) && !curTokenIs(TokenType.EOF))
            {
                err = statement();
                if (err != null)
                    return err;

                nextToken();
            }

            return null;
        }

        static error functionLiteral()
        {
            enterScope();
            if (functionName != null && functionName != "")
            {
                symbol_table.DefineFunctionName(ref compiler.symbolTable, functionName);
                functionName = null;
            }

            if (!expectPeek(TokenType.LPAREN))
            {
                return "expect '('";
            }

            int paramCount = functionParameters(); // parameters
            if (paramCount < 0)
                return "error parsing parameters";

            if (!expectPeek(TokenType.LBRACE))
            {
                return "expect '{'";
            }

            error err = blockStatement(); // function body
            if (err != null)
                return err;

            if (lastInstructionIs(Opcode.OpPop))
            {
                replaceLastPopWithReturn();
            }
            if (!lastInstructionIs(Opcode.OpReturnValue))
            {
                /* 
                 * alternate implicit null return
                 * will require 2 VM "cycles"
                emit(Opcode.OpNull); 
                emit(Opcode.OpReturnValue);
                */
                emit(Opcode.OpReturn);
            }

            List<symbol_table.Symbol> freeSymbols = compiler.symbolTable.FreeSymbols;
            int numLocals = compiler.symbolTable.numDefinitions;
            Instructions instructions = leaveScope();

            foreach (symbol_table.Symbol s in freeSymbols)
            {
                loadSymbols(s);
            }

            Object.CompiledFunction compiledFn = new Object.CompiledFunction
            {
                Instructions = instructions,
                NumLocals = numLocals,
                NumParameters = paramCount,
            };

            int fnIndex = addConstant(compiledFn);
            if (fnIndex < 0)
                return "error parsing parameters";

            emit(Opcode.OpClosure, fnIndex, freeSymbols.Count);

            return null;
        }

        static int functionParameters()
        {
            if (peekTokenIs(TokenType.RPAREN)) // no parameters
            {
                nextToken();
                return 0;
            }

            nextToken();

            int paramCount = 1;

            symbol_table.Define(ref compiler.symbolTable, curToken.Literal());

            while (peekTokenIs(TokenType.COMMA))
            {
                nextToken();
                nextToken();

                symbol_table.Define(ref compiler.symbolTable, curToken.Literal());
                paramCount++;
            }

            if (!expectPeek(TokenType.RPAREN))
            {
                return -1;
            }

            return paramCount;
        }

        static error callExpression()
        {
            int argCount = expressionList(TokenType.RPAREN); // arguments
            if (argCount < 0)
                return "error parsing arguments";

            emit(Opcode.OpCall, argCount);

            return null;
        }

        static int expressionList(TokenType end)
        {
            if (peekTokenIs(end))
            {
                nextToken();
                return 0;
            }

            nextToken();

            error err = expression(Precedence.LOWEST);
            if (err != null)
                return -1;

            int argCount = 1;

            while (peekTokenIs(TokenType.COMMA))
            {
                nextToken();
                nextToken();
                err = expression(Precedence.LOWEST);
                if (err != null)
                    return -1;

                argCount++;
            }

            if (!expectPeek(end))
            {
                return -1;
            }

            return argCount;
        }

        static error arrayLiteral()
        {
            int elemCount = expressionList(TokenType.RBRACKET);
            if (elemCount < 0)
                return "error parsing elements";

            emit(Opcode.OpArray, elemCount);

            return null;
        }

        static error indexExpression()
        {
            nextToken();
            error err = expression(Precedence.LOWEST); // index
            if (err != null)
                return err;

            if (!expectPeek(TokenType.RBRACKET))
            {
                return "expect '}'";
            }


            emit(Opcode.OpIndex);

            return null;
        }

        static error hashLiteral()
        {
            error err;

            int pairCount = 0;
            while (!peekTokenIs(TokenType.RBRACE))
            {
                nextToken();
                err = expression(Precedence.LOWEST);  // key
                if (err != null)
                    return err;

                if (!expectPeek(TokenType.COLON))
                {
                    return "expect ';'";
                }

                nextToken();
                err = expression(Precedence.LOWEST);  // value
                if (err != null)
                    return err;

                if (!peekTokenIs(TokenType.RBRACE) && !expectPeek(TokenType.COMMA))
                {
                    return "invalid token";
                }

                pairCount++;
            }

            if (!expectPeek(TokenType.RBRACE))
            {
                return "expect '}'";
            }


            emit(Opcode.OpHash, pairCount << 1);

            return null;
        }

        /*** non-parsing compiler functions ***/

        public static Bytecode Bytecode()
        {
            return new Bytecode
            {
                Instructions = currentInstructions(),
                Constants = compiler.constants
            };
        }

        static int addConstant(Object.Object obj)
        {
            compiler.constants.Add(obj);
            return compiler.constants.Count - 1;
        }

        static int emit(Opcode op, params int[] operands)
        {
            Instructions ins = code.Make(op, operands);
            int pos = addInstruction(ins);

            setLastInstruction(op, pos);

            return pos;
        }

        static int addInstruction(List<byte> ins)
        {
            int posNewInstruction = currentInstructions().Count;
            compiler.scopes[compiler.scopeIndex].instructions.AddRange(ins);

            return posNewInstruction;
        }

        static void setLastInstruction(Opcode op, int pos)
        {
            EmittedInstruction previous = compiler.scopes[compiler.scopeIndex].lastInstruction;
            EmittedInstruction last = new EmittedInstruction { Opcode = op, Position = pos };

            CompilationScope _scope = compiler.scopes[compiler.scopeIndex];
            _scope.previousInstruction = previous;
            _scope.lastInstruction = last;
            compiler.scopes[compiler.scopeIndex] = _scope;
        }

        static bool lastInstructionIs(Opcode op)
        {
            if (currentInstructions().Count == 0)
            {
                return false;
            }
            return compiler.scopes[compiler.scopeIndex].lastInstruction.Opcode == op;
        }

        static void removeLastPop()
        {
            EmittedInstruction last = compiler.scopes[compiler.scopeIndex].lastInstruction;
            EmittedInstruction previous = compiler.scopes[compiler.scopeIndex].previousInstruction;

            List<byte> old = currentInstructions();
            List<byte> new_ = new List<byte>(last.Position + 1);
            for (int i = 0; i < last.Position; i++)
                new_.Add(old[i]);

            CompilationScope _scope = compiler.scopes[compiler.scopeIndex];
            _scope.instructions = new_;
            _scope.lastInstruction = previous;
            compiler.scopes[compiler.scopeIndex] = _scope;
        }

        static void replaceInstruction(int pos, List<byte> newInstruction)
        {
            List<byte> ins = currentInstructions();

            for (int i = 0; i < newInstruction.Count; i++)
            {
                ins[pos + i] = newInstruction[i];
            }

            CompilationScope _scope = compiler.scopes[compiler.scopeIndex];
            _scope.instructions = ins;
            compiler.scopes[compiler.scopeIndex] = _scope;
        }

        static void changeOperand(int opPos, int operand)
        {
            Opcode op = (Opcode)currentInstructions()[opPos];
            Instructions newInstruction = code.Make(op, operand);

            replaceInstruction(opPos, newInstruction);
        }

        static Instructions currentInstructions()
        {
            return compiler.scopes[compiler.scopeIndex].instructions;
        }

        static void enterScope()
        {
            CompilationScope scope = new CompilationScope
            {
                instructions = new Instructions { },
                lastInstruction = new EmittedInstruction { },
                previousInstruction = new EmittedInstruction { },
            };
            compiler.scopes.Add(scope);
            compiler.scopeIndex++;

            compiler.symbolTable = symbol_table.NewEnclosedSymbolTable(compiler.symbolTable);
        }

        static Instructions leaveScope()
        {
            Instructions instructions = currentInstructions();

            compiler.scopes.RemoveAt(compiler.scopes.Count - 1);
            compiler.scopeIndex--;

            compiler.symbolTable = compiler.symbolTable.Outer;

            return instructions;
        }

        static void replaceLastPopWithReturn()
        {
            int lastPos = compiler.scopes[compiler.scopeIndex].lastInstruction.Position;
            replaceInstruction(lastPos, code.Make(Opcode.OpReturnValue));

            CompilationScope _scope = compiler.scopes[compiler.scopeIndex];
            _scope.lastInstruction.Opcode = Opcode.OpReturnValue;
            compiler.scopes[compiler.scopeIndex] = _scope;
        }

        static void loadSymbols(symbol_table.Symbol s)
        {
            switch (s.Scope)
            {
                case SymbolScope.GlobalScope:
                    emit(Opcode.OpGetGlobal, s.Index);
                    break;
                case SymbolScope.LocalScope:
                    emit(Opcode.OpGetLocal, s.Index);
                    break;
                case SymbolScope.BuiltinScope:
                    emit(Opcode.OpGetBuiltin, s.Index);
                    break;
                case SymbolScope.FreeScope:
                    emit(Opcode.OpGetFree, s.Index);
                    break;
                case SymbolScope.FunctionScope:
                    emit(Opcode.OpCurrentClosure);
                    break;
            }
        }
    }
}
