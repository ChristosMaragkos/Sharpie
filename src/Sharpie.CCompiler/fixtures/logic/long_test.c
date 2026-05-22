long global_long = 100000;

long add_long(long a, long b) { return a + b; }

long sub_long(long a, long b) { return a - b; }

long mul_long(long a, long b) { return a * b; }

long div_long(long a, long b) { return a / b; }

long mod_long(long a, long b) { return a % b; }

long shl_long(long a, long b) { return a << b; }

long shr_long(long a, long b) { return a >> b; }

long neg_long(long a) { return -a; }

long not_long(long a) { return ~a; }

long and_long(long a, long b) { return a & b; }

long or_long(long a, long b) { return a | b; }

long xor_long(long a, long b) { return a ^ b; }

void test_incdec() {
    long x = 100000;
    x++;
    long y = x;
}

int main() {
    long a = 100000;
    long b = 200000;
    long c = a + b;
    long d = c - a;
    d++;
    if (c > a) {
        return 1;
    }

    // Multiplication
    long m = a * 3;

    // Division and modulus
    long dv = 300000;
    long q = dv / b;
    long r = dv % b;

    // Shifts
    long sl = a << 1;
    long sr = sl >> 1;

    // Bitwise AND, OR, XOR
    long ba = a & 0xFFFF;
    long bo = a | 0xFF;
    long bx = a ^ 0xFFFF;

    // Unary minus and NOT
    long ne = -a;
    long nt = ~a;

    // Compound assignment
    a += 50000;
    a -= 30000;
    a *= 2;
    a &= 0xFFFF;
    a |= 0xFF00;
    a ^= 0x0F0F;

    // Shift compound assignment
    long sh = 0x00FF;
    sh <<= 4;
    sh >>= 2;

    // Div/mod compound assignment
    long dm = 300000;
    dm /= 150000;
    dm %= 7;

    // Comparisons
    long zero = 0;
    long one = 1;
    long two = 2;
    long big = 300000;
    if (a < b) {
    }
    if (b > a) {
    }
    if (a <= b) {
    }
    if (b >= a) {
    }
    if (a != b) {
    }
    if (big != zero) {
    }
    if (one < two) {
    }
    if (two > one) {
    }
    if (a > zero) {
    }
    if (zero < a) {
    }

    // Pre/post inc/dec in expressions
    long x = 100000;
    long pre = ++x;
    long post = x--;

    // Complex sub-expression via temp variables
    long t1 = a + b;
    long t2 = c - d;
    long cx = t1 * t2;

    // Function calls with simple args
    long fa = add_long(a, b);
    long fb = mul_long(a, b);

    a *= 2;
    return 0;
}
