int get_score(int id) {
  int score = 0;

  switch (id) {
  case 1:
    score = 100;
    break;
  case 2:
    score = 500;
    break;
  case 3:
    score = 800;
    break;
  case 4:
    score = 1000;
    break;
  default:
    score = -1;
    break;
  }

  return score;
}

int main(void) { return get_score(3); }
