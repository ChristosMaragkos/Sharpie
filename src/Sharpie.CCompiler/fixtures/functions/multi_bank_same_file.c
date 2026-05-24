__attribute__((annotate("bank_1"))) int bank1_func(int x) {
  return x + 100;
}

__attribute__((annotate("bank_2"))) int bank2_func(int y) {
  return y * 2;
}

int fixed_func(int z) {
  return z - 5;
}

int main(void) {
  int a = bank1_func(10);
  int b = bank2_func(a);
  return fixed_func(b);
}
