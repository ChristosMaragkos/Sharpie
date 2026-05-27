int g_score;
int g_lives = 3;
int g_map[3] = {10, 20, 30};

struct Player {
    int hp;
    char id;
};
static struct Player g_p1 = {100, 5};

int main(void) {
    struct Player local_p;

    g_score = 50;
    g_lives += 1;
    g_p1.hp = 200;
    local_p = g_p1;

    int result = g_score + g_lives + g_map[1] + local_p.hp;
    if (result != 274)
        return 1;

    return 0;
}
