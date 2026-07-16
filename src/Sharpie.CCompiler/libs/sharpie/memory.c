#include "memory.h"
#include "defs.h"

void *__builtin_sharpie_memchr(const void *src, unsigned char c, size_t n) {
    for (int i = 0; i < n; ++i) {
        if (((unsigned char *)(src))[i] == c) {
            return (void *)(src + i);
        }
    }

    return NULL;
}

void arena_init(ArenaAllocator *arena, void *backing, size_t size) {
    arena->data = backing;
    arena->size = size;
    arena->offset = 0;
}

void *arena_alloc(ArenaAllocator *arena, size_t size) {
    void *ptr = arena->data + arena->offset;
    if (arena->offset + size > arena->size) {
        return NULL;
    } else {
        arena->offset += size;
        return ptr;
    }
}

void *arena_alloc_zero(ArenaAllocator *arena, size_t size) {
    void *ptr = arena->data + arena->offset;
    if (arena->offset + size > arena->size) {
        return NULL;
    } else {
        memset(ptr, 0, size);
        arena->offset += size;
        return ptr;
    }
}

void arena_free_all(ArenaAllocator *arena) { arena->offset = 0; }

ArenaSnapshot arena_snapshot(ArenaAllocator *arena) { return arena->offset; }

void arena_restore(ArenaAllocator *arena, ArenaSnapshot snapshot) {
    arena->offset = snapshot;
}
