namespace Object
{
    using System.Collections.Generic;
    using Instructions = System.Collections.Generic.List<System.Byte>;


    delegate Object BuiltinFunction(List<Object> args);

    // originally a "const enum" with string entries
    enum ObjectType
    {
        NULL_OBJ,
        ERROR_OBJ,

        INTEGER_OBJ,
        BOOLEAN_OBJ,
        STRING_OBJ,

        RETURN_VALUE_OBJ,

        FUNCTION_OBJ,
        BUILTIN_OBJ,

        ARRAY_OBJ,
        HASH_OBJ,

        COMPILED_FUNCTION_OBJ,

        CLOSURE_OBJ,
    }

    struct HashKey
    {
        public ObjectType Type;
        public long Value;
    }

    interface Hashable
    {
        HashKey HashKey();
    }

    interface Object
    {
        ObjectType Type();
        string Inspect();
    }

    struct Integer : Object, Hashable
    {
        public long Value;

        public ObjectType Type() { return ObjectType.INTEGER_OBJ; }
        public string Inspect() { return Value.ToString(); }
        public HashKey HashKey()
        {
            return new HashKey { Type = ObjectType.INTEGER_OBJ, Value = (long)this.Value };
        }
    }

    struct Boolean : Object, Hashable
    {
        public bool Value;

        public ObjectType Type() { return ObjectType.BOOLEAN_OBJ; }
        public string Inspect() { return Value.ToString(); }
        public HashKey HashKey()
        {
            long value;

            if (this.Value)
            {
                value = 1;
            }
            else
            {
                value = 0;
            }
            return new HashKey { Type = ObjectType.BOOLEAN_OBJ, Value = value };
        }
    }

    struct Null : Object
    {
        public ObjectType Type() { return ObjectType.NULL_OBJ; }
        public string Inspect() { return "null"; }
    }

    /* zero references for VM
    struct ReturnValue : Object
    {
        public Object Value;

        public ObjectType Type() { return ObjectType.RETURN_VALUE_OBJ; }
        public string Inspect() { return this.Value.Inspect(); }
    }
    */

    struct Error : Object
    {
        public string Message;

        public ObjectType Type() { return ObjectType.ERROR_OBJ; }
        public string Inspect() { return "ERROR: " + this.Message; }
    }

    /* zero references for VM & AST dependent
    struct Function : Object
    {
        public List<ast.Identifier> Parameters;
        public ast.BlockStatement Body;

        public ObjectType Type() { return ObjectType.FUNCTION_OBJ; } // Type could have been a field pointing to an enum
        public string Inspect()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> params_ = new List<string>();
            foreach (ast.Identifier p in this.Parameters)
            {
                params_.Add(p.String());
            }

            out_.Append("fn");
            out_.Append("(");
            out_.Append(string.Join(", ", params_));
            out_.Append(") {\n");
            out_.Append(this.Body.String());
            out_.Append("\n}");

            return out_.ToString();
        }
    }
    */

    struct String : Object, Hashable
    {
        public string Value;

        public ObjectType Type() { return ObjectType.STRING_OBJ; }
        public string Inspect() { return this.Value; }
        public HashKey HashKey()
        {
            long h = util.hash.hashString(this.Value); // Official uses fnv
            return new HashKey { Type = ObjectType.STRING_OBJ, Value = h };
        }
    }

    class Builtin : Object
    {
        public BuiltinFunction Fn;

        public ObjectType Type() { return ObjectType.BUILTIN_OBJ; }
        public string Inspect() { return "builtin function"; }
    }

    struct Array : Object
    {
        public List<Object> Elements;

        public ObjectType Type() { return ObjectType.ARRAY_OBJ; }
        public string Inspect()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> elements = new List<string>();
            foreach (Object e in this.Elements)
            {
                elements.Add(e.Inspect());
            }

            out_.Append("[");
            out_.Append(string.Join(", ", elements));
            out_.Append("]");

            return out_.ToString();
        }
    }

    struct HashPair
    {
        public Object Key;
        public Object Value;
    }

    struct Hash : Object
    {
        public Dictionary<HashKey, HashPair> Pairs;

        public ObjectType Type() { return ObjectType.HASH_OBJ; }
        public string Inspect()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> pairs = new List<string>();
            foreach (KeyValuePair<HashKey, HashPair> pair in this.Pairs)
            {
                pairs.Add(string.Format("{0}: {1}", pair.Value.Key.Inspect(), pair.Value.Value.Inspect()));
            }

            out_.Append("{");
            out_.Append(string.Join(", ", pairs));
            out_.Append("}");

            return out_.ToString();
        }
    }

    struct CompiledFunction : Object
    {
        public Instructions Instructions;
        public int NumLocals;
        public int NumParameters;

        public ObjectType Type() { return ObjectType.COMPILED_FUNCTION_OBJ; }
        public string Inspect()
        {
            // Officially should be %p (pointer)
            return string.Format("Compiled function [{0:D}]", this.GetHashCode());
        }
    }

    struct Closure: Object
    {
        public CompiledFunction Fn;
        public List<Object> Free;

        public ObjectType Type() { return ObjectType.CLOSURE_OBJ; }
        public string Inspect()
        {
            // Officially should be %p (pointer)
            return string.Format("Closure [{0:D}]", this.GetHashCode());
        }
    }
}
