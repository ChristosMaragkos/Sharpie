int main(void) {
    long a = 0x00090009;
    long b = 0x00050005;
    long actual = a - b;
    if (actual != 0x00040004) return 1;
    return 0;
}
