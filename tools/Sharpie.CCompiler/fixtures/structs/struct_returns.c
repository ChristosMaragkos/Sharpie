struct Point {
  int x, y;
};

struct Point make_point(int a, int b) {
  struct Point p;
  p.x = a;
  p.y = b;

  return p; // should trigger a SYS_MEM_COPY
}

int main(void) {
  struct Point p1;

  p1 = make_point(10, 20);

  int imm_x = make_point(100, 200).x;

  return p1.y + imm_x;
}
