struct Point {
  int x, y;
};

int test_registers(int a, struct Point p) { return a + p.x + p.y; }

int test_stack(int a, int b, int c, struct Point p) {
  return a + b + c + p.x + p.y;
}

void test_pointer(struct Point *ptr) { ptr->x = 30; }

int main(void) {
  struct Point p1;
  p1.x = 10;
  p1.y = 20;

  int res1 = test_registers(5, p1);

  struct Point p2;
  p2.x = 100;
  p2.y = 200;

  int res2 = test_stack(1, 2, 3, p2);

  test_pointer(&p2);

  return res1 + res2 + p2.x;
}
