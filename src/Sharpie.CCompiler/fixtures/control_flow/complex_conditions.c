#include "../headers/sharpie/physics.c"

Body b1;
Body b2;

int main(void) {
    b1 = body_create((Vector2){0, 0}, (Vector2){10, 10});
    b2 = body_create((Vector2){5, 5}, (Vector2){10, 10});

    bool collide = check_collision(&b1, &b2); // they should collide

    if (!collide) {
        return 1;
    }

    return 0;
}
