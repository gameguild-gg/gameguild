// Phase 5 — Declarative test engine (skeleton).
//
// The engine iterates a TestPlan, dispatches each case to a kind handler,
// and aggregates a TestReport. It is runtime-agnostic: it only talks to the
// public EmceptionAPI surface (run / compileAndRun / workspace) so the same
// engine can grade in the browser or under Node worker_threads.
//
// Kind handlers are pluggable via the TestKindHandler map below. Two are
// implemented synchronously here (custom + stdio); stdio-file, clang-query,
// and doctest are stubbed and will be filled in once the corresponding
// runtime hooks (workspace file I/O, ast-dump invocation, doctest header
// vendoring) land. Stubs throw a descriptive "not yet implemented" error
// rather than silently passing so partial wiring can't ship false greens.

import { TestFailureError } from '../errors';
import type {
    EmceptionAPI,
    TestCase,
    TestCaseResult,
    TestPlan,
    TestReport,
} from '../types';

/**
 * Signature every per-kind handler implements. Receives the live API plus
 * the resolved per-case timeout so handlers don't have to re-derive it.
 */
export type TestKindHandler<K extends TestCase['kind']> = (
    em: EmceptionAPI,
    test: Extract<TestCase, { kind: K }>,
    plan: TestPlan,
    timeoutMs: number | undefined,
) => Promise<TestCaseResult>;

const handlers: { [K in TestCase['kind']]: TestKindHandler<K> } = {
    custom: async (em, test) => {
        const r = await test.run(em);
        return { ...r, name: r.name ?? test.name ?? 'custom' };
    },
    stdio: async (em, test, plan, timeoutMs) => {
        const start = nowMs();
        const result = await em.compileAndRun(undefined, {
            build: plan.build,
            stdin: test.stdin ?? 'none',
            stdout: 'capture',
            stderr: 'capture',
            timeoutMs,
        });
        const stdoutOk = matches(result.stdout, test.expectedStdout);
        const stderrOk = test.expectedStderr === undefined
            ? true
            : matches(result.stderr, test.expectedStderr);
        const exitOk = test.expectedExit === undefined
            ? true
            : result.exitCode === test.expectedExit;
        const passed = stdoutOk && stderrOk && exitOk && !result.timedOut;
        return {
            name: test.name ?? 'stdio',
            passed,
            durationMs: nowMs() - start,
            diagnostic: passed
                ? undefined
                : describeStdioFailure(result, test, { stdoutOk, stderrOk, exitOk }),
        };
    },
    'stdio-file': async () => {
        throw new TestFailureError(
            'stdio-file kind not yet implemented (Phase 5.3 pending workspace file reads).',
        );
    },
    'clang-query': async () => {
        throw new TestFailureError(
            'clang-query kind not yet implemented (Phase 5.4 pending AST-dump pipeline).',
        );
    },
    doctest: async () => {
        throw new TestFailureError(
            'doctest kind not yet implemented (Phase 5.5 pending doctest.h vendoring).',
        );
    },
};

/**
 * Run every case in `plan`, emitting a `test-case` event after each, and
 * return an aggregated report. Cases run sequentially because they share a
 * single workspace + build state; parallelism belongs at the plan level
 * (run multiple plans concurrently against separate cores).
 */
export async function runTests(
    em: EmceptionAPI,
    plan: TestPlan,
    opts: { signal?: AbortSignal } = {},
): Promise<TestReport> {
    const totalStart = nowMs();
    const cases: TestCaseResult[] = [];
    let passed = 0;
    let failed = 0;

    for (const test of plan.cases) {
        if (opts.signal?.aborted) {
            const aborted: TestCaseResult = {
                name: test.name ?? test.kind,
                passed: false,
                durationMs: 0,
                diagnostic: 'Aborted before execution.',
            };
            cases.push(aborted);
            failed += 1;
            continue;
        }

        const result = await runOne(em, test, plan);
        cases.push(result);
        if (result.passed) passed += 1;
        else failed += 1;
    }

    return {
        passed,
        failed,
        totalDurationMs: nowMs() - totalStart,
        cases,
    };
}

async function runOne(
    em: EmceptionAPI,
    test: TestCase,
    plan: TestPlan,
): Promise<TestCaseResult> {
    const start = nowMs();
    try {
        // The cast keeps each handler's narrowed test type intact; TS can't
        // see that `handlers[test.kind]` matches `test` without help.
        const handler = handlers[test.kind] as TestKindHandler<TestCase['kind']>;
        return await handler(em, test as never, plan, plan.timeoutMsPerCase);
    } catch (err) {
        return {
            name: test.name ?? test.kind,
            passed: false,
            durationMs: nowMs() - start,
            diagnostic: err instanceof Error ? err.message : String(err),
        };
    }
}

function matches(actual: string, expected: string | RegExp): boolean {
    return typeof expected === 'string'
        ? actual === expected
        : expected.test(actual);
}

function describeStdioFailure(
    result: { stdout: string; stderr: string; exitCode: number; timedOut: boolean },
    test: Extract<TestCase, { kind: 'stdio' }>,
    flags: { stdoutOk: boolean; stderrOk: boolean; exitOk: boolean },
): string {
    if (result.timedOut) return `Timed out after ${test.name ?? 'stdio'} case.`;
    const parts: string[] = [];
    if (!flags.stdoutOk) {
        parts.push(`stdout mismatch:\n  expected: ${stringify(test.expectedStdout)}\n  actual:   ${JSON.stringify(result.stdout)}`);
    }
    if (!flags.stderrOk && test.expectedStderr !== undefined) {
        parts.push(`stderr mismatch:\n  expected: ${stringify(test.expectedStderr)}\n  actual:   ${JSON.stringify(result.stderr)}`);
    }
    if (!flags.exitOk && test.expectedExit !== undefined) {
        parts.push(`exit code mismatch: expected ${test.expectedExit}, got ${result.exitCode}`);
    }
    return parts.join('\n');
}

function stringify(v: string | RegExp): string {
    return v instanceof RegExp ? v.toString() : JSON.stringify(v);
}

function nowMs(): number {
    // performance.now() exists in browsers, Node 16+, and Workers.
    if (typeof performance !== 'undefined' && typeof performance.now === 'function') {
        return performance.now();
    }
    return Date.now();
}
