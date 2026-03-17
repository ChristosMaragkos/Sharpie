typedef struct {
  int x, y;
} Point;

int main(void) {
  Point p = {30, 30};
  int arr[3] = {1, 2, 3};

  return p.x + p.y + (arr[0] + arr[1] + arr[2]) / 3;
}
