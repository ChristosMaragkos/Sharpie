int main(void) {
    int x = 150;

    for (int i = 0; i < 50; ++i) {
        --x;
    }

    while (x > 50) {
        --x;
    }

    do {
        --x;
    } while (x > 0);

    if (x == 0) {
        return 0;
    }

    return 1;
}
