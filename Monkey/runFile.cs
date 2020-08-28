namespace runFile
{
    using lexer;
    //using token;
    //using Object;
    using compiler;
    using vm;
    using util;

    using error = System.String;

    class runFile
    {
        static string readFile(string path)
        {
            System.Text.StringBuilder buffer = null;
            if (!System.IO.File.Exists(path))
            {
                System.Console.WriteLine("Could not open file {0}.", path);
                System.Environment.Exit(74);
            }
            buffer = new System.Text.StringBuilder(System.IO.File.ReadAllText(path));
            buffer.Append('\0');
            if (buffer == null)
            {
                System.Console.WriteLine("Not enough memory to read {0}.", path);
                System.Environment.Exit(74);
            }

            return buffer.ToString();
        }

        public static void Start(string path)
        {
            // Benchmarking variables
            System.DateTime start = new System.DateTime();
            System.TimeSpan duration = new System.TimeSpan();
            Object.Object result = null;

            string input = readFile(path);
            Lexer l = Lexer.New(input);
            Compiler.InitParser(l);
            Compiler.New();

            error err = Compiler.ParseProgram();

            if (Compiler.ParseErrors().Count != 0)
            {
                repl.repl.printParserErrors(Compiler.ParseErrors());
                System.Console.ReadKey();
                System.Environment.Exit(77);
            }

            if (err != null)
            {
                System.Console.WriteLine("Woops! Compilation failed:\n {0}", err);
                System.Console.ReadKey();
                System.Environment.Exit(78);
            }

            Bytecode code = Compiler.Bytecode();
            if (code.Instructions.Count == 0) // nothing compiled
                return;

            VM.New(code);

            if (flag.EnableBenchmark)
            {
                start = System.DateTime.Now;
            }

            err = VM.Run();
            if (err != null)
            {
                System.Console.WriteLine("Woops! Executing bytecode failed:\n {0}", err);
                System.Console.ReadKey();
                System.Environment.Exit(79);
            }

            if (flag.EnableBenchmark)
            {
                duration = System.DateTime.Now.Subtract(start);
            }

            Object.Object lastPopped = VM.LastPoppedStackElem();
            if (lastPopped.Type() != Object.ObjectType.NULL_OBJ)
                System.Console.Write(lastPopped.Inspect());

            System.Console.WriteLine();

            if (flag.EnableBenchmark)
                result = lastPopped;

            if (flag.EnableBenchmark)
            {
                System.Console.WriteLine
                    (
                    "\nresult={0}, duration={1}s",
                    result.Inspect(),
                    duration.TotalSeconds.ToString()
                    );

                System.Console.ReadKey();
            }
        }
    }
}
