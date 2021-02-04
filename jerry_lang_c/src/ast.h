#pragma once

#include "lexer.h"

typedef struct Expr Expr;

typedef struct {
    int xxx;
} AstKind;

typedef enum TypeKind {
    TYPE_NONE,
    TYPE_PRIMITIVE,
} TypeKind;

typedef struct Type {
    TypeKind kind;
} Type;

typedef enum PrimitiveKind {
    PRIMITIVE_NUMBER,
    PRIMITIVE_BOOL,
    PRIMITIVE_VOID,
} PrimitiveKind;

typedef struct PrimitiveType {
    Type base;

    PrimitiveKind kind;
    uint16 integer_size;
    bool is_unsigned;
} PrimitiveType;

typedef enum StmtKind {
    STMT_NONE,
    STMT_VAR_ASSIGN,
    STMT_RETURN,
} StmtKind;

typedef struct Stmt {
    StmtKind kind;
} Stmt;

typedef struct ReturnStmt {
    Stmt base;

    Expr* subexpr;
} ReturnStmt;

typedef struct VariableAssignment {
    Stmt stmt;

    Token token_name;

    const char* name;
    size_t name_size;
    Expr* init;
    bool is_decl : 1;
} VariableAssignment;

typedef enum ExprKind {
    EXPR_NONE,
    EXPR_INT_LIT,
    EXPR_BOOL_LIT,
    EXPR_PAREN,
    EXPR_UNARY,
    EXPR_BINARY,
    EXPR_VAR,
} ExprKind;

typedef struct Expr {
    ExprKind kind;
    Type* type;
} Expr;

typedef struct VariableReferenceExpr {
    Expr expr;

    Token token_name;
    VariableAssignment* declaration;
} VariableReferenceExpr;

typedef enum UnaryKind {
    UNARY_NONE,
    UNARY_MINUS,
    UNARY_PLUS,
    UNARY_ADDRESS_OF,
} UnaryKind;

typedef struct UnaryExpr {
    Expr base;

    UnaryKind kind;
    Expr* subexpression;
} UnaryExpr;

typedef enum BinaryKind {
    BINARY_NONE,
    BINARY_MINUS,
    BINARY_PLUS,
    BINARY_MUL,
    BINARY_DIV,

    BINARY_EQ,
    BINARY_NOT_EQ,
} BinaryKind;

typedef struct BinaryExpr {
    Expr expr;

    Expr* left;
    Expr* right;
    BinaryKind kind;
} BinaryExpr;

typedef struct IntLitExpr {
    Expr expr;

    uint64 number;
    bool is_unsigned;
    uint16 integer_size;

} IntLitExpr;

typedef struct BoolLitExpr {
    Expr expr;

    bool value;
} BoolLitExpr;

typedef struct ParenExpr {
    Expr expr;

    Expr* subexpression;
} ParenExpr;

typedef struct FunctionArgument {
    Token token_name;
    Token token_type;
} FunctionArgument;

typedef enum ItemKind {
    ITEM_FUNCTION,
} ItemKind;

typedef struct Item {
    ItemKind kind;
} Item;

typedef struct Block {
    Stmt** stmts;
    size_t stmts_size;
} Block;

typedef struct FunctionItem {
    Item base;

    Token token_function_name;
    Token token_return_type;

    const char* name;
    size_t name_size;
    FunctionArgument* arguments;
    size_t arguments_size;
    Block* block;
    Type* return_type;
} FunctionItem;

VECTOR_OF(AstKind*, AstKindPtr);

typedef struct InlinedTypes {
    PrimitiveType type_void;
    PrimitiveType type_bool;
    PrimitiveType type_u64;
} InlinedTypes;

typedef struct {
    VectorAstKindPtr memory;
    const char* original_text;

    InlinedTypes inlined_types;

    Type* type_void;
    Type* type_bool;
    Type* type_u64;

    Item** items;
    size_t items_size;
} AstContext;

void* ast_alloc_impl(AstContext* context, size_t bytes);

void ast_context_create(AstContext* ast, const char* original_text);

bool types_equal(const Type* l, const Type* r);
bool type_is_void(const Type* t);
bool type_is_number(const Type* t);

enum { MAX_FUNCTION_SIZE = 255 };

VECTOR_OF(Item*, ItemPtr);

#define ITERATE_DEFAULT_RETURN(var, name, value, type, function_to_call, arg)                                          \
    case value:                                                                                                        \
        return function_to_call##_##name(arg, (type*) var);

#define ITERATE_DEFAULT_RETURN_VOID(var, name, value, type, function_to_call, arg)                                     \
    case value:                                                                                                        \
        function_to_call##_##name(arg, (type*) var);                                                                   \
        return;

#define ITERATE_ITEMS(impl, var, function_to_call, arg)                                                                \
    switch (var->kind) { impl(var, function, ITEM_FUNCTION, FunctionItem, function_to_call, arg) }

#define ITERATE_TYPES(impl, var, function_to_call, arg)                                                                \
    switch (var->kind) {                                                                                               \
        impl(var, primitive, TYPE_PRIMITIVE, PrimitiveType, function_to_call, arg);                                    \
    default:                                                                                                           \
        abort();                                                                                                       \
    }

#define ITERATE_STMTS(impl, var, function_to_call, arg)                                                                \
    switch (var->kind) {                                                                                               \
        impl(var, var_assign, STMT_VAR_ASSIGN, VariableAssignment, function_to_call, arg);                             \
        impl(var, return, STMT_RETURN, ReturnStmt, function_to_call, arg);                                             \
    default:                                                                                                           \
        abort();                                                                                                       \
    }

#define ITERATE_EXPRS(impl, var, function_to_call, arg)                                                                \
    switch (var->kind) {                                                                                               \
        impl(var, binary, EXPR_BINARY, BinaryExpr, function_to_call, arg);                                             \
        impl(var, unary, EXPR_UNARY, UnaryExpr, function_to_call, arg);                                                \
        impl(var, int_lit, EXPR_INT_LIT, IntLitExpr, function_to_call, arg);                                           \
        impl(var, bool_lit, EXPR_BOOL_LIT, BoolLitExpr, function_to_call, arg);                                        \
        impl(var, paren, EXPR_PAREN, ParenExpr, function_to_call, arg);                                                \
        impl(var, var_ref, EXPR_VAR, VariableReferenceExpr, function_to_call, arg);                                    \
    default:                                                                                                           \
        abort();                                                                                                       \
    }