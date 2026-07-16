#include "include/sharpie.h"
#include "include/sharpie/graphics/hardware.h"
#include "include/sharpie/graphics/software.h"

static const unsigned int SIZE = 64;

BANK(0) Color b1[sizeof(Color) * 64 * 64];
BANK(0) Color b2[sizeof(Color) * 64 * 64];
BANK(0) Color b3[sizeof(Color) * 64 * 64];

int main(void) {
    set_blit_mode(BLT_NONE);
    Image i1 = gen_image_noise(64, 64, b1, 2);
    Image i2 = gen_image_noise(64, 64, b2, 3);
    Image i3 = gen_image_noise(64, 64, b3, 4);
    restart_frame();
    draw_image(&i1, 0, 0);
    draw_image(&i2, 65, 0);
    draw_image(&i3, 130, 0);
    yield();
    return 0;
}
