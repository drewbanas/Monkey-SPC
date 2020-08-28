namespace util
{
    class hash
    {
        // Taken from clox
        public static uint hashString(string key)
        {
            uint hash = 2166136261u;
            for (int i = 0; i < key.Length; i++)
            {
                hash ^= key[i];
                hash *= 16777619;
            }
            return hash;
        }
    }

    class flag
    {
        public enum runType
        {
            file,
            repl,
        }

        public static runType RunType;
        public static bool EnableBenchmark;
        public static int ArgsFileIndex;

        /*
         * Quick and dirty non-general function
         * for checking command line flags
         */
        public static void Parse(string[] args)
        {
            // defaults
            RunType = runType.repl;
            EnableBenchmark = false;
            ArgsFileIndex = 0;

            for(int i = 0; i < args.Length; i++)
            {
                string s = args[i];
                if (s.StartsWith("-b"))
                {
                    EnableBenchmark = true;
                }
                else
                {
                    ArgsFileIndex = i;
                }
            }

            if (EnableBenchmark && args.Length > 1)
                RunType = runType.file;

            if (!EnableBenchmark && args.Length > 0)
                RunType = runType.file;
        }
    }
}
