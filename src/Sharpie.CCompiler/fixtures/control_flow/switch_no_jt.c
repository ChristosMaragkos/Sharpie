int get_score(int id) {
    int score = 1;

    switch (id) {
    case 51:
        score = 0;
        break;
    case 118:
        score = 500;
        break;
    case 88:
    case 44:
        score = 1000;
        break;
    default:
        score = -1;
    }
    return score;
}

int main(void) { return get_score(51); }
