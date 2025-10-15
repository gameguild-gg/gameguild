# Hastables

![img.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F06-hashtables%2Fimg.png)

Hashtables ane associative datastructures that stores key-value pairs. It uses a hash function to compute an index into an array of buckets or slots, from which the desired value can be found.

The core of the generic associative container is to implement ways to get and set values by keys such as:

- `void insert(K key, V value)`: Add a new key-value pair to the hashtable. If the key already exists, update the value.
- `V at(K key)`: Get the value of a given key. If the key does not exist, return a default value.
- `void remove(K key)`: Remove a key-value pair from the hashtable.
- `bool contains(K key)`: Check if a key exists in the hashtable.
- `int size()`: Get the number of key-value pairs in the hashtable.
- `bool isEmpty()`: Check if the hashtable is empty.
- `void clear()`: Remove all key-value pairs from the hashtable.
- `V& operator[](K key)`: Get the value of a given key. If the key does not exist, insert a new key-value pair with a default value.

## Key-value pairs

In C++ you could use `std::pair` from the `utility` library to store key-value pairs. 

```c++
#include <utility>
#include <iostream>

int main() {
  std::pair<int, int> pair = std::make_pair(1, 2);
  std::cout << pair.first << " " << pair.second << std::endl;
  // prints 1 2
  return 0;
}
```

Or you could create your own key-value pair class.

```c++
#include <iostream>

template <typename K, typename V>
struct KeyValuePair {
  K key;
  V value;
  KeyValuePair(K key, V value) : key(key), value(value) {}
};

int main() {
  KeyValuePair<int, int> pair(1, 2);
  std::cout << pair.key << " " << pair.value << std::endl;
  // prints 1 2
  return 0;
}
```

## Hash function

![img_3.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F06-hashtables%2Fimg_3.png)

The hash function will process the key data and return an index. Usually in C++, the index is of type `size_t` which is biggest unsigned integer the platform can handle.

The hash function should be fast and should distribute the keys uniformly across the array of buckets. The hash function should be deterministic, meaning that the same key should always produce the same hash.

If the size of your key is less than the `size_t` you could just use the key casted to `size_t` as the hash function. If it is not, you will have to implement your own hash function. You probably should use bitwise operations to do so.

```c++
struct MyCustomDataWith128Bits {
  uint32_t a;
  uint32_t b;
  uint32_t c;
  uint32_t d;
  size_t hash() const {
    return (a << 32) ^ (b << 24) ^ (c << 16) ^ d;
  }
};
```

Think a bit and try to come up with a nice answer: what is the ideal hash function for a given type? What are the requirements for a good hash function?

### Special case: String or arrays 

In order to use strings as keys, you will have to create a way to convert the string's underlying data structure into a `size_t`. You could use the `std::hash` function from the `functional` library. Or create your own hash function.

```c++  
#include <iostream>
#include <functional>

size_t hash(const std::string& key) {
  size_t hash=0; // accumulator pattern
  // the cost of this operation is O(n)
  for (char c : key)
    hash = (hash << 5) ^ c;
  return hash;
}

int main() {
  std::hash<std::string> hash;
  std::string key = "hello";
  std::cout << hash(key) << std::endl;
  // prints number
  return 0;
}
```

You can hide and amortize the cost of the hash function by cashing it. There are plenty of ideas for that. Try to come up with your own.

## Hash tables

Now that you have the hash function for you type and the key-value data structure, you can implement the hash table.

There are plenty of algorithms to do so, and even the `std::unordered_map` is not the best, please watch those videos to understand the trade-offs and the best way to implement a hash table.

- [CppCon 2017: Matt Kulukundis “Designing a Fast, Efficient, Cache-friendly Hash Table, Step by Step”](https://www.youtube.com/watch?v=ncHmEUmJZf4&t=2768s)

For the sake of simplicity I will use the operator modulo to convert the hash into an index array. This is not the best way to do so, but it is the easiest way to implement a hash table.

## Collision resolution

### Linked lists

![img_2.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F06-hashtables%2Fimg_2.png)

Assuming that your hash function is not perfect, you will have to deal with collisions. Two or more different keys could produce the same hash. There are plenty of ways to deal with that, but the easiest way is to use a linked list to store the key-value pairs that have the same hash.

Try to come up with your own strategy to deal with collisions.

![img_1.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F06-hashtables%2Fimg_1.png)
[source](https://www.hackerearth.com/practice/data-structures/hash-tables/basics-of-hash-tables/tutorial/)

#### Key restrictions

In order for the hash table to work, the key should be:

- not modifiable
- implement a hash function
- implement the `==` operator

In C++20 you can use the `concept` feature to enforce those restrictions.

```c++
// concept for a hash table
template <typename T>
concept HasHashFunction =
requires(T t, T u) {
  { t.hash() } -> std::convertible_to<std::size_t>;
  { t == u } -> std::convertible_to<bool>;
  std::is_const_v<T>;
} || requires(T t, T u) {
  { std::hash<T>{}(t) } -> std::convertible_to<std::size_t>;
  { t == u } -> std::convertible_to<bool>;
};


int main() {
  struct MyHashableType {
    int value;
    size_t hash() const {
      return value;
    }
    bool operator==(const MyHashableType& other) const {
      return value == other.value;
    }
  };
  static_assert(HasHashFunction<const MyHashableType>);
  static_assert(HasHashFunction<int>);
  return 0;
}
```

But you can require more from the key if you are going to implement a more complex collision resolution strategy.

#### Hash table implementation with linked lists (chaining) 

![kitten-cat.gif](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F06-hashtables%2Fkitten-cat.gif)

This implementation is naive and not efficient. It is just to give you an idea of how to implement a hash table.

```c++
#include <iostream>

// key should not be modifiable
// implements hash function and implements == operator
template <typename T>
concept HasHashFunction =
requires(T t, T u) {
  { t.hash() } -> std::convertible_to<std::size_t>;
  { t == u } -> std::convertible_to<bool>;
  std::is_const_v<T>;
} || requires(T t, T u) {
  { std::hash<T>{}(t) } -> std::convertible_to<std::size_t>;
  { t == u } -> std::convertible_to<bool>;
};

// hash table
template <HasHashFunction K, typename V>
struct Hashtable {
private:
    // key pair
    struct KeyValuePair {
        K key;
        V value;
        KeyValuePair(K key, V value) : key(key), value(value) {}
    };

    // node of the linked list
    struct HashtableNode {
        KeyValuePair data;
        HashtableNode* next;
        HashtableNode(K key, V value) : data(key, value), next(nullptr) {}
    };

    // array of linked lists
    HashtableNode** table;
    int size;
public:
    // the hashtable will start with a constant size. You can resize it if you want or use any other strategy
    // a good size is something similar to the number of elements you are going to store
    explicit Hashtable(size_t size) {
        // you colud make it automatically resize and increase the complexity of the implementation 
        // for the sake of simplicity I will not do that
        this->size = size;
        table = new HashtableNode*[size];
        for (size_t i = 0; i < size; i++) {
            table[i] = nullptr;
        }
    }
private:
    inline size_t convertKeyToIndex(K t) {
            return t.hash() % size;
    }
public:
    // inserts a new key value pair
    void insert(K key, V value) {
        // you can optionally resize the table and rearrange the elements if the table is too full
        size_t index = convertKeyToIndex(key);
        auto* node = new HashtableNode(key, value);
        if (table[index] == nullptr) {
            table[index] = node;
        } else {
            HashtableNode* current = table[index];
            while (current->next != nullptr)
                current = current->next;
            current->next = node;
        }
    }

    // contains the key
    bool contains(K key) {
        size_t index = convertKeyToIndex(key);
        HashtableNode* current = table[index];
        while (current != nullptr) {
            if (current->data.key == key) {
                return true;
            }
            current = current->next;
        }
        return false;
    }

    // subscript operator
    // creates a new element if the key does not exist
    // fails if the key is not found
    V& operator[](K key) {
        size_t index = convertKeyToIndex(key);
        HashtableNode* current = table[index];
        while (current != nullptr) {
            if (current->data.key == key) {
                return current->data.value;
            }
            current = current->next;
        }
        throw std::out_of_range("Key not found");
    }

    // deletes the key
    // fails if the key is not found
    void remove(K key) {
        size_t index = convertKeyToIndex(key);
        HashtableNode* current = table[index];
        HashtableNode* previous = nullptr;
        while (current != nullptr) {
            if (current->data.key == key) {
                if (previous == nullptr) {
                    table[index] = current->next;
                } else {
                    previous->next = current->next;
                }
                delete current;
                return;
            }
            previous = current;
            current = current->next;
        }
        throw std::out_of_range("Key not found");
    }

    ~Hashtable() {
        for (size_t i = 0; i < size; i++) {
            HashtableNode* current = table[i];
            while (current != nullptr) {
                HashtableNode* next = current->next;
                delete current;
                current = next;
            }
        }
    }
};

struct MyHashableType {
    int value;
    size_t hash() const {
        return value;
    }
    bool operator==(const MyHashableType& other) const {
        return value == other.value;
    }
};

int main() {
    // keys shouldn't be modifiable, implement hash function and == operator
    Hashtable<const MyHashableType, int> hashtable(5);
    hashtable.insert(MyHashableType{1}, 1);
    hashtable.insert(MyHashableType{2}, 2);
    hashtable.insert(MyHashableType{3}, 3);
    hashtable.insert(MyHashableType{6}, 6); // should add to the same index as 1

    std::cout << hashtable[MyHashableType{1}] << std::endl;
    std::cout << hashtable[MyHashableType{2}] << std::endl;
    std::cout << hashtable[MyHashableType{3}] << std::endl;
    std::cout << hashtable[MyHashableType{6}] << std::endl;
    return 0;
}
```

### Open addressing with linear probing

Open addressing is a method of collision resolution in hash tables. In this approach, each cell is not a pointer to the linked list of contents of that bucket, but instead contains a single key-value pair. In linear probing, when a collision occurs, the next cell is checked. If it is occupied, the next cell is checked, and so on, until an empty cell is found.

![img_5.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F06-hashtables%2Fimg_5.png) [source](https://www.slideshare.net/rajshreemuthiah/linear-probing) 

The main advantage of open addressing is cache-friendliness. The main disadvantage is that it is more complex to implement, and it is not as efficient as linked lists when the table is too full. That's why we have to resize the table earlier, usually at 50% full, but at least 70% full.

![img_4.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F06-hashtables%2Fimg_4.png) [source](https://en.wikipedia.org/wiki/File:Hash_table_average_insertion_time.png)

In this implementation below, I have implemented a strategy to resize the table when it is half full. This is a common strategy to mitigate the O(n) search time when we have a lot of collisions. But on each resize, we have to rehash all elements: O(n) when it grows. This growth will occur rarely so this O(n) is amortized.

#### Implementation with open addressing and linear probing

```c++
#include <iostream>
#include <type_traits>

// key should not be modifiable
// template concept to require the custom data to implement hash function and == operator
// or use standard library hash and == operators
template <typename T>
concept HasHashFunction =
requires(T t, T u) {
  { t.hash() } -> std::convertible_to<std::size_t>;
  { t == u } -> std::convertible_to<bool>;
} || requires(T t, T u) {
  { std::hash<std::remove_const_t<T>>{}(t) } -> std::convertible_to<std::size_t>;
  { t == u } -> std::convertible_to<bool>;
};

// hash table
template <HasHashFunction K, typename V>
struct Hashtable {
private:
  // key pair internal data structure. should never be used outside the hashtable
  // the key should not be modifiable
  struct KeyValuePair {
    friend class Hashtable;
    // the slot can be empty, occupied or deleted
    enum class State : uint8_t {
      empty,
      occupied,
      deleted
    };
  private:
    State state = State::empty;
    // the pair key value - store without const for assignment
    K key;
    V value;

    // to be used only on initialization
    void clear() { state = State::empty; }

  public:
    [[nodiscard]] const State& GetState() const { return state;}
    void SetDeleted() { state = State::deleted; }
    void Set(const K& k, const V& v) {
      key = k;
      value = v;
      state = State::occupied;
    }

    // const because once is set, the key should not be modifiable
    [[nodiscard]] const K& Key() const {
      if (state == State::occupied)
        return key;
      throw std::invalid_argument("Cannot get key from empty or deleted KeyValuePair");
    }
    V& Value() {
      if (state == State::occupied)
        return value;
      throw std::invalid_argument("Cannot get value from empty or deleted KeyValuePair");
    }

    // constructors
    KeyValuePair(const K& key, const V& value) : key(key), value(value), state(State::occupied) {}
    KeyValuePair(): state(State::empty), key(), value() {}
  };

private:
  // array of key value pairs
  KeyValuePair* table;
  // how many deleted elements are in the table
  size_t deletedCount = 0;
  // how many elements are in the table
  size_t size=0;
  // initial capacity of the table as 4 to avoid too many grow calls
  size_t capacity = 4;
  // when resizing, multiply capacity by growthFactor
  float growthFactor = 2.0;
  // load factor is size/capacity
  // grow when the load factor is greater than 0.75
  const float growThreshold = 0.75;
  // shrink when the load factor is less than 0.25
  const float shrinkThreshold = 0.25;
  // rehash when the deleted elements are more than 25% of the capacity
  const float deletedThreshold = 0.25;

public:
  explicit Hashtable(size_t capacity=4) {
    if(capacity < 4)
      throw std::invalid_argument("Capacity must be greater than 4");

    this->size = 0;
    this->capacity = capacity;
    table = new KeyValuePair[capacity];
    for (size_t i = 0; i < capacity; i++)
      table[i].clear();
  }

  [[nodiscard]] float LoadFactor(const bool countDeleted=false) const {
    return (float)(size + (countDeleted?deletedCount:0)) / (float)capacity;
  }

  [[nodiscard]] size_t Size() const { return size; }
  [[nodiscard]] size_t Capacity() const { return capacity; }

private:
  inline size_t convertKeyToIndex(const K& t) {
    if constexpr (requires { t.hash(); }) {
      // Type has custom hash() method
      return t.hash() % capacity;
    } else {
      // Use std::hash for standard types
      return std::hash<std::remove_const_t<K>>{}(t) % capacity;
    }
  }

  void resizeIfNeeded() {
    // case where we have too many deleted elements but the load factor is still acceptable
    bool shouldRehashForDeleted = ((float)deletedCount / (float)capacity) >= deletedThreshold;

    // decide new capacity
    auto newCapacity = capacity;

    // grow or shrink
    if (LoadFactor(true) >= growThreshold)
      newCapacity *= growthFactor;
    else if (LoadFactor(false) <= shrinkThreshold && capacity > 4)
      newCapacity = std::max((size_t)4, (size_t)(newCapacity/growthFactor));

    // Rehash if capacity changes OR if we have too many deleted elements
    if (newCapacity != capacity || shouldRehashForDeleted) {
      auto oldTable = table;
      auto oldCapacity = capacity;
      table = new KeyValuePair[newCapacity];
      capacity = newCapacity;
      for (size_t i = 0; i < capacity; i++)
        table[i].clear();
      size = 0;
      deletedCount = 0;
      // insert all elements again
      for (size_t i = 0; i < oldCapacity; i++)
        if (oldTable[i].GetState() == KeyValuePair::State::occupied)
          _insert(oldTable[i].Key(), oldTable[i].Value());
      delete[] oldTable;
    }
  }

private:
  void _insert(const K& key, const V& value) {
    size_t index = convertKeyToIndex(key);

    // Check if key already exists and update instead
    while (table[index].GetState() != KeyValuePair::State::empty) {
      if (table[index].GetState() == KeyValuePair::State::occupied && table[index].Key() == key) {
        table[index].Set(key, value); // Update existing
        return;
      }
      index = (index + 1) % capacity;
    }

    // Key doesn't exist, find next available slot (empty or deleted)
    index = convertKeyToIndex(key); // Reset index to start position
    while (table[index].GetState() == KeyValuePair::State::occupied) {
      index = (index + 1) % capacity;
    }

    // If we're reusing a deleted slot, decrease deleted count
    if (table[index].GetState() == KeyValuePair::State::deleted) {
      deletedCount--;
    }
    table[index].Set(key, value);
    size++;
  }

public:
  // inserts a new key value pair
  // this implementation uses open addressing and resize the table when it is half full
  void insert(const K& key, const V& value) {
    // insert the new element
    _insert(key, value);

    // resize if necessary
    // in open addressing, it is common to resize when the table is half full
    // this help mitigate O(n) search time when we have a lot of collisions or deletions
    // but on each resize, we have to rehash all elements: O(n)
    resizeIfNeeded();
  }

  // contains the key
  bool contains(K key) {
    // probable location
    size_t index = convertKeyToIndex(key);
    // linear search until we find the key or an empty slot
    while (table[index].GetState() != KeyValuePair::State::empty) {
      if (table[index].GetState() == KeyValuePair::State::occupied && table[index].Key() == key) {
        return true;
      }
      index = (index + 1) % capacity;
    }

    return false;
  }

  // subscript operator
  // fails if the key is not found
  V& operator[](K key) {
    size_t index = convertKeyToIndex(key);
    while (table[index].GetState() != KeyValuePair::State::empty) {
      if (table[index].GetState() == KeyValuePair::State::occupied && table[index].Key() == key) {
        return table[index].Value();
      }
      index = (index + 1) % capacity;
    }
    throw std::out_of_range("Key not found");
  }

  // deletes the key
  // fails if the key is not found
  void remove(K key) {
    size_t index = convertKeyToIndex(key);
    while (table[index].GetState() != KeyValuePair::State::empty) {
      if (table[index].GetState() == KeyValuePair::State::occupied && table[index].Key() == key) {
        table[index].SetDeleted();
        size--;
        deletedCount++;
        resizeIfNeeded();
        return;
      }
      index = (index + 1) % capacity;
    }
    throw std::out_of_range("Key not found");
  }

  ~Hashtable() {
    delete[] table;
  }
};

struct MyCustomHashableType {
  int x, y;
  [[nodiscard]] size_t hash() const {
    return std::hash<int>()(x) ^ (std::hash<int>()(y));
  }
  bool operator==(const MyCustomHashableType& other) const {
    return x == other.x && y == other.y;
  }
};

int main() {
  // Test with standard integer keys (uses std::hash<int>)
  Hashtable<int, std::string> intHashtable;
  intHashtable.insert(1, "one"); // load factor 0.25 (1/4)
  intHashtable.insert(2, "two"); // load factor 0.5 (2/4)
  // after the insertion, it should resize
  intHashtable.insert(3, "three"); // load factor 0.375 (3/8)
  intHashtable.insert(4, "four"); // load factor 0.5 (4/8)
  intHashtable.remove(3); // load factor 0.375 (3/8) with 1 deleted
  // after the removal, it should resize
  intHashtable.remove(4); // load factor 0.5 (2/4) with 0 deleted

  std::cout << "Integer hashtable:" << std::endl;
  std::cout << "Key 1: " << intHashtable[1] << std::endl;
  std::cout << "Key 2: " << intHashtable[2] << std::endl;

  // Test with string keys (uses std::hash<std::string>)
  Hashtable<std::string, int> stringHashtable;
  stringHashtable.insert("hello", 42);
  stringHashtable.insert("world", 99);

  std::cout << "\nString hashtable:" << std::endl;
  std::cout << "Key 'hello': " << stringHashtable["hello"] << std::endl;
  std::cout << "Key 'world': " << stringHashtable["world"] << std::endl;

  // Test with custom type (uses custom hash() method)
  Hashtable<MyCustomHashableType, std::string> customHashtable;
  MyCustomHashableType key1{1, 2};
  MyCustomHashableType key2{2, 3};
  MyCustomHashableType key3{3,8};

  customHashtable.insert(key1, "custom one");
  customHashtable.insert(key2, "custom two");
  customHashtable.insert(key3, "custom three");

  std::cout << "\nCustom type hashtable:" << std::endl;
  std::cout << "Key {" << key1.x << ", " << key1.y << "}: " << customHashtable[key1] << std::endl;
  std::cout << "Key {" << key2.x << ", " << key2.y << "}: " << customHashtable[key2] << std::endl;
  std::cout << "Key {" << key3.x << ", " << key3.y << "}: " << customHashtable[key3] << std::endl;

  return 0;
}
}
```