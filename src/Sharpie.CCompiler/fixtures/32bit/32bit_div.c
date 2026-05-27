int main(void) {
    long a = 0xFFFF;
    long b = 0x0050;
    long actual = a / b;
    if (actual != 0x0333) return 1;
    return 0;
}
