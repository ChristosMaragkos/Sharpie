#pragma once
#include "../defs.h"

typedef enum {
    ATTR_NONE = 0,
    ATTR_HFLIP = 1 << 0,
    ATTR_VFLIP = 1 << 1,
    ATTR_HUD = 1 << 2,
    ATTR_BG = 1 << 3,
    ATTR_ALTPAL = 1 << 4
} SpriteAttr;

void __sharpie_delay(int frames);
#define delay(frames) __sharpie_delay(frames)

void __sharpie_pal_reset(void);
#define reset_palette() __sharpie_pal_reset()

void __sharpie_print(const char *str, int grid_x, int grid_y);
#define draw_string(str, x, y) __sharpie_print(str, x, y)

void __sharpie_draw(int x, int y, char id, int attr_and_meta);
#define draw_sprite(x, y, id, attr, type)                                      \
    __sharpie_draw(x, y, id, ((attr) | ((type) << 8)))

void __sharpie_cls(int color);
#define clear_screen(color) __sharpie_cls(color)

void __sharpie_hard_cls(int color);
#define clear_screen_and_oam(color) __sharpie_hard_cls(color)

void __sharpie_cam(int dx, int dy);
#define move_camera(dx, dy) __sharpie_cam(dx, dy)

void __sharpie_set_cam(int x, int y);
#define set_camera(x, y) __sharpie_set_cam(x, y)

void __sharpie_swc(int active, int master);
#define swap_color(active, master) __sharpie_swc(active, master)

int __sharpie_col(int oam_idx);
#define check_collision(idx) __sharpie_col(idx)

int __sharpie_oam_tag(int oam_idx);
#define get_sprite_metadata(idx) __sharpie_get_oam(idx)

int __sharpie_get_oam(void);
#define get_oam_cursor() __sharpie_get_oam()

void __sharpie_set_oam(int cursor);
#define set_oam_cursor(idx) __sharpie_set_oam(idx)

void __sharpie_vblnk(void);
#define yield() __sharpie_vblnk()

void __sharpie_force_vblnk(void);
#define restart_frame() __sharpie_force_vblnk()

void __sharpie_set_cursor(int x, int y);
#define set_cursor(x, y) __sharpie_set_cursor(x, y)

void __sharpie_move_cursor(int x, int y);
#define move_cursor(x, y) __sharpie_move_cursor(x, y)
