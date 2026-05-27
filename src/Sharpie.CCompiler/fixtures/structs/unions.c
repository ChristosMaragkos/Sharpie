union Register {
    int word;
};

int main(void) {
    union Register reg;
    reg.word = 258;

    int result = reg.word;
    if (result != 258) return 1;
    return 0;
}
