# Dynamic data

In C++'s Standard Library, we  have a bunch of data structures already implemented for us. But you need to understand what is inside it in order do ponder the best tool for your job. In this week we are going to cover Dynamic Arrays (equivalent of std::vector) and Linked Lists(equivalent of std::list) .

## Dynamic Arrays

A dynamic array is a random access, variable-size list data structure that allows elements to be added or removed. It is supplied with standard libraries in many modern mainstream programming languages. Let's try to implement one here for the sake of teaching purposes;

```c++
template<typename T>
struct Vector {
private:
  size_t _size;
  size_t _capacity;
  T* _data;
public:
  // constructors
  Vector() : _size(0), _capacity(1), _data(new T[1]) {}
  explicit Vector(size_t size) : _size(size), _capacity(size), _data(new T[size]) {}

  // destructor
  ~Vector() { delete[] _data;}

  // accessors
  size_t size() const { return _size; }
  size_t capacity() const { return _capacity;}

  // push_back takes care of resizing the array if necessary
  // this insertion will amortize the cost of resizing
  void push_back(const T& value) {
    if (_size == _capacity) {
      // growth factor of 2
      _capacity = _capacity == 0 ? 1 : _capacity * 2;
      // allocate new memory
      T* new_data = new T[_capacity];
      // copy the old data into the new memory
      for (size_t i = 0; i < _size; ++i)
          new_data[i] = _data[i];
      // release the old memory
      delete[] _data;
      // update the data pointer
      _data = new_data;
    }
    _data[_size++] = value;
  }

  // operator[] for read-write access
  T& operator[](size_t index) { return _data[index]; }

  // other functions
  // ...
};
```

With this boilerplate you should be able to implement the rest of the functions for the Vector class.

## Linked Lists

A linked list is a linear access, variable-size list data structure that allows elements to be added or removed without the need of resizing. It is supplied with standard libraries in many modern mainstream programming languages. Let's try to build one here for the sake of teaching purposes;

```c++
// linkedlist
template <typename T>
struct LinkedList {
private:
    // linkedlist node
    struct LinkedListNode {
        T data;
        LinkedListNode *next;
        LinkedListNode(T data) : data(data), next(nullptr) {}
    };

    LinkedListNode *_head;
    size_t _size;
public:
    LinkedList() : _head(nullptr), _size(0) {}

    // delete all nodes in the linkedlist
    ~LinkedList() {
        while (_head) {
            LinkedListNode *temp = _head;
            _head = _head->next;
            delete temp;
        }
    }

    // _size
    size_t size() const { return _size; }

    // is it possible to make it O(1) instead of O(n)?
    void push_back(T data) {
        if (!_head) {
            _head = new LinkedListNode(data);
            _size++;
            return;
        }
        auto* temp = _head;
        while (temp->next)
            temp = temp->next;
        temp->next = new LinkedListNode(data);
        _size++;
    }

    // operator[] for read-write access
    T &operator[](size_t index) {
        auto* temp = _head;
        for (size_t i = 0; i < index; i++)
            temp = temp->next;
        return temp->data;
    };
    
    // other functions
};
```

## Homework

For both, implement the following functions:

- `T* find(const T& value)`: returns a pointer to the first occurrence of the value in the collection, or nullptr if the value is not found.
- `bool contains(const T& value)`: returns true if the value is found in the collection, false otherwise.
- `T& at(size_t index)`: returns a reference to the element at the specified index. If the index is out of bounds, throw an `std::out_of_range` exception.
- `void push_front(const T& value)`: adds a new element to the beginning of the collection.
- improve push_back of the linkedlist to be O(1) instead of O(n);
- `void insert_at(size_t index, const T& value)`: inserts a new element at the specified index. If the index is out of bounds, throw an `std::out_of_range` exception.
- `void pop_front()`: removes the first element of the collection.
- `void pop_back()`: removes the last element of the collection. Is it possible to make it O(1) instead of O(n)? 
- `void remove_all(const T& value)`: removes all occurrences of the value from the collection.
- `void remove_at(size_t index)`: removes the element at the specified index. If the index is out of bounds, throw an `std::out_of_range` exception.

Now compare the complexity of linked list and dynamic array for each of the functions you implemented and create a table. What is the best data structure for each use case? Why?