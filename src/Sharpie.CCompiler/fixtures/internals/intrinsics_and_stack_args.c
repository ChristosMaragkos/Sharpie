#include "../headers/sharpie.h"

int add_six_numbers(int a, int b, int c, int d, int e, int f) {
    return a + b + c + d + e + f;
}

int main(void) {
    int sum = add_six_numbers(1, 2, 3, 4, 5, 6);
    if (sum != 21) return 1;
    return 0;
}
