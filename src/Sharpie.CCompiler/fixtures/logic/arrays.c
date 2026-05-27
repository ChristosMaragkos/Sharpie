void fill_array(int *arr, int size) {
    int i = 0;
    while (i < size) {
        arr[i] = i * 10;
        i++;
    }
}

int main(void) {
    int my_array[3];
    fill_array(my_array, 3);

    int result = my_array[2];
    if (result != 20) return 1;
    return 0;
}
