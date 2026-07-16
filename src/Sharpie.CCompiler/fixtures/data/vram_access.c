#include "../headers/sharpie/graphics/software.h"

int main(void) {
    set_blit_mode(BLT_NONE);

    Color color_white = CLR_WHITE;
    write_pixel(127, 127, color_white);

    Color read = read_pixel(127, 127);
    if (read == CLR_WHITE) {
        return 0;
    } else {
        return 1;
    }
}
