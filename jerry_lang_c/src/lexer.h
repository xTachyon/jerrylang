#pragma once

#include "common.h"

enum TokenType {
    TOKEN_IDENT,
    TOKEN_INTEGER,
    TOKEN_SPACE,

    TOKEN_OPEN_PAREN,
    TOKEN_CLOSED_PAREN,
    TOKEN_OPEN_BRACE,
    TOKEN_CLOSED_BRACE,

    TOKEN_COMMA,
    TOKEN_COLON,
    TOKEN_SEMI,
    TOKEN_AMPERSAND,

    TOKEN_EQUAL,
    TOKEN_DOUBLE_EQUAL,
    TOKEN_LESS,
    TOKEN_LESS_EQUAL,
    TOKEN_GREATER,
    TOKEN_GREATER_EQUAL,

    TOKEN_PLUS,
    TOKEN_PLUS_EQUAL,
    TOKEN_MINUS,
    TOKEN_MINUS_EQUAL,
    TOKEN_STAR,
    TOKEN_STAR_EQUAL,
    TOKEN_SLASH,
    TOKEN_SLASH_EQUAL,

    TOKEN_FN,

    TOKEN_END_SIZE,
};

typedef struct {
    enum TokenType type;
    size_t offset;
    size_t size;
} Token;

VECTOR_OF(Token, Token);

VectorOfToken parse_tokens(const char* text, size_t text_size);
void print_tokens(const char* text, const Token* tokens, size_t size);
void remove_spaces(Token* tokens, size_t* size);