# Djikstra's algorithm

![img.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F11-dijkstra%2Fimg.png) [source](https://en.wikipedia.org/wiki/Edsger_W._Dijkstra)

Dijkstra's algorithm is a graph traversal algorithm similar to BFS, but it takes into account the weight of the edges. It uses a priority list to visit the nodes with the smallest cost first and a set to keep track of the visited nodes. A came_from map can be used to store the parent node of each node to create a pathfinding algorithm.

It uses the producer-consumer pattern, where the producer is the algorithm that adds the nodes to the priority queue and the consumer is the algorithm that removes the nodes from the priority queue and do the work.

The algorithm is greedy and works well with positive weights. It is not optimal for negative weights, for that you should use the Bellman-Ford algorithm.

![img_2.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F11-dijkstra%2Fimg_2.png)

## Data Structure

For the graph, we will use an adjacency list:

```c++
// node registry
// K is the key type for indexing the nodes, usually it can be a string or an integer
// Node is the Node type to store node related data 
unordered_map<K, N> nodes;

// K is the key type of the index
// W is the weight type of the edge, usually it can be an integer or a float
// W can be more robust and become a struct, for example, to store the weight and the edge name
// if W is a struct, remember no implement the < operator for the priority queue work
// unordered_map is used to exploit the O(1) access time and be tolerant to sparse keys
unordered_map<K, unordered_map<K, W>> edges;

// usage
// the cost from node A to node B is 5
edges["A"]["B"] = 5;
// if you want to make it bidirectional set the opposite edge too
// edges["B"]["A"] = 5;
```

For the algoritm to work we will need a priority queue to store the nodes to be visited:

```c++
// priority queue to store the nodes to be visited
// C is the W type and stores the accumulated cost to reach the node
// K is the key type of the index of the node
priority_queue<pair<C, K>> frontier;
```

For the visited nodes we will use a set:

```c++
// set to store the visited nodes
// K is the key type of the index of the node
unordered_set<K> visited;
```

## Algorithm

List of visualizations:

- https://www.cs.usfca.edu/~galles/visualization/Dijkstra.html
- https://visualgo.net/en/sssp
- https://qiao.github.io/PathFinding.js/visual/

![img_1.png](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?preview=true&prefix=dsa%2F11-dijkstra%2Fimg_1.png)

Example of Dijkstra's algorithm in C++ to build a path from the start node to the end node:

```c++
#include <iostream>
#include <unordered_map>
#include <unordered_set>
#include <string>
#include <queue>
using namespace std;

// Dijikstra
struct Node {
  // add your custom data here
  string name;
};

// nodes indexed by id
unordered_map<uint64_t, Node> nodes;
// edges indexed by source id and destination id, the value is the
unordered_map<uint64_t, unordered_map<uint64_t, double>> edges;
// priority queue for the frontier
// this could be declared inside the Dijkstra function
priority_queue<pair<double, uint64_t>> frontier;

// optionally, in order to create a pathfinding, use came_from map to store the parent node
unordered_map<uint64_t, uint64_t> came_from;
// cost to reach the node so far
unordered_map<uint64_t, double> cost_so_far;

void Visit(Node* node){
  // add your custom code here
  cout << node->name << endl;
}

void Dijkstra(uint64_t start_id) {
  cout << "Visiting nodes:" << endl;
  // clear the costs so far
  cost_so_far.clear();
  // boostrap the frontier
  // 0 means the cost to reach the start node is 0
  frontier.emplace(0, start_id);
  cost_so_far[start_id] = 0;
  // while there are nodes to visit
  while (!frontier.empty()) {
    // get the node with the lowest cost
    auto [cost, current_id] = frontier.top();
    frontier.pop();
    // get the node
    Node* current = &nodes[current_id];
    // visit the node
    Visit(current);
    // for each neighbor
    for (const auto& [neighbor_id, edge_cost] : edges[current_id]) {
      // calculate the new cost to reach the neighbor
      double new_cost = cost_so_far[current_id] + edge_cost;
      // if the neighbor is not visited yet or the new cost is less than the previous cost
      if (!cost_so_far.contains(neighbor_id) || new_cost < cost_so_far[neighbor_id]) {
        // update the cost
        cost_so_far[neighbor_id] = new_cost;
        // add the neighbor to the frontier
        frontier.emplace(new_cost, neighbor_id);
        // update the parent node
        came_from[neighbor_id] = current_id;
      }
    }
  }
}

int main() {
  // build the graph
  nodes[0] = {"A"}; // this will be our start
  nodes[1] = {"B"};
  nodes[2] = {"C"};
  nodes[3] = {"D"}; // this will be our end
  // store the edges costs
  edges[0][1] = 1;
  edges[0][2] = 2;
  edges[0][3] = 100; // this is a very expensive edge
  edges[1][3] = 3;
  edges[2][3] = 1;
  // the path from 0 to 3 is A -> C -> D even though the edge A -> D have less steps
  Dijkstra(0);
  // print the path from the end to the start
  cout << "Path:" << endl;
  uint64_t index = 3;
  // prevents infinite loop if the end is unreachable
  if(!came_from.contains(index)) {
    cout << "No path found" << endl;
    return 0;
  }
  while (index != 0) {
    cout << nodes[index].name << endl;
    index = came_from[index];
  }
  cout << nodes[0].name << endl;
  return 0;
}
```