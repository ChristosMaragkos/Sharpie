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

  return f.a + f.b;
}
