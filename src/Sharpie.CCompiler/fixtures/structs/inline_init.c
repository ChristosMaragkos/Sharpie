typedef struct {
    int x, y;
} Point;

int main(void) {
    Point p = {30, 30};
    int arr[3] = {1, 2, 3};

    int result = p.x + p.y + (arr[0] + arr[1] + arr[2]) / 3;
    if (result != 62) return 1;
    return 0;
}
