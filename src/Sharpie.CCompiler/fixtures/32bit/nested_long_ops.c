long nested_add(long a, long b, long c, long d) {
    return ((a + b) + (c + d)) + ((a + c) + (b + d));
}

long nested_mul(long a, long b, long c, long d) {
    return ((a * b) + (c * d)) * ((a * c) + (b * d));
}

long nested_mixed(long a, long b, long c, long d) {
    return ((a + b) * (c - d)) + ((a - c) * (b + d));
}

long deep_nest(long a) {
    return ((((a + 1) * 2) + ((a + 3) * 4)) + (((a + 5) * 6) + ((a + 7) * 8))) / 2;
}

int main() {
    long a = 100000;
    long b = 200000;
    long c = 300000;
    long d = 400000;
    long r1 = nested_add(a, b, c, d);
    long r2 = nested_mul(a, b, c, d);
    long r3 = nested_mixed(a, b, c, d);
    long r4 = deep_nest(a);
    return (int)(r1 + r2 + r3 + r4);
}
