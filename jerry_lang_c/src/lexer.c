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
    return strchr("(){},:;*&", ch);
}

static bool is_operator(char ch) {
    return strchr("<=>", ch);
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

static struct Token parse_number(struct Lexer* lexer) {
    size_t start_offset = lexer->offset;
    for (; lexer->offset < lexer->text_size; ++lexer->offset) {
        char current = lexer->text[lexer->offset];
        if (!is_number(current)) {
            break;
        }
    }

    size_t size         = lexer->offset - start_offset;
    enum TokenType type = get_ident_type(lexer->text + start_offset, size);
    struct Token result = { .type = TOKEN_INTEGER, .offset = start_offset, .size = size };
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
    case '*':
        type = TOKEN_STAR;
        break;
    case '&':
        type = TOKEN_AMPERSAND;
        break;
    default:
        bail_out("unknown special");
    }

    struct Token result = { .type = TOKEN_SPACE, .offset = lexer->offset, .size = 1 };
    lexer->offset++;
    return result;
}

static struct Token parse_operator(struct Lexer* lexer) {
    char current          = lexer->text[lexer->offset];
    bool maybe_has_second = lexer->offset + 1 < lexer->text_size;
    char next             = maybe_has_second ? lexer->text[lexer->offset] : '\0';

    enum TokenType type;
    bool has_second = false;
    if (current == '=') {
        if (next == '=') {
            type       = TOKEN_DOUBLE_EQUAL;
            has_second = true;
        } else {
            type = TOKEN_EQUAL;
        }
    } else if (current == '<') {
        if (next == '=') {
            type       = TOKEN_LESS_EQUAL;
            has_second = true;
        } else {
            type = TOKEN_LESS;
        }
    } else if (current == '>') {
        if (next == '=') {
            type       = TOKEN_GREATER_EQUAL;
            has_second = true;
        } else {
            type = TOKEN_GREATER;
        }
    } else {
        bail_out("unknown operator");
    }

    struct Token result = { .type = type, .offset = lexer->offset, .size = 1 + (size_t) has_second };
    lexer->offset += 1 + (size_t) has_second;
    return result;
}

static struct Token parse_one(struct Lexer* lexer) {
    char current = lexer->text[lexer->offset];
    if (is_letter(current)) {
        return parse_ident(lexer);
    }
    if (is_number(current)) {
        return parse_number(lexer);
    }
    if (is_space(current)) {
        return parse_space(lexer);
    }
    if (is_special(current)) {
        return parse_special(lexer);
    }
    if (is_operator(current)) {
        return parse_operator(lexer);
    }

    bail_out("unknown token");
}

struct VectorOfToken parse_tokens(const char* text, size_t text_size) {
    struct Lexer lexer = { .text = text, .text_size = text_size, .tokens = create_vector_Token(), .offset = 0 };

    while (lexer.offset < lexer.text_size) {
        struct Token token = parse_one(&lexer);
        vector_push_back(&lexer.tokens, &token);
    }

    return lexer.tokens;
}

static const char* get_token_name(enum TokenType type) {
    const char* names[TOKEN_END_SIZE];
    names[TOKEN_IDENT]         = "ident";
    names[TOKEN_INTEGER]       = "integer";
    names[TOKEN_SPACE]         = "space";
    names[TOKEN_OPEN_PAREN]    = "open_paren";
    names[TOKEN_CLOSED_PAREN]  = "closed_paren";
    names[TOKEN_OPEN_BRACE]    = "open_brace";
    names[TOKEN_CLOSED_BRACE]  = "closed_brace";
    names[TOKEN_COMMA]         = "comma";
    names[TOKEN_COLON]         = "colon";
    names[TOKEN_SEMI]          = "semi";
    names[TOKEN_AMPERSAND]     = "ampersand";
    names[TOKEN_STAR]          = "star";
    names[TOKEN_EQUAL]         = "equal";
    names[TOKEN_DOUBLE_EQUAL]  = "double_equal";
    names[TOKEN_LESS]          = "less";
    names[TOKEN_LESS_EQUAL]    = "less_equal";
    names[TOKEN_GREATER]       = "greater";
    names[TOKEN_GREATER_EQUAL] = "greater_equal";
    names[TOKEN_FN]            = "fn";

    return names[type];
}

static void escape(const char* input, size_t input_size, char* output, size_t max_output) {
    size_t m = min(input_size, max_output / 2 - 1);
    for (size_t i = 0; i < m; ++i) {
        char current = input[i];

        const char* add;
        switch (current) {
        case '\n':
            add = "\\n";
            break;
        case '\r':
            add = "\\r";
            break;
        case '\t':
            add = "\\t";
            break;
        default:
            add = NULL;
        }

        if (add) {
            output[0] = add[0];
            output[1] = add[1];
            output += 2;
        } else {
            output[0] = current;
            output++;
        }
    }
    *output = '\0';
}

void print_tokens(const char* text, const struct Token* tokens, size_t size) {
    char buffer[8 * 1024];
    char escaped[4 * 1024];
    for (size_t i = 0; i < size; ++i) {
        const struct Token current = tokens[i];
        escape(text + current.offset, current.size, escaped, sizeof(escaped));
        sprintf(
              buffer,
              "%s[%zu-%zu] : %s\n",
              get_token_name(current.type),
              current.offset,
              current.offset + current.size,
              escaped);

        printf(buffer);
    }
}
