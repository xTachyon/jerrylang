#include "ast.h"

void* ast_alloc_impl(AstContext* context, size_t bytes) {
    void* memory = my_malloc(bytes);
    vector_push_back(context, &memory);
    return memory;
}