#pragma once
#include "defs.h"

typedef struct Vector2 {
    signed int x, y;
} Vector2;

typedef struct Vector3 {
    signed int x, y, z;
} Vector3;

typedef struct Body {
    Vector2 pos, size, velocity;
    unsigned int id;
} Body;

void physics_reset(void);
Body body_create(Vector2 pos, Vector2 size);
bool body_collides(const Body *b1, const Body *b2);
Body *collision_dispatch(const Body *src, const Body *bodies, size_t count);
