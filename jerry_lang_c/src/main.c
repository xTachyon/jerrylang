#include <stdio.h>
#include <string.h>
#include "common.h"
#include "lexer.h"
#include "parser.h"
#include "ast.h"
#include "codegen.h"

static const char* read_file(const char* path) {
    FILE* file = fopen(path, "rb");
    bail_out_if(file != NULL, "can't read file");

    size_t file_size = 0;
    uint8 buffer[16 * 1024];
    while (true) {
        size_t ret = fread(buffer, 1, sizeof(buffer), file);
        if (ret == 0) {
            break;
        }

        file_size += ret;
    }
    rewind(file);

    char* result = my_malloc(file_size + 1);
    fread(result, 1, file_size, file);
    result[file_size] = '\0';

    fclose(file);

    return result;
}

int main(int argc, char** argv) {
    const char* file_path       = argv[1];
    const char* file            = read_file(file_path);
    VectorOfToken vector_tokens = parse_tokens(file, strlen(file));
    Token* tokens               = vector_tokens.ptr;
    size_t tokens_size          = vector_tokens.size;
    remove_spaces(tokens, &tokens_size);
    print_tokens(file, tokens, tokens_size);

    AstContext context = { .original_text = file, .memory = create_vector_AstKindPtr() };

    parse(&context, tokens, tokens_size);

    CodeGen* codegen = codegen_create(&context);
}