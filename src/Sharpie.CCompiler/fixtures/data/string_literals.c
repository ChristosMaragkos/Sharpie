#include "../headers/sharpie/memory.h"

int main(void) {
    char *a = "Hello from Sharpie";
    char *b = "This is a compiler test";
    char *c = "Hello from Sharpie";

    char *allocated = alloca(20);
    allocated[0] = 'a';
    allocated[19] = 0;

    if (allocated[0] != 'a')
        return 1;
    if (allocated[19] != 0)
        return 1;
    return 0;
}
