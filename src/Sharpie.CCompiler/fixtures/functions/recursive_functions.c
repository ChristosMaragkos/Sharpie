int factorial(int number) {
  if (number == 1)
    return 1;
  else
    return (number * factorial(number - 1));
}

int main(void) { return factorial(5); }
