typedef struct {
    int x, y;
} Vector2;

char array[3] = {10, 10, 10};

int main(void) {
    Vector2 v = {10, 10};
    v.x++;
    if (v.x != 11)
        return 1;

    v.y--;
    if (v.y != 9)
        return 1;

    if (--array[0] != 9)
        return 1;

    return 0;
}
