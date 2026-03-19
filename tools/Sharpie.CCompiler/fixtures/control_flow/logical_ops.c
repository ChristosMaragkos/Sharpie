int test_logic(int a, int b, int c, int d) {
  // gotta hide the actual numbers from clang so it doesn't optimize the checks
  if (a == b || c < d && 20 - 10 == 10)
    return 420;
  else
    return 69;
}

int main(void) { return test_logic(1, 2, 3, 5); }
