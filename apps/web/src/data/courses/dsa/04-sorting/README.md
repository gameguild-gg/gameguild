# Sorting algorithms

## Swap function

```c++
void swap(int &a, int &b) {
  int temp = a;
  a = b;
  b = temp;
}
```

## Bubble sort

```c++
void bubble_sort(int arr[], int n) {
  for (int i = 0; i < n; i++) { // n passes
    for (int j = 0; j < n - 1; j++) { // linear pass
      if (arr[j] > arr[j + 1]) { // swap if the element is greater than the next
        swap(arr[j], arr[j + 1]);
      }
    }
  }
}
```

Is it possible to optimize the bubble sort algorithm? 
- The example above always pass from the beginning to the end of the array, but it is possible to stop the inner loop earlier if the right side of the array is already sorted.
- You can count how many swaps you did in the inner loop, and if you did 0 swaps, you can stop the outer loop.

### Questions:

- What is the best case scenario for the bubble sort?
- What is the worst case scenario for the bubble sort?
- How many writes does the bubble sort do?
- How many reads does the bubble sort do?
- What is the time complexity of the bubble sort?
- What is the space complexity of the bubble sort?

## Selection sort

```c++
void selection_sort(int arr[], int n) {
  for (int i = 0; i < n - 1; i++) { // n - 1 passes
    int min_index = i; // the minimum element in the unsorted part of the array
    for (int j = i + 1; j < n; j++) { // linear search
      if (arr[j] < arr[min_index]) { // find the minimum element
        min_index = j;
      }
    }
    swap(arr[i], arr[min_index]); // swap the minimum element with the first element of the unsorted part
  }
}
```

### Questions:

- What is the best case scenario for the selection sort?
- What is the worst case scenario for the selection sort?
- How many writes does the selection sort do?
- How many reads does the selection sort do?
- What is the time complexity of the selection sort?
- What is the space complexity of the selection sort?
- What is the difference between the bubble sort and the selection sort?

## Insertion sort

```c++
void insertion_sort(int arr[], int n) {
  for (int i = 1; i < n; i++) { // n - 1 passes
    int key = arr[i]; // the key element to be inserted in the sorted part of the array
    int j = i - 1; // the last element of the sorted part of the array
    while (j >= 0 && arr[j] > key) { // shift the elements to the right to make space for the key
      arr[j + 1] = arr[j];
      j--;
    }
    arr[j + 1] = key; // insert the key in the right position
  }
}
```

### Questions:

- What is the best case scenario for the insertion sort?
- What is the worst case scenario for the insertion sort?
- How many writes does the insertion sort do?
- How many reads does the insertion sort do?
- What is the time complexity of the insertion sort?
- What is the space complexity of the insertion sort?
- What is the difference between the bubble sort, the selection sort, and the insertion sort?

## Discussion

- Why is sorting typically taught towards the beginning of an algorithms course?
- Why do we study algorithms like bubble sort that are almost never used in practice?
- Can you describe a non-comparative sorting algorithm?
- Which of these sorting algorithms is the best: bubble sort, selection sort, or insertion sort?

Table of differences between the sorting algorithms:

| Algorithm | Best case | Worst case | Time complexity | Space complexity | Swaps |
|-----------|-----------|------------|-----------------|------------------|-------|
| Bubble    | O(n)      | O(n^2)     | O(N^2)          | O(1)             | O(n^2)|
| Selection | O(n^2)    | O(n^2)     | O(N^2)          | O(1)             | O(n)  |
| Insertion | O(n)      | O(n^2)     | O(N^2)          | O(1)             | O(n^2)|