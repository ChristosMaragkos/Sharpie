#include "../headers/sharpie.h"

int add_six_numbers(int a, int b, int c, int d, int e, int f) {
  return a + b + c + d + e + f;
}

int test_memory() {
  int *buffer = alloca(sizeof(int) * 10);

  memset(buffer, 255, sizeof(int) * 10);

  return 1; // We haven't implemented array indexing yet, so just
            // returning 1 is fine
}

int main() {
  int sum = add_six_numbers(1, 2, 3, 4, 5, 6);

  clear_screen(0);
  draw_sprite(10, 20, 5, ATTR_HFLIP | ATTR_ALTPAL, 2);

  return sum;
}
