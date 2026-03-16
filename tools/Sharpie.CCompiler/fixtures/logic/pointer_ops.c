int main(void) {
  int x = 500;
  int *p = &x;

  // Manual pointer math
  int *ptr = (int *)1000;
  *ptr = 42;      // STA
  int val = *ptr; // LDP

  return val;
}
