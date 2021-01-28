#pragma once

#include "lexer.h"

typedef struct {
    int xxx;
} AstKind;
struct Type;
struct Stmt;
struct Expr;
struct Item;

typedef struct {
    Token token_name;
    Token token_type;
} FunctionArgument;

typedef struct {
    int xxx;
} Block;

struct FunctionItem {
    Token token_function_name;

    const char* name;
    size_t name_size;
    FunctionArgument* arguments;
    size_t arguments_size;
    Block* block;
};

VECTOR_OF(AstKind*, AstKindPtr);

typedef struct {
    VectorOfAstKindPtr memory;
} AstContext;