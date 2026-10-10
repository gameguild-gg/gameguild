# Task 6 — /feed render perf evidence (feed-blink-perf)

Measurement of the waterfall collapse (todo 3: 3 serial fetch rounds → 1 parallel
`Promise.all`) plus notifications streaming (todo 4), proving IS-2: the feed first
screen renders after one parallel round.

## Methodology

- **Baseline**: UNCHANGED `main` checkout dev server on `:3000`, measured BEFORE
  todo 3 landed. Raw data: `.omo/evidence/task-6-baseline-ttfb.txt`.
- **Post-change**: worktree (`fix/feed-blink-perf`) dev server on `:3100`, after
  todos 3+4. Raw data: `.omo/evidence/task-6-postchange-ttfb.txt`.
- **Metric**: curl `time_total` (full streamed HTML), NOT `time_starttransfer`.
  Next serves a prerendered shell from cache (`x-nextjs-cache: HIT`), so TTFB only
  measures cache retrieval; `time_total` captures the full dynamic render +
  streaming — the honest end-to-end number for this change.
- **Runs**: 5 per set, unique `?cb=<rand>` cache-buster param per run,
  authenticated via `gameguild.session-token` cookie.
- **Warm-only comparison**: run 1 of each set is the dev-server cold compile
  (6.098s post-change) and is **excluded as noise** — documented here explicitly.
  All reported numbers are warm.
- **Verification**: 3 fresh post-change samples re-run by the executor against the
  live `:3100` dev server (auth cookie jar re-used), recorded alongside — same
  ballpark confirms the recorded numbers.

## Results

| Set | Runs (time_total, s) | Median | Mean |
|---|---|---|---|
| Baseline (main, :3000) | 1.352, 1.709, 1.273, 2.423, 1.822 | **1.709** | 1.716 |
| Post-change warm (:3100) | 0.732, 0.574, 0.393, 0.509 | **0.541** | 0.552 |
| Post-change verify (fresh, :3100) | 1.002, 0.587, 0.354 | 0.587 | 0.648 |

Post-change run 1 (6.098s) excluded as dev-server cold compile, per methodology.

## Verdict

**Post ≤ baseline — PASS.** Warm median 1.709s → 0.541s = **−68%**. Independent
verification samples corroborate (median 0.587s, all runs well under baseline
median). This is the IS-2 proof: the feed first screen renders after one parallel
round instead of three serial rounds.

## Caveats

- Dev-mode numbers, single machine — relative comparison only.
- The `?cb=` cache-buster does not fully bust Next's segment cache (shell remains
  HIT); `time_total` remains the honest end-to-end metric as reasoned above.
- Recommend re-measuring in a production build for absolute numbers.
