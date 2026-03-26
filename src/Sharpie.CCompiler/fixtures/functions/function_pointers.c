int add(int a, int b) { return a + b; }

int sub(int a, int b) { return a - b; }

int do_math(int (*operation)(int, int), int x, int y) {
  return operation(x, y);
}

int main(void) {
  int (*ops[2])(int, int); // array of function pointers

  ops[0] = add;
  ops[1] = sub;

  int res1 = do_math(add, 10, 5);
  int res2 = do_math(ops[1], 10, 5);

  return res1 + res2;
}
