long global_long = 100000;

long add_long(long a, long b) { return a + b; }

long sub_long(long a, long b) { return a - b; }

long mul_long(long a, long b) { return a * b; }

void test_incdec() {
    long x = 100000;
    x++;
    long y = x;
}

int main() {
    long a = 100000;
    long b = 200000;
    long c = a + b;
    long d = c - a;
    d++;
    if (c > a) {
        return 1;
    }
    a *= 2;
    return 0;
}
