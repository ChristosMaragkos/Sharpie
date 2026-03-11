int main(void) {
  int x = 0;
  for (int i = 0; i < 10; i++) {
    x++;
  }

  int y = 0;
  int j = 0;
  while (j < 9) {
    y += j;
    j++;
  }

  int z = 1000;
  int k = 0;
  do {
    z -= k;
    k++;
  } while (k < 10);
}
