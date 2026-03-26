int get_score(int id) {
  int score = 0;
  switch (id) {
  case 5:
    score = 100;
    break;
  case 10:
    score = 500;
    break;
  case 15:
  case 20:
    score = 1000;
    break;
  default:
    score = -1;
  }
  return score;
}

int main(void) { return get_score(20); }
