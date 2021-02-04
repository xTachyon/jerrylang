#include <llvm-c/Core.h>
#include <llvm-c/Analysis.h>
#include "codegen.h"

typedef struct CodeGen {
    const AstContext* ast;

    LLVMContextRef context;
    LLVMModuleRef module;
    LLVMBuilderRef builder;

    LLVMTypeRef type_void;
    LLVMTypeRef type_bool;
} CodeGen;

CodeGen* codegen_create(const AstContext* ast_context) {
    CodeGen* codegen = my_malloc(sizeof(CodeGen));
    codegen->ast     = ast_context;
    codegen->context = LLVMContextCreate();
    bail_out_if(codegen->context, "can't");

    codegen->module  = LLVMModuleCreateWithNameInContext("mouse", codegen->context);
    codegen->builder = LLVMCreateBuilderInContext(codegen->context);

    codegen->type_void = LLVMVoidTypeInContext(codegen->context);
    codegen->type_bool = LLVMInt1TypeInContext(codegen->context);

    return codegen;
}

#define make_string_stack(name_brrr, max_string_size, string, string_size)                                             \
    bail_out_if(string_size + 1 <= max_string_size, "string too big");                                                 \
    char name_brrr[max_string_size];                                                                                   \
    strncpy(name_brrr, string, string_size);                                                                           \
    name_brrr[string_size] = '\0';

static LLVMValueRef codegen_expr(CodeGen* codegen, const Expr* expr);

static LLVMTypeRef translate_primitive(CodeGen* codegen, const PrimitiveType* type) {
    switch (type->kind) {
    case PRIMITIVE_NUMBER:
        return LLVMIntTypeInContext(codegen->context, type->integer_size);
    case PRIMITIVE_BOOL:
        return codegen->type_bool;
    }
    abort();
}

static LLVMTypeRef translate_type(CodeGen* codegen, const Type* type) {
    ITERATE_TYPES(ITERATE_DEFAULT_RETURN, type, translate, codegen);
}

static LLVMValueRef codegen_integer_literal(CodeGen* codegen, const IntegerLiteralExpr* integer) {
    LLVMTypeRef type = translate_type(codegen, integer->expr.type);
    return LLVMConstInt(type, integer->number, !integer->is_unsigned);
}

static LLVMValueRef codegen_paren(CodeGen* codegen, const ParenExpr* expr) {
    return NULL;
}

static LLVMValueRef codegen_var_ref(CodeGen* codegen, const VariableReferenceExpr* expr) {
    return NULL;
}

static LLVMValueRef codegen_unary(CodeGen* codegen, const UnaryExpr* expr) {
    return NULL;
}

static LLVMValueRef codegen_binary(CodeGen* codegen, const BinaryExpr* binary) {
    LLVMValueRef left  = codegen_expr(codegen, binary->left);
    LLVMValueRef right = codegen_expr(codegen, binary->right);

    switch (binary->kind) {
    case BINARY_PLUS:
        return LLVMBuildAdd(codegen->builder, left, right, "");
    }
    abort();
}

static LLVMValueRef codegen_expr(CodeGen* codegen, const Expr* expr) {
    ITERATE_EXPRS(ITERATE_DEFAULT_RETURN, expr, codegen, codegen);
}

static void codegen_var_assign(CodeGen* codegen, const VariableAssignment* var) {
    make_string_stack(name, MAX_FUNCTION_SIZE, var->name, var->name_size);

    LLVMValueRef value = codegen_expr(codegen, var->init);
    LLVMTypeRef type   = LLVMTypeOf(value);
    LLVMValueRef alloc = LLVMBuildAlloca(codegen->builder, type, name);

    LLVMBuildStore(codegen->builder, value, alloc);
}

static void codegen_stmt(CodeGen* codegen, const Stmt* stmt) {
    ITERATE_STMTS(ITERATE_DEFAULT_RETURN_VOID, stmt, codegen, codegen);
}

static void codegen_block(CodeGen* codegen, const Block* block) {
    for (size_t i = 0; i < block->stmts_size; ++i) {
        codegen_stmt(codegen, block->stmts[i]);
    }
}

static void codegen_function(CodeGen* codegen, const FunctionItem* function) {
    make_string_stack(name, MAX_FUNCTION_SIZE, function->name, function->name_size);

    LLVMTypeRef function_type = LLVMFunctionType(codegen->type_void, NULL, 0, false);

    LLVMValueRef l_function = LLVMAddFunction(codegen->module, name, function_type);
    LLVMBasicBlockRef entry = LLVMAppendBasicBlockInContext(codegen->context, l_function, name);
    LLVMPositionBuilderAtEnd(codegen->builder, entry);

    codegen_block(codegen, function->block);

    if (type_is_void(function->return_type)) {
        LLVMBuildRetVoid(codegen->builder);
    }
}

static void codegen_item(CodeGen* codegen, const Item* item) {
    ITERATE_ITEMS(ITERATE_DEFAULT_RETURN_VOID, item, codegen, codegen);
}

void codegen_run(CodeGen* codegen) {
    for (size_t i = 0; i < codegen->ast->items_size; ++i) {
        codegen_item(codegen, codegen->ast->items[i]);
    }

    printf("\n\n");
    if (LLVMPrintModuleToFile(codegen->module, "llvm.ir", NULL) == 1) {
        abort();
    }

    bool huh = LLVMVerifyModule(codegen->module, LLVMAbortProcessAction, NULL);
}
