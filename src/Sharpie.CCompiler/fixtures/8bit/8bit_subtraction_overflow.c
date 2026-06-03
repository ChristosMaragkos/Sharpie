int main(void) {
    unsigned char a = 0;
    --a;
    if (a == 255)
        return 0;
    else
        return 1;
}
