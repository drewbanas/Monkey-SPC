namespace lexer
{
    using token;

    struct Lexer
    {
        public string input;
        public int position; // current position in input (points to current char)
        public int readPosition; // current reading position in input (after current char)
        public char ch; // current char under examination

        TokenType lastTokenType;

        public static Lexer New(string input)
        {
            Lexer l = new Lexer { input = input };
            token.Source = input;

            l.readChar();
            return l;
        }

        public token.Token NextToken()
        {
            token.Token tok = new token.Token();

            this.skipWhitespace();
            skipSoloSemiColons();

            switch (this.ch)
            {
                case '=':
                    if (this.peekChar() == '=')
                    {
                        this.readChar();
                        tok = newToken(TokenType.EQ, 2);
                    }
                    else
                    {
                        tok = newToken(TokenType.ASSIGN, 1);
                    }
                    break;
                case '+':
                    tok = newToken(TokenType.PLUS, 1);
                    break;
                case '-':
                    tok = newToken(TokenType.MINUS, 1);
                    break;
                case '!':
                    if (this.peekChar() == '=')
                    {
                        this.readChar();
                        tok = newToken(TokenType.NOT_EQ, 2);
                    }
                    else
                    {
                        tok = newToken(TokenType.BANG, 1);
                    }
                    break;
                case '/':
                    tok = newToken(TokenType.SLASH, 1);
                    break;
                case '*':
                    tok = newToken(TokenType.ASTERISK, 1);
                    break;
                case '<':
                    if (this.peekChar() == '=')
                    {
                        this.readChar();
                        tok = newToken(TokenType.LT_EQ, 2);
                    }
                    else if (this.peekChar() == '<')
                    {
                        this.readChar();
                        tok = newToken(TokenType.SHL, 2);
                    }
                    else
                    {
                        tok = newToken(TokenType.LT, 1);
                    }
                    break;
                case '>':
                    if (this.peekChar() == '=')
                    {
                        this.readChar();
                        tok = newToken(TokenType.GT_EQ, 2);
                    }
                    else if (this.peekChar() == '>')
                    {
                        this.readChar();
                        tok = newToken(TokenType.SHR, 2);
                    }
                    else
                    {
                        tok = newToken(TokenType.GT, 1);
                    }
                    break;
                case '^':
                    tok = newToken(TokenType.CARET, 1);
                    break;
                case '%':
                    tok = newToken(TokenType.PERCENT, 1);
                    break;
                case ';':
                    tok = newToken(TokenType.SEMICOLON, 1);
                    break;

                case ':':
                    tok = newToken(TokenType.COLON, 1);
                    break;
                case ',':
                    tok = newToken(TokenType.COMMA, 1);
                    break;
                case '{':
                    tok = newToken(TokenType.LBRACE, 1);
                    break;
                case '}':
                    tok = newToken(TokenType.RBRACE, 1);
                    break;
                case '(':
                    tok = newToken(TokenType.LPAREN, 1);
                    break;
                case ')':
                    tok = newToken(TokenType.RPAREN, 1);
                    break;
                case '"':
                    tok.Type = TokenType.STRING;
                    tok.Start = position + 1;
                    tok.Length = this.readString();
                    break;
                case '[':
                    tok = newToken(TokenType.LBRACKET, 1);
                    break;
                case ']':
                    tok = newToken(TokenType.RBRACKET, 1);
                    break;
                case '\0':
                    tok.Length = 0;
                    tok.Type = TokenType.EOF;
                    break;
                default:
                    tok.Start = position;
                    if (isLetter(this.ch))
                    {

                        tok.Length = this.readIdentifier();
                        tok.Type = this.LookupIdent(tok);
                        lastTokenType = tok.Type;
                        return tok;
                    }
                    else if (isDigit(this.ch))
                    {
                        tok.Type = TokenType.INT;
                        tok.Length = this.readNumber();
                        lastTokenType = tok.Type;
                        return tok;
                    }
                    else
                    {
                        tok = newToken(TokenType.ILLEGAL, 1);
                    }
                    break;
            }

            this.readChar();
            lastTokenType = tok.Type;
            return tok;
        }

        /* based on clox */
        void skipWhitespace()
        {

            for (;;)
            {
                switch (this.ch)
                {
                    case ' ':
                    case '\r':
                    case '\t':
                        this.readChar();
                        break;
                    case '\n':
                        this.readChar();
                        break;

                    case '/':
                        if (peekChar() == '/') // comments
                        {
                            while (this.peekChar() != '\n' && !isAtEnd())
                                this.readChar();
                        }
                        else if (peekChar() == '*') // Multi-line comment
                        {
                            for (;;)
                            {
                                this.readChar();

                                if (isAtEnd())
                                    break;

                                if (this.ch == '*')
                                {
                                    if (this.peekChar() == '/')
                                    {
                                        this.readChar();
                                        this.readChar();
                                        break;
                                    }
                                }
                            }

                        }
                        else
                        {
                            return;
                        }
                        break;

                    default:
                        return;
                }
            }
        }

        /*
            So far Monkey doesn't have a use for a series of semicolons
            handling should be different if there are expressions like "for(;;)"
         */
        void skipSoloSemiColons()
        {
            if (lastTokenType != TokenType.SEMICOLON)
                return;

            while (this.ch == ';')
            {
                readChar();
                skipWhitespace();
            }
        }

        void readChar()
        {
            if (this.readPosition >= input.Length)
            {
                this.ch = '\0';
            }
            else
            {
                this.ch = this.input[this.readPosition];
            }
            this.position = this.readPosition;
            this.readPosition += 1;
        }

        char peekChar()
        {
            if (this.position >= this.input.Length)
            {
                return '\0';
            }
            else
            {
                return this.input[this.readPosition];
            }
        }

        bool isAtEnd()
        {
            return this.input[this.readPosition] == '\0';
        }

        /* not used
        char peekNextChar()
        {
            if (isAtEnd())
                return '\0';
            return this.input[this.readPosition + 1];
        }
        */

        int readIdentifier()
        {
            int _start = this.position;
            while (isAlphaNumeric(this.ch)) // NEW: allow digits in identifier names
            {
                this.readChar();
            }
            return this.position - _start;
        }

        int readNumber()
        {
            int _start = this.position;
            while (isDigit(this.ch))
            {
                this.readChar();
            }
            return this.position - _start;
        }

        int readString()
        {
            int _strStart = this.position + 1; // skips the starting quotation mark
            for (;;)
            {
                this.readChar();
                if (this.ch == '"' || this.ch == '\0')
                {
                    break;
                }
            }
            return this.position - _strStart;
        }

        static bool isLetter(char ch)
        {
            return 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z' || ch == '_';
        }

        static bool isDigit(char ch)
        {
            return '0' <= ch && ch <= '9';
        }

        static bool isAlphaNumeric(char ch)
        {
            return isLetter(ch) || isDigit(ch);
        }


         token.Token newToken(TokenType tokenType, int length)
        {
            return new token.Token { Type = tokenType, Start = position, Length = length };
        }

        /*
         Originally in the Token class
         */
        TokenType LookupIdent(token.Token token)
        {
            /* KEYWORDS
             * 
             * else
             * exit
             * false
             * fn
             * if
             * let
             * return
             * true
             * 
             */

            int tokenStart = token.Start;
            if (token.Length == 1) // no single char keywords
                return TokenType.IDENT;

            // clox's approach
            switch (input[tokenStart])
            {
                case 'e':
                    switch (input[tokenStart + 1])
                    {
                        case 'l':
                            return checkKeyword(token, 2, "se", TokenType.ELSE);
                        case 'x':
                            return checkKeyword(token, 2, "it", TokenType.EXIT);
                    }
                    break;

                case 'f':
                    switch (input[tokenStart + 1])
                    {
                        case 'a':
                            return checkKeyword(token, 2, "lse", TokenType.FALSE);

                        case 'n':
                            return checkKeyword(token, 2, "", TokenType.FUNCTION);
                    }
                    break;
                case 'i':
                    return checkKeyword(token, 1, "f", TokenType.IF);
                case 'l':
                    return checkKeyword(token, 1, "et", TokenType.LET);
                case 'r':
                    return checkKeyword(token, 1, "eturn", TokenType.RETURN);
                case 't':
                    return checkKeyword(token, 1, "rue", TokenType.TRUE);

            }

            return TokenType.IDENT;
        }

        TokenType checkKeyword(token.Token token, int tokenOffset, string chkRest, TokenType type)
        {
            int chkLength = chkRest.Length;
            if (token.Length != chkLength + tokenOffset)
                return TokenType.IDENT;

            int offset = token.Start + tokenOffset;
            for (int i = 0; i < chkLength; i++)
            {
                if (input[offset + i] != chkRest[i])
                    return TokenType.IDENT;
            }

            return type;
        }
    }
}
