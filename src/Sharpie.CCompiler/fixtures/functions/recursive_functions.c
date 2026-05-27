int factorial(int number) {
    if (number == 1)
        return 1;
    else
        return (number * factorial(number - 1));
}

int main(void) {
    int result = factorial(5);
    if (result != 120) return 1;
    return 0;
}
