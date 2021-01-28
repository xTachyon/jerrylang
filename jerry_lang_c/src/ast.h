#pragma once

#include "lexer.h"

typedef struct Expr Expr;

typedef struct {
    int xxx;
} AstKind;
struct Type;

typedef enum StmtKind {
    STMT_NONE,
    STMT_VAR_ASSIGN,
} StmtKind;

typedef struct Stmt {
    StmtKind kind;
} Stmt;

typedef struct VariableAssignment {
    Stmt stmt;

    Token token_name;

    const char* name;
    size_t name_size;
    Expr* init;
} VariableAssignment;

typedef enum ExprKind {
    EXPR_NONE,
    EXPR_INTEGER_LITERAL,
    EXPR_PAREN,
    EXPR_UNARY,
    EXPR_BINARY,
    EXPR_VAR,
} ExprKind;

typedef struct Expr {
    ExprKind kind;
} Expr;

typedef struct VariableReferenceExpr {
    Expr expr;

    const char* name;
    size_t name_size;
} VariableReferenceExpr;

typedef enum UnaryKind {
    UNARY_NONE,
    UNARY_MINUS,
    UNARY_PLUS,
    UNARY_ADDRESS_OF,
} UnaryKind;

typedef struct UnaryExpr {
    Expr expr;

    UnaryKind kind;
    Expr* subexpression;
} UnaryExpr;

typedef enum BinaryKind {
    BINARY_NONE,
    BINARY_MINUS,
    BINARY_PLUS,
    BINARY_MUL,
    BINARY_DIV,
} BinaryKind;

typedef struct BinaryExpr {
    Expr expr;

    Expr* left;
    Expr* right;
    BinaryKind kind;
} BinaryExpr;

typedef struct IntegerLiteralExpr {
    Expr expr;

    Token token_number;

    uint64 number;
} IntegerLiteralExpr;

typedef struct ParenExpr {
    Expr expr;

    Expr* subexpression;
} ParenExpr;

typedef struct FunctionArgument {
    Token token_name;
    Token token_type;
} FunctionArgument;

typedef struct Item {
    int xx;
} Item;

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
