struct Point {
    int x, y;
};

void copy_point(struct Point *dst, struct Point *src) {
    dst->x = src->x;
    dst->y = src->y;
}

int main(void) {
    struct Point p1;
    p1.x = 10;
    p1.y = 20;

    struct Point p2;
    copy_point(&p2, &p1);
    p2.x = 100;

    int result = p1.y + p2.x;
    if (result != 120) return 1;
    return 0;
}
