# Maze Data structures

Mazes are a pretty common type of scenario for game development, and they can be represented in many ways. In this document, we will explore some of the most common data structures used to represent mazes.

## Grid of Rooms

The most common way to represent a maze is a grid of rooms. It usually can be a squared grid, or rarely, a hexagonal grid. Here I will talk about the squared grid, but you can adapt the concepts to a hexagonal grid too. 

Let's simplify the maze to a grid of rooms where each room can have walls in any of the four directions (north, south, east, west) and the data the room should store.

```
Example: of a 3x3 grid maze
 _ _ _
|_|_|_|
|_|_|_|
|_|_|_| 
```

## Data Structure

The abstract idea of the room could be something like this:

```c++
struct RoomInfo {
    // data of the room
};

struct Room {
    RoomInfo data;
    bool northWall;
    bool southWall;
    bool eastWall;
    bool westWall;
};
```

We can store the rooms into a simple 2D array:

```c++
Room maze[3][3];
```

But 2D arrays are a bit worse in terms of cache locality compared to a 1D array, so we can flatten the 2D array into a 1D array:

```c++
Room maze[3*3];
Room& getRoom(int x, int y) {
    return maze[y*3 + x];
}
```

To further improve the cache locality, applying concepts of Data Oriented Programming we could create a registry for our maze and isolate the room data from the walls:

```c++
struct RoomInfo {
    // data of the room
};
struct RoomWall {
    bool north;
    bool south;
    bool east;
    bool west;
};
struct RoomRegistry {
    int width, height;
    vector<RoomInfo> data;
    vector<RoomWall> walls;
    RoomInfo& getRoomInfo(int x, int y) {
        return data[y*width + x];
    }
    RoomWall& getRoomWall(int x, int y) {
        return walls[y*width + x];
    }
};
```

If you have a sparse maze, you can use a hash map to store the rooms, and be a bit more memory efficient:

```c++
struct RoomRegistry {
    unordered_map<pair<int, int>, RoomInfo> data;
    unordered_map<pair<int, int>, RoomWall> walls;
    RoomInfo& getRoomInfo(int x, int y) {
        return data[{x, y}];
    }
    RoomWall& getRoomWall(int x, int y) {
        return walls[{x, y}];
    }
};
```

Or you can use pointers for the neighbors, if the pointer is null, it means there is no neighbor in that direction. But it will use more memory (pointers usually uses 8 bytes), will be less cache efficient (data information would be scattered in the heap), and you will have extra effort to query rooms at position (X,Y). 

```c++
struct Room {
    RoomInfo data;
    Room* north; 
    Room* south;
    Room* east;
    Room* west;
};
```

Let's assume we don't have a sparse maze, so we will use the 2D array representation.

Now I will try to reduce the amount of memory used by the walls. Consider the current state of the RoomWall struct:

```c++
struct RoomWall {
    bool north; // uses 1 byte
    bool south; // uses 1 byte
    bool east; // uses 1 byte
    bool west; // uses 1 byte
};
```

Can we make it more memory efficient? Yes, we can use bitfields to store the walls in a single byte:

```c++
struct RoomWall {
    uint8_t walls; // one byte
    bool hasNorthWall() const {
        return walls & 1;
    }
    bool hasSouthWall() const {
        return walls & 2;
    }
    bool hasEastWall() const {
        return walls & 4;
    }
    bool hasWestWall() const {
        return walls & 8;
    }
    void setNorthWall(bool value) {
        if (value) walls |= 1;
        else walls &= ~1;
    }
    void setSouthWall(bool value) {
        if (value) walls |= 2;
        else walls &= ~2;
    }
    void setEastWall(bool value) {
        if (value) walls |= 4;
        else walls &= ~4;
    }
    void setWestWall(bool value) {
        if (value) walls |= 8;
        else walls &= ~8;
    }
}; // one byte
```

Or we can simplify it by just using data layout to do the same thing but with less code, and left the compiler to do the work:

```c++
struct RoomWall {
    bool north: 1; // uses 1 bit
    bool south: 1; // uses 1 bit
    bool east: 1;  // uses 1 bit
    bool west: 1;  // uses 1 bit
}; // uses 1 byte because byte is the smallest unit of addressable memory
```

There is another issue with that representation: two adjacent rooms will have duplicated walls. Ex.: the north wall of a given room is the same as the south wall of the room above it. 

To fix that issue, we will need to not use the abstraction of RoomWall anymore and store the walls directly in the RoomRegistry struct:

```c++
struct RoomRegistry {
    int width, height;
    vector<RoomInfo> data;
    vector<bool> walls; 
};
```

Before going deep into how can we address the indexes for the walls, you need to know `vector<bool>` are not common vectors where each element returns a reference to a bool. Instead, it returns a proxy object that behaves like a bool. This is because the standard `vector<bool>` is a specialization of the vector class that is optimized for space efficiency. 

```c++ 
// Example of how vector<bool> works
template<typename T>
stuct vector<bool> {
    // other controlling fields and methods
    uint_t* data;
    bool operator[](size_t index) {
        return data[index / 8] & (1 << (index % 8));
    }
};
```

![vector of bools](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Fvector_bool.png)

Now we have a way to address bits directly using `vector<bool>`, but you need to remember that for an X x Y grid, we will need X+1 vertical walls and Y+1 horizontal walls. Check the following example below for a 2x2 grid:

```
 _ _
|_|_|
|_|_| 
```

So we will need 3 vertical walls and 3 horizontal walls.

Now, we reached to the next issue. How can we address the walls in the `std::vector<bool>`? We will need to change our point of view from addressing Rooms at position (X,Y) to WallIntersections. Every intersection will be 2 bits to represent vertical and horizontal walls.

```
 _ _
|_|_|
|_|_| 
```

In the previous example of a 2x2 grid, we will have the following intersections:

| y | x | vertical | horizontal |
|---|---|----------|------------|
| 0 | 0 | false    | true       |
| 0 | 1 | false    | true       |
| 0 | 2 | false    | false      |
| 1 | 0 | true     | true       |
| 1 | 1 | true     | true       |
| 1 | 2 | true     | false      |
| 2 | 0 | true     | true       |
| 2 | 1 | true     | true       |
| 2 | 2 | true     | false      |

So that grid is represented as an array of 18 bits like this:

```
01 01 00 11 11 10 11 11 10
``` 

So if we address it via index,

| index | value | y | x | orientation |
|-------|-------|---|---|-------------|
| 0     | 0     | 0 | 0 | vertical    |
| 1     | 1     | 0 | 0 | horizontal  |
| 2     | 0     | 0 | 1 | vertical    |
| 3     | 1     | 0 | 1 | horizontal  |
| 4     | 0     | 0 | 2 | vertical    |
| 5     | 0     | 0 | 2 | horizontal  |
| 6     | 1     | 1 | 0 | vertical    |
| 7     | 1     | 1 | 0 | horizontal  |
| 8     | 1     | 1 | 1 | vertical    |
| 9     | 1     | 1 | 1 | horizontal  |
| 10    | 1     | 1 | 2 | vertical    |
| 11    | 0     | 1 | 2 | horizontal  |
| 12    | 1     | 2 | 0 | vertical    |
| 13    | 1     | 2 | 0 | horizontal  |
| 14    | 1     | 2 | 1 | vertical    |
| 15    | 1     | 2 | 1 | horizontal  |
| 16    | 1     | 2 | 2 | vertical    |
| 17    | 0     | 2 | 2 | horizontal  |

Now all we need to do is to create functions to get and set the walls for a specific room at position (X,Y). In this world reference, we will consider the top-left corner as the origin (0,0) and the bottom-right corner as (width-1, height-1).

```c++
struct RoomRegistry {
    int width, height;
    vector<RoomInfo> data;
    vector<bool> walls; 
    bool getNorthWall(int x, int y) {
        return walls[2*(y*(width+1)+x)+1];
    }
    bool getSouthWall(int x, int y) {
        return walls[2*((y+1)*(width+1) + x)+1];
    }
    bool getWestWall(int x, int y) {
        return walls[2*((y+1)*(width+1)+x)];
    }
    bool getEastWall(int x, int y) {
        return walls[2*((y+1)*(width+1)+x+1)];
    }
    // set functions
};
```

## Conclusion

Now we are using the most memory efficient way to represent a dense maze. We learned matrix flattening, bit index addressing, data layout, and discovered why `vector<bool>` is a bit different from other vectors.