# Wave Function Collapse

<details>
<summary>Lecture Notes</summary>

- What is Wave Function Collapse?
- Constraint Satisfaction and Local Similarity
- The Two Models: Tiled vs. Overlapping
- Observation: Minimum Entropy Heuristic
- Propagation: Enforcing Adjacency Constraints
- Handling Contradictions and Backtracking
- Extensions: Non-Local Constraints, 3D, Irregular Grids
- WFC in Production Games

</details>

**Video explanations:** [WFC Demo](https://youtu.be/DOQTr2Xmlz0) | [Martin Donald - Superpositions & Sudoku](https://www.youtube.com/watch?v=qRnUBiTJ66Y) | [Brian Bucklew - Caves of Qud GDC 2019](https://www.youtube.com/watch?v=AdCgi9E90jw) | [Oskar Stålberg - Bad North EPC 2018](https://www.youtube.com/watch?v=0bcZb-SsnrA)

Wave Function Collapse (WFC) is a procedural generation algorithm that fills a grid with tiles such that every pair of adjacent tiles satisfies a set of adjacency rules — and optionally, such that every NxN region of the output looks locally similar to a sample input. The name comes loosely from quantum mechanics: each cell starts as a superposition of all possible tile types, and the algorithm collapses those superpositions one cell at a time until the entire grid is determined.

WFC was created by Maxim Gumin and published in 2016. It is fundamentally a **constraint satisfaction algorithm** dressed up with procedural generation heuristics. Understanding it as constraint satisfaction (rather than as a mysterious "AI") is the key to using it effectively and extending it.

## WFC as Constraint Satisfaction

A constraint satisfaction problem (CSP) consists of:

- A set of **variables**, each with a **domain** of possible values
- A set of **constraints** relating variables to each other
- A goal: assign each variable a value from its domain such that all constraints are satisfied

WFC maps these directly:

| CSP concept | WFC equivalent                                                         |
| ----------- | ---------------------------------------------------------------------- |
| Variable    | Each cell in the output grid                                           |
| Domain      | The set of tile types that are still valid candidates for this cell    |
| Constraint  | Adjacency rule: tile A in cell X is incompatible with tile B in cell Y |
| Solution    | A complete tile assignment where all adjacency rules are satisfied     |

The fundamental algorithm is the same as solving a Sudoku by constraint propagation and search: reduce domains using constraints, and when stuck, guess and propagate.

#### C++: Tile Types and Direction Helpers

Before writing any algorithm code, define the vocabulary used throughout: tile identifiers, directions, and the opposite-direction lookup needed during propagation.

```cpp
#include <array>
#include <vector>
#include <queue>
#include <stack>
#include <random>
#include <cmath>
#include <limits>
#include <cassert>

// Concrete tile types for a simple dungeon generator.
// In a real project these map to sprites/assets.
enum class Tile : int {
    Empty  = 0,
    Wall   = 1,
    Floor  = 2,
    Corner = 3,
    Door   = 4,
    COUNT  // always last — gives the total number of tiles
};
constexpr int TILE_COUNT = static_cast<int>(Tile::COUNT);

// The four cardinal directions, stored as consecutive integers so we can
// index arrays with them directly.
enum class Dir : int { North = 0, East = 1, South = 2, West = 3 };
constexpr int DIR_COUNT = 4;

// Opposite of a direction: North<->South, East<->West.
constexpr Dir opposite(Dir d) {
    return static_cast<Dir>((static_cast<int>(d) + 2) % DIR_COUNT);
}

// Unit offsets for each direction: [row delta, col delta].
constexpr int DY[DIR_COUNT] = { -1,  0, +1,  0 };  // N, E, S, W
constexpr int DX[DIR_COUNT] = {  0, +1,  0, -1 };
```

### Local Similarity Guarantee

WFC specifically imposes a **local similarity** constraint:

> **(C1):** Every NxN patch in the output must appear somewhere in the input.

For the Tiled Model, N=1x2: each pair of adjacent tiles must appear as an adjacent pair in the input specification. For the Overlapping Model, N is typically 3 or 4: every 3x3 region of the output must appear somewhere in the input image.

This is stronger than just saying "tiles can appear adjacent if we allow it" — it means the output is quantifiably similar to the input at a local level, which is why WFC outputs often look like more of the same input.

## The Two Models

### Simple Tiled Model

In the tiled model, the user manually specifies which tiles may appear adjacent to each other in each of the four cardinal directions (Up, Down, Left, Right). This is expressed as a table of allowed pairs:

```
Tile "grass" may appear to the LEFT of tile "grass"
Tile "grass" may appear to the LEFT of tile "forest-edge"
Tile "forest-edge" may appear to the LEFT of tile "forest"
Tile "water" may appear to the LEFT of tile "beach"
...
```

This gives the designer full control over which configurations are legal. It is the model used by games like Bad North, Townscaper, and Caves of Qud.

#### C++: Adjacency Constraints

The adjacency table is indexed by direction and source tile, and stores a bitmask (here `std::vector<bool>`) of which tiles are allowed as neighbours in that direction. `O(1)` lookup in both directions.

```cpp
// adjacency[dir][sourceTile][neighbourTile] = true if the pair is allowed.
// Index: adjacency[ static_cast<int>(Dir::East) ][ static_cast<int>(Tile::Floor) ]
//                 [ static_cast<int>(Tile::Wall) ] == true  means
//   "Floor may have Wall to its East".
using AdjacencyTable = std::array<                     // indexed by Dir
    std::vector<                                       // indexed by source Tile
        std::vector<bool>                              // indexed by neighbour Tile
    >, DIR_COUNT
>;

// Helper to register a symmetric rule A <-> B in direction d/opposite(d).
void addRule(AdjacencyTable& adj, Tile a, Dir d, Tile b) {
    int ia = static_cast<int>(a), ib = static_cast<int>(b);
    int id = static_cast<int>(d),  od = static_cast<int>(opposite(d));
    adj[id][ia][ib] = true;   // a may have b in direction d
    adj[od][ib][ia] = true;   // symmetrically, b may have a in opposite(d)
}

// Build a simple dungeon adjacency table.
AdjacencyTable buildDungeonAdjacency() {
    AdjacencyTable adj;
    // Resize inner vectors to TILE_COUNT × TILE_COUNT, defaulting to false.
    for (auto& byTile : adj)
        byTile.assign(TILE_COUNT, std::vector<bool>(TILE_COUNT, false));

    // Wall borders Empty and other Walls; Floor borders Floor, Door, Corner.
    addRule(adj, Tile::Empty,  Dir::North, Tile::Empty);
    addRule(adj, Tile::Empty,  Dir::North, Tile::Wall);
    addRule(adj, Tile::Wall,   Dir::North, Tile::Wall);
    addRule(adj, Tile::Wall,   Dir::North, Tile::Floor);
    addRule(adj, Tile::Wall,   Dir::North, Tile::Corner);
    addRule(adj, Tile::Floor,  Dir::North, Tile::Floor);
    addRule(adj, Tile::Floor,  Dir::North, Tile::Door);
    addRule(adj, Tile::Corner, Dir::North, Tile::Wall);
    addRule(adj, Tile::Door,   Dir::North, Tile::Floor);
    // (Repeat addRule calls for East/South/West as needed.)
    return adj;
}
```

**Symmetry shortcuts:** In practice, tilesets often use symmetry types to reduce the number of adjacency pairs that must be specified. If a tile is rotationally symmetric (e.g., an open square), all four rotations share the same adjacency data. Gumin's original implementation uses dihedral group D4 representations.

**Tile weights:** Each tile type can be assigned a frequency weight $w_i$. Higher-weight tiles appear more frequently in the output. The probability of choosing tile $i$ from a set of candidates is:

$$P(i) = \frac{w_i}{\sum_j w_j}$$

#### C++: Frequency Weights

```cpp
// One weight per tile type.  Higher weight = appears more often.
// Weights do NOT need to sum to 1; the algorithm normalises on the fly.
using WeightTable = std::array<float, TILE_COUNT>;

constexpr WeightTable DUNGEON_WEIGHTS = {
    0.5f,  // Empty  — rare; mostly surrounded by walls
    4.0f,  // Wall   — most common tile
    6.0f,  // Floor  — generous floor space
    2.0f,  // Corner — moderately frequent at room edges
    1.0f,  // Door   — scarce
};
```

Adjusting weights allows designers to say "forest tiles should appear 3× more often than cliff tiles" without changing the adjacency constraints.

### Overlapping Model

In the overlapping model, adjacency rules are **automatically inferred** from an example input image. The algorithm:

1. Extracts every NxN tile-sized patch from the input (with wrap-around)
2. Records which patches appear adjacent to each other (with 1-pixel offset in each direction)
3. These recorded adjacencies become the constraint table
4. The frequency of each patch in the input becomes its weight

This allows artists to "paint" a small example and have WFC learn the rules automatically. The output will contain only patches that appeared in the input, so the generation looks continuously similar to the source material.

The cost is that the constraint table becomes much larger (potentially thousands of patch types vs. tens of tile types), and the algorithm is correspondingly slower.

## The Core Algorithm

WFC's main loop repeats until either the entire grid is filled or a contradiction is reached:

```
while (there exist uncollapsed cells):
    cell = selectCellWithMinimumEntropy()
    tile = sampleFromDomain(cell)
    collapseCell(cell, tile)
    propagate()
```

#### C++: Cell Structure and Grid Initialisation

The `Cell` struct holds the full state needed across all three phases (observation, collapse, propagation). Build it up piece by piece:

```cpp
struct Cell {
    // ---- Domain ----
    std::vector<bool> possible;   // possible[t] = true  <=>  tile t is still valid
    int collapsedTile = -1;       // -1 means not yet collapsed

    // ---- Incremental entropy bookkeeping ----
    // Instead of recomputing H from scratch each time a tile is removed,
    // maintain running sums and update them in O(1) per removal.
    float weightSum    = 0.0f;    // sum of weights of remaining tiles
    float weightLogSum = 0.0f;    // sum of w_i * log2(w_i) for remaining tiles
    float noiseOffset  = 0.0f;    // tiny random value to break entropy ties

    // ---- Enabler counts (used during propagation) ----
    // enablers[dir][tile] = number of tiles still present in the neighbour in
    //   direction <dir> that are compatible with <tile> here.
    // When this drops to zero, <tile> can no longer exist in this cell.
    std::array<std::vector<int>, DIR_COUNT> enablers;

    bool isCollapsed()  const { return collapsedTile != -1; }
    int  domainSize()   const { return static_cast<int>(std::count(possible.begin(), possible.end(), true)); }

    // Shannon entropy (weighted), maintained incrementally.
    float entropy() const {
        if (weightSum <= 0.0f) return 0.0f;
        return std::log2f(weightSum) - weightLogSum / weightSum + noiseOffset;
    }
};

// Initialise a width×height grid: every cell starts with all tiles possible.
std::vector<std::vector<Cell>> initGrid(
        int width, int height,
        const WeightTable&    weights,
        const AdjacencyTable& adj,
        std::mt19937& rng) {

    std::uniform_real_distribution<float> jitter(0.0f, 1e-4f);
    std::vector<std::vector<Cell>> grid(height, std::vector<Cell>(width));

    for (int y = 0; y < height; ++y) {
        for (int x = 0; x < width; ++x) {
            Cell& c = grid[y][x];
            c.possible.assign(TILE_COUNT, true);
            c.noiseOffset = jitter(rng);

            // Accumulate weight sums for all tiles.
            for (int t = 0; t < TILE_COUNT; ++t) {
                c.weightSum    += weights[t];
                c.weightLogSum += weights[t] * std::log2f(weights[t]);
            }

            // Initialise enabler counts: how many neighbours (in each dir) could
            // legally provide each tile?  At start, all neighbours are uncollapsed
            // so the count is simply the number of tiles compatible in that dir.
            for (int d = 0; d < DIR_COUNT; ++d) {
                c.enablers[d].resize(TILE_COUNT, 0);
                for (int t = 0; t < TILE_COUNT; ++t)
                    for (int n = 0; n < TILE_COUNT; ++n)
                        if (adj[d][t][n]) c.enablers[d][t]++;
            }
        }
    }
    return grid;
}
```

### Phase 1: Observation — Selecting Which Cell to Collapse

We want to choose the cell that is **most constrained** — the one with the fewest valid tile options remaining. This is the **minimum entropy heuristic**: choose the cell whose tile probability distribution has the lowest [Shannon entropy](<https://en.wikipedia.org/wiki/Entropy_(information_theory)>).

Shannon entropy for a discrete distribution:

$$H = -\sum_{i} p_i \log_2 p_i$$

For WFC with weighted tiles, if the remaining candidates in a cell have weights $w_1, w_2, \ldots, w_k$ and the total weight $W = \sum w_i$:

$$H = \log_2(W) - \frac{\sum_i w_i \log_2(w_i)}{W}$$

This can be maintained incrementally: when a tile is removed from a cell's domain, subtract its contribution to both the weight sum and the weight-log-weight sum.

**Tie-breaking:** Multiple cells may have the same minimum entropy. Add a tiny noise term $\epsilon$ to each cell's entropy at initialization to break ties randomly without re-rolling the RNG at every step.

**Why minimum entropy?** Choosing the most constrained cell first reduces the chance of getting into a contradiction. If you leave a heavily-constrained cell for later, it might become impossible to fill as its neighbors fill in. Choosing it early means its constraints propagate sooner, pruning bad configurations before they spread.

#### C++: Minimum Entropy Selection with a Priority Queue

A `std::priority_queue` (min-heap by default when combined with `std::greater`) gives `O(log n)` pop and `O(log n)` push. Because cell states change during propagation, stale entries (cells that were later collapsed or reduced by propagation) are left in the heap and discarded on pop.

```cpp
// Each heap entry stores (entropy_value, row, col).
// std::greater makes this a min-heap (lowest entropy pops first).
using HeapEntry = std::tuple<float, int, int>;
using EntropyHeap = std::priority_queue<HeapEntry,
                                        std::vector<HeapEntry>,
                                        std::greater<HeapEntry>>;

// Seed the heap with initial cell entropies (including the jitter offset).
EntropyHeap buildHeap(const std::vector<std::vector<Cell>>& grid,
                      int height, int width) {
    EntropyHeap heap;
    for (int y = 0; y < height; ++y)
        for (int x = 0; x < width; ++x)
            heap.push({ grid[y][x].entropy(), y, x });
    return heap;
}

// Pop until we find an uncollapsed cell with more than one option.
// Returns {-1,-1} when every cell has been collapsed.
std::pair<int,int> selectMinEntropy(
        std::vector<std::vector<Cell>>& grid, EntropyHeap& heap) {
    while (!heap.empty()) {
        auto [e, y, x] = heap.top();
        heap.pop();
        Cell& c = grid[y][x];
        // Skip entries that are stale (cell already collapsed or entropy changed).
        if (c.isCollapsed()) continue;
        if (c.domainSize() <= 1) continue;  // will be handled by propagation
        // Re-check: heap entry may be stale if entropy decreased since insertion.
        if (std::abs(e - c.entropy()) > 1e-3f) {
            heap.push({ c.entropy(), y, x }); // re-insert with updated entropy
            continue;
        }
        return { y, x };
    }
    return { -1, -1 };  // all cells collapsed
}
```

### Phase 2: Collapse — Assigning a Tile

Once a cell is selected, pick one tile from its remaining domain using a weighted random draw (weights proportional to the frequency hints). Remove all other tiles from the cell's domain. Mark the cell as collapsed.

#### C++: Weighted Random Tile Selection

```cpp
// Perform a weighted reservoir sample over the surviving tiles in a cell.
// Runs in O(TILE_COUNT) — no pre-sorting needed.
int collapseCell(Cell& cell, const WeightTable& weights, std::mt19937& rng) {
    assert(!cell.isCollapsed());

    // Draw a uniform random value in [0, weightSum).
    std::uniform_real_distribution<float> dist(0.0f, cell.weightSum);
    float r = dist(rng);

    float cumulative = 0.0f;
    for (int t = 0; t < TILE_COUNT; ++t) {
        if (!cell.possible[t]) continue;
        cumulative += weights[t];
        if (cumulative > r) {
            cell.collapsedTile = t;
            return t;
        }
    }
    // Floating-point rounding safety: return the last surviving tile.
    for (int t = TILE_COUNT - 1; t >= 0; --t)
        if (cell.possible[t]) { cell.collapsedTile = t; return t; }

    return -1; // contradiction — caller must handle
}
```

### Phase 3: Propagation — Enforcing Constraints

After collapsing a cell, the new information must be propagated to neighboring cells. Any tile type that is no longer compatible with the collapsed cell must be removed from neighboring cells' domains. Those removals may themselves trigger further removals, cascading through the grid.

#### Enabler Counts

The most efficient propagation data structure is an **enabler count table**. For each cell $c$, for each candidate tile $t$ in $c$'s domain, and for each direction $d \in \{N, S, E, W\}$, maintain:

$$\text{enablers}_{c,t,d} = \left|\{t' \in \text{domain}(\text{neighbor}(c, d)) : (t, t', d) \text{ is allowed}\}\right|$$

This is the number of tiles in the neighbor in direction $d$ that are compatible with tile $t$ in cell $c$.

When a tile $t'$ is removed from a cell $c'$, for each direction $d$, for each tile $t$ that $t'$ was enabling in the neighbor $c$ in direction opposite $d$:

1. Decrement `enablers[c][t][opposite(d)]` by 1
2. If it reaches 0 and tile $t$ hasn't been removed from $c$ yet:
   - Remove $t$ from $c$'s domain
   - Check for contradiction (if domain is now empty)
   - Add $(c, t)$ to the propagation queue

This runs in $O(|removed tiles| \times |adjacency rules per tile|)$ per propagation step and avoids redundant constraint checks.

#### C++: Enabler-Count Propagation

The queue holds `(row, col, tile)` triples for every tile that has just been removed. We drain the queue to completion before the next collapse step.

```cpp
struct Removal { int y, x, tile; };

// Remove tile t from cell (y,x).
// Updates incremental entropy bookkeeping, pushes to the removal queue.
// Returns false if the cell becomes empty (contradiction).
bool removeTile(std::vector<std::vector<Cell>>& grid,
                const WeightTable& weights,
                std::queue<Removal>& q,
                int y, int x, int t) {
    Cell& c = grid[y][x];
    if (!c.possible[t]) return true;   // already removed, nothing to do

    c.possible[t]    = false;
    c.weightSum     -= weights[t];
    c.weightLogSum  -= weights[t] * std::log2f(weights[t]);

    if (c.weightSum <= 0.0f) return false;  // CONTRADICTION
    q.push({ y, x, t });
    return true;
}

// Propagate all queued removals across the grid via arc consistency.
// Returns false if a contradiction is found.
bool propagate(std::vector<std::vector<Cell>>& grid,
               const WeightTable&    weights,
               const AdjacencyTable& adj,
               int height, int width,
               std::queue<Removal>&  q) {
    while (!q.empty()) {
        auto [cy, cx, removedTile] = q.front();
        q.pop();

        for (int d = 0; d < DIR_COUNT; ++d) {
            int ny = cy + DY[d];
            int nx = cx + DX[d];
            if (ny < 0 || ny >= height || nx < 0 || nx >= width) continue;

            Cell& nb  = grid[ny][nx];
            int   opp = static_cast<int>(opposite(static_cast<Dir>(d)));

            // For every tile t that removedTile was enabling in nb from direction opp:
            for (int t = 0; t < TILE_COUNT; ++t) {
                // adj[d][removedTile][t]: removedTile at (cy,cx) allowed t at (ny,nx)
                // i.e. removedTile was one of nb's enablers in direction opp for tile t.
                if (!adj[d][removedTile][t]) continue;
                if (!nb.possible[t])         continue; // already gone

                nb.enablers[opp][t]--;
                if (nb.enablers[opp][t] == 0) {
                    // No compatible tile remains in the (cy,cx) direction → remove t.
                    if (!removeTile(grid, weights, q, ny, nx, t))
                        return false; // contradiction
                }
            }
        }
    }
    return true; // all constraints satisfied
}
```

## Handling Contradictions

A contradiction occurs when a cell's domain becomes empty — no tile type can be placed there without violating some adjacency constraint. This happens when:

1. The tileset is "difficult" — certain configurations have no legal completion
2. The random collapse choices have cornered the algorithm
3. The output dimensions are too large relative to the input sample

### Restart Strategy

The simplest strategy: when a contradiction is detected, wipe the entire grid and start over. Track the restart count. If restarts are too frequent, the tileset or configuration likely has a fundamental issue.

#### C++: Restart Strategy

```cpp
// Sentinel returned when generation fails or a contradiction is detected.
struct Contradiction {};

// Attempt a single WFC run. Throws Contradiction on failure.
std::vector<std::vector<Cell>> runOnce(
        int width, int height,
        const WeightTable&    weights,
        const AdjacencyTable& adj,
        std::mt19937& rng) {

    auto grid = initGrid(width, height, weights, adj, rng);
    auto heap = buildHeap(grid, height, width);
    std::queue<Removal> q;

    while (true) {
        auto [y, x] = selectMinEntropy(grid, heap);
        if (y == -1) break;  // every cell is collapsed — done!

        int chosen = collapseCell(grid[y][x], weights, rng);
        if (chosen == -1) throw Contradiction{};

        // Remove all other tiles from this cell and propagate.
        for (int t = 0; t < TILE_COUNT; ++t)
            if (t != chosen && grid[y][x].possible[t])
                if (!removeTile(grid, weights, q, y, x, t)) throw Contradiction{};

        if (!propagate(grid, weights, adj, height, width, q)) throw Contradiction{};

        // Re-insert neighbours with updated entropy estimates.
        for (int d = 0; d < DIR_COUNT; ++d) {
            int ny = y + DY[d], nx = x + DX[d];
            if (ny >= 0 && ny < height && nx >= 0 && nx < width)
                heap.push({ grid[ny][nx].entropy(), ny, nx });
        }
    }
    return grid;
}

// Outer restart loop.
std::vector<std::vector<Cell>> wfcWithRestart(
        int width, int height,
        const WeightTable&    weights,
        const AdjacencyTable& adj,
        std::mt19937& rng,
        int maxAttempts = 100) {

    for (int attempt = 0; attempt < maxAttempts; ++attempt) {
        try {
            return runOnce(width, height, weights, adj, rng);
        } catch (const Contradiction&) {
            // Wipe and retry with the same rng (continues the sequence).
        }
    }
    throw std::runtime_error("WFC failed after " + std::to_string(maxAttempts) + " attempts");
}
```

Most WFC implementations default to restart. The original mxgmn implementation does this, and in practice restarts are rare for well-designed tilesets.

### Backtracking Strategy

A more robust approach: when a contradiction is reached, roll back to the most recent collapse decision and try a different tile. This eliminates the "luck" aspect of WFC at the cost of more memory (storing snapshots) and potentially more computation.

DeBroglie's library implements full backtracking, enabling WFC to solve arbitrarily constrained generation problems that would fail with restart-only approaches.

#### C++: Stack-Based Backtracking

```cpp
// A snapshot captures everything needed to undo one collapse step.
struct Snapshot {
    std::vector<std::vector<Cell>> grid;     // full deep copy
    int y, x;                                // cell that was collapsed
    std::vector<int> remainingChoices;       // tiles not yet tried at this cell
};

// WFC with chronological backtracking.
// Guarantees a solution if one exists; may explore many paths for hard tilesets.
std::vector<std::vector<Cell>> wfcWithBacktracking(
        int width, int height,
        const WeightTable&    weights,
        const AdjacencyTable& adj,
        std::mt19937& rng) {

    std::stack<Snapshot> history;
    auto grid = initGrid(width, height, weights, adj, rng);
    auto heap = buildHeap(grid, height, width);
    std::queue<Removal> q;

    while (true) {
        auto [y, x] = selectMinEntropy(grid, heap);

        if (y == -1) return grid;   // fully collapsed — success!

        // Build the list of candidate tiles in random order.
        std::vector<int> choices;
        for (int t = 0; t < TILE_COUNT; ++t)
            if (grid[y][x].possible[t]) choices.push_back(t);
        std::shuffle(choices.begin(), choices.end(), rng);

        int chosen = choices.back();
        choices.pop_back();

        // Save a snapshot before committing this collapse.
        history.push({ grid, y, x, choices });

        // Apply the collapse.
        for (int t = 0; t < TILE_COUNT; ++t)
            if (t != chosen && grid[y][x].possible[t])
                removeTile(grid, weights, q, y, x, t);
        grid[y][x].collapsedTile = chosen;

        if (!propagate(grid, weights, adj, height, width, q)) {
            // Contradiction — backtrack until we find a snapshot with choices left.
            while (!history.empty()) {
                Snapshot& snap = history.top();
                if (!snap.remainingChoices.empty()) {
                    // Restore grid state and try the next untried tile.
                    grid = snap.grid;
                    heap = buildHeap(grid, height, width);
                    int nextChoice = snap.remainingChoices.back();
                    snap.remainingChoices.pop_back();

                    while (!q.empty()) q.pop(); // clear stale removals
                    for (int t = 0; t < TILE_COUNT; ++t)
                        if (t != nextChoice && grid[snap.y][snap.x].possible[t])
                            removeTile(grid, weights, q, snap.y, snap.x, t);
                    grid[snap.y][snap.x].collapsedTile = nextChoice;

                    if (propagate(grid, weights, adj, height, width, q)) break;
                }
                history.pop(); // this snapshot is exhausted too
            }
            if (history.empty())
                throw std::runtime_error("No solution exists for this tileset/size");
        }
    }
}
```

| Strategy        | Restart                    | Backtracking                            |
| --------------- | -------------------------- | --------------------------------------- |
| Cost            | Low (start over)           | Higher (save/restore grid state)        |
| Completeness    | Not guaranteed             | Guaranteed (if a solution exists)       |
| Speed (typical) | Fast (rare contradictions) | Slower per run but fewer total attempts |
| Use case        | Games with simple tilesets | Complex puzzles, high-constraint levels |

## Extensions and Variants

### Non-Local Constraints

WFC's fundamental weakness: it only enforces local constraints. A generated dungeon may have rooms scattered around with no connecting paths. The path constraint (implemented in DeBroglie) requires that a subset of tiles must form a single connected component, ensuring all areas are reachable. This is one of the most practically important extensions for game level generation.

### Constrained Initialization

Fix certain tiles before generation begins. WFC treats these pre-placed tiles as already-collapsed cells and propagates their constraints before the main loop runs. Uses:

- Pre-determined entrance/exit locations for levels
- Forced landmarks (a boss room must be at the top of the dungeon)
- Human-authored areas that WFC fills around
- Boundary conditions (the edge of the map must always be wall tiles)

### Higher Dimensions

WFC extends naturally to 3D voxel generation simply by adding two more directions (Up and Down) to the adjacency rules. Paul Merrell's original Model Synthesis was already 3D. Townscaper uses WFC on irregular polygon grids on a sphere surface — demonstrating that WFC works on any topology, not just regular grids.

### Infinite Generation

Standard WFC generates a fixed-size grid upfront. Marian Kleineberg's Infinite City Generator implements an "online" variant that generates tiles just-in-time as the camera moves, using the [modifying in blocks](https://www.boristhebrave.com/2021/11/08/infinite-modifying-in-blocks/) technique by Boris the Brave to maintain consistency at block boundaries.

### Multi-Pass Generation

Used in Caves of Qud: run WFC multiple times on the same grid with different settings for different zones. First pass: large-scale structure (biome zones). Second pass: fill each zone with zone-appropriate tiles. Third pass: detail decoration. This overcomes WFC's local-only nature for large-scale structure design.

## Design Considerations for Game Developers

### Tileset Design

Good tileset design is the most important factor in WFC output quality. Key principles:

**Marching Cubes Strategy:** Design tiles by thinking about what happens at each corner/vertex, not the center. If tiles align based on corner states, they always slot together, eliminating illegal adjacencies. This scales well: 3D marching cubes gives complete tilesets with relatively few tiles.

**Start minimal:** Begin with the fewest tiles that produce interesting output. Add complexity only when you need specific patterns. A 4-tile dungeon set (empty, wall, corner, t-junction) can produce great dungeons before needing expansion.

**Wang tiles:** Design edges explicitly as "edge types" (e.g., red edge, blue edge, open edge). Two tiles can be adjacent if and only if their touching edges are the same type. This gives a principled adjacency system with clear design rules.

### Failure Modes

| Problem                          | Cause                                                       | Fix                                                                |
| -------------------------------- | ----------------------------------------------------------- | ------------------------------------------------------------------ |
| Frequent contradictions          | Under-constrained tileset with many dead-end configurations | Add missing transitions, reduce output size, enable backtracking   |
| Homogeneous output               | Tileset too simple / weights too uniform                    | Add biome variation, weight adjustments per zone, multi-pass       |
| Disconnected areas               | WFC has no path constraint                                  | Add non-local path constraint or use constructive post-processing  |
| Output looks too much like input | Overlapping model pattern size N too large                  | Decrease N, or switch to a tiled model with hand-crafted rules     |
| Very slow generation             | Too many unique tiles / large grid size                     | Use fast-wfc propagation, reduce pattern count, add spatial limits |

---

## The Complete Algorithm

Putting all the pieces together into a self-contained C++ program:

```cpp
// ── Complete Wave Function Collapse implementation ─────────────────────────
//
// Compile with:  g++ -std=c++17 -O2 wfc.cpp -o wfc
//
// This file is intentionally single-header for readability.
// In a game engine you would split into wfc.h / wfc.cpp.

#include <array>
#include <vector>
#include <queue>
#include <stack>
#include <random>
#include <cmath>
#include <cassert>
#include <stdexcept>
#include <iostream>

// ── 1. Tile & direction types ──────────────────────────────────────────────

enum class Tile : int { Empty=0, Wall, Floor, Corner, Door, COUNT };
constexpr int TILE_COUNT = static_cast<int>(Tile::COUNT);

enum class Dir  : int { North=0, East, South, West };
constexpr int DIR_COUNT = 4;
constexpr int DY[DIR_COUNT] = {-1, 0,+1, 0};
constexpr int DX[DIR_COUNT] = { 0,+1, 0,-1};
constexpr Dir opposite(Dir d) { return static_cast<Dir>((static_cast<int>(d)+2)%DIR_COUNT); }

using WeightTable   = std::array<float, TILE_COUNT>;
using AdjacencyTable = std::array<std::vector<std::vector<bool>>, DIR_COUNT>;

void addRule(AdjacencyTable& adj, Tile a, Dir d, Tile b) {
    adj[static_cast<int>(d)][static_cast<int>(a)][static_cast<int>(b)] = true;
    adj[static_cast<int>(opposite(d))][static_cast<int>(b)][static_cast<int>(a)] = true;
}

// ── 2. Cell structure ──────────────────────────────────────────────────────

struct Cell {
    std::vector<bool>              possible;   // remaining tile candidates
    int                            collapsedTile = -1;
    float                          weightSum     = 0.0f;
    float                          weightLogSum  = 0.0f;
    float                          noiseOffset   = 0.0f;
    std::array<std::vector<int>, DIR_COUNT> enablers;

    bool  isCollapsed() const { return collapsedTile != -1; }
    int   domainSize()  const { return (int)std::count(possible.begin(),possible.end(),true); }
    float entropy()     const {
        if (weightSum <= 0.0f) return 0.0f;
        return std::log2f(weightSum) - weightLogSum / weightSum + noiseOffset;
    }
};

// ── 3. Grid initialisation ─────────────────────────────────────────────────

struct Removal { int y, x, tile; };

std::vector<std::vector<Cell>> initGrid(
        int w, int h,
        const WeightTable&    weights,
        const AdjacencyTable& adj,
        std::mt19937& rng) {
    std::uniform_real_distribution<float> jitter(0.0f, 1e-4f);
    std::vector<std::vector<Cell>> grid(h, std::vector<Cell>(w));
    for (int y = 0; y < h; ++y) for (int x = 0; x < w; ++x) {
        Cell& c = grid[y][x];
        c.possible.assign(TILE_COUNT, true);
        c.noiseOffset = jitter(rng);
        for (int t = 0; t < TILE_COUNT; ++t) {
            c.weightSum    += weights[t];
            c.weightLogSum += weights[t] * std::log2f(weights[t]);
        }
        for (int d = 0; d < DIR_COUNT; ++d) {
            c.enablers[d].resize(TILE_COUNT, 0);
            for (int t = 0; t < TILE_COUNT; ++t)
                for (int n = 0; n < TILE_COUNT; ++n)
                    if (adj[d][t][n]) c.enablers[d][t]++;
        }
    }
    return grid;
}

// ── 4. Tile removal & propagation ──────────────────────────────────────────

bool removeTile(std::vector<std::vector<Cell>>& grid,
                const WeightTable& weights,
                std::queue<Removal>& q, int y, int x, int t) {
    Cell& c = grid[y][x];
    if (!c.possible[t]) return true;
    c.possible[t]    = false;
    c.weightSum     -= weights[t];
    c.weightLogSum  -= weights[t] * std::log2f(weights[t]);
    if (c.weightSum <= 0.0f) return false;  // contradiction
    q.push({y, x, t});
    return true;
}

bool propagate(std::vector<std::vector<Cell>>& grid,
               const WeightTable&    weights,
               const AdjacencyTable& adj,
               int h, int w,
               std::queue<Removal>& q) {
    while (!q.empty()) {
        auto [cy, cx, rem] = q.front(); q.pop();
        for (int d = 0; d < DIR_COUNT; ++d) {
            int ny = cy+DY[d], nx = cx+DX[d];
            if (ny<0||ny>=h||nx<0||nx>=w) continue;
            Cell& nb  = grid[ny][nx];
            int   opp = static_cast<int>(opposite(static_cast<Dir>(d)));
            for (int t = 0; t < TILE_COUNT; ++t) {
                if (!nb.possible[t] || !adj[d][rem][t]) continue;
                if (--nb.enablers[opp][t] == 0)
                    if (!removeTile(grid, weights, q, ny, nx, t)) return false;
            }
        }
    }
    return true;
}

// ── 5. Observation & collapse ──────────────────────────────────────────────

using HeapEntry = std::tuple<float,int,int>;
using EntropyHeap = std::priority_queue<HeapEntry,
                                        std::vector<HeapEntry>,
                                        std::greater<HeapEntry>>;

EntropyHeap buildHeap(const std::vector<std::vector<Cell>>& grid, int h, int w) {
    EntropyHeap heap;
    for (int y = 0; y < h; ++y)
        for (int x = 0; x < w; ++x)
            heap.push({grid[y][x].entropy(), y, x});
    return heap;
}

std::pair<int,int> selectMinEntropy(
        std::vector<std::vector<Cell>>& grid, EntropyHeap& heap) {
    while (!heap.empty()) {
        auto [e, y, x] = heap.top(); heap.pop();
        Cell& c = grid[y][x];
        if (c.isCollapsed() || c.domainSize() <= 1) continue;
        if (std::abs(e - c.entropy()) > 1e-3f) {
            heap.push({c.entropy(), y, x}); continue;
        }
        return {y, x};
    }
    return {-1, -1};
}

int collapseCell(Cell& c, const WeightTable& weights, std::mt19937& rng) {
    std::uniform_real_distribution<float> dist(0.0f, c.weightSum);
    float r = dist(rng), cum = 0.0f;
    for (int t = 0; t < TILE_COUNT; ++t) {
        if (!c.possible[t]) continue;
        cum += weights[t];
        if (cum > r) { c.collapsedTile = t; return t; }
    }
    for (int t = TILE_COUNT-1; t >= 0; --t)
        if (c.possible[t]) { c.collapsedTile = t; return t; }
    return -1;
}

// ── 6. Main WFC entry point (restart strategy) ────────────────────────────

struct Contradiction {};

std::vector<std::vector<Cell>> wfc(
        int width, int height,
        const WeightTable&    weights,
        const AdjacencyTable& adj,
        std::mt19937& rng,
        int maxRestarts = 100) {

    for (int attempt = 0; attempt < maxRestarts; ++attempt) {
        try {
            auto grid = initGrid(width, height, weights, adj, rng);
            auto heap = buildHeap(grid, height, width);
            std::queue<Removal> q;

            while (true) {
                auto [y, x] = selectMinEntropy(grid, heap);
                if (y == -1) return grid;  // success

                int chosen = collapseCell(grid[y][x], weights, rng);
                if (chosen == -1) throw Contradiction{};

                for (int t = 0; t < TILE_COUNT; ++t)
                    if (t != chosen && grid[y][x].possible[t])
                        if (!removeTile(grid, weights, q, y, x, t)) throw Contradiction{};

                if (!propagate(grid, weights, adj, height, width, q)) throw Contradiction{};

                for (int d = 0; d < DIR_COUNT; ++d) {
                    int ny = y+DY[d], nx = x+DX[d];
                    if (ny>=0 && ny<height && nx>=0 && nx<width)
                        heap.push({grid[ny][nx].entropy(), ny, nx});
                }
            }
        } catch (const Contradiction&) { /* restart */ }
    }
    throw std::runtime_error("WFC could not find a solution");
}

// ── 7. Usage example ──────────────────────────────────────────────────────

int main() {
    // Build adjacency rules for the dungeon tileset.
    AdjacencyTable adj;
    for (auto& d : adj) d.assign(TILE_COUNT, std::vector<bool>(TILE_COUNT, false));
    addRule(adj, Tile::Empty,  Dir::North, Tile::Empty);
    addRule(adj, Tile::Empty,  Dir::North, Tile::Wall);
    addRule(adj, Tile::Wall,   Dir::North, Tile::Wall);
    addRule(adj, Tile::Wall,   Dir::North, Tile::Floor);
    addRule(adj, Tile::Wall,   Dir::North, Tile::Corner);
    addRule(adj, Tile::Floor,  Dir::North, Tile::Floor);
    addRule(adj, Tile::Floor,  Dir::North, Tile::Door);
    addRule(adj, Tile::Corner, Dir::North, Tile::Wall);
    addRule(adj, Tile::Door,   Dir::North, Tile::Floor);
    // (add more rules for East/South/West)

    constexpr WeightTable weights = {0.5f, 4.0f, 6.0f, 2.0f, 1.0f};
    std::mt19937 rng(42);

    constexpr int W = 20, H = 20;
    auto grid = wfc(W, H, weights, adj, rng);

    // Render to ASCII for quick verification.
    const char symbols[] = { '.', '#', ' ', '+', 'D' };
    for (int y = 0; y < H; ++y) {
        for (int x = 0; x < W; ++x)
            std::cout << symbols[grid[y][x].collapsedTile];
        std::cout << '\n';
    }
}
```

---

## WFC in Commercial Games

### Bad North (2018) — Oskar Stålberg

Uses a tiled model on 3D triangular grids to generate small Nordic islands. The key innovation is a **navigability heuristic** that selects tiles not just by minimum entropy but by whether choosing that tile ensures the resulting island remains navigable at each generation step. This prevents the algorithm from generating islands with unreachable sections without needing a post-hoc path check.

### Townscaper (2021) — Oskar Stålberg

WFC on irregular polygon grids projected onto a sphere, combined with a marching cubes tileset for building placement. The tiles use edge-based compatibility (Wang-tile-like design). Townscaper became famous for demonstrating that WFC could underpin a genuinely satisfying player-facing tool, not just a backend procedural system.

### Caves of Qud — Freehold Games (Brian Bucklew)

Multi-stage WFC pipeline: large-scale zone layout → zone-specific tile filling → detail decoration → connectivity enforcement. The team found that plain WFC produced boring, homogeneous levels, and only by combining it with zone-specific templates and post-generation pathing corrections did it produce levels indistinguishable from hand-crafted content.

### Rodina — Brendan Anthony

Uses WFC for surface wall decoration on procedurally generated spaceships, generating tile patterns for interior wall surfaces that maintain consistency across irregular surfaces.

### DeepMind Research — Open-Ended Learning

DeepMind used WFC to generate training arenas for reinforcement learning agents, demonstrating WFC's applicability outside game development. The local-similarity guarantee ensures generated environments share structural properties with human-designed training environments.

---

## Relationship to Other Algorithms

| Algorithm                         | Relationship to WFC                                                                                              |
| --------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| **Constraint Propagation (AC-4)** | WFC's propagation phase is essentially AC-4: arc consistency enforcement on pairwise constraints                 |
| **Texture Synthesis**             | Efros & Leung (1999) generated similar textures by non-parametric sampling; WFC adds constraint-driven selection |
| **Model Synthesis**               | Merrell (2007) published the direct predecessor; WFC adds the entropy heuristic and overlapping model            |
| **Markov Random Fields**          | WFC's probability model is a special case of an MRF with binary pairwise potentials                              |
| **Sudoku Solvers**                | Isomorphic: a Sudoku is a WFC problem on a 9×9 grid with row/column/box constraints instead of adjacency         |
| **Cellular Automata**             | WFC can simulate (d-1)-dimensional CA by treating one dimension as time — noted by Gumin in the README           |
