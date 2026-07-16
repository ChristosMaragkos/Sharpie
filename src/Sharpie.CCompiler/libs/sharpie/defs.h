#pragma once

typedef unsigned int size_t;

typedef signed int int16_t;
typedef unsigned int uint16_t;

typedef signed char int8_t;
typedef unsigned char uint8_t;

typedef signed long int32_t;
typedef unsigned long uint32_t;

#define bool _Bool
#define true 1
#define false 0

#define BANK(n) __attribute__((annotate("bank_" #n)))

#define NULL ((void *)(0))
