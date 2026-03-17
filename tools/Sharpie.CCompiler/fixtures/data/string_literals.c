#include "../headers/sharpie.h"

int main(void) {
  char *a = "Hello from Sharpie";
  char *b = "This is a compiler test";
  char *c = "Hello from Sharpie";

  __sharpie_print("Something in the way", 0, 0);

  char *allocated = alloca(20);
  allocated[0] = 'a';

  allocated[19] = 0;

  print(allocated, 0, 0);

  return 0;
}
