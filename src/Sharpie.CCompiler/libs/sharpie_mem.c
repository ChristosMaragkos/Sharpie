#include "sharpie_mem.h"
#include "sharpie_defs.h"

struct ArenaAllocator {
    void *data;
    size_t offset;
    size_t size;
};

void arena_init(ArenaAllocator *arena, void *backing, size_t size) {
    arena->data = backing;
    arena->size = size;
    arena->offset = 0;
}

void *arena_malloc(ArenaAllocator *arena, size_t size) {
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
