#include "../headers/sharpie.h"

char *str = "This should be shared";

int main(void) {
    char *str2 = "This should not";
    str2[0] = 'Z';

    char str3[] = "This should not";

    if (str2[0] != 'Z') return 1;
    return 0;
}
