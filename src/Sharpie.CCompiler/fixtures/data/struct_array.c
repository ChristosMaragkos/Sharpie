typedef struct {
    int x, y;
} Vec2;

Vec2 vertices[2] = {{1, 2}, {3, 4}};

const int expected_result = 1 + 2 + 3 + 4;

int main(void) {
    int actual_result =
        vertices[0].x + vertices[0].y + vertices[1].x + vertices[1].y;

    if (expected_result == actual_result)
        return 0;

    return 1;
}
