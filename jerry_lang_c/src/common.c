#include <stdlib.h>
#include <string.h>
#include "common.h"

void* my_malloc(size_t bytes) {
    void* result = malloc(bytes);
    bail_out_if(result != NULL, "out of memory");
    return result;
}

static void grow_vector(struct VectorOfBase* vector, size_t with) {
    if (vector->size + with <= vector->capacity) {
        return;
    }
    size_t new_capacity = max(vector->size + with, vector->capacity + vector->capacity / 2);
    uint8* memory       = my_malloc(vector->element_size * new_capacity);
    if (vector->ptr) {
        memcpy(memory, vector->ptr, sizeof(vector->element_size * vector->size));
    }
    free(vector->ptr);
    vector->ptr      = memory;
    vector->capacity = new_capacity;
}

void vector_push_back(void* vector_par, void* element) {
    struct VectorOfBase* vector = (struct VectorOfBase*) vector_par;
    grow_vector(vector, 1);
    memcpy(vector->ptr + vector->element_size * vector->size, element, vector->element_size);
    vector->size++;
}

int string_compare(const char* first, size_t first_size, const char* second, size_t second_size) {
    size_t m = min(first_size, second_size);
    for (size_t i = 0; i < m; ++i) {
        if (first[i] < second[i]) {
            return -1;
        }
        if (first[i] > second[i]) {
            return 1;
        }
    }
    if (first_size < second_size) {
        return -1;
    }
    if (first_size > second_size) {
        return 1;
    }
    return 0;
}
