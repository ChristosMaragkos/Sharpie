// direct function residing in bank 1
__attribute__((annotate("bank_1"))) int fetch_enemy_sprite(int enemy_id);

// target function residing in bank 2
__attribute__((annotate("bank_2"))) int calculate_path(int start_x,
                                                       int start_y);

int main(void) {
  // Direct cross-bank call
  // Should load '1' into r14, '_func_fetch_enemy_sprite' into r13, and call
  // SYS_FAR_CALL
  int sprite = fetch_enemy_sprite(42);

  // Indirect cross-bank call
  // We declare a local pointer, annotate it for Bank 2, and point it at our
  // Bank 2 function.
  __attribute__((annotate("bank_2"))) int (*path_ptr)(int, int) =
      calculate_path;

  // Should retrieve the pointer from the stack into r13, load '2' into r14, and
  // call SYS_FAR_CALL
  int distance = path_ptr(10, 20);

  return sprite + distance;
}
