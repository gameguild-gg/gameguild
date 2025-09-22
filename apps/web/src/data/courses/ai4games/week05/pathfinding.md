# Pathfinding

## Random Walk

In AI for games we usually refers to **Random Walk** as a simple way to move an agent in a grid world. It is a very basic way to move an agent in a grid world. It is usually used as a baseline to compare more advanced pathfinding algorithms.

## Depth First Search

The Depth First Search (DFS) is a graph traversal algorithm that explores as far as possible along each branch before backtracking. It is a recursive algorithm that uses a stack to keep track of the nodes to visit. If you combine DFS with Random Walk, you can generate a random path in a world like you did on the last week. 

The central idea of DFS is to keep a set(`unordered_set`) of visited nodes and a stack(`stack`) of nodes you are currently exploring. You should initialize the `bootstrap` case with the `start` node by adding it to the stack. Then loop over the stack: in every step you get the `top` element of the stack, mark it as visited and check the `visitable neighbors` of it:

- Test if it is the `goal`, if yes, reconstruct the path using the stack;
- If there are `more than one`, randomize the next one to visit and add it to the stack;
- If there is `only one`, there is no need for randomizing, just add it to the stack;
- If the top has `no visitable neighbors`, `pop` it from the stack. This is called `Backtrack`.
- If the stack is empty, the search is finished.

### DFS Flow Chart

``` mermaid
graph TD;
    init[Add a start node to the stack] --> top["Get the top element of the stack"]
    top --> visit["Mark the top element as visited"]
    visit --> check["Check the visitable neighbors of the top element"]
    check --> |"More than one"| randomize["Randomize the next one to visit"]
    randomize --> add["Add it to the stack"]
    check --> |"Only one"| add
    add --> top
    check --> |"No visitable neighbors"| backtrack["Pop the top element from the stack. This is called Backtrack"]
    backtrack --> |"Stack is empty"| finish["The search is finished"]
    backtrack --> |"Stack is not empty"| top
```

### DFS simple Implementation in C++

``` c++
#include <iostream>
#include <stack>
#include <vector>
#include <unordered_set>

// Node struct to represent a position in the grid
// You should modify it to meet your needs
// Assume operator == is defined for Node
// Assume the hash function is implemented, so it will work for unordered_set<Node>
struct Node { int x, y; };

// list all visitable neighbors of the current node
std::vector<Node> getVisitableNeighbors(Node& current, std::unordered_set<Node>& visited) {
  // implement this as you like this is just an example for a squared grid, and I am not checking the bounds
  std::vector<Node> neighbors = {{current.x + 1, current.y}, {current.x - 1, current.y}, {current.x, current.y + 1}, {current.x, current.y - 1}};
  // filter out the visited neighbors
  std::vector<Node> visitableNeighbors;
  for (auto& neighbor : neighbors)
    if (!visited.contains(neighbor))
      visitableNeighbors.push_back(neighbor);
  return visitableNeighbors;
}

// search for a path between from start to goal
std::vector<Node> dfs(Node start, Node goal) {
  // the path we are currently exploring
  std::stack<Node> stack;
  // the set of visited nodes
  std::unordered_set<Node> visited;

  // bootstrap the search
  stack.push(start);
  visited.insert(start);

  // loop over the stack
  while (!stack.empty()) {
    Node current = stack.top();

    // test if we found the goal
    if (current == goal) {
      // we found the goal, build the path and return it
      std::vector<Node> path;
      while (!stack.empty()) {
        path.push_back(stack.top());
        stack.pop();
      }
      // reverse the path to get the correct order from the start to the goal
      std::reverse(path.begin(), path.end());
      return path;
    }
    auto neighbors = getVisitableNeighbors(current, visited);
    auto nsize = neighbors.size();
    // this is a minor optimization to prevent calling random when there is only one visitable neighbor
    if (nsize == 1) {
      // if there is only one visitable neighbor, add it to the stack
      stack.push(neighbors[0]);
      visited.insert(neighbors[0]);
    }
    else if (nsize > 1) {
      // if there are visitable neighbors, randomize one and add it to the stack
      // remember to use a better random algorithm
      Node next = neighbors[rand() % nsize];
      stack.push(next);
      visited.insert(next);
    } else {
      // if there are no visitable neighbors, backtrack
      stack.pop();
    }
  }
}
```

### DFS Optimality

::: note

The DFS is not optimal, it will not find the shortest path. 

:::

## Breadth First Search

The Breadth First Search (BFS) is another graph traversal algorithm that explores all nodes at the current depth level before moving to the next depth level. It is an iterative algorithm that uses a `queue` to keep track of the nodes to visit.

The central idea of BFS is a queue(`queue`) of nodes you are currently exploring, a map(`unordered_map`) of parents (or any form to store that a neighbor was first explored from another neighbor), and keep a set(`unordered_set`) of visited nodes optionally (you can rely on the camefrom map). 

You should initialize the `bootstrap` case with the `start` node by adding it to the queue, marking it as it came from itself (or null) and optionally marking it as visited. Then loop over the queue:

- Get the `front` element of the queue;
- Test if it is the `goal`, if yes, reconstruct the path using the parent map;
- For all visitable neighbors:
    - Push (`push_back`) them to the queue;
    - Mark them as visited immediately to avoid duplicates;
    - Track the parent of each neighbor to reconstruct the path later;
- Remove (`pop_front`) the current node from the front of the queue;
- If the queue is empty, the search is finished.

### BFS Flow Chart

``` mermaid
graph TD;
    init[Add a start node to the queue and to the visited set] --> front["Get the front element of the queue"]
    front --> check["Check all visitable neighbors of the front element"]
    check --> add["Add all unvisited neighbors to the queue and mark them as visited"]
    add --> pop["Pop the front element from the queue"]
    pop --> |"Queue is empty"| finish["The search is finished"]
    pop --> |"Queue is not empty"| front
```

### BFS simple Implementation in C++

``` c++
#include <iostream>
#include <queue>
#include <vector>
#include <unordered_set>
#include <unordered_map>

// Node struct to represent a position in the grid
// You should modify it to meet your needs
// Assume operator == is defined for Node
// Assume the hash function is implemented, so it will work for unordered_set<Node>
struct Node { int x, y; };

// list all visitable neighbors of the current node
// implement as you like
std::vector<Node> getVisitableNeighbors(Node& current, std::unordered_set<Node>& visited);

// search for a path between from start to goal
std::vector<Node> bfs(Node start, Node goal) {
  // the queue of nodes to explore
  std::queue<Node> queue;
  // the set of visited nodes
  // note: you can use the camefrom map to check if a node is visited
  std::unordered_set<Node> visited;
  // parent map to reconstruct the path
  std::unordered_map<Node, Node> camefrom;

  // bootstrap the search
  queue.push(start);
  visited.insert(start);
  camefrom[start] = start;

  // loop over the queue
  while (!queue.empty()) {
    Node current = queue.front();
    queue.pop();

    // test if we found the goal
    if (current == goal) {
      // we found the goal, build the path and return it
      std::vector<Node> path;
      Node node = goal;
      while (node != start) {
        path.push_back(node);
        node = camefrom[node];
      }
      path.push_back(start);
      // reverse the path to get the correct order from the start to the goal
      std::reverse(path.begin(), path.end());
      return path;
    }

    auto neighbors = getVisitableNeighbors(current, visited);
    
    // add all unvisited neighbors to the queue
    for (auto& neighbor : neighbors) {
      if (!visited.contains(neighbor)) {
        queue.push(neighbor);
        visited.insert(neighbor);
        camefrom[neighbor] = current; // track the parent for path reconstruction
      }
    }
  }
  
  // no path found
  return {};
}
```

### BFS Optimality

::: note

The BFS is optimal for finding the shortest path, when the weights or distances between the nodes are stable and constant.

:::

## Dijkstra's Algorithm

## A\* Algorithm

### Heuristics