#include "../headers/sharpie.h"

int main(void) {
    set_blit_mode(BLT_NONE);

    char color_white = 1;
    write_vram(127, 127, color_white);

    return 0;
}
