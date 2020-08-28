namespace token
{
    enum TokenType
    {
        ILLEGAL,
        EOF,

        // Identifiers + literals
        IDENT,   // add, foobar, x, y, ...
        INT,     // 1343456
        STRING,  // "foobar"

        // Operators
        ASSIGN,
        PLUS,
        MINUS,
        BANG,
        ASTERISK,
        SLASH,
        CARET, // ^ power
        PERCENT, // % modulo
        SHL, // << bit shift to left
        SHR, // >> bit shift to right

        LT,
        GT,
        LT_EQ,
        GT_EQ,

        EQ,
        NOT_EQ,

        // Delimeters
        COMMA,
        SEMICOLON,
        COLON,

        LPAREN,
        RPAREN,
        LBRACE,
        RBRACE,
        LBRACKET,
        RBRACKET,

        // Keywords
        FUNCTION,
        LET,
        TRUE,
        FALSE,
        IF,
        ELSE,
        RETURN,
        EXIT, // EXIT to REPL or terminate a script
    }


    class token
    {
        /*
        Tokens are no longer stored as seprate strings
        intead 2 numbers represent their positoin and length in the full source code
        */
        public static string Source;

        public struct Token
        {
            public TokenType Type;

            // just take positions relative to source string
            public int Start;
            public int Length;

            public string Literal() // Literals are evaulated only when needed
            {
                if (Length == 0)
                    return null;

                return Source.Substring(Start, Length);
            }
        }
    }
}
