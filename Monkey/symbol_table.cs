namespace compiler
{
    using System.Collections.Generic;

    // originally a "const enum" with string entries
    enum SymbolScope
    {
        LocalScope,
        GlobalScope,
        BuiltinScope,
        FreeScope,
        FunctionScope
    }

    struct symbol_table
    {
        public class Symbol
        {
            public string Name;
            public SymbolScope Scope;
            public int Index;
        }

        public class SymbolTable
        {
            public SymbolTable Outer;

            public Dictionary<string, Symbol> store;
            public int numDefinitions;

            public List<Symbol> FreeSymbols;
        }

        public static SymbolTable NewEnclosedSymbolTable(SymbolTable outer)
        {
            SymbolTable s = NewSymbolTable();
            s.Outer = outer;
            return s;
        }

        public static SymbolTable NewSymbolTable()
        {
            Dictionary<string, Symbol> s = new Dictionary<string, Symbol>();
            List<Symbol> free = new List<Symbol> { };
            return new SymbolTable { store = s, FreeSymbols = free };
        }

        public static Symbol Define(ref SymbolTable s, string name)
        {
            Symbol symbol = new Symbol { Name = name, Index = s.numDefinitions };

            if (s.Outer == null)
            {
                symbol.Scope = SymbolScope.GlobalScope;
            }
            else
            {
                symbol.Scope = SymbolScope.LocalScope;
            }

            if (s.store.ContainsKey(name))
                s.store[name] = symbol;
            else
                s.store.Add(name, symbol);

            s.numDefinitions++;
            return symbol;
        }

        public static Symbol Resolve(ref SymbolTable s, string name)
        {
            Symbol obj;
            if (!s.store.TryGetValue(name, out obj)) 
            {
                if (s.Outer != null)
                {
                    obj = Resolve(ref s.Outer, name);
                    if (obj == null)
                    {
                        return obj;
                    }

                    if (obj.Scope == SymbolScope.GlobalScope || obj.Scope == SymbolScope.BuiltinScope)
                    {
                        return obj;
                    }

                    Symbol free = defineFree(ref s, ref obj);
                    return free;
                }
                else
                {
                    return null; // not found
                }
            }

            return obj;
        }

        public static Symbol DefineBuiltin(ref SymbolTable s, int index, string name)
        {
            Symbol symbol = new Symbol { Name = name, Index = index, Scope = SymbolScope.BuiltinScope };
            s.store.Add(name, symbol);
            return symbol;
        }

        public static Symbol DefineFunctionName(ref SymbolTable s, string name)
        {
            Symbol symbol = new Symbol { Name = name, Index = 0, Scope = SymbolScope.FunctionScope }; // index could be anything
            s.store.Add(name, symbol);
            return symbol;
        }

        static Symbol defineFree(ref SymbolTable s, ref Symbol original)
        {
            s.FreeSymbols.Add(original);

            Symbol symbol = new Symbol { Name = original.Name, Index = s.FreeSymbols.Count - 1 };
            symbol.Scope = SymbolScope.FreeScope;

            s.store.Add(original.Name, symbol);
            return symbol;
        }
    }
}
