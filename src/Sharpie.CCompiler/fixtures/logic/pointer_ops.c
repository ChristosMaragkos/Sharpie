int main(void) {
    int x = 500;
    int *p = &x;

    int *ptr = (int *)1000;
    *ptr = 42;
    int val = *ptr;

    if (val != 42) return 1;
    return 0;
}
