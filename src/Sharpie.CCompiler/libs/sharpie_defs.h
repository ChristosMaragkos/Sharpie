#pragma once

typedef unsigned int size_t;

typedef signed int int16_t;
typedef unsigned int uint16_t;

typedef signed char int8_t;
typedef unsigned char uint8_t;

typedef signed long int32_t;
typedef unsigned long uint32_t;

#define BANK(n) __attribute__((annotate("bank_" #n)))

#define NULL ((void *)(0))

typedef struct Vector2 {
    signed int x, y;
} Vector2;

typedef struct Vector3 {
    signed int x, y, z;
} Vector3;

typedef struct ScreenPos {
    uint8_t x, y;
} ScreenPos;
