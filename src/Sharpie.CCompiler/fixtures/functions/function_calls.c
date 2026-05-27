int square(int x) { return x * x; }

const int expected_result = 25;

int main(void) {
    int actual_result = square(5);
    if (expected_result != actual_result)
        return 1;
    return 0;
}
