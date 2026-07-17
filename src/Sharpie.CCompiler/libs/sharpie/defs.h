#pragma once

typedef unsigned int size_t;

typedef signed int int16_t;
typedef unsigned int uint16_t;

#define INT16_MIN -32768
#define INT16_MAX 32767
#define UINT16_MIN 0
#define UINT16_MAX 65535

typedef signed char int8_t;
typedef unsigned char uint8_t;

#define INT8_MIN -128
#define INT8_MAX 127
#define UINT8_MIN 0
#define UINT8_MAX 255

typedef signed long int32_t;
typedef unsigned long uint32_t;

#define INT32_MIN -2147483648l
#define INT32_MAX 2147483647l
#define UINT32_MIN 0l
#define UINT32_MAX 4294967295l

#define bool _Bool
#define true 1
#define false 0

#define BANK(n) __attribute__((annotate("bank_" #n)))

#define NULL ((void *)(0))
