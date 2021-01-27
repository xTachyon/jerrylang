#pragma once

#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <stdint.h>
#include <string.h>

void* my_malloc(size_t bytes);

#define bail_out_if(cond, message)                                                                                     \
    do {                                                                                                               \
        if (!(cond)) {                                                                                                 \
            fprintf(stderr, "[%s:%u] %s", __FUNCTION__, __LINE__, message);                                            \
            abort();                                                                                                   \
        }                                                                                                              \
    } while (false)

#define bail_out(message) bail_out_if(false, message)

typedef uint8_t uint8;

#define VECTOR_OF(type)                                                                                                \
    struct VectorOf##type {                                                                                            \
        struct type* ptr;                                                                                              \
        size_t size;                                                                                                   \
        size_t capacity;                                                                                               \
        size_t element_size;                                                                                           \
    };                                                                                                                 \
    inline struct VectorOf##type create_vector_##type() {                                                              \
        struct VectorOf##type vector;                                                                                  \
        vector.ptr          = NULL;                                                                                    \
        vector.size         = 0;                                                                                       \
        vector.capacity     = 0;                                                                                       \
        vector.element_size = sizeof(struct type);                                                                     \
        return vector;                                                                                                 \
    }

struct VectorOfBase {
    uint8* ptr;
    size_t size;
    size_t capacity;
    size_t element_size;
};

void vector_push_back(void* vector, void* element);

int string_compare(const char* first, size_t first_size, const char* second, size_t second_size);

#define array_size(var) sizeof(var) / sizeof(*var)