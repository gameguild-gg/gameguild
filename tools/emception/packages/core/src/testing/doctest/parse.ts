/**
 * Doctest console-reporter output parser.
 *
 * doctest (https://github.com/doctest/doctest) is the C++ unit-test
 * framework Emception ships in the sysroot for the `doctest` test kind.
 * When invoked without an XML reporter it prints a small, stable
 * grammar to stdout that we parse into structured results so the test
 * runner can:
 *
 *   - report pass/fail counts in the same shape as other test kinds,
 *   - surface individual failed assertions (file, line, expression,
 *     expanded values) without forcing the host to read raw stdout,
 *   - distinguish a clean pass from a test-framework crash.
 *
 * This module is pure string-in / object-out — no I/O, no DOM, no Node
 * APIs — so the same parser runs in the browser worker (where doctest
 * runs under WASM) and in the future Node worker.
 *
 * Sample doctest output we parse (with `--no-colors --no-version`):
 *
 *   src/list_test.cpp:12:
 *   TEST CASE:  push appends
 *
 *   src/list_test.cpp:15: ERROR: CHECK( list.size() == 1 ) is NOT correct!
 *     values: CHECK( 0 == 1 )
 *
 *   ===============================================================================
 *   [doctest] test cases:      2 |      1 passed |      1 failed | 0 skipped
 *   [doctest] assertions:      4 |      3 passed |      1 failed |
 *   [doctest] Status: FAILURE!
 *
 * If the summary lines are missing we treat the run as a crash and
 * return `status: 'crash'` so callers can distinguish a failed test
 * (parsed cleanly, some assertion failed) from a binary that died
 * before doctest could print its summary.
 */

/** Aggregate counts for a single counter type. */
export interface DoctestCounts {
    passed: number;
    failed: number;
    /** Skipped cases. Always 0 for assertions (doctest does not skip them). */
    skipped: number;
    total: number;
}

/** A single failed CHECK / REQUIRE / etc. assertion. */
export interface DoctestFailure {
    /** Owning test case name (parsed from the most recent `TEST CASE:` line). */
    testCase: string;
    /** Source file path as reported by doctest (compiler-relative). */
    file?: string;
    /** 1-based source line of the failing assertion. */
    line?: number;
    /** Macro name, e.g. `CHECK`, `REQUIRE`, `CHECK_EQ`. */
    macro?: string;
    /** Original assertion text, e.g. `CHECK( list.size() == 1 )`. */
    expression: string;
    /** Expanded form with substituted values, when doctest emitted it. */
    expanded?: string;
}

export interface DoctestReport {
    /** 'success' = explicit doctest "Status: SUCCESS!" line. */
    /** 'failure' = explicit doctest "Status: FAILURE!" line. */
    /** 'crash'   = neither summary line found (binary likely died). */
    status: 'success' | 'failure' | 'crash';
    cases: DoctestCounts;
    assertions: DoctestCounts;
    failures: DoctestFailure[];
}

/**
 * Parse the captured stdout of a doctest binary into a structured report.
 *
 * Tolerant of extra application output before/after the doctest blocks —
 * useful when the test binary also prints log lines.
 */
export function parseDoctestConsole(stdout: string): DoctestReport {
    const lines = stdout.split(/\r?\n/);
    const cases = emptyCounts();
    const assertions = emptyCounts();
    const failures: DoctestFailure[] = [];

    let status: DoctestReport['status'] = 'crash';
    let currentTestCase = '';
    /** Failure currently being assembled across consecutive lines. */
    let pending: DoctestFailure | null = null;

    const flushPending = () => {
        if (pending) {
            failures.push(pending);
            pending = null;
        }
    };

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i] ?? '';
        const trimmed = line.trim();

        // --- Summary lines (most authoritative — checked first) ---
        const m1 = trimmed.match(
            /^\[doctest\]\s+test cases:\s*(\d+)\s*\|\s*(\d+)\s+passed\s*\|\s*(\d+)\s+failed\s*\|\s*(\d+)\s+skipped/,
        );
        if (m1) {
            flushPending();
            cases.total = +m1[1];
            cases.passed = +m1[2];
            cases.failed = +m1[3];
            cases.skipped = +m1[4];
            continue;
        }

        const m2 = trimmed.match(
            /^\[doctest\]\s+assertions:\s*(\d+)\s*\|\s*(\d+)\s+passed\s*\|\s*(\d+)\s+failed\s*\|/,
        );
        if (m2) {
            flushPending();
            assertions.total = +m2[1];
            assertions.passed = +m2[2];
            assertions.failed = +m2[3];
            assertions.skipped = 0;
            continue;
        }

        const m3 = trimmed.match(/^\[doctest\]\s+Status:\s+(SUCCESS|FAILURE)!/i);
        if (m3) {
            flushPending();
            status = m3[1].toUpperCase() === 'SUCCESS' ? 'success' : 'failure';
            continue;
        }

        // --- TEST CASE marker (sets context for subsequent failures) ---
        const tc = trimmed.match(/^TEST CASE:\s+(.+)$/);
        if (tc) {
            flushPending();
            currentTestCase = tc[1];
            continue;
        }

        // --- Failure line: "<file>:<line>: ERROR: <macro>( ... ) is NOT correct!" ---
        // The file:line prefix is the source location; doctest emits this
        // immediately before the expansion line.
        const err = line.match(
            /^(.+?):(\d+):\s*ERROR:\s*([A-Z_]+)\(\s*(.+?)\s*\)\s+is NOT correct!/,
        );
        if (err) {
            flushPending();
            pending = {
                testCase: currentTestCase,
                file: err[1],
                line: +err[2],
                macro: err[3],
                expression: `${err[3]}( ${err[4]} )`,
            };
            continue;
        }

        // --- "values:" continuation line attaches to the pending failure ---
        if (pending) {
            const vals = trimmed.match(/^values:\s+(.+)$/);
            if (vals) {
                pending.expanded = vals[1];
                continue;
            }
            // Blank line / separator → finalize the pending failure.
            if (trimmed === '' || /^=+$/.test(trimmed)) {
                flushPending();
                continue;
            }
        }
    }
    flushPending();

    return { status, cases, assertions, failures };
}

function emptyCounts(): DoctestCounts {
    return { passed: 0, failed: 0, skipped: 0, total: 0 };
}
