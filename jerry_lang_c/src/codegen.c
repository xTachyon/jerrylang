#include <llvm-c/Core.h>
#include "codegen.h"

typedef struct CodeGen {
    LLVMContextRef context;
    const AstContext* ast_context;
} CodeGen;

CodeGen* codegen_create(const AstContext* ast_context) {
    CodeGen* codegen = my_malloc(sizeof(CodeGen));
    codegen->context = LLVMContextCreate();
    bail_out_if(codegen->context, "can't");
    codegen->ast_context = ast_context;

    return codegen;
}

void codegen_run(CodeGen* codegen) {
    int x = 5;
}
