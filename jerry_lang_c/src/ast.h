#pragma once

#include "lexer.h"

typedef struct {
    int xxx;
} AstKind;
struct Type;
struct Stmt;
typedef struct {
    int xxx;
} Expr;
struct Item;

typedef struct NumberLiteralExpr {
    Token token_number;

    uint64 number;
} NumberLiteralExpr;

typedef struct ParenExpr {
    Expr* subexpression;
} ParenExpr;

typedef struct FunctionArgument {
    Token token_name;
    Token token_type;
} FunctionArgument;

typedef struct Block {
    int xxx;
} Block;

typedef struct FunctionItem {
    Token token_function_name;

    const char* name;
    size_t name_size;
    FunctionArgument* arguments;
    size_t arguments_size;
    Block* block;
} FunctionItem;

VECTOR_OF(AstKind*, AstKindPtr);

typedef struct {
    VectorOfAstKindPtr memory;
    const char* original_text;
} AstContext;

void* ast_alloc_impl(AstContext* context, size_t bytes);
