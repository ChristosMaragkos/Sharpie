#pragma once

typedef unsigned int size_t;

typedef enum {
  ATTR_NONE = 0,
  ATTR_HFLIP = 1 << 0,
  ATTR_VFLIP = 1 << 1,
  ATTR_HUD = 1 << 2,
  ATTR_BG = 1 << 3,
  ATTR_ALTPAL = 1 << 4
} SpriteAttr;

typedef enum {
  SQ_1,
  SQ_2,
  TR_1,
  TR_2,
  SAW_1,
  SAW_2,
  NS_1,
  NS_2,
} AudioChannel;

// BIOS
void *__sharpie_alloca(size_t byteAmount);
void *__sharpie_stackalloc(void *src, size_t byteAmount);
void __sharpie_delay(int frames);
void __sharpie_memcpy(void *dst, void *src, size_t length);
void __sharpie_memset(void *dst, int value, size_t length);
int __sharpie_memcmp(void *ptr1, void *ptr2, size_t length);
void __sharpie_pal_reset(void);

// Hardware
void __sharpie_draw(int x, int y, int id, int attr_and_type);
void __sharpie_cls(int color);
void __sharpie_hard_cls(int color);
void __sharpie_cam(int dx, int dy);
void __sharpie_set_cam(int x, int y);
void __sharpie_swc(int active, int master);
int __sharpie_input(int controller);
int __sharpie_col(int oam_idx);
int __sharpie_oam_tag(int oam_idx);
int __sharpie_get_oam(void);
void __sharpie_set_oam(int cursor);
void __sharpie_play_note(AudioChannel channel, int note, int instr);
void __sharpie_play_song(void *address);
void __sharpie_stop(int channel);
void __sharpie_mute(void);
void __sharpie_hard_mute(void);
void __sharpie_vblnk(void);
void __sharpie_bank(int bank);
void __sharpie_save(void);
void __sharpie_append_save(void);
void __sharpie_halt(void);
int __sharpie_random(int maxExclusive);
void __sharpie_set_cursor(int x, int y);
void __sharpie_move_cursor(int x, int y);

// --- Standard C Aliases ---
#define alloca(size) __sharpie_alloca(size)
#define memcpy(dst, src, len) __sharpie_memcpy(dst, src, len)
#define memset(dst, val, len) __sharpie_memset(dst, val, len)
#define memcmp(p1, p2, len) __sharpie_memcmp(p1, p2, len)

// --- Sharpie Macros & Aliases ---
// Automatically packs Attr (Low Byte) and Type (High Byte)
#define draw_sprite(x, y, id, attr, type)                                      \
  __sharpie_draw(x, y, id, (attr) | ((type) << 8))

#define yield() __sharpie_vblnk()
#define delay(frames) __sharpie_delay(frames)
#define clear_screen(color) __sharpie_cls(color)
#define set_camera(x, y) __sharpie_set_cam(x, y)
#define move_camera(dx, dy) __sharpie_cam(dx, dy)
#define get_input(controller) __sharpie_input(controller)
#define check_collision(idx) __sharpie_col(idx)
#define play_note(ch, note, i) __sharpie_play_note(ch, note, i)
#define play_song(addr) __sharpie_play_song(addr)
#define halt() __sharpie_halt()
#define random(maxExclusive) __sharpie_random(maxExclusive)
#define set_cursor(x, y) __sharpie_set_cursor(x, y)
#define move_cursor(x, y) __sharpie_move_cursor(x, y)
