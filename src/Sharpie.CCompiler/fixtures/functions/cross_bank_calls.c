__attribute__((annotate("bank_0"))) int fetch_enemy_sprite(int enemy_id) {
    return enemy_id * 3;
}

__attribute__((annotate("bank_1"))) int calculate_path(int start_x,
                                                       int start_y) {
    return start_x + start_y;
}

__attribute__((annotate("bank_2"))) int do_stuff(void) { return 42; }

int main(void) {
    int sprite = fetch_enemy_sprite(42);
    int distance = calculate_path(10, 20);

    __attribute__((annotate("bank_2"))) int (*do_indirect_stuff)(void) =
        do_stuff;

    int result = sprite + distance + do_indirect_stuff();
    if (result != 198)
        return 1;
    return 0;
}
