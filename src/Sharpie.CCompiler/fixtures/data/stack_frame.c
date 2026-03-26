#include "../headers/sharpie.h"

struct Big {
  int a, b, c, d, e, f, g;
};

struct Bigger {
  char a, b, c, d, e, f, g, h;
};

int do_stuff(struct Big *big) {
  struct Bigger bigger = {9, 10, 11, 12, 13, 14, 15, 16};

  void *shit = alloca(sizeof(struct Bigger));

  bigger.a = 99;

  return big->a + bigger.b;
}

int main(void) {
  struct Big big = {1, 2, 3, 4, 5, 6, 7};
  return do_stuff(&big);
}
