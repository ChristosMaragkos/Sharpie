#pragma once

#include "defs.h"

size_t __sharpie_strlen(const char *str);
#define strlen(s) __sharpie_strlen(s)

int __sharpie_strcmp(const char *str1, const char *str2);
#define strcmp(s1, s2) __sharpie_strcmp(s1, s2)

size_t __sharpie_strnlen(const char *str, size_t max);
#define strnlen(s, l) __sharpie_strnlen(s, l)

int __sharpie_strncmp(const char *str1, const char *str2, size_t max);
#define strncmp(s1, s2, l) __sharpie_strncmp(s1, s2, l)
