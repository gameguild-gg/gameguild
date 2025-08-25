# Stack and queue

::: warning
    
This section is a continuation of the [Dynamic Data](dynamic-data) section. Please make sure to read it before continuing.

:::

## Stack

![img_1.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F08-stack-and-queue%2Fimg_1.png)[source](https://programmerhumor.io/stackoverflow-memes/data-structures-in-a-nutshell/)

Stacks are a type of dynamic data where the last element added is the first one to be removed. This is known as LIFO (Last In First Out) or FILO (First In Last Out). Stacks are used in many algorithms and data structures, such as the depth-first search algorithm, back-track and the call stack.

### Stack Basic Operations

- `push` - Add an element to the top of the stack.
- `pop` - Remove the top element from the stack.
- `top` - Return the top element of the stack.

![stack.gif](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F08-stack-and-queue%2Fstack.gif) [source](https://medium.com/@joshalphonse/stacks-queues-97037b3c01c6)

### Stack Implementation

You can either implement it using a dynamic array or a linked list. But the dynamic array implementation is more efficient in terms of memory and speed. So let's use it.

```c++
#include <iostream>

// stack
template <typename T>
class Stack {
  T* data; // dynamic array
  size_t size; // number of elements in the stack
  size_t capacity; // capacity of the stack
public:
  Stack() : data(nullptr), size(0), capacity(0) {}
  ~Stack() {
    delete[] data;
  }
  void push(const T& value) {
    // if it needs to be resized
    // amortized cost of push is O(1)
    if (size == capacity) {
      capacity = capacity == 0 ? 1 : capacity * 2;
      T* new_data = new T[capacity];
      std::copy(data, data + size, new_data);
      delete[] data;
      data = new_data;
    }
    // stores the value and then increments the size
    data[size++] = value; 
  }
  T pop() {
    if (size == 0)
      throw std::out_of_range("Stack is empty");

    // shrink the array if necessary
    // ammortized cost of pop is O(1)
    if (size <= capacity / 4) {
      capacity /= 2;
      T* new_data = new T[capacity];
      std::copy(data, data + size, new_data);
      delete[] data;
      data = new_data;
    }
    return data[--size];
  }
  T& top() const {
    if (size == 0)
      throw std::out_of_range("Stack is empty");
    // cost of top is O(1)
    return data[size - 1];
  }
  size_t get_size() const {
    return size;
  }
  bool is_empty() const {
    return size == 0;
  }
};
```

## Queue

![Queue.gif](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F08-stack-and-queue%2FQueue.gif) [source](https://embetronicx.com/tutorials/p_language/c/queue-in-c/)

A queue is a type of dynamic data where the first element added is the first one to be removed. This is known as FIFO (First In First Out). Queues are used in many algorithms and data structures, such as the breadth-first search algorithm. Usually it is implemented as a linked list, in order to provide O(1) time complexity for the `enqueue` and `dequeue` operations. But it can be implemented using a dynamic array as well and amortize the cost for resizing. The dynamic array implementation is more efficient in terms of memory and speed(if not resized frequently).

### Queue Basic Operations

- `enqueue` - Add an element to the end of the queue.
- `dequeue` - Remove the first element from the queue.
- `front` - Return the first element of the queue.

![queue-operations.gif](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F08-stack-and-queue%2Fqueue-operations.gif)[source](https://algo.aspires.cc/fundamental/ch1-linear-structure)

### Queue Implementation

```c++
// queue
template <typename T>
class Queue {
  // dynamic array approach instead of linked list
  T* data;
  size_t front; // index of the first valid element
  size_t back; // index of the next free slot
  size_t capacity; // current capacity of the array
  size_t size; // number of elements in the queue

  explicit Queue() : data(nullptr), front(0), back(0), capacity(capacity), size(0) {};

  void enqueue(T value) {
    // resize if necessary
    // amortized O(1) time complexity
    if (size == capacity) {
      auto old_capacity = capacity;
      capacity = capacity ? capacity * 2 : 1;
      T* new_data = new T[capacity];
      for (size_t i = 0; i < size; i++)
        new_data[i] = data[(front + i) % old_capacity];
      delete[] data;
      data = new_data;
      front = 0;
      back = size;
    }
    data[back] = value;
    back = (back + 1) % capacity;
    size++;
  }

  void dequeue() {
    if (size) {
      front = (front + 1) % capacity;
      size--;
    }
    // shrink if necessary
    if(size <= capacity / 4) {
      auto old_capacity = capacity;
      capacity /= 2;
      T* new_data = new T[capacity];
      for (size_t i = 0; i < size; i++)
        new_data[i] = data[(front + i) % old_capacity];
      delete[] data;
      data = new_data;
      front = 0;
      back = size;
    }
  }

  T& head() {
    return data[front];
  }
};
```