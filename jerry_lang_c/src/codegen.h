#pragma once

#include "common.h"
#include "ast.h"

typedef struct CodeGen CodeGen;

CodeGen* codegen_create(const AstContext* ast_context);
void codegen_run(CodeGen* codegen);