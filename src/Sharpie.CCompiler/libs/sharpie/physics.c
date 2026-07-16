#include "physics.h"

static unsigned int _id = 0;

void physics_reset(void) { _id = 0; }

Body body_create(Vector2 pos, Vector2 size) {
    return (Body){
        .pos = pos,
        .size = size,
        .velocity = (Vector2){0, 0},
        .id = _id++,
    };
}

bool body_collides(const Body *b1, const Body *b2) {
    if (b1->id == b2->id)
        return false;

    return b1->pos.x < b2->pos.x + b2->size.x &&
           b1->pos.x + b1->size.x > b2->pos.x &&
           b1->pos.y < b2->pos.y + b2->size.y &&
           b1->pos.y + b1->size.y > b2->pos.y;
}

Body *collision_dispatch(const Body *src, const Body *bodies, size_t count) {
    for (unsigned int i = 0; i < count; ++i) {
        if (body_collides(src, &bodies[i]))
            return (Body *)bodies + i;
    }

    return NULL;
}
