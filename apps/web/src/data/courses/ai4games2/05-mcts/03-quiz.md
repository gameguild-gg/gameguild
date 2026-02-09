# Week 05 Quiz - Monte Carlo Tree Search

---

!!! quiz
{
"title": "Question 1",
"question": "What are the four phases of Monte Carlo Tree Search, in order?",
"options": [
"Selection, Expansion, Simulation, Backpropagation",
"Expansion, Selection, Evaluation, Pruning",
"Generation, Evaluation, Selection, Propagation",
"Search, Expand, Rollout, Update"
],
"answers": ["Selection, Expansion, Simulation, Backpropagation"]
}
!!!

---

!!! quiz
{
"title": "Question 2",
"question": "In the UCB1 formula, what does the exploration term C√(ln N / nᵢ) encourage?",
"options": [
"Visiting nodes with the highest win rate",
"Visiting nodes that have been explored less frequently",
"Pruning nodes with low win rates",
"Expanding all children of a node simultaneously"
],
"answers": ["Visiting nodes that have been explored less frequently"]
}
!!!

---

!!! quiz
{
"title": "Question 3",
"question": "Why is MCTS preferred over Minimax for games like Go?",
"options": [
"Go has a small branching factor that MCTS exploits",
"MCTS can handle Go's large branching factor (~250) without needing an evaluation function",
"Minimax produces better moves but is slower",
"MCTS guarantees optimal play in fewer iterations"
],
"answers": ["MCTS can handle Go's large branching factor (~250) without needing an evaluation function"]
}
!!!

---

!!! quiz
{
"title": "Question 4",
"question": "During backpropagation in MCTS, why must the result be flipped at each level?",
"options": [
"To correct for rounding errors in floating-point arithmetic",
"Because alternating players have opposite goals — a win for one player is a loss for the other",
"To ensure the tree remains balanced",
"Because UCB1 requires negative values at MIN nodes"
],
"answers": ["Because alternating players have opposite goals — a win for one player is a loss for the other"]
}
!!!

---

!!! quiz
{
"title": "Question 5",
"question": "What UCB1 value is assigned to a node that has never been visited (nᵢ = 0)?",
"options": [
"0",
"The parent's win rate",
"Negative infinity (-∞)",
"Positive infinity (+∞)"
],
"answers": ["Positive infinity (+∞)"]
}
!!!

---

!!! quiz
{
"title": "Question 6",
"question": "After MCTS search completes, which strategy is most commonly used to select the final move?",
"options": [
"Choose the child with the highest UCB1 value",
"Choose the child with the most visits",
"Choose the child with the fewest visits",
"Choose a random child"
],
"answers": ["Choose the child with the most visits"]
}
!!!

---

!!! quiz
{
"title": "Question 7",
"question": "What does 'anytime algorithm' mean in the context of MCTS?",
"options": [
"The algorithm runs in constant time regardless of game complexity",
"The algorithm can return a valid move at any point — more iterations improve quality",
"The algorithm must complete all iterations before returning a result",
"The algorithm adjusts its time based on the game clock"
],
"answers": ["The algorithm can return a valid move at any point — more iterations improve quality"]
}
!!!

---

!!! quiz
{
"title": "Question 8",
"question": "In AlphaZero's PUCT formula, what role does the policy network's prior probability pᵢ play?",
"options": [
"It replaces the win rate entirely",
"It guides exploration toward moves the neural network considers promising",
"It eliminates the need for backpropagation",
"It determines the maximum search depth"
],
"answers": ["It guides exploration toward moves the neural network considers promising"]
}
!!!

---

!!! quiz
{
"title": "Question 9",
"question": "True or False: MCTS requires a hand-crafted evaluation function to work.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 10",
"question": "What is 'virtual loss' in the context of parallel MCTS?",
"options": [
"A penalty applied to nodes in losing positions to prune them faster",
"A temporary loss recorded when a thread starts processing a node, discouraging other threads from visiting it",
"The loss calculated during the simulation phase using random play",
"A reduction in the exploration constant C for visited nodes"
],
"answers": ["A temporary loss recorded when a thread starts processing a node, discouraging other threads from visiting it"]
}
!!!
