#include "software.h"
#include "../../sharpie.h"
#include "../memory.h"

void draw_image(const Image *img, int x, int y) {
    int src_x = 0, src_y = 0;
    int w = img->width;
    int h = img->height;

    if (x < 0) {
        src_x = -x;
        w += x;
        x = 0;
    }
    if (y < 0) {
        src_y = -y;
        h += y;
        y = 0;
    }
    if (w <= 0 || h <= 0)
        return;

    if (x + w > SCREEN_WIDTH)
        w = SCREEN_WIDTH - x;
    if (y + h > SCREEN_HEIGHT)
        h = SCREEN_HEIGHT - y;

    for (int row = 0; row < h; ++row) {
        for (int col = 0; col < w; ++col) {
            write_pixel(
                x + col, y + row,
                img->pixels[img->width * (src_y + row) + (src_x + col)]);
        }
    }
}

void draw_image_scaled(const Image *img, int x, int y, uint8_t scale_x,
                       uint8_t scale_y) {
    if (scale_x == 0)
        scale_x = 1;
    if (scale_y == 0)
        scale_y = 1;

    int sx = x < 0 ? 0 : x;
    int sy = y < 0 ? 0 : y;
    int ex = x + (int)img->width * (int)scale_x;
    int ey = y + (int)img->height * (int)scale_y;
    if (ex > SCREEN_WIDTH)
        ex = SCREEN_WIDTH;
    if (ey > SCREEN_HEIGHT)
        ey = SCREEN_HEIGHT;
    if (sx >= ex || sy >= ey)
        return;

    for (int row = sy; row < ey; ++row) {
        for (int col = sx; col < ex; ++col) {
            int tex_x = (col - x) / (int)scale_x;
            int tex_y = (row - y) / (int)scale_y;
            write_pixel(col, row, img->pixels[img->width * tex_y + tex_x]);
        }
    }
}

Image image_deep_copy(const Image *src, Color *copy_buffer) {
    size_t total_pixels = (size_t)src->height * src->width;
    return (Image){
        .width = src->width,
        .height = src->height,
        .pixels = memcpy(copy_buffer, src->pixels, total_pixels),
    };
}

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wint-to-pointer-cast"
Image image_from_sprite(uint8_t sprite_index) {
    return (Image){
        .width = 8,
        .height = 8,
        .pixels = POINTER_TO_SPRITE(sprite_index),
    };
}
#pragma clang diagnostic pop

void draw_image_postprocess(const Image *img, int x, int y,
                            void (*effect)(PixelData *)) {
    int src_x = 0, src_y = 0;
    int w = img->width;
    int h = img->height;

    if (x < 0) {
        src_x = -x;
        w += x;
        x = 0;
    }
    if (y < 0) {
        src_y = -y;
        h += y;
        y = 0;
    }
    if (w <= 0 || h <= 0)
        return;

    if (x + w > SCREEN_WIDTH)
        w = SCREEN_WIDTH - x;
    if (y + h > SCREEN_HEIGHT)
        h = SCREEN_HEIGHT - y;

    for (int row = 0; row < h; ++row) {
        for (int col = 0; col < w; ++col) {
            uint8_t local_x = src_x + col;
            uint8_t local_y = src_y + row;
            PixelData data;
            data.screen_x = x + col;
            data.screen_y = y + row;
            data.tex_x = local_x;
            data.tex_y = local_y;
            data.color = img->pixels[img->width * local_y + local_x];
            effect(&data);
            write_pixel(data.screen_x, data.screen_y, data.color);
        }
    }
}

Image gen_image_noise(uint8_t width, uint8_t height, Color *data_buffer,
                      NoiseDepth depth) {
    const Color noise_colors[4] = {
        CLR_WHITE,
        CLR_BLACK,
        CLR_GRAY,
        CLR_CHARCOAL,
    };

    for (int y = 0; y < height; ++y) {
        for (int x = 0; x < width; ++x) {
            data_buffer[width * y + x] = noise_colors[random(depth)];
        }
    }

    return (Image){
        .width = width,
        .height = height,
        .pixels = data_buffer,
    };
}

Image gen_image_color(uint8_t width, uint8_t height, Color color,
                      Color *data_buffer) {
    size_t total_pixels = (size_t)height * width;
    return (Image){
        .width = width,
        .height = height,
        .pixels = memset(data_buffer, color, total_pixels),
    };
}
