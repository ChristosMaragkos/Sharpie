#include "string.h"

size_t __sharpie_strlen(const char *str) {
    size_t s = 0;
    while (str[s] != '\0') {
        ++s;
    }
    return s;
}

size_t __sharpie_strnlen(const char *str, size_t max) {
    size_t s = 0;
    while (str[s] != '\0' && s < max) {
        ++s;
    }
    return s;
}

int __sharpie_strcmp(const char *str1, const char *str2) {
    unsigned int i = 0;
    while (str1[i] == str2[i]) {
        ++i;
    }

    return str1[i] - str2[i];
}

int __sharpie_strncmp(const char *str1, const char *str2, size_t max) {
    unsigned int i = 0;
    while (str1[i] == str2[i]) {
        ++i;

        if (i == max)
            return 0;
    }

    return str1[i] - str2[i];
}
