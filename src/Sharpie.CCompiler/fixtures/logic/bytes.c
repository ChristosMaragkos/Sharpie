typedef struct {
    char a;
    char b;
} Flags;

int main(void) {
    char c = 68;
    c = c + 1;

    Flags f;
    f.a = c;
    f.b = 99;

    int result = f.a + f.b;
    if (result != 168) return 1;
    return 0;
}
