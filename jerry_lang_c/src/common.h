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
typedef uint64_t uint64;

#define VECTOR_OF(type, name)                                                                                          \
    typedef struct {                                                                                                   \
        type* ptr;                                                                                                     \
        size_t size;                                                                                                   \
        size_t capacity;                                                                                               \
        size_t element_size;                                                                                           \
    } VectorOf##name;                                                                                                  \
    inline VectorOf##name create_vector_##name() {                                                                     \
        VectorOf##name vector;                                                                                         \
        vector.ptr          = NULL;                                                                                    \
        vector.size         = 0;                                                                                       \
        vector.capacity     = 0;                                                                                       \
        vector.element_size = sizeof(type);                                                                            \
        return vector;                                                                                                 \
    }                                                                                                                  \
    inline void delete_vector_##name(VectorOf##name* vector) {                                                         \
        free(vector->ptr);                                                                                             \
        vector->ptr      = NULL;                                                                                       \
        vector->size     = 0;                                                                                          \
        vector->capacity = 0;                                                                                          \
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

#define zero_array(var) memset(var + 0, 0, sizeof(var))

VECTOR_OF(void*, Void);
