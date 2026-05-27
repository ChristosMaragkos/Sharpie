// gotta hide the actual numbers from clang so it doesn't optimize the checks
int test_logic(int a, int b, int c, int d) {
    if ((a + b + c + d) >= 5 && (d < 10 || c > 0)) {
        return 0;
    }
    return 1;
}

int main(void) { return test_logic(1, 2, 3, 5); }
