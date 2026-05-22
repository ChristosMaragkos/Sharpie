typedef struct {
    long a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x,
        y, z;
} Padding;

Padding create_padding(void) {
    Padding p = {};
    return p;
}

int main(void) {
    Padding p = create_padding();
    return 0;
}
