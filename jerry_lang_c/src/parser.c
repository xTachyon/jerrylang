#include <inttypes.h>
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

static bool is_binary_token_operator(TokenType type) {
    switch (type) {
    case TOKEN_PLUS:
    case TOKEN_MINUS:
    case TOKEN_STAR:
    case TOKEN_SLASH:
        return true;
    default:
        return false;
    }
}

static BinaryKind parse_binary_operator(TokenType type) {
    switch (type) {
    case TOKEN_PLUS:
        return BINARY_PLUS;
    case TOKEN_MINUS:
        return BINARY_MINUS;
    case TOKEN_STAR:
        return BINARY_MUL;
    case TOKEN_SLASH:
        return BINARY_DIV;
    case TOKEN_AMPERSAND:
        return UNARY_ADDRESS_OF;
    default:
        abort();
    }
}

static Expr* parse_expression(Parser* parser, size_t end);
static Expr* parse_one_expression(Parser* parser);

static bool is_unary_token_operator(TokenType type) {
    switch (type) {
    case TOKEN_PLUS:
    case TOKEN_MINUS:
        return true;
    default:
        return false;
    }
}

static Expr* parse_unary(Parser* parser) {
    Token type           = get_current_token_eat();
    UnaryExpr* unary     = ast_alloc(UnaryExpr);
    unary->base.kind     = EXPR_UNARY;
    unary->subexpression = parse_one_expression(parser);
    switch (type.type) {
    case TOKEN_MINUS:
        unary->kind = UNARY_MINUS;
        break;
    case TOKEN_PLUS:
        unary->kind = UNARY_PLUS;
        break;
    case TOKEN_AMPERSAND:
        unary->kind = UNARY_ADDRESS_OF;
        break;
    default:
        abort();
    }

    return (Expr*) unary;
}

static size_t find_closed_brace(const Parser* parser, size_t start_at) {
    for (size_t i = max(start_at, parser->offset); i < parser->tokens_size; ++i) {
        const Token* current = parser->tokens + i;
        if (current->type == TOKEN_OPEN_BRACE) {
            i = find_closed_brace(parser, i);
        } else if (current->type == TOKEN_CLOSED_PAREN) {
            return i;
        }
    }
    return -1;
}

static IntegerLiteralExpr* parse_integer_literal(Parser* parser) {
    Token token_number;
    expect_get_eat(token_number, TOKEN_INTEGER);

    uint64 the_number;
    char specifier;
    uint16 integer_size;

    sscanf(
          parser->context->original_text + token_number.offset,
          "%" PRIu64 "%c%" PRIu16,
          &the_number,
          &specifier,
          &integer_size);

    IntegerLiteralExpr* number = ast_alloc(IntegerLiteralExpr);
    number->expr.kind          = EXPR_INTEGER_LITERAL;
    number->number             = the_number;
    number->is_unsigned        = specifier == 'u';
    number->integer_size       = integer_size;
    return number;
}

static Expr* parse_one_expression(Parser* parser) {
    Token token = get_current_token();
    if (token.type == TOKEN_INTEGER) {
        return (Expr*) parse_integer_literal(parser);
    }
    if (token.type == TOKEN_OPEN_PAREN) {
        expect_token_eat(TOKEN_OPEN_PAREN);
        ParenExpr* paren     = ast_alloc(ParenExpr);
        paren->expr.kind     = EXPR_PAREN;
        paren->subexpression = parse_expression(parser, find_closed_brace(parser, 0));
        expect_token_eat(TOKEN_CLOSED_PAREN);
        return (Expr*) paren;
    }
    if (token.type == TOKEN_MINUS || token.type == TOKEN_PLUS || token.type == TOKEN_AMPERSAND) {
        return parse_unary(parser);
    }
    if (token.type == TOKEN_IDENT) {
        expect_token_eat(TOKEN_IDENT);
        VariableReferenceExpr* var = ast_alloc(VariableReferenceExpr);
        var->expr.kind             = EXPR_VAR;
        var->name                  = parser->context->original_text + token.offset;
        var->name_size             = token.size;
        return (Expr*) var;
    }

    return NULL;
}

static uint8 get_op_priority(BinaryKind kind) {
    switch (kind) {
    case BINARY_MINUS:
    case BINARY_PLUS:
        return 1;
    case BINARY_MUL:
    case BINARY_DIV:
        return 2;
    default:
        abort();
    }
}

typedef struct ExprToken {
    union {
        Expr* expr;
        BinaryKind binary;
    };
    bool is_expr;
} ExprToken;

static size_t find_lowest_precedence_op(const ExprToken* tokens, size_t size) {
    size_t result  = -1;
    uint8 priority = -1;
    for (size_t i = 0; i < size; ++i) {
        const ExprToken* current = tokens + i;
        if (!current->is_expr && priority > get_op_priority(current->binary)) {
            result   = i;
            priority = get_op_priority(current->binary);
        }
    }
    return result;
}

static BinaryExpr* parse_binary(Parser* parser, const ExprToken* tokens, size_t size) {
    size_t middle     = find_lowest_precedence_op(tokens, size);
    size_t left_size  = middle;
    size_t right_size = size - middle - 1;

    BinaryExpr* binary = ast_alloc(BinaryExpr);
    binary->expr.kind  = EXPR_BINARY;
    if (left_size == 1) {
        binary->left = tokens[0].expr;
    } else {
        binary->left = (Expr*) parse_binary(parser, tokens, middle);
    }

    if (right_size == 1) {
        binary->right = tokens[middle].expr;
    } else {
        binary->right = (Expr*) parse_binary(parser, tokens + middle + 1, size - middle - 1);
    }
    binary->kind = tokens[middle].binary;

    return binary;
}

static Expr* parse_expression(Parser* parser, size_t end) {
    Expr* first = parse_one_expression(parser);
    if (parser->offset == end) {
        return first;
    }

    ExprToken expr_tokens[256];
    size_t expr_tokens_size = 0;
    zero_array(expr_tokens);

    expr_tokens[expr_tokens_size].expr      = first;
    expr_tokens[expr_tokens_size++].is_expr = true;

    bool last_op = false;
    while (parser->offset < end) {
        ExprToken* current = expr_tokens + expr_tokens_size;
        if (last_op) {
            current->expr    = parse_one_expression(parser);
            current->is_expr = true;
        } else {
            current->binary  = parse_binary_operator(get_current_token_eat().type);
            current->is_expr = false;
        }
        expr_tokens_size++;
        last_op = !last_op;
    }
    return (Expr*) parse_binary(parser, expr_tokens, expr_tokens_size);
}

static size_t find_semi(const Parser* parser) {
    for (size_t i = parser->offset; i < parser->tokens_size; ++i) {
        if (parser->tokens[i].type == TOKEN_SEMI) {
            return i;
        }
    }
    abort();
}

static VariableAssignment* parse_variable_assignment(Parser* parser, bool let) {
    if (let) {
        expect_token_eat(TOKEN_LET);
    }
    Token name;
    expect_get_eat(name, TOKEN_IDENT);
    expect_token_eat(TOKEN_EQUAL);
    Expr* init = parse_expression(parser, find_semi(parser));
    expect_token_eat(TOKEN_SEMI);

    VariableAssignment* assign = ast_alloc(VariableAssignment);
    assign->stmt.kind          = STMT_VAR_ASSIGN;
    assign->token_name         = name;
    assign->name               = parser->context->original_text + name.offset;
    assign->name_size          = name.size;
    assign->init               = init;

    return assign;
}

static Block* parse_block(Parser* parser) {
    expect_token_eat(TOKEN_OPEN_BRACE);

    VectorVoid vector = create_vector_Void();
    while (parser->offset < parser->tokens_size) {
        TokenType current_type = get_current_token().type;
        Stmt* stmt;
        if (current_type == TOKEN_LET) {
            stmt = (Stmt*) parse_variable_assignment(parser, true);
        } else {
            abort();
        }
        vector_push_back(&vector, &stmt);
        if (get_current_token().type == TOKEN_CLOSED_BRACE) {
            break;
        }
    }
    expect_token_eat(TOKEN_CLOSED_BRACE);

    Block* block      = ast_alloc(Block);
    block->stmts      = ast_alloc_array(Stmt*, vector.size);
    block->stmts_size = vector.size;
    memcpy(block->stmts, vector.ptr, vector.element_size * vector.size);
    delete_vector(&vector);

    return block;
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
    function->base.kind           = ITEM_FUNCTION;
    function->token_function_name = function_name;
    function->name                = parser->context->original_text + function_name.offset;
    function->name_size           = function_name.size;
    function->arguments           = ast_alloc_array(FunctionArgument, arguments_size);
    function->arguments_size      = arguments_size;
    function->block               = block;
    memcpy(function->arguments, arguments, arguments_size * sizeof(*arguments));

    return function;
}

static Item* do_parse(Parser* parser) {
    if (get_current_token().type == TOKEN_FN) {
        return (Item*) parse_function(parser);
    }

    bail_out("unexpected token");
}

#undef ast_alloc
#define ast_alloc(type) (type*) ast_alloc_impl(ast, sizeof(type))

static void* fix_types_expr(AstContext* ast, Expr* expr);

static void* fix_types_paren(AstContext* ast, ParenExpr* paren) {
    return NULL;
}

static void* fix_types_binary(AstContext* ast, BinaryExpr* binary) {
    binary->left  = fix_types_expr(ast, binary->left);
    binary->right = fix_types_expr(ast, binary->right);

    bail_out_if(types_equal(binary->left->type, binary->right->type), "types not equal");
    binary->expr.type = binary->left->type;
    return NULL;
}

static void* fix_types_unary(AstContext* ast, UnaryExpr* unary) {
    fix_types_expr(ast, unary->subexpression);

    switch (unary->kind) {
    case UNARY_MINUS:
    case UNARY_PLUS:
        unary->base.type = unary->subexpression->type;
        break;
    default:
        abort();
    }
    return NULL;
}

static void* fix_types_integer_literal(AstContext* ast, IntegerLiteralExpr* integer) {
    PrimitiveType* type = ast_alloc(PrimitiveType);
    type->base.kind     = TYPE_PRIMITIVE;
    type->kind          = PRIMITIVE_NUMBER;
    type->integer_size  = integer->integer_size;
    type->is_unsigned   = integer->is_unsigned;
    integer->expr.type  = (Type*) type;
    return NULL;
}

static void* fix_types_var_ref(AstContext* ast, VariableReferenceExpr* var) {
    return NULL;
}

static void* fix_types_expr(AstContext* ast, Expr* expr) {
    ITERATE_EXPRS(ITERATE_DEFAULT, expr, fix_types, ast);

    abort();
}

static void* fix_types_var_assign(AstContext* ast, VariableAssignment* assign) {
    fix_types_expr(ast, assign->init);
    return NULL;
}

static void* fix_types_stmt(AstContext* ast, Stmt* stmt) {
    ITERATE_STMTS(ITERATE_DEFAULT, stmt, fix_types, ast);

    abort();
}

static void* fix_types_block(AstContext* ast, Block* block) {
    for (size_t i = 0; i < block->stmts_size; ++i) {
        fix_types_stmt(ast, block->stmts[i]);
    }

    return NULL;
}

static void* fix_types_function(AstContext* ast, FunctionItem* item) {
    fix_types_block(ast, item->block);
    return NULL;
}

static void* fix_types_item(AstContext* ast, Item* item) {
    ITERATE_ITEMS(ITERATE_DEFAULT, item, fix_types, ast);

    abort();
}

void fix_types(AstContext* ast) {
    for (size_t i = 0; i < ast->items_size; ++i) {
        fix_types_item(ast, ast->items[i]);
    }
}

void parse(AstContext* ast, const Token* tokens, size_t size) {
    Parser parser_owned = { .context = ast, .tokens = tokens, .tokens_size = size, .offset = 0 };
    Parser* parser      = &parser_owned;

    VectorItemPtr items = create_vector_ItemPtr();
    while (parser->offset < parser->tokens_size) {
        Item* item = do_parse(parser);
        vector_push_back(&items, &item);
    }

    ast->items      = ast_alloc_array(Item*, items.size);
    ast->items_size = items.size;
    for (size_t i = 0; i < items.size; ++i) {
        ast->items[i] = items.ptr[i];
    }
    memcpy(ast->items, items.ptr, items.element_size * items.size);
    delete_vector(&items);

    fix_types(ast);
}