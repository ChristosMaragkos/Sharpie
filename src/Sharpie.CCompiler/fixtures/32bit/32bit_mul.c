int main(void) {
    long a = 0x0050;
    long b = 0xFFFF;
    long actual = a * b;
    if (actual != 0x004FFFB0)
        return 1;
    return 0;
}
