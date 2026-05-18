#include "../headers/sharpie.h"

int main(void) {
  set_blit_mode(NONE);

  char color_white = 1;
  char halted = 0;
  write_vram(127, 127, color_white);

  while (halted == 0) {
    halted = get_input(0);
  }

  return 0;
}
