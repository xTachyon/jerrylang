#include "lexer.h"

struct Lexer {
    struct VectorOfToken tokens;
    const char* text;
    size_t text_size;
    size_t offset;
};

static bool is_letter(char ch) {
    return ('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z');
}

static bool is_number(char ch) {
    return '0' <= ch && ch <= '9';
}

static bool is_space(char ch) {
    return strchr(" \n\r\t", ch);
}

static bool is_ident_char(char ch) {
    return is_letter(ch) || is_number(ch) || ch == '_';
}

static bool is_special(char ch) {
    return strchr("(){},:;", ch);
}

struct TokenTypeData {
    const char* name;
    enum TokenType type;
};

enum TokenType get_ident_type(const char* text, size_t size) {
    struct TokenTypeData keywords[] = { { .name = "fn", .type = TOKEN_FN } };

    for (size_t i = 0; i < array_size(keywords); ++i) {
        if (string_compare(text, size, keywords[i].name, strlen(keywords[i].name)) == 0) {
            return keywords[i].type;
        }
    }
    return TOKEN_IDENT;
}

static struct Token parse_ident(struct Lexer* lexer) {
    size_t start_offset = lexer->offset;
    for (; lexer->offset < lexer->text_size; ++lexer->offset) {
        char current = lexer->text[lexer->offset];
        if (!is_ident_char(current)) {
            break;
        }
    }

    size_t size         = lexer->offset - start_offset;
    enum TokenType type = get_ident_type(lexer->text + start_offset, size);
    struct Token result = { .type = TOKEN_IDENT, .offset = start_offset, .size = size };
    return result;
}

static struct Token parse_space(struct Lexer* lexer) {
    size_t start_offset = lexer->offset;
    for (; lexer->offset < lexer->text_size; ++lexer->offset) {
        char current = lexer->text[lexer->offset];
        if (!is_space(current)) {
            break;
        }
    }

    size_t size         = lexer->offset - start_offset;
    struct Token result = { .type = TOKEN_SPACE, .offset = start_offset, .size = size };
    return result;
}

static struct Token parse_special(struct Lexer* lexer) {
    char current = lexer->text[lexer->offset];

    enum TokenType type;
    switch (current) {
    case '(':
        type = TOKEN_OPEN_PAREN;
        break;
    case ')':
        type = TOKEN_CLOSED_PAREN;
        break;
    case ':':
        type = TOKEN_COLON;
        break;
    case ';':
        type = TOKEN_SEMI;
        break;
    case '{':
        type = TOKEN_OPEN_BRACE;
        break;
    case '}':
        type = TOKEN_CLOSED_BRACE;
        break;
    case ',':
        type = TOKEN_COMMA;
        break;
    default:
        bail_out("unknown special");
    }

    struct Token result = { .type = TOKEN_SPACE, .offset = lexer->offset, .size = 1 };
    lexer->offset++;
    return result;
}

static struct Token parse_one(struct Lexer* lexer) {
    char current = lexer->text[lexer->offset];
    if (is_letter(current)) {
        return parse_ident(lexer);
    }
    if (is_space(current)) {
        return parse_space(lexer);
    }
    if (is_special(current)) {
        return parse_special(lexer);
    }

    bail_out_if(false, "unknown token");
}

struct VectorOfToken parse_tokens(const char* text, size_t text_size) {
    struct Lexer lexer = { .text = text, .text_size = text_size, .tokens = create_vector_Token(), .offset = 0 };

    while (lexer.offset < lexer.text_size) {
        struct Token token = parse_one(&lexer);
        vector_push_back(&lexer.tokens, &token);
    }

    return lexer.tokens;
}
