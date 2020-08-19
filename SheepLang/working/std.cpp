#include <cstdio>

extern "C" {
    
void println_string(const char* string) {
    printf("%s\n", string);
}

void println_number(__int64 number) {
    long long n = number;
    printf("%llu\n", n);
}

void salut();

__declspec(dllexport) void do_thing() {
    salut();
}

}