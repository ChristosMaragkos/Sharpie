#include "../headers/sharpie.h"

int main(void) {
    int before = 10;
    int *buffer = (int *)alloca(100);
    *buffer = 42;
    int after = 20;

    int result = before + after + *buffer;
    if (result != 72) return 1;
    return 0;
}
