# Heap

Heap is a data structure and algorithm that always keep the highest or lowest element at the top. You can implement it in many ways, such as a tree or a priority queue.

## Heap in a tree

In a tree implementation, the most common is a binary tree. In this the max heap scenario, the parent is always greater than the both children. The binary tree should be complete, which means all levels are filled except possibly for the last one, which is filled from left to right. 

The complexity of a heap is `O(log(n))` for insert and remove operations, for peeking or popping the top element is `O(1)`. 

## Heap in an array

The most common implementation of a heap is in an array(vector). Assuming the current element is at index `i`, the parent is at `(i-1)/2`, the left child is at `2*i+1` and the right child is at `2*i+2`. This approach is called tree linearization. It is more memory efficient and cache friendly.

The complexity of a heap in a priority queue in array is the same as in a tree, but with lower overhead.

![Heap](https://upload.wikimedia.org/wikipedia/commons/3/38/Max-Heap.svg)

## Heapfy

Heapfy is an operation that for a given array, it will transform it into a heap. The complexity is `O(n)` if done bottom-up. That's one of the reasons heaps are so efficient. After that, all calls to get the top element will be `O(1)`.

If the heapfy is done top-down, the complexity is `O(n log(n))`. 

![Heap](https://upload.wikimedia.org/wikipedia/commons/a/ac/Binary_heap_bottomup_vs_topdown.svg)

The algorithm is simple, 

- For each element in the array, it will compare with the parent and swap if necessary;
- If the swap causes imbalance on the children, it will repeat the process until the element is in the right place.

See animation here https://www.cs.usfca.edu/~galles/JavascriptVisual/Heap.html

## Insert

In the insertion operation, the element is added to the end of the array, and then it is compared with the parent and swapped if necessary. The parent comparison goes up to the top of the heap. The complexity is `O(log(n))`.

## Operation Peek and Pop

Peek will return the top element of the heap, but not remove it. Pop is similar to peek, but it will remove the top.

When you remove the top element, you will need to reorganize it. The steps are:

1. Get the top element and store to return later;
2. Swap the top element with the last element;
3. Remove the last element;
4. Put the last element in the top;
5. Compare the top element with the children and swap if necessary;
6. Repeat until the element is in the right place.

The complexity of the pop operation is `O(log(n))`.
