#pragma once
#include "sharpie.h"

typedef struct ArenaAllocator ArenaAllocator;

typedef size_t ArenaSnapshot;

void arena_init(ArenaAllocator *arena, void *backing, size_t size);
void *arena_malloc(ArenaAllocator *arena, size_t size);
void *arena_alloc_zero(ArenaAllocator *arena, size_t size);
ArenaSnapshot arena_snapshot(ArenaAllocator *arena);
void arena_restore(ArenaAllocator *arena, ArenaSnapshot snapshot);
void arena_free_all(ArenaAllocator *arena);
