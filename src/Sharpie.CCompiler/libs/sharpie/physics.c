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

CollisionDirection body_collides(const Body *b1, const Body *b2) {
    if (b1->id == b2->id)
        return COL_NONE;

    CollisionDirection direction = COL_NONE;

    if (!(b1->pos.x < b2->pos.x + b2->size.x &&
          b1->pos.x + b1->size.x > b2->pos.x))
        return COL_NONE;

    if (b2->pos.x + b2->pos.x + b2->size.x < b1->pos.x + b1->pos.x + b1->size.x)
        direction |= COL_LEFT;
    else if (b2->pos.x + b2->pos.x + b2->size.x > b1->pos.x + b1->pos.x + b1->size.x)
        direction |= COL_RIGHT;

    if (!(b1->pos.y < b2->pos.y + b2->size.y &&
          b1->pos.y + b1->size.y > b2->pos.y))
        return COL_NONE;

    if (b2->pos.y + b2->pos.y + b2->size.y < b1->pos.y + b1->pos.y + b1->size.y)
        direction |= COL_UP;
    else if (b2->pos.y + b2->pos.y + b2->size.y > b1->pos.y + b1->pos.y + b1->size.y)
        direction |= COL_DOWN;

    return direction;
}

Body *collision_dispatch(const Body *src, const Body *bodies, size_t count,
                         CollisionDirection *direction) {
    for (unsigned int i = 0; i < count; ++i) {
        CollisionDirection dir = body_collides(src, &bodies[i]);
        if (dir != COL_NONE) {
            if (direction)
                *direction = dir;
            return (Body *)bodies + i;
        }
    }

    if (direction)
        *direction = COL_NONE;
    return NULL;
}
