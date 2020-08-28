namespace parse_rule
{
    enum Precedence
    {
        LOWEST = 0,
        EQUALS = 1,      // ==
        LESSGREATER = 2, // > or <
        SHIFT = 3,       // << or >>
        SUM = 4,         // +
        PRODUCT = 5,     // *
        RAISE = 6,       // x^2
        PREFIX = 7,      // -X or !X
        CALL = 8,        // myFunction(X)
        INDEX = 9,       // array[index]
    }

    enum PrefixParser
    {
        NONE,
        Identifier,
        IntegerLiteral,
        StringLiteral,
        PrefixExpression,
        Boolean,
        GroupedExpression,
        IfExpression,
        FunctionLiteral,
        ArrayLiteral,
        HashLiteral,
    }

    enum InfixParser
    {
        NONE,
        InfixExpression,
        CallExpression,
        IndexExpression,
    }

    /* clox's approach */
    struct ParseRule
    {
        public PrefixParser Prefix;
        public InfixParser Infix;
        public Precedence Precedence;

        public ParseRule(PrefixParser prefix, InfixParser infix, Precedence precedence)
        { this.Prefix = prefix; this.Infix = infix; this.Precedence = precedence; }
    }

    class parse_rule
    {
        static ParseRule NO_PARSE_RULE  = new ParseRule(PrefixParser.NONE, InfixParser.NONE, Precedence.LOWEST);

        public static ParseRule[] Rules =
        {
            NO_PARSE_RULE, //ILLEGAL,
            NO_PARSE_RULE, //EOF,

            // Identifiers + literals
            new ParseRule(PrefixParser.Identifier, InfixParser.NONE, Precedence.LOWEST), //IDENT,
            new ParseRule(PrefixParser.IntegerLiteral, InfixParser.NONE, Precedence.LOWEST), //INT,
            new ParseRule(PrefixParser.StringLiteral, InfixParser.NONE, Precedence.LOWEST), //STRING,

            // Operators
            NO_PARSE_RULE, //ASSIGN,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.SUM), //PLUS,
            new ParseRule(PrefixParser.PrefixExpression, InfixParser.InfixExpression, Precedence.SUM), //MINUS,
            new ParseRule(PrefixParser.PrefixExpression, InfixParser.NONE, Precedence.LOWEST), //BANG,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.PRODUCT), //ASTERISK,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.PRODUCT), //SLASH,

            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.RAISE), //CARET,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.PRODUCT), //PERCENT,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.SHIFT), //SHL,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.SHIFT), //SHR,

            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.LESSGREATER), //LT,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.LESSGREATER), //GT,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.LESSGREATER), //LT_EQ,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.LESSGREATER), //GT_EQ,

            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.EQUALS), //EQ,
            new ParseRule(PrefixParser.NONE, InfixParser.InfixExpression, Precedence.EQUALS), //NOT_EQ,

            // Delimeters
            NO_PARSE_RULE, //COMMA,
            NO_PARSE_RULE, //SEMICOLON,
            NO_PARSE_RULE, //COLON,

            new ParseRule(PrefixParser.GroupedExpression, InfixParser.CallExpression, Precedence.CALL), //LPAREN,
            NO_PARSE_RULE, //RPAREN,
            new ParseRule(PrefixParser.HashLiteral, InfixParser.NONE, Precedence.LOWEST), //LBRACE,
            NO_PARSE_RULE, //RBRACE,
            new ParseRule(PrefixParser.ArrayLiteral, InfixParser.IndexExpression, Precedence.INDEX), //LBRACKET,
            NO_PARSE_RULE, //RBRACKET,

            // Keywords
            new ParseRule(PrefixParser.FunctionLiteral, InfixParser.NONE, Precedence.LOWEST), //FUNCTION,
            NO_PARSE_RULE, //LET,
            new ParseRule(PrefixParser.Boolean, InfixParser.NONE, Precedence.LOWEST), //TRUE,
            new ParseRule(PrefixParser.Boolean, InfixParser.NONE, Precedence.LOWEST), //FALSE,
            new ParseRule(PrefixParser.IfExpression, InfixParser.NONE, Precedence.LOWEST), //IF,
            NO_PARSE_RULE, //ELSE,
            NO_PARSE_RULE, //RETURN,
            NO_PARSE_RULE, //EXIT
        };
    }
}
