int add(int a, int b) { return a + b; }

int sub(int a, int b) { return a - b; }

int do_math(int (*operation)(int, int), int x, int y) {
    return operation(x, y);
}

const int expected_result = 20;

int main(void) {
    int (*ops[2])(int, int);
    ops[0] = add;
    ops[1] = sub;

    int res1 = do_math(add, 10, 5);
    int res2 = do_math(ops[1], 10, 5);

    int actual_result = res1 + res2;
    if (expected_result != actual_result)
        return 1;

    return 0;
}
