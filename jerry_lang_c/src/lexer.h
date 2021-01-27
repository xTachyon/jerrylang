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
    TOKEN_FN,
};

struct Token {
    enum TokenType type;
    size_t offset;
    size_t size;
};

VECTOR_OF(Token);

struct VectorOfToken parse_tokens(const char* text, size_t text_size);