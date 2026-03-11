int main(void) {
    int a = 5;
    int b = 3;

    a += b;
    a -= 1;
    a *= 2;
    a /= 3;
    a %= 4;

    a = a & 7;
    a = a | 8;
    a = a ^ 2;
    a = a << 1;
    a = a >> 2;

    return -a;
}