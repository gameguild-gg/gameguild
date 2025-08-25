# Mergesort and QuickSort

Both algorithms are based on recursion and divide-and-conquer strategy. Both are efficient for large datasets, but they have different performance characteristics.

## Recursion

![img.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F05-divide-and-conquer%2Fimg.png)

Recursion is a programming technique where a function calls itself. It is a powerful tool to solve problems that can be divided into smaller problems of the same type.

The recursion has two main parts:

- Base case: the condition that stops the recursion;
- Recursive case: the condition that calls the function again.

Let's see an example of a recursive function to calculate the factorial of a number:

```c++
#include <iostream>

int factorial(int n) {
  if (n == 0) {
    return 1;
  }
  return n * factorial(n - 1);
}
```

## Mergesort

![img_2.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F05-divide-and-conquer%2Fimg_2.png)

Mergesort divides the input array into two halves, calls itself for the two halves, and then merges the two sorted halves.

You can split the algorithm into two main parts:

- Mergesort: the function that calls itself for the two halves;
- Merge: the function that merges the two sorted halves.

![img_4.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F05-divide-and-conquer%2Fimg_4.png)
[image source](https://en.wikipedia.org/wiki/Merge_sort)

The algorthims will keep dividing the array (in red) until it reaches the base case, where the array has only one element(in gray). Then it will merge the two sorted subarrays (in green).

![Merge-sort-example-300px.gif](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F05-divide-and-conquer%2FMerge-sort-example-300px.gif)
[image source](https://en.wikipedia.org/wiki/Merge_sort)

### Mergesort time complexity

- Mergesort is time `O(n*lg(n))` in the worst case scenario. It is the best time complexity for a comparison-based sorting algorithm.
- The algorithm is stable. It maintain the relative order of elements with the same keys during the sorting process.

### Mergesort implementation

```c++
#include <iostream>
#include <vector>
#include <queue>

// inplace merge without extra space
template <typename T>
requires std::is_arithmetic<T>::value // C++20
void mergeInplace(std::vector<T>& arr, const size_t start, size_t mid,  const size_t end) {
  size_t left = start;
  size_t right = mid + 1;

  while (left <= mid && right <= end) {
    if (arr[left] <= arr[right]) {
      left++;
    } else {
      T temp = arr[right];
      for (size_t i = right; i > left; i--) {
        arr[i] = arr[i - 1];
      }
      arr[left] = temp;
      left++;
      mid++;
      right++;
    }
  }
}

// Merge two sorted halves
template <typename T>
requires std::is_arithmetic<T>::value // C++20
void merge(std::vector<T>& arr, const size_t start, const size_t mid,  const size_t end) {
  // create a temporary array to store the merged array
  std::vector<T> temp(end - start + 1);

  // indexes for the subarrays:
  const size_t leftStart = start;
  const size_t leftEnd = mid;
  const size_t rightStart = mid + 1;
  const size_t rightEnd = end;

  // indexes for
  size_t tempIdx = 0;
  size_t leftIdx = leftStart;
  size_t rightIdx = rightStart;

  // merge the subarrays
  while (leftIdx <= leftEnd && rightIdx <= rightEnd) {
    if (arr[leftIdx] < arr[rightIdx])
      temp[tempIdx++] = arr[leftIdx++];
    else
      temp[tempIdx++] = arr[rightIdx++];
  }

  // copy the remaining elements of the left subarray
  while (leftIdx <= leftEnd)
    temp[tempIdx++] = arr[leftIdx++];

  // copy the remaining elements of the right subarray
  while (rightIdx <= rightEnd)
    temp[tempIdx++] = arr[rightIdx++];

  // copy the merged array back to the original array
  std::copy(temp.begin(), temp.end(), arr.begin() + start);
}

// recursive mergesort
template <typename T>
requires std::is_arithmetic<T>::value // C++20
void mergesortRecursive(std::vector<T>& arr,
                        size_t left,
                        size_t right) {
  if (right - left > 0) {
    size_t mid = (left + right) / 2;
    mergesortRecursive(arr, left, mid);
    mergesortRecursive(arr, mid+1, right);
    merge(arr, left, mid, right);
    // if the memory is limited, use the inplace merge at the cost of performance
    // mergeInplace(arr, left, mid - 1, right - 1);
  }
}

// interactive mergesort
template <typename T>
requires std::is_arithmetic<T>::value // C++20
void mergesortInteractive(std::vector<T>& arr) {
  for(size_t width = 1; width < arr.size(); width *= 2) {
    for(size_t left = 0; left < arr.size(); left += 2 * width) {
      size_t mid = std::min(left + width, arr.size());
      size_t right = std::min(left + 2 * width, arr.size());
      merge(arr, left, mid - 1, right - 1);
      // if the memory is limited, use the inplace merge at the cost of performance
      // mergeInplace(arr, left, mid - 1, right - 1);
    }
  }
}


int main() {
  std::vector<int> arr1;
  for(int i = 1000; i > 0; i--)
    arr1.push_back(rand()%1000);
  std::vector<int> arr2 = arr1;

  for(auto i: arr1) std::cout << i << " ";
  std::cout << std::endl;

  mergesortRecursive(arr1, 0, arr1.size() - 1);
  for(auto i: arr1) std::cout << i << " ";
  std::cout << std::endl;

  mergesortInteractive(arr2);
  for(auto i: arr2) std::cout << i << " ";
  std::cout << std::endl;

  return 0;
}
```

### Mergesort space complexity

You can implement Mergesort in two ways:

- Recursive: the function calls itself for the two halves;
- Iterative: the function uses a loop to merge the two sorted halves.

The interactive version is more efficient than the recursive version, but it is more complex to understand. But both uses the same core algorithm to merge the two sorted halves.

The main issue with Mergesort is that it requires extra space `O(n)` to merge the subarrays, which can be problem for large datasets.

- The recursive version will increase the call stack by `O(lg(n)` and can potentially cause a stack overflow;
- The iterative version does not add pressure to the stack;

## QuickSort

![img_1.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F05-divide-and-conquer%2Fimg_1.png)

Quicksort is prtetty similar to mergesort, but it solves the extra memory allocation at expense of stability. So quicksort is an `in-place` and `unstable` sorting algorithm.

One of the core strategy of quicksort is pivoting. It will be selected and the array will be partitioned in two subarrays: one with elements smaller than the pivot (left) and the other with elements greater than the pivot (right).

The partitioning process consists of the following steps:

- Select a pivotIndex (left, right, random or median);
- Swap the pivot with the leftmost element (this can be delayed to the end of the step);
- Set the pivot to the leftmost element (assuming you swapped);
- Set the left and right indexes;
- While the left index is less than or equal to the right index:
    - If the left element is less than or equal to the pivot, increment the left index;
    - If the right element is greater than the pivot, decrement the right index;
    - If the left element is greater than the pivot and the right element is less than or equal to the pivot, swap the left and right elements;

<iframe scrolling="no" style="position:relative;top:0px;width:100%;height:500px;" src="https://opendsa-server.cs.vt.edu/embed/quicksortAV"></iframe>
<a href="https://opendsa-server.cs.vt.edu/ODSA/Books/CS3/html/QuicksortAV.html">source</a>

![Sorting_quicksort_anim.gif](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F05-divide-and-conquer%2FSorting_quicksort_anim.gif)

### Quicksort time complexity

The main issue with quicksort is that it can degrade to `O(n^2)` in an already sorted array. To solve this issue, we can select a random pivot, or the median of the first, middle and last element of the array. This can increase the stability of the algorithm at the expense of performance. The best case scenario is `O(n*lg(n))` and the average case is `O(n*lg(n))`.

### QuickSort implementation

```c++
#include <iostream>
#include <vector>
#include <utility>
#include <stack>
#include <random>

using std::stack;
using std::swap;
using std::pair;
using std::vector;
using std::cout;

// Function to generate a random pivot index within the range [left, right]
template<typename T>
requires std::integral<T> // c++20
T randomRange(T left, T right) {
    static std::random_device rd;
    static std::mt19937 gen(rd());
    std::uniform_int_distribution<T> dist(left, right);
    return dist(gen);
}

// partition
template<typename T>
requires std::is_arithmetic_v<T>
size_t partition(std::vector<T>& arr, size_t left, size_t right) {
    // random pivot to increase stability at the cost of performance by random call
    size_t pivotIndex = randomRange(left, right);
    swap(arr[left], arr[pivotIndex]);

    size_t pivot = left;
    size_t l = left + 1;
    size_t r = right;

    while (l <= r) {
        if (arr[l] <= arr[pivot]) l++;
        else if (arr[r] > arr[pivot]) r--;
        else swap(arr[l], arr[r]);
    }
    swap(arr[pivot], arr[r]);
    return r;
}

// quicksort recursive
template<typename T>
requires std::is_arithmetic_v<T>
void quicksortRecursive(std::vector<T>& arr, size_t left, size_t right) {
    if (left < right) {
        // partition the array
        size_t pivot = partition(arr, left, right);
        // recursive call to left and right subarray
        quicksortRecursive(arr, left, pivot - 1);
        quicksortRecursive(arr, pivot + 1, right);
    }
}

// quicksort interactive
template<typename T>
requires std::is_arithmetic_v<T>
void quicksortInteractive(std::vector<T>& arr, size_t left, size_t right) {
    // simulate recursive call and avoid potential stack overflow
    // std::stack allocate memory to hold data content on heap.
    stack<pair<size_t, size_t>> stack;
    // produce the initial state
    stack.emplace(left, right);
    // iterate
    while (!stack.empty()) {
        // consume
        auto [left, right] = stack.top(); // C++17
        stack.pop();
        if (left < right) {
            auto pivot = partition(arr, left, right);
            // produce
            stack.emplace(left, pivot - 1);
            stack.emplace(pivot + 1, right);
        }
    }
}

int main() {
    std::vector<int> arr1;
    for (int i = 0; i < 100; i++) arr1.push_back(rand() % 100);
    vector<int> arr2 = arr1;

    for (auto& i : arr1) cout << i << " ";
    cout << std::endl;

    quicksortRecursive(arr1, 0, arr1.size() - 1);
    for (auto& i : arr1) cout << i << " ";
    cout << std::endl;

    quicksortInteractive(arr2, 0, arr2.size() - 1);
    for (auto& i : arr2) cout << i << " ";
    cout << std::endl;

    return 0;
}
```

### Quicksort space consumption

- The recursive version of quicksort can increase the function call stack by `O(lg(n))` on average, but it can degrade to `O(n)` in the worst case scenario. Potentially causing a stack overflow;
- The interactive version of quicksort avoids the function call stack issue, avoiding stack overflow. But the memory required for the replacement is still `O(lg(n))` on average and `O(n)` for the indexes in the worst case scenario.

![img_3.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F05-divide-and-conquer%2Fimg_3.png)