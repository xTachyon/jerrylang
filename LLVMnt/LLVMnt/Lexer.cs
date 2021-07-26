using System;
using System.Collections.Generic;

namespace LLVMnt {
    enum TokenType {
        SPACE,
        IDENT,
        STRING,
        INTEGER,

        TARGET,
        DATALAYOUT,
        TRIPLE,

        DEFINE,
        DSO_LOCAL,

        LPAREN,
        RPAREN,
        LBRACE,
        RBRACE,
        DOT,
        COMMA,
        EXCLAMATION,
        PERCENT,
        EQ,

        ALLOCA,
        LOAD,
        STORE,
        RET,
        ADD,

        NSW
    }

    struct Token {
        public string Text { get; }
        public TokenType Type { get; }
        public Token(string text, TokenType type) {
            Text = text;
            Type = type;
        }
    }

    class Lexer {
        private string Text { get; }
        private int Offset { get; set; }
        public List<Token> Tokens { get; } = new List<Token>();
        public Lexer(string data) {
            foreach (var i in data) {
                if (char.IsHighSurrogate(i)) {
                    throw new ArgumentException("the lexer doesn't support high surrogates");
                }

                Text = data;
            }
        }
        public void Run() {
            while (Offset < Text.Length) {
                RunOne();
            }
        }
        char First() {
            return Text[Offset];
        }
        char Eat() {
            return Text[Offset++];
        }

        static bool IsIdentStart(char c) {
            return char.IsLetter(c) || c == '_';
        }
        static bool IsIdent(char c) {
            return IsIdentStart(c) || char.IsNumber(c);
        }
        static bool IsSpecial(char c) {
            return "=%{}().,".Contains(c);
        }
        static TokenType GetSpecial(char c) {
            return c switch {
                '=' => TokenType.EQ,
                _ => throw new ArgumentException("unknown special"),
            };
        }
        void RunOne() {
            var first = First();
            if (first == ';') {
                do {
                    first = Eat();
                } while (first != '\n' && first != '\r');
                return;
            }

            if (IsIdentStart(first)) {
                var start = Offset;
                for (; IsIdent(Eat()); ++Offset) ;
                var size = Offset - start - 2;
                var text = Text.Substring(start, size);
                Tokens.Add(new Token(text, TokenType.IDENT));
                return;
            }

            if (first == '"') {

            }

            if (IsSpecial(first)) {
                var type = GetSpecial(first);
                Tokens.Add(new Token(first.ToString(), type));
                Offset++;
                return;
            }

            if (char.IsWhiteSpace(first)) {
                for (; char.IsWhiteSpace(Eat()); ++Offset) ;
                return;
            }
        }
    }
}
