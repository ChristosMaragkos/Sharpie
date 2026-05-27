struct Point {
    int x, y;
};

void set_x(struct Point *ptr, int val) { ptr->x = val; }

int main(void) {
    struct Point p;
    p.x = 10;
    p.y = 20;

    set_x(&p, 30);

    int result = p.x + p.y;
    if (result != 50) return 1;
    return 0;
}
