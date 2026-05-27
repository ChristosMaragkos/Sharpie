typedef struct {
    int x;
    int y;
} Point;

int main(void) {
    Point p1;
    p1.x = 10;
    p1.y = 20;

    Point *ptr = &p1;
    ptr->x = 30;

    int result = p1.x + p1.y;
    if (result != 50) return 1;
    return 0;
}
