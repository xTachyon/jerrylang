#include "ast.h"

void* ast_alloc_impl(AstContext* context, size_t bytes) {
    void* memory = my_malloc(bytes);
    vector_push_back(context, &memory);
    return memory;
}

static init_inlined_types(InlinedTypes* inlined) {
    memset(inlined, 0, sizeof(*inlined));

    inlined->type_void.base.kind   = TYPE_PRIMITIVE;
    inlined->type_void.kind        = PRIMITIVE_VOID;
    inlined->type_bool.base.kind   = TYPE_PRIMITIVE;
    inlined->type_bool.kind        = PRIMITIVE_BOOL;
    inlined->type_u64.base.kind    = TYPE_PRIMITIVE;
    inlined->type_u64.kind         = PRIMITIVE_NUMBER;
    inlined->type_u64.integer_size = 64;
    inlined->type_u64.is_unsigned  = true;
}

void ast_context_create(AstContext* ast, const char* original_text) {
    ast->original_text = original_text;
    ast->memory        = create_vector_AstKindPtr();

    init_inlined_types(&ast->inlined_types);

    ast->type_void = (Type*) &ast->inlined_types.type_void;
    ast->type_bool = (Type*) &ast->inlined_types.type_bool;
    ast->type_u64  = (Type*) &ast->inlined_types.type_u64;
}

bool types_equal(const Type* l, const Type* r) {
    if (l->kind != r->kind) {
        return false;
    }
    switch (l->kind) {
    case TYPE_PRIMITIVE: {
        const PrimitiveType* left  = (const PrimitiveType*) l;
        const PrimitiveType* right = (const PrimitiveType*) r;
        return left->kind == right->kind && left->integer_size == right->integer_size &&
               left->is_unsigned == right->is_unsigned;
    }
    }

    abort();
}

bool type_is_void(const Type* t) {
    const PrimitiveType* primitive = (const PrimitiveType*) t;
    return t->kind == TYPE_PRIMITIVE && primitive->kind == PRIMITIVE_VOID;
}
