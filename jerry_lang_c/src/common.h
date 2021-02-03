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
typedef uint16_t uint16;
typedef uint64_t uint64;

#define VECTOR_OF(type, name)                                                                                          \
    typedef struct {                                                                                                   \
        type* ptr;                                                                                                     \
        size_t size;                                                                                                   \
        size_t capacity;                                                                                               \
        size_t element_size;                                                                                           \
    } Vector##name;                                                                                                  \
    inline Vector##name create_vector_##name() {                                                                     \
        Vector##name vector;                                                                                         \
        vector.ptr          = NULL;                                                                                    \
        vector.size         = 0;                                                                                       \
        vector.capacity     = 0;                                                                                       \
        vector.element_size = sizeof(type);                                                                            \
        return vector;                                                                                                 \
    }                                                                                                                  

typedef struct VectorBase {
    uint8* ptr;
    size_t size;
    size_t capacity;
    size_t element_size;
} VectorBase;

void vector_push_back(void* vector, void* element);
void delete_vector(void* vector);

int string_compare(const char* first, size_t first_size, const char* second, size_t second_size);

#define array_size(var) sizeof(var) / sizeof(*var)

#define zero_array(var) memset(var + 0, 0, sizeof(var))

VECTOR_OF(void*, Void);
