# Catch the Cat

You are in charge of creating 2 agents that will be playing the game of [Catch the Cat](https://llerrah.com/cattrap.htm).

## Game rules

The game is played on a NxN board where N is an odd number that follows the sequence of `1+4*x` with `x` starnig from `1`: `5, 9, 13, 17, 21, 25, 29, 33, 37, 41, 45, ...`. The game starts with a cat in the center of the board, and it starts with some random blocks placed randomly.

The game is played in turns, where each player can move the cat or a catcher. 

### Board

The board position follows `{x, y}` notation.

The center of the board is `{0,0}` and the board is a square with `N` cells on each side.

The board is a pointy top hexagon with the first line aligned to the left. Here goes an example of a 5x5 board indexes:

```
 /  \ /  \ /  \ /  \ /  \
|-2-2|-1-2| 0-2| 1-2| 2-2|
 \  / \  / \  / \  / \  / \
  |-2-1|-1-1| 0-1| 1-1| 2-1|
 /  \ /  \ /  \ /  \ /  \ /
|-2 0|-1 0| 0 0| 1 0| 2 0|
 \  / \  / \  / \  / \  / \
  |-2 1|-1 1| 0 1| 1 1| 2 1|
 /  \ /  \ /  \ /  \ /  \ /
|-2 2|-1 2| 0 2| 1 2| 2 2|
 \  / \  / \  / \  / \  /
```

### Moves

The Cat moves in any of the 6 immediate neighbors, but it cannot move to a blocked cell.

The Catcher moves by blocking a cell. A cell can be blocked only once each turn.

### Win condition

1. If the cat is surrounded by blocked cells in all 6 directions, it cannot move and the catcher wins.
2. If the cat reaches a border cell, it wins.
3. If the cat makes invalid moves, it loses. Invalid moves are:

    - Move to a blocked cell;
    - Move to a cell that is not a neighbor;
    - Stay in the same cell;

4. The catcher makes invalid moves, it loses. Invalid moves are:

    - Block an already blocked cell;
    - Block a cell outside the board;
    - Block a cell where the cat is;

## Competition

All students enrolled in the competition will submit both agents. The agents will play against each other, and the winner will be the one that wins the most games. 

The points will be counted as how many moves each one does;

If Cat Wins:

- CatPoints: SideSize * SideSize/2 - CatMoves - K*CpuCatTime;
- CatcherPoints: CatcherMoves - K*CpuCatcherTime;

If Catcher Wins:

- CatPoints: CatMoves - K*CpuCatTime;
- CatcherPoints: SideSize * SideSize/2 - CatcherMoves - K*CpuCatherTime;

## How to participate:

I will create an automation that will use your agents to play against each other.

1. Place the interface [below](#iagenth) in a file called `IAgent.h` on the root of your repo;
2. Agents are stateless. At every turn, the state of all classes everything will be reset.
3. The classes should be named `Cat` and `Catcher`;
4. The simulator will include `Cat.h` and `Catcher.h`, so you should have at least these two files;
5. Both agents should inherit `IAgent.h` and include `#include "IAgent.h"`; 
6. All `.cpp` and `.h` files should be at the same directory level. Don't use subdirs; 
7. Your submission will be a zip containing only `.h` and `.cpp` files. 
8. Do not submit any file with a `main` function;

The reasoning is: I will create an automation for:

1. Receive your zip and version them for auditing purposes and diagnostics;
2. Create a folder for your user if not created yet;
3. Clear the folder and keep the executable;
4. Unzip the contents of your submission into a folder with your username;
5. Add a `main.cpp` for the simulator;
6. Compile the whole folder into one executable named as your username. Only the last working subimission will be kept;

It will generate `N` executables that will be managed and called via terminal to generate the final report with points;

The report will be generated via another automation that will generate 100 initial states randomly. All agents from all students play against each other.

```
executables = fetchAllExecutables() 
initialstates = generateRandomStates(100);
foreach cat of executables{
  foreach catcher of executables {
    turnIsCat = true;
    foreach state of initialstate {
      while(nat have winner && correct output){
        if(turnIsCat)
          state = cat(state)
        else
          state = catcher(state)
        turnIsCat = !turnIsCat
      }
      generate partial report from current cat and catcher  
    }
  }
}
compose final report of the run
```

### IAgent.h

```cpp title="IAgent.h"
#pragma once
#include <vector>
#include <utility>

// NO NOT CHANGE THIS FILE
struct IAgent {
public:
    /**
     * @brief the agent implementation. the center of the world is {0,0}, top left is {-sideSize/2, -sideSize/2} and the bottom right is {sideSize/2, sideSize/2}.
     *
     * @param world the world as a vector of booleans. true means there is a wall, false means there is no wall. The vector is the linearization of the matrix of the world.
     * @param catPos the position of the cat in the world {x,y} relative to the center of the world.
     * @param sideSize the side size of the world. it will be always a square that follows the sequence of 4*i+1: 5, 9, 13, 17, 21, 25, 29, 33, 37, 41, ...
     *
     * @return the position to move to {x,y}. relative to the center of the world.
     */
    virtual std::pair<int,int> move(const std::vector<bool>& world, std::pair<int,int> catPos, int sideSize ) = 0;
};
```
