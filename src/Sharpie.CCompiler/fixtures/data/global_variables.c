// uninitialized global (should zero-pad by 2 bytes)
int g_score;

// initialized global
int g_lives = 3;

// initialized array
int g_map[3] = {10, 20, 30};

// static initialized struct (Should NOT be wrapped in .GLOBAL)
struct Player {
  int x;
  char id;
};
static struct Player g_p1 = {100, 5};

int main(void) {
  struct Player local_p;

  // Write directly (STM)
  g_score = 50;

  // Compound Write (LDM, Math, STM)
  g_lives += 1;

  // Struct Member Write (LDI and STA because of the offset)
  g_p1.x = 200;

  // Struct-to-Struct assignment, should emit SYS_MEM_MOVE
  local_p = g_p1;

  return g_score + g_lives + g_map[1] + local_p.x;
}
