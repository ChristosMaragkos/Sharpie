#pragma once
#include "defs.h"

typedef struct ArenaAllocator ArenaAllocator;

typedef size_t ArenaSnapshot;

void *__sharpie_alloca(size_t byteAmount);
void *__sharpie_stackalloc(void *src, size_t byteAmount);
void *__sharpie_memcpy(void *dst, const void *src, size_t length);
void *__sharpie_memmove(void *dst, const void *src, size_t length);
void *__sharpie_memset(void *dst, uint8_t value, size_t length);
int __sharpie_memcmp(const void *ptr1, const void *ptr2, size_t length);

void *__sharpie_memchr(const void *src, unsigned char c, size_t n);

#define alloca(size) __sharpie_alloca(size)
#define memcpy(dst, src, len) __sharpie_memcpy(dst, src, len)
#define memmove(dst, src, len) __sharpie_memmove(dst, src, len)
#define memset(dst, val, len) __sharpie_memset(dst, val, len)
#define memcmp(p1, p2, len) __sharpie_memcmp(p1, p2, len)

#define memchr(s, c, n) __sharpie_memchr(s, c, n)

#define stackalloc(src, len) __sharpie_stackalloc(src, len)

void arena_init(ArenaAllocator *arena, void *backing, size_t size);
void *arena_alloc(ArenaAllocator *arena, size_t size);
void *arena_alloc_zero(ArenaAllocator *arena, size_t size);
ArenaSnapshot arena_snapshot(ArenaAllocator *arena);
void arena_restore(ArenaAllocator *arena, ArenaSnapshot snapshot);
void arena_free_all(ArenaAllocator *arena);
