struct Bytes {
  char low, high;
};

union Register {
  int word;
  struct Bytes bytes;
};

int main(void) {
  union Register reg;

  reg.word = 258;

  char l = reg.bytes.low;
  char h = reg.bytes.high;

  reg.bytes.low = 10; // UB Central

  return reg.word;
}
