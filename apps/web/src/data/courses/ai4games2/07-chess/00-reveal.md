# Week 07: Advanced Chess Techniques

Chess Engine Optimization + Competition Prep

---

## Agenda

- Recap: Week 06 engine core
- Search optimizations
- Zobrist hashing + transposition tables
- Opening books + endgames
- Practice tournament
- Debugging clinic

---

## Recap (Week 06)

- Iterative deepening
- Aspiration windows
- Quiescence search
- Time management discipline

---

## Optimization Stack

- Move ordering → more cutoffs
- Selective pruning/reduction → fewer nodes
- Transposition tables → avoid re-search
- Opening book + tablebases → instant knowledge

---

## Null-Move Pruning

- “If I pass and still get a cutoff, I’m winning”
- Reduced depth search with a null move
- Danger: zugzwang (endgames)

---

## Late Move Reductions (LMR)

- First moves full depth
- Later moves reduced depth
- Re-search if a reduced move beats alpha

---

## Killer + History Heuristics

- **Killer:** quiet move causing cutoff at same depth
- **History:** global score for cutoff-causing moves
- Better ordering = faster alpha-beta

---

## Zobrist Hashing

- Random 64-bit keys per piece-square
- XOR in/out to update in $O(1)$
- Include: side-to-move, castling, en passant

---

## Transposition Tables

- Cache position evaluations
- Store depth + score + bound type
- Turns tree search into graph search

---

## Opening Book

- Free good moves in theory lines
- Decide when to “leave book”
- Even a tiny book helps

---

## Endgame + Syzygy

- Evaluation shifts (king activity, passed pawns)
- Tablebases give perfect play ≤7 pieces

---

## Practice Tournament

https://gameguild-gg.github.io/chess-competition/competition/

- Pick bots
- Choose time limit
- Start game or bracket

---

## Competition Rules (Quick)

- Implement only in `chess-bot`
- No subfolders
- Zip only `chess-bot` contents
- No external libraries
- Obey time limit passed to `Move()`

---

## Debugging Clinic

- Illegal moves?
- Timeouts?
- TT key correctness?
- Repro with FEN

---

## Exit Ticket

1. Biggest speedup you can add this week?
2. Where does null-move fail?
3. What must be in your TT key?

---

## Reminders

- Quiz 6: 2026/02/26
- Assignment 6 due: 2026/03/01
- Test on the competition site
