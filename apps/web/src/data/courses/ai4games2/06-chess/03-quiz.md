# Week 06 Quiz - Chess Engine Core

---

!!! quiz
{
"title": "Question 1",
"question": "What does UCI stand for, and what is its purpose in chess programming?",
"options": [
"Universal Chess Interface — a standard text-based protocol for communication between chess engines and GUIs",
"Unified Computation Index — a benchmark for measuring engine speed",
"Universal Check Inspector — a tool for validating legal moves",
"User Control Input — a method for players to input moves manually"
],
"answers": ["Universal Chess Interface — a standard text-based protocol for communication between chess engines and GUIs"]
}
!!!

---

!!! quiz
{
"title": "Question 2",
"question": "In FEN notation, what does the field 'KQkq' represent?",
"options": [
"The positions of the kings and queens on the board",
"Castling availability for both sides (K=white kingside, Q=white queenside, k=black kingside, q=black queenside)",
"The last four moves played in the game",
"The material balance between both sides"
],
"answers": ["Castling availability for both sides (K=white kingside, Q=white queenside, k=black kingside, q=black queenside)"]
}
!!!

---

!!! quiz
{
"title": "Question 3",
"question": "How is white kingside castling (O-O) represented in UCI move notation?",
"options": [
"\"O-O\"",
"\"e1g1\"",
"\"e1h1\"",
"\"Kg1\""
],
"answers": ["\"e1g1\""]
}
!!!

---

!!! quiz
{
"title": "Question 4",
"question": "In the 0x88 board representation, how is off-board detection performed?",
"options": [
"By checking if rank >= 0 && rank < 8 && file >= 0 && file < 8",
"By using a sentinel value at the array boundary",
"By testing (index & 0x88) != 0 in a single AND instruction",
"By maintaining a separate boolean array of valid squares"
],
"answers": ["By testing (index & 0x88) != 0 in a single AND instruction"]
}
!!!

---

!!! quiz
{
"title": "Question 5",
"question": "What is a bitboard in chess programming?",
"options": [
"A physical board used for debugging",
"A 64-bit integer where each bit represents one square on the chess board",
"An 8x8 2D array storing piece identifiers",
"A compressed format for storing multiple games"
],
"answers": ["A 64-bit integer where each bit represents one square on the chess board"]
}
!!!

---

!!! quiz
{
"title": "Question 6",
"question": "How can you generate all white pawn single pushes using bitboards?",
"options": [
"Loop through each pawn and check the square ahead",
"(whitePawns << 8) & empty — shifting all pawns up one rank and masking with empty squares",
"popcount(whitePawns) to count available moves",
"PAWN_TABLE[sq] for each pawn square"
],
"answers": ["(whitePawns << 8) & empty — shifting all pawns up one rank and masking with empty squares"]
}
!!!

---

!!! quiz
{
"title": "Question 7",
"question": "What technique do modern engines like Stockfish use for O(1) sliding piece attack generation?",
"options": [
"Ray-casting loops in all directions",
"0x88 offset tables",
"Magic bitboards — a multiply, shift, and table lookup using precomputed magic numbers",
"Recursive flood-fill from the piece's square"
],
"answers": ["Magic bitboards — a multiply, shift, and table lookup using precomputed magic numbers"]
}
!!!

---

!!! quiz
{
"title": "Question 8",
"question": "What is the standard piece value of a knight in centipawns?",
"options": [
"100",
"300",
"320",
"500"
],
"answers": ["320"]
}
!!!

---

!!! quiz
{
"title": "Question 9",
"question": "What does a piece-square table (PST) encode?",
"options": [
"The number of legal moves each piece type can make",
"A bonus or penalty for each piece on each square, encoding positional knowledge",
"The probability that a piece will be captured on each square",
"The shortest path from each piece to the enemy king"
],
"answers": ["A bonus or penalty for each piece on each square, encoding positional knowledge"]
}
!!!

---

!!! quiz
{
"title": "Question 10",
"question": "In tapered evaluation, why are two separate PSTs maintained for each piece?",
"options": [
"One for white and one for black",
"One for the opening book and one for the endgame tablebase",
"One for middlegame and one for endgame, because optimal piece placement changes with game phase (e.g., kings should centralize in endgames)",
"One for attack and one for defense"
],
"answers": ["One for middlegame and one for endgame, because optimal piece placement changes with game phase (e.g., kings should centralize in endgames)"]
}
!!!

---

!!! quiz
{
"title": "Question 11",
"question": "Why is king safety evaluation often non-linear (quadratic) with respect to the number of attackers?",
"options": [
"Because more attackers means more legal moves, which is always good",
"Because one attacker is manageable, but multiple attackers compound danger disproportionately — 3+ attackers are often fatal in chess",
"Because the king can only be attacked by one piece at a time",
"Because linear scaling produces integer overflow"
],
"answers": ["Because one attacker is manageable, but multiple attackers compound danger disproportionately — 3+ attackers are often fatal in chess"]
}
!!!

---

!!! quiz
{
"title": "Question 12",
"question": "What is the approximate Elo range of a chess engine with material-only evaluation (no positional terms)?",
"options": [
"~400 Elo",
"~1200–1400 Elo",
"~2000–2200 Elo",
"~3000+ Elo"
],
"answers": ["~1200–1400 Elo"]
}
!!!

---

!!! quiz
{
"title": "Question 13",
"question": "Why is iterative deepening NOT wasteful despite re-searching previous depths?",
"options": [
"Because deeper depths search fewer nodes than shallower ones",
"Because exponential growth means the deepest level dominates cost — all previous depths combined add only ~3% overhead for chess",
"Because earlier depths are skipped after the first iteration",
"Because the branching factor decreases at each depth"
],
"answers": ["Because exponential growth means the deepest level dominates cost — all previous depths combined add only ~3% overhead for chess"]
}
!!!

---

!!! quiz
{
"title": "Question 14",
"question": "What is the primary benefit of iterative deepening beyond time control?",
"options": [
"It reduces the branching factor of the game tree",
"It provides move ordering — the best move from depth d-1 is searched first at depth d, dramatically improving alpha-beta pruning",
"It eliminates the need for an evaluation function",
"It guarantees finding the optimal move at every depth"
],
"answers": ["It provides move ordering — the best move from depth d-1 is searched first at depth d, dramatically improving alpha-beta pruning"]
}
!!!

---

!!! quiz
{
"title": "Question 15",
"question": "What are aspiration windows in chess search?",
"options": [
"Windows that display the chess board in the GUI",
"Narrowing the alpha-beta search window around the previous depth's score to achieve more cutoffs",
"The time window between when the engine starts and must return a move",
"A technique for generating only capture moves"
],
"answers": ["Narrowing the alpha-beta search window around the previous depth's score to achieve more cutoffs"]
}
!!!

---

!!! quiz
{
"title": "Question 16",
"question": "What happens when an aspiration window search 'fails high'?",
"options": [
"The engine found a forced checkmate",
"The true score is above the upper bound (beta), so the window must be widened upward and the position re-searched",
"The search completed successfully within the window",
"The engine ran out of time and must return immediately"
],
"answers": ["The true score is above the upper bound (beta), so the window must be widened upward and the position re-searched"]
}
!!!

---

!!! quiz
{
"title": "Question 17",
"question": "What is the horizon effect in chess engines?",
"options": [
"The engine can't see beyond the edge of the board",
"The engine stops searching in the middle of a tactical sequence (e.g., after QxR but before PxQ), giving a misleading evaluation",
"The engine plays worse as it approaches the endgame",
"The engine takes longer to search positions with more pieces"
],
"answers": ["The engine stops searching in the middle of a tactical sequence (e.g., after QxR but before PxQ), giving a misleading evaluation"]
}
!!!

---

!!! quiz
{
"title": "Question 18",
"question": "What is the stand-pat score in quiescence search?",
"options": [
"The score assigned to a checkmate position",
"The static evaluation at a node, representing the option to not capture — providing a lower bound since the side to move can always decline to trade",
"The score of the best capture found so far",
"The average score across all possible captures"
],
"answers": ["The static evaluation at a node, representing the option to not capture — providing a lower bound since the side to move can always decline to trade"]
}
!!!

---

!!! quiz
{
"title": "Question 19",
"question": "In MVV-LVA (Most Valuable Victim – Least Valuable Attacker) capture ordering, which capture would be searched first?",
"options": [
"Queen captures pawn (QxP)",
"Pawn captures queen (PxQ)",
"Knight captures knight (NxN)",
"Rook captures bishop (RxB)"
],
"answers": ["Pawn captures queen (PxQ)"]
}
!!!

---

!!! quiz
{
"title": "Question 20",
"question": "Why should a chess engine use only ~85% of its time budget rather than the full amount?",
"options": [
"To save time for the opening book lookup",
"To leave margin for OS scheduling jitter — the process might get paused unpredictably, and exceeding the limit means forfeiting the game",
"Because the evaluation function only needs 85% accuracy",
"Because the remaining 15% is reserved for move generation"
],
"answers": ["To leave margin for OS scheduling jitter — the process might get paused unpredictably, and exceeding the limit means forfeiting the game"]
}
!!!
