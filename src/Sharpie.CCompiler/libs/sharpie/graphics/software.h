#pragma once
#include "../defs.h"

#define SCREEN_WIDTH 256
#define SCREEN_HEIGHT 256

typedef enum {
    BLT_DEFAULT = 0,
    BLT_NO_TEXT = 1,
    BLT_NO_OAM = 2,
    BLT_NONE = 3
} BlitMode;

typedef enum Color : unsigned char {
    CLR_NONE = 0,
    CLR_WHITE,
    CLR_RED,
    CLR_BLUE,
    CLR_GREEN,
    CLR_YELLOW,
    CLR_MAGENTA,
    CLR_PURPLE,
    CLR_DARK_GREEN,
    CLR_ORANGE,
    CLR_BROWN,
    CLR_DARK_YELLOW,
    CLR_DARK_RED,
    CLR_SKY_BLUE,
    CLR_GRAY,
    CLR_BLACK,
    CLR_PINK,
    CLR_TAN,
    CLR_PEACH,
    CLR_CYAN,
    CLR_LIME,
    CLR_GOLD,
    CLR_LAVENDER,
    CLR_LIGHT_PURPLE,
    CLR_LIGHT_GREEN,
    CLR_LIGHT_ORANGE,
    CLR_LIGHT_BROWN,
    CLR_CHARTREUSE,
    CLR_MINT,
    CLR_BUBBLEGUM,
    CLR_AQUA,
    CLR_CHARCOAL,
} Color;

typedef struct Gradient {
    uint8_t size;
    const Color colors[];
} Gradient;

// Intrinsics

Color __sharpie_read_vram(unsigned int yxPacked);
#define read_pixel(x, y) __sharpie_read_vram((((y) & 0xFF) << 8) | ((x) & 0xFF))

void __sharpie_write_vram(unsigned int yxPacked, Color color);

#define write_pixel(x, y, color)                                               \
    __sharpie_write_vram((((y) & 0xFF) << 8) | ((x) & 0xFF), (color))

void __sharpie_blit_mode(BlitMode mode);
#define set_blit_mode(mode) __sharpie_blit_mode(mode)

// Helpers

#define pixel_is_color(x, y, color) (read_pixel((x), (y)) == (color))

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wint-to-pointer-cast"
#define POINTER_TO_SPRITE(idx) ((Color *)(0xE7FF - (32 * ((idx) + 1))))
#pragma clang diagnostic pop

// Image APIs inspired by raylib. This is the software rendered equivalent to
// sprites. A few differences when using Images:
// - The ability to use all 32 colors of the palette
// - Palette swaps do not affect them
// - They chew through CPU cycles faster than you'd think
typedef struct Image {
    uint8_t width, height;
    const Color *pixels;
} Image;

void draw_image(const Image *img, int x, int y);
void draw_image_scaled(const Image *img, int x, int y, uint8_t scale_x,
                       uint8_t scale_y);

typedef struct PixelData {
    int screen_x, screen_y;
    uint8_t tex_x, tex_y;
    Color color;
} PixelData;

// A postprocessing API that runs a per-pixel hook. Borderline insane to use it
// given the 16k cycle budget but enables some neat visual effects.
#ifdef SUPPORT_POSTPROCESSING
void draw_image_postprocess(const Image *img, int x, int y,
                            void (*effect)(PixelData *));
#endif

// Creates an exact copy of an image. copy_buffer MUST be at least width *
// height in size, but that is never verified at runtime, so allocate
// responsibly.
Image image_deep_copy(const Image *src, Color *copy_buffer);

// Creates an 8x8 Image pointing to a sprite in the sprite atlas region. This
// does not allocate anything.
Image image_from_sprite(uint8_t sprite_index);

// These methods generate images. You have to deliberately opt in to use them.
// As always, you (the programmer) are responsible for allocating a large enough
// pixel buffer.
#ifdef SUPPORT_IMAGE_GENERATION
typedef enum NoiseDepth : unsigned char {
    NOISE_2 = 2,
    NOISE_3 = 3,
    NOISE_4 = 4,
} NoiseDepth;

Image gen_image_color(uint8_t width, uint8_t height, Color color,
                      Color *data_buffer);
Image gen_image_noise(uint8_t width, uint8_t height, Color *data_buffer,
                      NoiseDepth depth);
#endif

#if SUPPORT_SHAPES
#endif
