# Game of Life

You are applying for an internship position at Valvule Corp, and they want to test your abilities to manage states. You were tasked to code the Conway's Game of Life.

The game consists in a $C \times L$ matrix of cells (Columns and Lines), where each cell can be either alive or dead. The game is played in turns, where each turn the state of the cells are updated according to the following rules:

1. Any live cell with fewer than two live neighbours dies, as if by underpopulation.
2. Any live cell with two or three live neighbours lives on to the next generation.
3. Any live cell with more than three live neighbours dies, as if by overpopulation.
4. Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.

![glider](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Fglider.gif)

The map is continuous on every direction, so the cells on the edges have the cells on the opposite edge as neighbors. It is effectively a toroidal surface.

![toroidal](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Ftoroidal.gif)

## Input

The first line of the input are three numbers, C, L and T, the number of columns, lines and turns, respectively. The next L lines are the initial state of the cells, where each line has C characters, either `.` for dead cells or `#` for alive cells.

``` text
5 5 4
.#...
..#..
###..
.....
.....
```

## Output

The output should be the state of the cells after T turns, in the same format as the input.

``` text
.....
..#..
...#.
.###.
.....
```

## References

- [Animated Example](https://playgameoflife.com/)
- [Conway's Game of Life Wiki](https://conwaylife.com/wiki/Conway%27s_Game_of_Life)
- [Hexagonal Game of Life](https://arunarjunakani.github.io/HexagonalGameOfLife/)