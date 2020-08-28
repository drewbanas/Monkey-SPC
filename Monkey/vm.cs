namespace vm
{
    using System.Collections.Generic;
    using code;
    //using Object;
    using error = System.String;

    using Instructions = System.Collections.Generic.List<System.Byte>; // code.Instructions

    class VM_t
    {
        public List<Object.Object> constants;
        public List<Object.Object> globals;
        public List<Object.Object> stack;
        public int sp; // Always points to the next value. Top of stack is stack[sp-1].

        public Frame_t[] frames;
        public int frameIndex;
    }

    class VM
    {
        const int StackSize = 2048;
        public const int GlobalSize = 65536;
        const int MaxFrames = 1024;

        static Object.Boolean True = new Object.Boolean { Value = true };
        static Object.Boolean False = new Object.Boolean { Value = false };
        static Object.Null Null = new Object.Null { };

        // Work arounds
        static VM_t vm;

        public static bool ExitVM = false; // New


        public static void New(compiler.Bytecode bytecode)
        {
            Object.CompiledFunction mainFn = new Object.CompiledFunction { Instructions = bytecode.Instructions };
            Object.Closure mainClosure = new Object.Closure { Fn = mainFn };
            Frame_t mainFrame = Frame.NewFrame(mainClosure, 0);
            Frame_t[] frames = new Frame_t[MaxFrames];
            frames[0] = mainFrame;

            VM.vm = new VM_t
            {
                constants = bytecode.Constants,

                stack = new List<Object.Object>(new Object.Object[StackSize]),
                sp = 0,

                globals = new List<Object.Object>(new Object.Object[GlobalSize]),

                frames = frames,
                frameIndex = 1,
            };
        }

        public static void NewWithGlobalStore(compiler.Bytecode bytecode, ref List<Object.Object> s)
        {
            VM.New(bytecode);
            vm.globals = s;
        }

        public static Object.Object LastPoppedStackElem()
        {
            return vm.stack[vm.sp];
        }

        public static error Run()
        {
            int ip;
            Instructions ins;
            Opcode op;

            error err = null;

            while (currentFrame().ip < Frame.Instructions(currentFrame()).Count - 1)
            {
                vm.frames[vm.frameIndex - 1].ip++;

                ip = currentFrame().ip;
                ins = Frame.Instructions(currentFrame());
                op = (Opcode)ins[ip];

                switch (op)
                {
                    case Opcode.OpConstant:
                        {
                            int constIndex = code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;

                            err = push(vm.constants[constIndex]);
                        }
                        break;

                    case Opcode.OpPop:
                        pop();
                        break;

                    case Opcode.OpAdd:
                    case Opcode.OpSub:
                    case Opcode.OpMul:
                    case Opcode.OpDiv:
                    case Opcode.OpRaise:
                    case Opcode.OpModulo:
                    case Opcode.OpShiftLeft:
                    case Opcode.OpShiftRight:
                        {
                            err = executeBinaryOperation(op);
                        }
                        break;

                    case Opcode.OpTrue:
                        {
                            err = push(True);
                        }
                        break;

                    case Opcode.OpFalse:
                        {
                            err = push(False);
                        }
                        break;


                    case Opcode.OpEqual:
                    case Opcode.OpNotEqual:
                    case Opcode.OpGreaterThan:
                    case Opcode.OpGreaterThanEqual:
                    case Opcode.OpLessThan:
                    case Opcode.OpLessThanEqual:
                        {
                            err = executeComparison(op);
                        }
                        break;

                    case Opcode.OpBang:
                        {
                            err = executeBangOperator();
                        }
                        break;

                    case Opcode.OpMinus:
                        {
                            err = executeMinusOperator();
                        }
                        break;

                    case Opcode.OpJump:
                        {
                            int pos = (int)code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip = pos - 1;
                        }
                        break;

                    case Opcode.OpJumpNotTruthy:
                        {
                            int pos = (int)code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;

                            Object.Object condition = pop();
                            if (!isTruthy(condition))
                            {
                                vm.frames[vm.frameIndex - 1].ip = pos - 1;
                            }
                        }
                        break;

                    case Opcode.OpNull:
                        {
                            err = push(Null);
                        }
                        break;

                    case Opcode.OpSetGlobal:
                        {
                            int globalIndex = code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;
                            vm.globals[globalIndex] = pop();
                        }
                        break;

                    case Opcode.OpGetGlobal:
                        {
                            int globalIndex = code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;

                            err = push(vm.globals[globalIndex]);
                        }
                        break;

                    case Opcode.OpArray:
                        {
                            int numElements = (int)code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;

                            Object.Object array = buildArray(vm.sp - numElements, vm.sp);
                            vm.sp = vm.sp - numElements;

                            err = push(array);
                        }
                        break;

                    case Opcode.OpHash:
                        {
                            int numElements = (int)code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;

                            Object.Object hash = buildHash(vm.sp - numElements, vm.sp, out err);
                            if (err != null)
                            {
                                return err;
                            }
                            vm.sp = vm.sp - numElements;

                            err = push(hash);
                        }
                        break;

                    case Opcode.OpIndex:
                        {
                            Object.Object index = pop();
                            Object.Object left = pop();

                            err = executeIndexExpression(left, index);
                        }
                        break;

                    case Opcode.OpCall:
                        {
                            int numArgs = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            err = executeCall(numArgs);
                        }
                        break;

                    case Opcode.OpReturnValue:
                        {
                            Object.Object returnValue = pop();

                            Frame_t frame = popFrame();
                            vm.sp = frame.basePointer - 1;

                            err = push(returnValue);
                        }
                        break;

                    case Opcode.OpReturn:
                        {
                            Frame_t frame = popFrame();
                            vm.sp = frame.basePointer - 1;

                            err = push(Null);
                        }
                        break;


                    case Opcode.OpSetLocal:
                        {
                            int localIndex = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            Frame_t frame = currentFrame();

                            vm.stack[frame.basePointer + localIndex] = pop();
                        }

                        break;

                    case Opcode.OpGetLocal:
                        {
                            int localIndex = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            Frame_t frame = currentFrame();

                            err = push(vm.stack[frame.basePointer + localIndex]);
                        }
                        break;

                    case Opcode.OpGetBuiltin:
                        {
                            int builtinIndex = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            Object._BuiltinDefinition definition = Object.builtins.Builtins[builtinIndex];

                            err = push(definition.Builtin);
                        }
                        break;

                    case Opcode.OpClosure:
                        {
                            int constIndex = (int)code.ReadUint16(ins, ip + 1);
                            int numFree = (int)code.ReadUint8(ins, ip + 3);
                            vm.frames[vm.frameIndex - 1].ip += 3;

                            err = pushClosure(constIndex, numFree);
                        }
                        break;

                    case Opcode.OpGetFree:
                        {
                            int freeIndex = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            Object.Closure curentClosure = currentFrame().cl;
                            err = push(curentClosure.Free[freeIndex]);
                        }
                        break;

                    case Opcode.OpCurrentClosure:
                        {
                            Object.Closure currentClosure = currentFrame().cl;
                            err = push(currentClosure);
                        }
                        break;

                    case Opcode.OpExit:
                        ExitVM = true;
                        return null;
                }

                // pulled out of the individual cases
                if (err != null)
                {
                    return err;
                }

            }

            return null;
        }

        static error push(Object.Object o)
        {
            if (vm.sp >= StackSize)
            {
                return "stack overflow";
            }

            vm.stack[vm.sp] = o;
            vm.sp++;

            return null;
        }

        static Object.Object pop()
        {
            Object.Object o = vm.stack[vm.sp - 1];
            vm.sp--;

            return o;
        }

        static error executeBinaryOperation(Opcode op)
        {
            Object.Object right = pop();
            Object.Object left = pop();

            Object.ObjectType leftType = left.Type();
            Object.ObjectType rightType = right.Type();

            if (leftType == Object.ObjectType.INTEGER_OBJ && rightType == Object.ObjectType.INTEGER_OBJ)
            {
                return executeBinaryIntegerOperation(op, left, right);
            }

            if (leftType == Object.ObjectType.STRING_OBJ && rightType == Object.ObjectType.STRING_OBJ)
            {
                return executeBinaryStringOperation(op, left, right);
            }

            // default:
            return string.Format("unsupported typesfor binary opreation: {0} {1}", leftType, rightType);
        }

        static error executeBinaryIntegerOperation(Opcode op, Object.Object left, Object.Object right)
        {
            long leftValue = ((Object.Integer)left).Value;
            long rightValue = ((Object.Integer)right).Value;

            long result;

            switch (op)
            {
                case Opcode.OpAdd:
                    result = leftValue + rightValue;
                    break;
                case Opcode.OpSub:
                    result = leftValue - rightValue;
                    break;
                case Opcode.OpMul:
                    result = leftValue * rightValue;
                    break;
                case Opcode.OpDiv:
                    result = leftValue / rightValue;
                    break;
                case Opcode.OpRaise:
                    result = (long)System.Math.Pow(leftValue, rightValue);
                    break;
                case Opcode.OpModulo:
                    result = leftValue % rightValue;
                    break;
                case Opcode.OpShiftLeft:
                    result = leftValue << (int)rightValue;
                    break;
                case Opcode.OpShiftRight:
                    result = leftValue >> (int)rightValue;
                    break;
                default:
                    return string.Format("unknown integer operator: {0:D}", op);
            }

            return push(new Object.Integer { Value = result });
        }

        static error executeComparison(Opcode op)
        {
            Object.Object right = pop();
            Object.Object left = pop();

            if (left.Type() == Object.ObjectType.INTEGER_OBJ && right.Type() == Object.ObjectType.INTEGER_OBJ)
            {
                return executeIntegerComparison(op, left, right);
            }

            switch (op)
            {
                case Opcode.OpEqual:
                    return push(nativeToBooleanObject(right == left));
                case Opcode.OpNotEqual:
                    return push(nativeToBooleanObject(right != left));
                default:
                    return string.Format("unsupported typesfor binary opreation: {0:D} ({1} {1})", op, left.Type(), right.Type());
            }
        }

        static error executeIntegerComparison(Opcode op, Object.Object left, Object.Object right)
        {
            long leftValue = ((Object.Integer)left).Value;
            long rightValue = ((Object.Integer)right).Value;

            switch (op)
            {
                case Opcode.OpEqual:
                    return push(nativeToBooleanObject(rightValue == leftValue));
                case Opcode.OpNotEqual:
                    return push(nativeToBooleanObject(rightValue != leftValue));
                case Opcode.OpGreaterThan:
                    return push(nativeToBooleanObject(leftValue > rightValue));
                case Opcode.OpGreaterThanEqual:
                    return push(nativeToBooleanObject(leftValue >= rightValue));
                case Opcode.OpLessThan:
                    return push(nativeToBooleanObject(leftValue < rightValue));
                case Opcode.OpLessThanEqual:
                    return push(nativeToBooleanObject(leftValue <= rightValue));
                default:
                    return string.Format("unknown operator: {0:D}", op);
            }
        }
        
        static error executeBangOperator()
        {
            Object.Object operand = pop();

            /* not necessary
            if (operand.Equals(True))
                return push(False);
            */
            if (operand.Equals(False))
                return push(True);
            if (operand.Equals(Null))
                return push(True);

            // default:
            return push(False);
        }

        static error executeMinusOperator()
        {
            Object.Object operand = pop();

            if (operand.Type() != Object.ObjectType.INTEGER_OBJ)
            {
                return string.Format("unsupported type for negation: {0}", operand.Type());
            }

            long value = ((Object.Integer)operand).Value;
            return push(new Object.Integer { Value = -value });
        }

        static error executeBinaryStringOperation(Opcode op, Object.Object left, Object.Object right)
        {
            if (op != Opcode.OpAdd)
            {
                return string.Format("unknown integer operator: {0:D}", op);
            }

            string leftValue = ((Object.String)left).Value;
            string rightValue = ((Object.String)right).Value;

            return push(new Object.String { Value = leftValue + rightValue });
        }

        static Object.Object buildArray(int startIndex, int endIndex)
        {
            List<Object.Object> elements = new List<Object.Object>(new Object.Object[endIndex - startIndex]);

            for (int i = startIndex; i < endIndex; i++)
            {
                elements[i - startIndex] = vm.stack[i];
            }

            return new Object.Array { Elements = elements };
        }

        static Object.Object buildHash(int startIndex, int endIndex, out error _err)
        {
            Dictionary<Object.HashKey, Object.HashPair> hashedPairs = new Dictionary<Object.HashKey, Object.HashPair>();

            for (int i = startIndex; i < endIndex; i += 2)
            {
                Object.Object key = vm.stack[i];
                Object.Object value = vm.stack[i + 1];

                Object.HashPair pair = new Object.HashPair { Key = key, Value = value };

                if (!(key is Object.Hashable))
                {
                    _err = string.Format("unusable as hash key: {0}", key.Type());
                    return null;
                }
                Object.Hashable hashKey = (Object.Hashable)key;

                hashedPairs.Add(hashKey.HashKey(), pair);
            }

            _err = null;

            return new Object.Hash { Pairs = hashedPairs };
        }

        static error executeIndexExpression(Object.Object left, Object.Object index)
        {
            if (left.Type() == Object.ObjectType.ARRAY_OBJ && index.Type() == Object.ObjectType.INTEGER_OBJ)
                return executeArrayIndex(left, index);

            if (left.Type() == Object.ObjectType.HASH_OBJ)
                return executeHashIndex(left, index);

            // default:
            return string.Format("index operator not supported {0}", left.Type());
        }

        static error executeArrayIndex(Object.Object array, Object.Object index)
        {
            Object.Array arrayOject = (Object.Array)array;
            long i = ((Object.Integer)index).Value;
            long max = (long)(arrayOject.Elements.Count - 1);

            if (i < 0 || i > max)
            {
                return push(Null); // may be better to return an out of bounds error
            }

            return push(arrayOject.Elements[(int)i]);
        }

        static error executeHashIndex(Object.Object hash, Object.Object index)
        {
            Object.Hash hashObject = (Object.Hash)hash;

            if (!(index is Object.Hashable))
            {
                return string.Format("unusable as hash key: {0}", index.Type());
            }
            Object.Hashable key = (Object.Hashable)index;

            Object.HashPair pair;
            if (!hashObject.Pairs.TryGetValue(key.HashKey(), out pair))
            {
                return push(Null); // may be better to return an error
            }

            return push(pair.Value);
        }

        static Frame_t currentFrame()
        {
            return vm.frames[vm.frameIndex - 1];
        }

        static void pushFrame(Frame_t f)
        {
            vm.frames[vm.frameIndex] = f;
            vm.frameIndex++;
        }

        static Frame_t popFrame()
        {
            vm.frameIndex--;
            return vm.frames[vm.frameIndex];
        }

        static error executeCall(int numArgs)
        {
            Object.Object callee = vm.stack[vm.sp - 1 - numArgs];

            if (callee is Object.Closure)
                return callClosure((Object.Closure)callee, numArgs);
            if (callee is Object.Builtin)
                return callBuiltin((Object.Builtin)callee, numArgs);

            // default:
            return "calling non-function and non-built-in";
        }

        static error callClosure(Object.Closure cl, int numArgs)
        {
            if (numArgs != cl.Fn.NumParameters)
            {
                return string.Format("wrong number of arguments: want = {0:D}, got = {1:D}", cl.Fn.NumParameters, numArgs);
            }

            Frame_t frame = Frame.NewFrame(cl, vm.sp - numArgs);
            pushFrame(frame);

            vm.sp = frame.basePointer + cl.Fn.NumLocals;

            return null;
        }

        static error callBuiltin(Object.Builtin builtin, int numArgs)
        {
            List<Object.Object> args = vm.stack.GetRange(vm.sp - numArgs, numArgs);

            Object.Object result = builtin.Fn(args);
            vm.sp = vm.sp - numArgs - 1;

            if (result != null)
            {
                push(result);
            }
            else
            {
                push(Null);
            }

            return null;
        }

        static error pushClosure(int constIndex, int numFree)
        {
            Object.Object constant = vm.constants[constIndex];
            if (!(constant is Object.CompiledFunction))
            {
                return string.Format("not a function {0}", constant.ToString()); // Official uses %+v (prints field names of structs)
            }
            Object.CompiledFunction function = (Object.CompiledFunction)constant;

            List<Object.Object> free = new List<Object.Object>(new Object.Object[numFree]);
            for (int i = 0; i < numFree; i++)
            {
                free[i] = vm.stack[vm.sp - numFree - i];
            }
            vm.sp = vm.sp - numFree;

            Object.Closure closure = new Object.Closure { Fn = function, Free = free };
            return push(closure);
        }

        static Object.Boolean nativeToBooleanObject(bool input)
        {
            if (input)
            {
                return True;
            }
            return False;
        }

        static bool isTruthy(Object.Object obj)
        {
            if (obj is Object.Boolean)
                return ((Object.Boolean)obj).Value;

            if (obj is Object.Null)
                return false;

            //default:
            return true;
        }
    }
}
