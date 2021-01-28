#include "ast.h"
#include "parser.h"

struct Parser {
    const Token* tokens;
    size_t tokens_size;
    size_t offset;
};

#define expect_token(expected)                                                                                         \
    bail_out_if(parser->offset < parser->tokens_size, "no more tokens:(");                                             \
    bail_out_if(parser->tokens[parser->offset].type == expected, "unexpected token");

#define expect_token_eat(expected)                                                                                     \
    expect_token(expected);                                                                                            \
    parser->offset++;

#define get_current_token() parser->tokens[parser->offset]

#define get_current_token_eat() parser->tokens[parser->offset++]

#define expect_get_eat(var, expected)                                                                                  \
    expect_token(expected);                                                                                            \
    var = get_current_token_eat();

static void parse_function(struct Parser* parser) {
    expect_token_eat(TOKEN_FN);
    Token function_name;
    expect_get_eat(function_name, TOKEN_IDENT);
    expect_token_eat(TOKEN_OPEN_PAREN);

    FunctionArgument arguments[32];
    size_t arguments_size = 0;
    while (parser->offset < parser->tokens_size) {
        if (get_current_token().type == TOKEN_CLOSED_PAREN) {
            break;
        }

        bail_out_if(arguments_size != array_size(arguments), "too many arguments in function");

        FunctionArgument* current = arguments + arguments_size;
        expect_get_eat(current->token_name, TOKEN_IDENT);
        expect_token_eat(TOKEN_COLON);
        expect_get_eat(current->token_type, TOKEN_IDENT);

        if (get_current_token().type != TOKEN_COMMA) {
            break;
        }
    }

    expect_token(TOKEN_CLOSED_PAREN);

    if (get_current_token().type != TOKEN_SEMI) {
    }
}

static void do_parse(struct Parser* parser) {
    if (get_current_token().type == TOKEN_FN) {
        parse_function(parser);
        return;
    }

    bail_out("unexpected token");
}

void parse(AstContext* context, const Token* tokens, size_t size) {
    struct Parser parser = { .tokens = tokens, .tokens_size = size, .offset = 0 };

    while (parser.offset < parser.tokens_size) {
        do_parse(&parser);
    }
}