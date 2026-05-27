int main(void) {
    long a = 0x00010001;
    long b = 0x00200020;
    long c = 0x03D1;
    long d = 0xF0F0;

    long t0 = a + b;
    long t1 = t0 * c;
    long actual = t1 / d;

    if (actual != 34257) return 1;
    return 0;
}
