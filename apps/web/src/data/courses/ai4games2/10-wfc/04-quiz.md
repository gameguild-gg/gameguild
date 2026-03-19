# Week 10 Quiz — Wave Function Collapse

!!! quiz
{
"title": "Question 1",
"question": "Who created the Wave Function Collapse algorithm, and when was it published?",
"options": [
"Oskar Stålberg in 2018",
"Maxim Gumin in 2016",
"Paul Merrell in 2007",
"Brian Bucklew in 2019"
],
"answers": ["Maxim Gumin in 2016"]
}
!!!

!!! quiz
{
"title": "Question 2",
"question": "In the Constraint Satisfaction Problem (CSP) formulation of WFC, what does the domain of a variable represent?",
"options": [
"The four cardinal directions (N, E, S, W)",
"The weighted probability distribution over the entire grid",
"The set of tile types still valid for that cell",
"The set of adjacent cells in the grid"
],
"answers": ["The set of tile types still valid for that cell"]
}
!!!

!!! quiz
{
"title": "Question 3",
"question": "Why does WFC select the cell with the lowest Shannon entropy for the next collapse?",
"options": [
"Collapsing the most constrained cell first reduces the chance of contradictions and propagates information quickly",
"Low entropy cells are fastest to compute",
"It guarantees no contradictions will occur",
"It produces the most visually appealing output"
],
"answers": ["Collapsing the most constrained cell first reduces the chance of contradictions and propagates information quickly"]
}
!!!

!!! quiz
{
"title": "Question 4",
"question": "Shannon entropy is computed as H = −Σ p(t) × ln(p(t)), where p(t) = w(t) / W. Given tiles with weights w1=40, w2=25, w3=20 and total weight W=85, which description matches this formula?",
"options": [
"Sum of raw weights times their logarithms, without normalization",
"Negative logarithm of the total weight only",
"Negative sum of normalized probabilities times their logarithms",
"Total weight times the logarithm of total weight"
],
"answers": ["Negative sum of normalized probabilities times their logarithms"]
}
!!!

!!! quiz
{
"title": "Question 5",
"question": "What are the three core phases of WFC's main loop, in order?",
"options": [
"Initialize, Generate, Validate",
"Extract patterns, Build adjacency, Fill grid",
"Observation (select cell), Collapse (assign tile), Propagation (enforce constraints)",
"Sample, Place, Verify"
],
"answers": ["Observation (select cell), Collapse (assign tile), Propagation (enforce constraints)"]
}
!!!

!!! quiz
{
"title": "Question 6",
"question": "Which statement correctly distinguishes the Tiled Model from the Overlapping Model?",
"options": [
"The Overlapping Model is always faster than the Tiled Model",
"The Tiled Model automatically infers rules from an image; the Overlapping Model uses manual adjacency rules",
"Both models require manually specifying adjacency rules",
"The Tiled Model uses designer-specified adjacency rules; the Overlapping Model automatically extracts NxN patterns and infers adjacency from a sample image"
],
"answers": ["The Tiled Model uses designer-specified adjacency rules; the Overlapping Model automatically extracts NxN patterns and infers adjacency from a sample image"]
}
!!!

!!! quiz
{
"title": "Question 7",
"question": "In a WFC tileset, if Grass has weight 40, Water has weight 20, Sand has weight 25, and Road has weight 5, what is the normalized probability of selecting Road during a collapse where all four tiles are still possible?",
"options": [
"0.278",
"0.056",
"0.444",
"0.222"
],
"answers": ["0.056"]
}
!!!

!!! quiz
{
"title": "Question 8",
"question": "What causes a contradiction in WFC?",
"options": [
"The entropy heap becomes empty",
"A cell's domain becomes empty — no tile type can legally occupy that cell",
"Two adjacent cells are both collapsed to the same tile",
"The algorithm runs out of memory"
],
"answers": ["A cell's domain becomes empty — no tile type can legally occupy that cell"]
}
!!!

!!! quiz
{
"title": "Question 9",
"question": "What do enabler counts track in WFC's propagation data structure?",
"options": [
"How many times a tile has been collapsed across the entire grid",
"The Shannon entropy of each cell",
"For each tile in a cell and each direction, how many tiles in the neighboring cell are compatible with it",
"The number of cells remaining to be collapsed"
],
"answers": ["For each tile in a cell and each direction, how many tiles in the neighboring cell are compatible with it"]
}
!!!

!!! quiz
{
"title": "Question 10",
"question": "WFC's propagation phase is analogous to which constraint satisfaction technique?",
"options": [
"Min-conflicts local search",
"Forward checking",
"Arc consistency (AC-3 / AC-4)",
"Backjumping"
],
"answers": ["Arc consistency (AC-3 / AC-4)"]
}
!!!

!!! quiz
{
"title": "Question 11",
"question": "In the Overlapping Model with N=3, two patterns P and Q are compatible in direction East when:",
"options": [
"Their entire 3x3 grids are identical",
"The right 2 columns of P equal the left 2 columns of Q",
"Their center pixels match",
"Any single column of P matches any column of Q"
],
"answers": ["The right 2 columns of P equal the left 2 columns of Q"]
}
!!!

!!! quiz
{
"title": "Question 12",
"question": "Which statement about contradiction handling strategies is correct?",
"options": [
"Restart requires saving grid snapshots at each collapse step",
"Backtracking is simpler to implement than restart",
"Restart is guaranteed to find a solution if one exists",
"Backtracking guarantees finding a solution if one exists, but requires more memory for storing grid state snapshots"
],
"answers": ["Backtracking guarantees finding a solution if one exists, but requires more memory for storing grid state snapshots"]
}
!!!

!!! quiz
{
"title": "Question 13",
"question": "In the WFC-Sudoku analogy, what corresponds to a 'naked single' in Sudoku?",
"options": [
"The cell selected by minimum entropy",
"A cell that has not yet been visited",
"A tile that appears only once in the entire grid",
"A cell whose domain has been reduced to exactly one possible tile"
],
"answers": ["A cell whose domain has been reduced to exactly one possible tile"]
}
!!!

!!! quiz
{
"title": "Question 14",
"question": "Which game uses a multi-stage WFC pipeline with separate passes for zone layout, tile filling, and detail decoration?",
"options": [
"Townscaper",
"Caves of Qud",
"Bad North",
"Minecraft"
],
"answers": ["Caves of Qud"]
}
!!!

!!! quiz
{
"title": "Question 15",
"question": "In the tile socket (edge label) system, when are two tiles compatible in the East/West direction?",
"options": [
"When their North labels match",
"When they share at least one common edge label on any side",
"When all four of their edge labels match",
"When Tile A's East label equals Tile B's West label"
],
"answers": ["When Tile A's East label equals Tile B's West label"]
}
!!!

!!! quiz
{
"title": "Question 16",
"question": "What is WFC's fundamental weakness that requires extensions like path constraints?",
"options": [
"It only enforces local constraints, so generated output may have disconnected regions",
"It can only work on rectangular grids",
"It requires neural network training data",
"It cannot handle more than 16 tile types"
],
"answers": ["It only enforces local constraints, so generated output may have disconnected regions"]
}
!!!

!!! quiz
{
"title": "Question 17",
"question": "What advantage does a marching squares/cubes approach give to WFC tileset design?",
"options": [
"It makes the overlapping model unnecessary",
"It automatically assigns optimal tile weights",
"It reduces the number of tiles needed to just 2",
"It generates tiles where all adjacencies are valid by construction, eliminating contradictions between terrain types"
],
"answers": ["It generates tiles where all adjacencies are valid by construction, eliminating contradictions between terrain types"]
}
!!!

!!! quiz
{
"title": "Question 18",
"question": "How does WFC handle the case where multiple cells have the same minimum entropy?",
"options": [
"It collapses all tied cells simultaneously",
"It always picks the top-left cell",
"A tiny random noise value is added to each cell's entropy at initialization to break ties deterministically",
"It picks a random cell uniformly"
],
"answers": ["A tiny random noise value is added to each cell's entropy at initialization to break ties deterministically"]
}
!!!

!!! quiz
{
"title": "Question 19",
"question": "WFC maintains two running sums per cell (sum of weights, and sum of weight\*ln(weight)) for entropy. How does it update entropy when a tile is removed?",
"options": [
"It recomputes the full entropy formula from scratch for all cells",
"It uses a lookup table of pre-computed entropy values for every possible domain subset",
"It skips entropy calculation and uses domain size instead",
"It decrements both running sums by the removed tile's contribution in O(1) per removal"
],
"answers": ["It decrements both running sums by the removed tile's contribution in O(1) per removal"]
}
!!!

!!! quiz
{
"title": "Question 20",
"question": "What is constrained initialization in WFC, and what is it used for?",
"options": [
"Pre-placing certain tiles as already-collapsed cells before the main loop, used for fixed landmarks, entrances, or boundary conditions",
"Setting all cells to a random initial tile before running WFC",
"Running a pre-pass of cellular automata to seed the grid",
"Restricting WFC to only use a subset of the tileset"
],
"answers": ["Pre-placing certain tiles as already-collapsed cells before the main loop, used for fixed landmarks, entrances, or boundary conditions"]
}
!!!
