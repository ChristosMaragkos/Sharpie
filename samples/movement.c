#include "include/sharpie.h"
#include "include/sharpie/defs.h"
#include "include/sharpie/graphics/hardware.h"
#include "include/sharpie/input.h"
#include "include/sharpie/memory.h"
#include "include/sharpie/physics.h"

#define SPR_GROUND 0
#define SPR_PLAYER 1

#define WALK_SPEED 2
#define SPRINT_MULTIPLIER 2
#define JUMP_SPEED 8
#define GRAVITY 1
#define MAX_FALL_SPEED 4

#define PLAYER_SIZE 8
#define GROUND_Y 248
#define GROUND_HEIGHT 8
#define SCREEN_WIDTH 256

Body player;
Body ground;
bool grounded;

static uint16_t oam_after_floor;

int main(void) {
    player = body_create((Vector2){0, GROUND_Y - PLAYER_SIZE},
                         (Vector2){PLAYER_SIZE, PLAYER_SIZE});
    ground = body_create((Vector2){0, GROUND_Y},
                         (Vector2){SCREEN_WIDTH, GROUND_HEIGHT});

    for (int i = 0; i < SCREEN_WIDTH; i += PLAYER_SIZE) {
        draw_sprite(i, GROUND_Y, SPR_GROUND, ATTR_NONE, 0);
    }

    oam_after_floor = get_oam_cursor();

    draw_string("Move: L/R    Jump: A", 0, 0);
    draw_string("Sprint: B", 11, 1);

    while (true) {
        set_oam_cursor(oam_after_floor);

        Button input = get_input(0);

        int mv = 0;
        if (button_down(input, BTN_LEFT) != 0) {
            mv -= 1;
        }
        if (button_down(input, BTN_RIGHT) != 0) {
            mv += 1;
        }
        player.velocity.x = mv * WALK_SPEED;
        if (button_down(input, BTN_B) != 0) {
            player.velocity.x *= SPRINT_MULTIPLIER;
        }

        if (grounded) {
            if (button_down(input, BTN_A) != 0) {
                player.velocity.y = -JUMP_SPEED;
                grounded = false;
            }
        }

        if (!grounded) {
            player.velocity.y += GRAVITY;
            if (player.velocity.y > MAX_FALL_SPEED) {
                player.velocity.y = MAX_FALL_SPEED;
            }
        }

        player.pos.x += player.velocity.x;
        player.pos.y += player.velocity.y;

        if (player.pos.x < 0) {
            player.pos.x = 0;
        }
        if (player.pos.x > SCREEN_WIDTH - PLAYER_SIZE) {
            player.pos.x = SCREEN_WIDTH - PLAYER_SIZE;
        }

        CollisionDirection dir = body_collides(&player, &ground);
        if ((dir & COL_DOWN) != 0) {
            player.pos.y = ground.pos.y - player.size.y;
            player.velocity.y = 0;
            grounded = true;
        }

        draw_sprite(player.pos.x, player.pos.y, SPR_PLAYER, ATTR_NONE, 0);
        yield();
    }

    return 0;
}

// clang-format off
asm(".REGION SPRITE_ATLAS"
    ".SPRITE 0"
    ".DB 0x11, 0x11, 0x11, 0x11,0x11, 0x11, 0x11, 0x11,0x11, 0x11, 0x11, 0x11,0x11, 0x11, 0x11, 0x11,0x11, 0x11, 0x11, 0x11,0x11, 0x11, 0x11, 0x11,0x11, 0x11, 0x11, 0x11,0x11, 0x11, 0x11, 0x11"
    ".SPRITE 1"
    ".DB 0x22, 0x22, 0x22, 0x22,0x22, 0x22, 0x22, 0x22,0x22, 0x22, 0x22, 0x22,0x22, 0x22, 0x22, 0x22,0x22, 0x22, 0x22, 0x22,0x22, 0x22, 0x22, 0x22,0x22, 0x22, 0x22, 0x22,0x22, 0x22, 0x22, 0x22"
    ".ENDREGION");
// clang-format on
