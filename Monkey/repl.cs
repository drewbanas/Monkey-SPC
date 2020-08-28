namespace repl
{
    using System.Collections.Generic;
    using lexer;
    //using token;
    using Object;
    using compiler;
    using vm;

    using error = System.String;

    class repl
    {
        const string PROMPT = ">> ";

        // https://tomeko.net/online_tools/cpp_text_escape.php?lang=en
        const string MONKEY_FACE = "            __,__\n   .--.  .-\"     \"-.  .--.\n  / .. \\/  .-. .-.  \\/ .. \\\n | |  '|  /   Y   \\  |'  | |\n | \\   \\  \\ 0 | 0 /  /   / |\n  \\ '- ,\\.-\"\"\"\"\"\"\"-./, -' /\n   ''-' /_   ^ ^   _\\ '-''\n       |  \\._   _./  |\n       \\   \\ '~' /   /\n        '._ '-=-' _.'\n           '-----'";

        public static void Start()
        {
            List<Object> constants = new List<Object>();
            List<Object> globals = new List<Object>(new Object[VM.GlobalSize]);

            symbol_table.SymbolTable symbolTable = symbol_table.NewSymbolTable();
            for (int i = 0; i < builtins.Builtins.Length; i++)
            {
                _BuiltinDefinition v = builtins.Builtins[i];
                symbol_table.DefineBuiltin(ref symbolTable, i, v.Name);
            }

            for (;;)
            {
                System.Console.Write(PROMPT);
                string line = System.Console.ReadLine();

                Lexer l = Lexer.New(line);
                Compiler.InitParser(l);
                Compiler.NewWithState(ref symbolTable, constants);

                error err = Compiler.CompileProgram();

                if (Compiler.ParseErrors().Count != 0) // parse errors
                {
                    printParserErrors(Compiler.ParseErrors());
                    continue;
                }

                if (err != null)
                {
                    System.Console.WriteLine("Woops! Compilation failed:\n {0}", err);
                    continue;
                }

                Bytecode code = Compiler.Bytecode();
                constants = code.Constants;

                if (code.Instructions.Count == 0) // nothing compiled
                    continue;

                VM.NewWithGlobalStore(code, ref globals);

                err = VM.Run();
                if (err != null)
                {
                    System.Console.WriteLine("Woops! Executing bytecode failed:\n {0}", err);
                    continue;
                }

                if (VM.ExitVM)
                    return;

                Object lastPopped = VM.LastPoppedStackElem();

                System.Console.Write(lastPopped.Inspect());
                System.Console.WriteLine();

            }
        }


        public static void printParserErrors(List<string> errors)
        {
            System.Console.WriteLine(MONKEY_FACE);
            System.Console.WriteLine("Woops! We ran into some monkey business here!");
            System.Console.WriteLine(" parse errors:");
            foreach (string msg in errors)
            {
                System.Console.WriteLine("\t" + msg);
            }
        }
    }
}
