// Array decays to a pointer when passed
void fill_array(int *arr, int size) {
  int i = 0;
  while (i < size) {
    arr[i] = i * 10;
    i++;
  }
}

int main() {
  int my_array[3];

  fill_array(my_array, 3);

  // Should return 20
  return my_array[2];
}
