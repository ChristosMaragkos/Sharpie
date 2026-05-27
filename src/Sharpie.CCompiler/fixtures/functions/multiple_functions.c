int helper(void) { return 42; }

int main(void) {
    int result = helper();
    if (result != 42) return 1;
    return 0;
}
