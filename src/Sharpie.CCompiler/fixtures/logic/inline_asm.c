asm("; This block should exist outside of the method.");

int main(void) {
  asm("LDI r1, 0 ; This should exist in the method.");
  return 0;
}
