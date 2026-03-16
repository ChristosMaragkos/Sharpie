#include "../headers/sharpie.h"

int main(void) {
  int before = 10;

  int *buffer = (int *)alloca(100);

  *buffer = 42;

  int after = 20;

  return before + after + *buffer;
}
