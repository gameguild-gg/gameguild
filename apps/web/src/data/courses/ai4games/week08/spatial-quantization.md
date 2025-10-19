# Space quantization

Space quantization is a way to sample continuous space, and it can to be used in in many fields, such as Artificial Intelligence, Physics, Rendering, and more. Here we are going to focus primarily Spatial Quantization for AI, because it is the base for pathfinding, line of sight, field of view, and many other techniques.

Some of the most common techniques for space quantization are: grids, voxels, graphs, quadtrees, octrees, KD-trees, BSP, Spatial Hashing and more. Another notable techniques are line of sight(or field of view), map flooding, caching, and movement zones.

## Grids

Grids are the most common technique for space quantization. It is a very simple technique, but it is very powerful. It consists in dividing the space in a grid of cells, and then we can use the cell coordinates to represent the space. The most common grid is the square grid, but we can use hexagonal and triangular grids, you might find some irregular shapes useful to exploit the space conformation better.

### Square Grid

The square grid is a regular grid, where the cells are squares. It is very simple to implement and understand.

There are some ways to store data for squared grids. Arguably you could 2D arrays, arrays of arrays or vector of vectors, but depending on the way you implement it, it can hurt the performance. Example: if you use an array of arrays or vector of vectors, where every entry from de outer array is a pointer to the inner array, you will have a lot of cache misses, because the inner arrays are not contiguous in memory.

#### Notes on cache locality

So in order do increase data locality for squared grids, you can use a single array, and then use the following formula to calculate the index of the cell. We call this strategy matrix flattening.

```c++
int arrray[width * height]; // 1D array with the total size of the grid
int index = x + y * width; // index of the cell at x,y
```

There is a catch here, given we usually represent points as X and Y coordinates, we need to be careful with the order of the coordinates. While you are iterating over all the matrix, you need to iterate over the Y coordinate first, and then the X coordinate. This is because the Y coordinate is the one that changes the most, so it is better to have it in the inner loop. By doing that, you will have better cache locality and effectively the index will be sequential.

```c++
vector<YourStructure> data; // data is filled with some data elsewhere
for(int y = 0; y < height; y++) {
    for(int x = 0; x < width; x++) {
        // do something with the cell at index x,y
        data[y * width + x] = yourstrucure;
        // it is the same as: data[y][x] = yourstructure;
    }
}
```

#### Quantization and dequantization of square grids

If your world is based on floats, you can use the square by using the floor function or just cast to integer type, because the default behavior of casting from float to integer is to floor it. Example: In the case of a quantization resolution of size of 1.0f, everything between 0 and 1 will be in the cell (0,0), everything between 1 and 2 will be in the cell (1,0), and so on.

```c++
Vector2int quantize(Vector2f position, float resolution) {
    return Vector2int((int)floor(position.x/resolution), (int)floor(position.y/resolution));
}
```

If you need to get the center of the cell in the world coordinates following the quantization resolution, you can use the following code.

```c++
Vector2f dequantize(Vector2int index, float resolution) {
    return Vector2f((float)index.x * resolution + resolution/2.0f, (float)index.y * resolution + resolution/2.0f);
}
```

If you need to get the corners of the cell following the quantization resolution, you can use the following code.

```c++
Rectangle2f cell_bounds(Vector2int index, float resolution) {
    return {index.x * resolution, index.y * resolution, (index.x+1) * resolution, (index.y+1) * resolution};
}
```

If you need to get the neighbors of a cell, you can use the following code.

```c++
std::vector<Vector2int> get_neighbors(Vector2int index) {
    return {{index.x-1, index.y}, {index.x, index.y-1},
            {index.x+1, index.y}, {index.x, index.y+1}};
}
```

We already understood the idea of matrix flattening to improve efficiency, we can use it to represent a maze. But in a maze, we have walls to

Imagine that you are willing to be as memory efficient and more cache friendly as possible. You can use a single array to store the maze, and you can use the following formula to convert from matrix indexes to the index of the cell in the array.

### Hexagonal Grid

Hexagonal grid is an extension of a square grid, but the cells are hexagons. It feels nicer to human eyes because we have more equally distant neighbors. If used as subtract for pathfinding, it can be more efficient because the path can be more straight.

It can be implemented as single dimension array, but you need to be careful with shift that happens in different odd or even indexes. You can use the following formula to calculate the index of the cell. In this world quantization can be in 4 conformations, depending on the rotation of the hexagon and the alignment of the first cell.

1. Point pointy top hexagon with first line aligned to the left:

```text
  / \ / \ / \
 | A | B | C |
  \ / \ / \ / \
   | D | E | F |
  / \ / \ / \ /
 | G | H | I |
  \ / \ / \ /
```

2. Point pointy top hexagon with first line aligned to the right

```text
    / \ / \ / \
   | A | B | C |
  / \ / \ / \ /
 | D | E | F |
  \ / \ / \ / \
   | G | H | I |
    \ / \ / \ /
```

3. Flat top hexagon with first column aligned to the top:

```text
 __    __
/A \__/C \
\__/B \__/
/D \__/F \
\__/E \__/
/G \__/I \
\__/H \__/
   \__/
```

4. Flat top hexagon with first column aligned to the bottom:

```text
     __
  __/B \__
 /A \__/C \
 \__/E \__/
 /D \__/F \
 \__/H \__/
 /G \__/I \
 \__/  \__/
```

#### Quantization and dequantization of hexagonal grids

For simplicity, we are going to use the first conformation, where the first line is aligned to the left, and the hexagons are pointy top. The quantization is done by using the following formula.

```c++
// I am assuming that the hexagon is pointy top, and the first line is aligned to the left
// I am also assuming that the hexagon is centered in the cell, and the top left corner is at (0,0),
// y axis is pointing down and x axis is pointing right
// this dont work for all the cases, but it is a good approximation for locations near the center of the hexagon
/*
  / \ / \ / \
 | A | B | C |
  \ / \ / \ / \
   | D | E | F |
  / \ / \ / \ /
 | G | H | I |
  \ / \ / \ /
 */
Vector2int quantize(Vector2f position, float hexagonSide) {
    int y = (position.y - hexagonSide)/(hexagonSide * 2);
    int x = y%2==0 ?
      (position.x - hexagonSide * sqrt3over2) / (hexagonSide * sqrt3over2 * 2) : // even lines
      (position.x - hexagonSide * sqrt3over2 * 2)/(hexagonSide * sqrt3over2 * 2) // odd lines
    return Vector2int(x, y);
}
Vector2f dequantize(Vector2int index, float hexagonSide) {
    return Vector2f(index.y%2==0 ?
      hexagonSide * sqrt3over2 + index.x * hexagonSide * sqrt3over2 * 2 : // even lines
      hexagonSide * sqrt3over2 * 2 + index.x * hexagonSide * sqrt3over2 * 2, // odd lines
      hexagonSide + index.y * hexagonSide * 2);
}
```

You will have to figure out the formula for the other conformations. Or send a merge request to this repository adding more information.

## Voxels and Grid 3D

Grids in 3D works the same way as in 2D, but you need to use 3D vectors/arrays or voxel volumes. Most concepts applies here. If you want to expand this section, send a merge request.

## Quadtree

Quadtree is a tree data structure where each node has 4 children. It is used to partition a space in 2D. It is used to optimize collision detection, pathfinding, and other algorithms that need to iterate over a space. It is also used to optimize rendering, because you can render only the visible part of the space.

### Quadtree implementation

Quadtree is a recursive data structure, so you can implement it using a recursive data structure. The following code is a simple implementation of a quadtree.

```c++
// this code is not tested, but it should work. It is just an example and send a merge request if you find any errors.
// node
template<class T>
struct DataAtPosition {
    Vector2f center;
    T data;
};

template<class T>
struct QuadtreeNode {
    Rectangle2f bounds;
    std::vector<DataAtPosition<T>> data;
    std::vector<QuadtreeNode<T>> children;
};

// insert
template<class T>
void insert(QuadtreeNode<T>& root, DataAtPosition<T> data) {
    if (root.children.empty()) {
        root.data.push_back(data);
        if (root.data.size() > 4) {
            root.children.resize(4);
            for (int i = 0; i < 4; ++i) {
                root.children[i].bounds = root.bounds;
            }
            root.children[0].bounds.max.x = root.bounds.center().x; // top left
            root.children[0].bounds.max.y = root.bounds.center().y; // top left
            root.children[1].bounds.min.x = root.bounds.center().x; // top right
            root.children[1].bounds.max.y = root.bounds.center().y; // top right
            root.children[2].bounds.min.x = root.bounds.center().x; // bottom right
            root.children[2].bounds.min.y = root.bounds.center().y; // bottom right
            root.children[3].bounds.max.x = root.bounds.center().x; // bottom left
            root.children[3].bounds.min.y = root.bounds.center().y; // bottom left
            for (auto& data : root.data) {
                insert(root, data);
            }
            root.data.clear();
        }
    } else {
        for (auto& child : root.children) {
            if (child.bounds.contains(data.center)) {
                insert(child, data);
                break;
            }
        }
    }
}

// query
template<class T>
void query(QuadtreeNode<T>& root, Rectangle2f bounds, std::vector<DataAtPosition<T>>& result) {
    if (root.bounds.intersects(bounds)) {
        for (auto& data : root.data) {
            if (bounds.contains(data.center)) {
                result.push_back(data);
            }
        }
        for (auto& child : root.children) {
            query(child, bounds, result);
        }
    }
}
```

### Quadtree optimization

The quadtree is a recursive data structure, so it is not cache friendly. You can optimize it by using a flat array instead of a recursive data structure.

## Octree

Section WiP. Send a merge request if you want to contribute.

## KD-Tree

KD-Trees are a tree data structure that are used to partition a spaces in any dimension (2D, 3D, 4D, etc). They are used to optimize collision detection(Physics), pathfinding(AI), and other algorithms that need to iterate over a space. Also they are also used to optimize rendering, because you can render only the visible part of the space. Pay attention that KD-Trees are not the same as Quadtree and Octrees, even if they are similar.

In KD-trees, every node defines an orthogonal partition plan that alternate every deepening level of the tree. The partition plan is defined by a dimension, a value. The dimension is the axis that is used to partition the space, and the value is the position of the partition plan. The partition plan is orthogonal to the axis, so it is a line in 2D, a plane in 3D, and a hyperplane in 4D.

## BSP Tree

BSP inherits almost all characteristics of KD-Trees, but it is not a tree data structure, it is a graph data structure. The main difference is to instead of being orthogonal you define the plane of the section. The plane is defined by a point and a normal. The normal is the direction of the plane, and the point is a point in the plane.

# Spatial Hashing

A Spatial Hashing is a common technique to speed up queries in a multidimensional space. It is a data structure that allows you to quickly find all objects within a certain area of space. It is commonly used in games and simulations to speed up, artificial intelligence world queries, collision detection, visibility testing and other spatial queries.

Advantages of the spatial hashing:

- simple to implement;
- very fast: as fast as your key hashing function;
- easy to parallelize;
- a good choice for big worlds;

Problem with spatial hashing:

- it is not precise;
- it is not good for small worlds;
- needs fine tune to find the right cell size;
- have to update the bucket when the object moves;
- find the nearest objects is not trivial, you will have to query the adjacent cells;

## Buckets

The core of the spatial hashing is the bucket. It is a container that holds all the objects that are within a certain area of space contained in the cell area or volume. The terms cell and bucket can be interchangeable in this context.

In order to find buckets, you will have to create ways to quantize the world space into a grid of cells. It is hard to define the best cell size, but it is a good practice to make it be a couple of times bigger than the biggest object you have in the world. The cell size will define the precision of the spatial hashing, and the bigger it is, the less precise it will be.

## Spatial quantization

The spatial quantization is the process of converting a continuous space into a discrete space. This is the core process of finding the right bucket for an object. Let's assume that we have a 2D space, and we want to find the bucket for a given object.

```c++
// assuming Vector2f is a 2D vector with float components;
// and Vector2i is a 2D vector with integer components;
// the quantizations function will be:
Vector2<int32_t> quantized(float_t cellSize=1.0f) const {
  return Vector2<int32_t>{
    static_cast<int32_t>(std::floor(x + cellSize/2) / cellSize),
    static_cast<int32_t>(std::floor(y + cellSize/2) / cellSize)
  };
}
```

### Data structures

#### Data structure for the bucket

First, we have to decide the data structure your bucket will use to store the objects. The common choices are:

- `vector<GameObject*>` - a vector of pointers to game objects;
- `set<GameObject*>` - a set of pointers to game objects;
- `unordered_set<GameObject*>` - an unordered_set of pointers to game objects;

- The problem of using a `vector` is that it is not efficient to remove, and find an object in it: `O(n)`; but it is efficient to add (amortized `O(1)`) and iterate over it (random access is `O(1)`).
- The underlying data structure of a `set` and `map` is a binary search tree, so it is efficient to find, add and remove objects: `O(lg(n))`, but it is not efficient to iterate over it.
- Now, the `unordered_set` and `unordered_map` is a hash table, so it is efficient to find, add and remove objects: `O(1)`, and it is efficient to iterate over it. The overhead of using a hash table is the memory usage and the hashing function. It will be as fast as your hashing function.

In our use case, we will frequently list all elements in a bucket, we will add and remove elements from it, while they move in the world. So, the best choice is to use an `unordered_set` of pointers to game objects.

So lets define the bucket:

```cpp
using std::unordered_set<GameObject*> = bucket_t;
```

#### Data structure for indexing buckets

Ideally, we are looking for a data structure that will give us a bucket for a given position. We have some candidates for this job:

- `bucket_t[width][height]` - a 2D array of buckets;
- `vector<vector<bucket_t>>` - a 2D vector of buckets;
- `map<Vector2i, bucket_t>` - a map of buckets;
- `unordered_map<Vector2i, bucket_t>` - a map of buckets;

- `array`s and `vector`s are the fastest data structures to use, but they are not good choices if you have a sparse world;
- `map` is a binary search tree;
- `unordered_map` is a hash table.

The `unordered_map` is the best choice for this use case.

```c++
// quantized world
unordered_map<Vector2i, go_bucket_t> world;
```

#### Iterating over the whole world at once

Sometimes we just want to iterate over all objects in the world, add and remove elements. In this case, we can use a `unordered_set` to store all game objects.

```c++
// all game objects for faster global world iteration and cleanup
go_bucket_t worldObjects;
```

#### Neighbor cells

When you need to query the neighbors of an object, most of the time you will need to check the current cell and the adjacent cells. You can create a function for that or include the content of it in your logic.

```cpp
// neighbor buckets. not memory intensive
// returns the reference to the 9 buckets surrounding the given bucket, including itself
// but on the usage, you will have to check
vector<go_bucket_t*> neighborBuckets(const Vector2i& bucket) {
    vector<go_bucket_t*> neighbors;
    neighbors.reserve(9); // to avoid reallocations
    for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++){
            neighbors.push_back(&world()[Vector2i{bucket.x + i, bucket.y + j}]);
        }
    return neighbors;
}

// neighbors objects inside the 9 buckets surroundings the given bucket
// memory intensive.
go_bucket_t neighborObjects(const Vector2i& bucket) {
    go_bucket_t neighbors;
    for (auto& b: neighborBuckets(bucket))
        neighbors.insert(b->begin(), b->end());
    return neighbors;
}
```

## Implementation

This sample bellow a bit complex, but I added a bunch of support code to make it more complete, feel free to simplify it to your needs and split into multiple files.

```cpp
#include <iostream> // for cout
#include <unordered_map> // for unordered_map
#include <unordered_set> // for unordered_set
#include <random> // for random_device and default_random_engine
#include <cmath> // for floor
#include <cstdint> // for int32_t
#include <vector> // for vector

// to allow derivated structs to be used as keys in sorted containers and binary search algorithms
template<typename T>
struct IComparable { virtual bool operator<(const T& other) const = 0; };
// to allow derivated structs to be used as keys in hash based containers and linear search algorithms
template<typename T>
struct IEquatable { virtual bool operator==(const T& other) const = 0; };

// generic Vector2
// requires that T is a int32_t or float_t
template<typename T>
#ifdef __cpp_concepts
requires std::is_same_v<T, int32_t> || std::is_same_v<T, float_t>
#endif
struct Vector2:
        public IComparable<Vector2<T>>,
        public IEquatable<Vector2<T>> {
    T x, y;
    Vector2(): x(0), y(0) {}
    Vector2(T x, T y): x(x), y(y) {}
    // operator equals
    bool operator==(const Vector2& other) const override {
        return this == &other || (x == other.x && y == other.y);
    }
    // operator < for being able to use it as a key in a map or set
    bool operator<(const Vector2& other) const override {
        return x < other.x || (x == other.x && y < other.y);
    }

    // quantize the vector to a 2d index
    // to nearest integer
    Vector2<int32_t> quantized(float_t cellSize=1.0f) const {
        return Vector2<int32_t>{
                static_cast<int32_t>(std::floor(x + cellSize/2) / cellSize),
                static_cast<int32_t>(std::floor(y + cellSize/2) / cellSize)
        };
    }
};

// specialized Vector2 for int and float
using Vector2i = Vector2<int32_t>;
// float32_t is only available in c++23, so we use float_t instead
using Vector2f = Vector2<float_t>;

// helper struct to generate unique id for game objects
// mostly debug purposes
struct uid_type {
private:
    static inline size_t nextId = 0; // to be used as a counter
    size_t uid; // to be used as a unique identifier
public:
    // not thread safe, but it is not a problem for this example
    uid_type(): uid(nextId++) {}
    inline size_t getUid() const { return uid; }
};

// generic game object implementation
// replace this with your own data that you want to store in the world
class GameObject: public uid_type {
    Vector2f position;
public:
    GameObject();
    GameObject(const GameObject& other);
    // todo: add your other custom data here
    // when the it moves, it should check if it needs to update its bucket in the world
    void setPosition(const Vector2f& newPosition);
    Vector2f getPosition() const { return position; }
};

// hashing
namespace std {
    // Hash specialization for Vector2i
    template<>
    struct hash<Vector2i> {
        size_t operator()(const Vector2i& v) const {
            // shift and xor operator the other to get a unique hash
            // the problem of this approach is that it will generate neighboring cells with similar hashes
            // to fix that, you might want to use a more complex hashing function from std::hash<T>
            // copy to avoid const cast
            auto x = v.x, y = v.y;
            return (*reinterpret_cast<size_t*>(&x) << 32) ^ (*reinterpret_cast<size_t*>(&y));
        }
    };
}

// game object pointer
using GameObjectPtr = GameObject*;
// alias for the game object bucket
using go_bucket_t = std::unordered_set<GameObjectPtr>;
// alias for the world type
using world_t = std::unordered_map<Vector2i, go_bucket_t>;

// singletons here are being used to avoid global variables and to allow the world to be used in a visible scope
// you should use a better wrappers and abstractions in a real project
// singleton world
world_t& world() {
    static world_t world;
    return world;
}
// singleton world objects
go_bucket_t& worldObjects(){
    static go_bucket_t worldObjects;
    return worldObjects;
}

// Constructor
GameObject::GameObject(): uid_type(), position({0,0}) {
    // insert in the world
    worldObjects().insert(this);
    world()[position.quantized()].insert(this);
}

// Copy constructor
GameObject::GameObject(const GameObject& other): uid_type(other), position(other.position) {
    // insert in the world
    worldObjects().insert(this);
    world()[position.quantized()].insert(this);
}

// this function requires the world to be in a visible scope like this or change it to access through a singleton
// if in the movement, it changes its quantized position, we should remove it from the old bucket and insert it in the new one
void GameObject::setPosition(const Vector2f& newPosition) {
    world_t& w = world();
    // bucket ids
    auto oldId = position.quantized();
    auto newId = newPosition.quantized();
    // update position
    position = newPosition;
    // check if it needs to update its bucket in the world
    if (newId == oldId)
        return;
    // remove from the old bucket
    w[oldId].erase(this);
    if(w[oldId].empty()) [[unlikely]] // c++20
        w.erase(oldId);
    // insert in the new bucket
    w[newId].insert(this);
}

// random vector2f
Vector2f randomVector2f(float_t min, float_t max) {
    static std::random_device rd;
    static std::default_random_engine re(rd());
    static std::uniform_real_distribution<float_t> dist(min, max);
    return Vector2f{dist(re), dist(re)};
}

// neighbor buckets. not memory intensive
// returns potentially all 9 buckets surroundings the given bucket, including itself
std::vector<go_bucket_t*> neighborBuckets(const Vector2i& bucket) {
    std::vector<go_bucket_t*> neighbors;
    for (int i = -1; i <= 1; i++){
        for (int j = -1; j <= 1; j++){
            auto id = Vector2i{bucket.x + i, bucket.y + j};
            if(world().contains(id) && !world()[id].empty()) // contains is c++20
                neighbors.push_back(&world()[id]);
        }
    }
    return neighbors;
}

// neighbors objects inside the 9 buckets surroundings the given bucket
// memory intensive. use with caution
go_bucket_t neighborObjects(const Vector2i& bucket) {
    go_bucket_t neighbors;
    for (auto& b: neighborBuckets(bucket))
        neighbors.insert(b->begin(), b->end());
    return neighbors;
}

// dump world
void dumpWorld() {
    for (auto& bucket: world()) {
        std::cout << "bucket: [" << bucket.first.x << "," << bucket.first.y << "]:" << std::endl;
        for (auto& obj: bucket.second)
             std::cout <<" - "<< obj->getUid() << ": at (" << obj->getPosition().x << ", " << obj->getPosition().y << ")" << std::endl;
    }
    std::cout << std::endl;
}

int main() {
    // fill the world with some game objects
    for (int i = 0; i < 121; i++) {
        // the constructor will insert it in the world
        auto obj = new GameObject();
        // randomly move the game objects
        // this will update their position and their bucket in the world
        obj -> setPosition(randomVector2f(-5, 5));
    }

    // dump the world
    dumpWorld();

    // remove all game objects
    for (auto& obj: worldObjects())
        delete obj;

    // clear refs
    worldObjects().clear();
    world().clear();

    return 0;
}
```
