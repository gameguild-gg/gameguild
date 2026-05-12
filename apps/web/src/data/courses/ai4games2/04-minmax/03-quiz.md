# Week 04 Quiz - MinMax & Alpha-Beta Pruning

---

!!! quiz
{
"title": "Question 1",
"question": "In a two-player zero-sum game, if Player A's score is +5, what is Player B's score?",
"options": [
"+5 (same as A)",
"-5 (opposite of A)",
"0 (zero-sum means zero)",
"Unknown without more information"
],
"answers": ["-5 (opposite of A)"]
}
!!!

---

!!! quiz
{
"title": "Question 2",
"question": "In the minimax algorithm, the MAX player attempts to:",
"options": [
"Minimize the score",
"Maximize the score",
"Reach a random leaf node",
"Prune as many nodes as possible"
],
"answers": ["Maximize the score"]
}
!!!

---

!!! quiz
{
"title": "Question 3",
"question": "True or False: Alpha-beta pruning can return a different result than standard minimax search.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 4",
"question": "In alpha-beta pruning, what do the alpha (α) and beta (β) values represent?",
"options": [
"α = best for MAX so far; β = best for MIN so far",
"α = search depth; β = branching factor",
"α = evaluation score; β = move count",
"α = time limit; β = node limit"
],
"answers": ["α = best for MAX so far; β = best for MIN so far"]
}
!!!

---

!!! quiz
{
"title": "Question 5",
"question": "A beta cutoff occurs when:",
"options": [
"A MIN node finds a value ≤ alpha",
"A MAX node finds a value ≥ beta",
"The search reaches maximum depth",
"An evaluation function returns zero"
],
"answers": ["A MIN node finds a value ≤ alpha"]
}
!!!

---

!!! quiz
{
"title": "Question 6",
"question": "With perfect move ordering, alpha-beta pruning can reduce the effective branching factor from b to approximately:",
"options": [
"b/2",
"√b (square root of b)",
"log(b)",
"b-1"
],
"answers": ["√b (square root of b)"]
}
!!!

---

!!! quiz
{
"title": "Question 7",
"question": "Why is move ordering important in alpha-beta pruning?",
"options": [
"It determines which player moves first",
"Searching better moves first causes more cutoffs",
"It prevents infinite loops in the game tree",
"It reduces memory usage"
],
"answers": ["Searching better moves first causes more cutoffs"]
}
!!!

---

!!! quiz
{
"title": "Question 8",
"question": "What is the purpose of a transposition table in game tree search?",
"options": [
"To store the game rules",
"To cache previously evaluated positions and avoid recomputation",
"To generate random moves",
"To limit search depth"
],
"answers": ["To cache previously evaluated positions and avoid recomputation"]
}
!!!

---

!!! quiz
{
"title": "Question 9",
"question": "In a chess evaluation function, which factor typically has the highest weight?",
"options": [
"King safety",
"Pawn structure",
"Material balance (piece values)",
"Control of the center"
],
"answers": ["Material balance (piece values)"]
}
!!!

---

!!! quiz
{
"title": "Question 10",
"question": "The negamax formulation simplifies minimax by:",
"options": [
"Using separate MAX and MIN functions",
"Negating the recursive call's return value so one function handles both players",
"Eliminating the need for an evaluation function",
"Only searching winning moves"
],
"answers": ["Negating the recursive call's return value so one function handles both players"]
}
!!!