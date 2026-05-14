#include "../headers/sharpie.h"

char *str = "This should be shared";

int main(void) {
  print(str, 0, 0);

  char *str2 = "This should not";
  print(str2, 0, 1);

  str2[0] = 'Z';

  char str3[] = "This should not";
  print(str3, 0, 2);
}
