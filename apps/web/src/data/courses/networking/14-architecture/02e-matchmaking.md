# Matchmaking: Finding Fair, Fast, Fun Games

Matchmaking is a **multi-objective optimization problem**: find a group of players that will produce a good game, within a time budget that doesn't make players quit the queue. It is not a simple sort-by-skill operation — it balances skill fairness, latency bounds, queue health, party sizes, and population dynamics simultaneously.

---

## 1. What Matchmaking Optimizes

### The Competing Objectives

| Objective         | What It Means                                     | Conflict With                          |
| ----------------- | ------------------------------------------------- | -------------------------------------- |
| Skill fairness    | Players are matched with similar skill levels     | Queue time (fewer eligible opponents)  |
| Low latency       | Players are in the same or nearby region          | Skill fairness (smaller regional pool) |
| Short queue time  | Players find a match quickly                      | Skill and latency constraints          |
| Party support     | Friends can queue together                        | Balanced teams (parties vs solos)      |
| Population health | New players aren't crushed; veterans aren't bored | Strict skill matching                  |
| Match quality     | The resulting game is competitive and fun         | Speed of matching                      |

No single metric captures "good matchmaking." Every matchmaker makes tradeoffs, and the art is in tuning those tradeoffs for the game's population and genre.

### The Queue Time vs Quality Tradeoff

This is the fundamental tension:

- **Strict matching** (narrow skill range, same region): high match quality, long queue times.
- **Loose matching** (wide skill range, cross-region): short queue times, lower match quality.

Most matchmakers use **expanding windows**: start with strict criteria and gradually relax them as the player waits longer.

```
t=0s:   skill ±100, region=same, latency<50ms
t=30s:  skill ±200, region=same, latency<80ms
t=60s:  skill ±400, region=adjacent, latency<120ms
t=120s: skill ±800, any region, latency<200ms
```

---

## 2. Skill Rating Systems

### Why Skill Matters

If matchmaking doesn't account for skill, new players face veterans and quit (churn), while veterans face easy opponents and get bored (also churn). Skill-based matchmaking (SBMM) keeps matches competitive for everyone.

### Elo Rating

Originally designed for chess. Each player has a numeric rating. After a match:

- Winner gains points, loser loses points.
- The amount depends on the expected outcome: beating a higher-rated opponent gives more points than beating a lower-rated one.

$$E_A = \frac{1}{1 + 10^{(R_B - R_A)/400}}$$

$$R_A' = R_A + K(S_A - E_A)$$

Where $E_A$ is A's expected score, $R_A$ and $R_B$ are ratings, $S_A$ is the actual outcome (1=win, 0=loss), and $K$ is the update sensitivity.

Limitations: Elo was designed for 1v1. Team games require adaptations.

### Glicko / Glicko-2

Extends Elo by adding a **rating deviation** (confidence) and a **volatility** measure. A player with high rating deviation has an uncertain skill level (new player, returning player, or inconsistent performer). The matchmaker uses deviation to:

- Place uncertain players in a wider skill range (exploration).
- Weight match results more heavily for uncertain players (faster calibration).
- Weight match results less for stable players (resistance to noise).

### TrueSkill / TrueSkill 2

Microsoft's Bayesian skill system (used in Halo, Gears of War). Models each player as a Gaussian distribution (mean = estimated skill, variance = uncertainty). Supports:

- Team games (factors in team composition).
- Free-for-all (multiple player ranking per match).
- Rapid convergence for new players.

### OpenSkill

Open-source alternative to TrueSkill. Bayesian rating without Microsoft's patent restrictions. Supports teams, free-for-all, and partial rankings.

---

## 3. Matchmaking Architecture

### Components

A typical matchmaking system has these components:

```
Player → Queue Service → Match Function → Session Creator → Game Server
                ↑              ↑
          Player Pool     Evaluation Logic
```

1. **Queue Service**: accepts player requests, maintains the player pool, handles party grouping.
2. **Player Pool**: the set of all players currently waiting for a match. Indexed by skill, region, preferences.
3. **Match Function**: the algorithm that selects groups from the pool and forms matches. This is where all the optimization logic lives.
4. **Session Creator**: allocates a game server instance and creates a session for the matched group.
5. **Assignment**: notifies each player of their match and provides connection details.

### Open Match Architecture

Google's Open Match is an open-source matchmaking framework that separates these concerns:

- **Frontend**: handles player tickets (join queue, leave queue, get assignment).
- **Director**: orchestrates match functions and assigns matches to servers.
- **Match Function**: custom logic (skill evaluation, constraint checking) provided by the game developer.
- **Backend**: manages server allocation and session creation.

This separation lets developers customize the matching logic without rebuilding infrastructure.

### Matchmaking Is a Batch Process

Matchmaking does not match one player at a time. It accumulates a pool, then runs the match function periodically (every 1-5 seconds) to find the best set of matches from the current pool.

Why batch?

- **Better matches**: with more players in the pool, the optimizer has more options.
- **Fairness**: processing all players simultaneously avoids ordering bias (first-in-first-matched).
- **Efficiency**: one optimization pass over N players is cheaper than N individual searches.

---

## 4. Latency and Region Constraints

### Why Latency Matters for Matchmaking

A perfectly skill-balanced match where one team has 20ms ping and the other has 200ms ping is not a fair match. Latency directly affects gameplay performance:

- Higher-latency players have more prediction error, worse hit registration, and slower feedback.
- In many games, 50ms of additional latency is equivalent to a measurable skill disadvantage.

### Region-Based Matching

The simplest latency constraint: match players within the same geographic region. Each player's region is determined by:

- **Client self-report**: the game client pings regional endpoints and reports the best one.
- **IP geolocation**: approximate but fast; used as a fallback.
- **QoS probe results**: the matchmaker pings back or uses historical latency data.

Regional matching narrows the player pool, which increases queue times. During low-population hours, the matchmaker may cross regions to maintain reasonable queue times.

### Latency Budgets

Rather than strict regions, some matchmakers use latency budgets:

- "Match players such that the maximum RTT between any two players is < 100ms."
- This allows cross-region matching when two nearby regions have low latency (e.g., US East + US Central).

---

## 5. Party and Team Balancing

### The Party Problem

A pre-made 5-player party typically has better coordination than 5 solo players. Matching a full party against 5 randoms is unfair even if individual skill ratings are equal.

Solutions:

- **Party vs party**: match parties against parties of similar size. Increases queue time for parties.
- **Party skill bonus**: inflate the party's effective skill rating to account for coordination advantage. Requires tuning.
- **Solo/duo queue**: restrict queue entry to solo players or pairs, guaranteeing even team composition.

### Team Balancing

For team modes, the matchmaker must form balanced teams from the matched player pool:

- **Greedy alternating pick**: sort by skill, alternately assign to teams. Simple but doesn't optimize team-level balance.
- **Minimize skill difference**: find the team split that minimizes the difference in total team skill.
- **Role-based balancing**: in games with roles (tank, healer, DPS), ensure each team has the required role composition.

---

## 6. Population Health and Edge Cases

### Small Populations

When few players are online (late night, niche game modes, new game):

- **Expand criteria faster**: reduce time before skill/latency windows widen.
- **Cross-mode matching**: allow similar modes to share a pool (e.g., two bomb-defusal variants).
- **Bot backfill**: fill empty slots with AI players; replace bots when humans join.

### Smurf Detection

Smurfs (experienced players on new accounts) destroy match quality. Detection approaches:

- **Win rate anomaly**: a new account winning 90% of matches is likely not new.
- **Performance metrics**: kill/death ratio, accuracy, APM far above the account's rating.
- **Hardware/IP fingerprinting**: same hardware as an existing high-rated account.

Response: accelerate skill calibration (larger K-factor), place in higher skill bracket faster.

### Queue Abandonment

Players who wait too long leave the queue. This creates a negative feedback loop:

- Long queue → some players leave → smaller pool → even longer queue for remaining players.

Prevention: display estimated wait times, provide something to do in queue (practice mode, loadout customization), and prioritize reducing queue time for players approaching the abandonment threshold.

---

## 7. CSI vs GPR Framing

### CSI Perspective: Matchmaking as Service Discovery + Optimization

The CSI engineer sees matchmaking as:

- **Service discovery**: finding an available game server with appropriate capacity and proximity.
- **Constraint satisfaction**: the match function is a constraint solver operating on player attributes (skill, latency, party, preferences) with objective functions (match quality, time).
- **Queue theory**: the player pool is a queue; the match function is the server; arrival rate and service rate determine wait times.
- **Load balancing**: distributing players across servers to maintain even load and optimal resource utilization.

Performance metrics:

- Match function execution time (must complete within the batch interval).
- Queue depth and wait time distribution.
- Server utilization rate.
- Match creation rate.

### GPR Perspective: Matchmaking as Player Experience

The GPR engineer sees matchmaking as:

- **First impression**: for new players, the first match determines whether they keep playing. Matchmaking must protect new players from veterans.
- **Flow state**: matches that are too easy or too hard break flow. SBMM keeps matches in the "challenge zone."
- **Social glue**: party support and friend-of-friend matching increase retention.
- **Perceived fairness**: even if the match was objectively fair, if the player _feels_ it was unfair (one-sided stomp due to role mismatch, for example), the system has failed.

Experience metrics:

- Match competitiveness (how close was the final score?).
- Player retention after match (did they queue again?).
- Reported satisfaction (post-match surveys, if available).
- Churn correlation with match quality.
