#include <windows.h>

using function = void(*)();

int main() {
    auto lib = LoadLibraryA("code.dll");

    auto ptr = (function)GetProcAddress(lib, "do_thing");

    ptr();
}