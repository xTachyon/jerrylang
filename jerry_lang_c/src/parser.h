#pragma once

#include "ast.h"
#include "common.h"
#include "lexer.h"

void parse(AstContext* context, const Token* tokens, size_t size);