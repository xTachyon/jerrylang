#include "ast.h"
#include "parser.h"

typedef struct {
    AstContext* context;
    const Token* tokens;
    size_t tokens_size;
    size_t offset;
} Parser;

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

#define ast_alloc(type) (type*) ast_alloc_impl(parser->context, sizeof(type))

#define ast_alloc_array(type, size) (type*) ast_alloc_impl(parser->context, sizeof(type) * size)

static bool is_token_operator(TokenType type) {
    switch (type) {
    case TOKEN_PLUS:
    case TOKEN_STAR:
    case TOKEN_MINUS:
    case TOKEN_SLASH:
        return true;
    default:
        return false;
    }
}

static Expr* parse_expression(Parser* parser);

static Expr* parse_one_expression(Parser* parser) {
    Token token = get_current_token();
    if (token.type == TOKEN_INTEGER) {
        NumberLiteralExpr* number = ast_alloc(NumberLiteralExpr);
        number->token_number      = token;
        expect_token_eat(TOKEN_INTEGER);
        return (Expr*) number;
    }
    if (token.type == TOKEN_OPEN_PAREN) {
        expect_token_eat(TOKEN_OPEN_PAREN);
        ParenExpr* paren     = ast_alloc(ParenExpr);
        paren->subexpression = parse_expression(parser);
        expect_token_eat(TOKEN_CLOSED_PAREN);
        return (Expr*) paren;
    }

    return NULL;
}

static Expr* parse_expression(Parser* parser) {
    typedef struct {
        union {
            Expr* expr;
            Token token;
        };
        bool is_expr;
    } ExprToken;

    ExprToken expr_tokens[1024];
    size_t expr_tokens_size = 0;
    while (true) {
        bail_out_if(expr_tokens_size < array_size(expr_tokens), "too much stuff in an expr");

        Token token        = get_current_token();
        ExprToken* current = expr_tokens + expr_tokens_size;
        if (is_token_operator(token.type)) {
            current->token   = token;
            current->is_expr = false;
            parser->offset++;
        } else {
            current->expr = parse_one_expression(parser);
            if (current->expr == NULL) {
                break;
            }
            current->is_expr = true;
        }
        ++expr_tokens_size;
    }
}

static Block* parse_variable_assignment(Parser* parser, bool let) {
    if (let) {
        expect_token_eat(TOKEN_LET);
    }
    Token name;
    expect_get_eat(name, TOKEN_IDENT);
    expect_token_eat(TOKEN_EQUAL);
    Expr* init = parse_expression(parser);
    expect_token_eat(TOKEN_SEMI);
}

static Block* parse_block(Parser* parser) {
    expect_token_eat(TOKEN_OPEN_BRACE);

    TokenType current_type = get_current_token().type;
    if (current_type == TOKEN_LET) {
        return parse_variable_assignment(parser, true);
    }
    return NULL;
}

static FunctionItem* parse_function(Parser* parser) {
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

    expect_token_eat(TOKEN_CLOSED_PAREN);

    Block* block = NULL;
    if (get_current_token().type == TOKEN_SEMI) {
        expect_token_eat(TOKEN_SEMI);
    } else {
        block = parse_block(parser);
    }

    FunctionItem* function        = ast_alloc(FunctionItem);
    function->token_function_name = function_name;
    function->name                = parser->context->original_text + function_name.offset;
    function->name_size           = function_name.size;
    function->arguments           = ast_alloc_array(FunctionArgument, arguments_size);
    function->arguments_size      = arguments_size;
    function->block               = block;
    memcpy(function->arguments, arguments, arguments_size * sizeof(*arguments));

    return function;
}

static void do_parse(Parser* parser) {
    if (get_current_token().type == TOKEN_FN) {
        parse_function(parser);
        return;
    }

    bail_out("unexpected token");
}

void parse(AstContext* context, const Token* tokens, size_t size) {
    Parser parser = { .context = context, .tokens = tokens, .tokens_size = size, .offset = 0 };

    while (parser.offset < parser.tokens_size) {
        do_parse(&parser);
    }
}