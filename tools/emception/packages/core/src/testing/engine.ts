// Declarative test engine (skeleton).
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

import type { EmceptionAPI, TestCase, TestCaseResult, TestPlan, TestReport } from '../types.js';
import { compileMatcher, runMatcher, type ClangAstNode, type MatchResult } from './clang-query/matcher.js';
import { parseDoctestConsole } from './doctest/parse.js';

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
    const stderrOk = test.expectedStderr === undefined ? true : matches(result.stderr, test.expectedStderr);
    const exitOk = test.expectedExit === undefined ? true : result.exitCode === test.expectedExit;
    const passed = stdoutOk && stderrOk && exitOk && !result.timedOut;
    return {
      name: test.name ?? 'stdio',
      passed,
      durationMs: nowMs() - start,
      diagnostic: passed ? undefined : describeStdioFailure(result, test, { stdoutOk, stderrOk, exitOk }),
    };
  },
  'stdio-file': async (em, test, plan, timeoutMs) => {
    // Read both fixtures from the workspace. Hidden visibility is fine —
    // the runtime ignores visibility metadata; redaction keeps
    // these paths out of student-visible diagnostics.
    const start = nowMs();
    const [inBytes, expectedBytes] = await Promise.all([em.workspace.readFile(test.inFile), em.workspace.readFile(test.expectedOutFile)]);
    if (inBytes === null) {
      return {
        name: test.name ?? 'stdio-file',
        passed: false,
        durationMs: nowMs() - start,
        diagnostic: `Input fixture not found: ${test.inFile}`,
      };
    }
    if (expectedBytes === null) {
      return {
        name: test.name ?? 'stdio-file',
        passed: false,
        durationMs: nowMs() - start,
        diagnostic: `Expected-output fixture not found: ${test.expectedOutFile}`,
      };
    }

    const decoder = new TextDecoder();
    const stdinText = decoder.decode(inBytes);
    const expectedStdout = decoder.decode(expectedBytes);

    const result = await em.compileAndRun(undefined, {
      build: plan.build,
      stdin: stdinText,
      stdout: 'capture',
      stderr: 'capture',
      timeoutMs,
    });

    const passed = !result.timedOut && result.stdout === expectedStdout;
    return {
      name: test.name ?? 'stdio-file',
      passed,
      durationMs: nowMs() - start,
      diagnostic: passed
        ? undefined
        : result.timedOut
          ? `Timed out reading ${test.inFile}.`
          : `stdout mismatch for ${test.inFile}:\n  expected: ${JSON.stringify(expectedStdout)}\n  actual:   ${JSON.stringify(result.stdout)}`,
    };
  },
  'clang-query': async (em, test, plan, timeoutMs) => {
    // Runtime-agnostic half is the matcher engine; the
    // adapter half is `clang -Xclang -ast-dump=json`. We assume the
    // resolved sources are already on disk in the workspace and shell
    // out via `em.run('clang', ...)`. The plan's `build` is consulted
    // for include paths / std / defines so the AST sees the same
    // declarations the build does; we deliberately skip cflags that
    // would change AST shape (e.g. `-O*`) by relying on `clang`'s
    // dump mode ignoring most codegen flags.
    const start = nowMs();
    const sources = (plan.build?.sources ?? []) as string[];
    if (sources.length === 0) {
      return {
        name: test.name ?? 'clang-query',
        passed: false,
        durationMs: nowMs() - start,
        diagnostic: 'clang-query: plan.build.sources is empty.',
      };
    }
    const argv: string[] = ['-Xclang', '-ast-dump=json', '-fsyntax-only'];
    for (const inc of plan.build?.includePaths ?? []) argv.push(`-I${inc}`);
    if (plan.build?.defines) {
      for (const key of Object.keys(plan.build.defines).sort()) {
        const v = plan.build.defines[key];
        argv.push(v === true ? `-D${key}` : `-D${key}=${v}`);
      }
    }
    argv.push(...sources);

    const result = await em.run('clang', argv, {
      stdout: 'capture',
      stderr: 'capture',
      timeoutMs,
    });
    if (result.timedOut) {
      return {
        name: test.name ?? 'clang-query',
        passed: false,
        durationMs: nowMs() - start,
        diagnostic: `clang-query: ast-dump timed out after ${timeoutMs}ms.`,
      };
    }
    if (result.exitCode !== 0) {
      return {
        name: test.name ?? 'clang-query',
        passed: false,
        durationMs: nowMs() - start,
        diagnostic: `clang-query: ast-dump failed (exit ${result.exitCode}).\n${result.stderr}`,
      };
    }

    let root: ClangAstNode;
    try {
      root = JSON.parse(result.stdout) as ClangAstNode;
    } catch (err) {
      return {
        name: test.name ?? 'clang-query',
        passed: false,
        durationMs: nowMs() - start,
        diagnostic: `clang-query: ast-dump JSON parse failed: ${err instanceof Error ? err.message : String(err)}`,
      };
    }

    let match: MatchResult;
    try {
      match = runMatcher(compileMatcher(test.matcher), root);
    } catch (err) {
      return {
        name: test.name ?? 'clang-query',
        passed: false,
        durationMs: nowMs() - start,
        diagnostic: err instanceof Error ? err.message : String(err),
      };
    }

    const passed = evalClangQueryExpect(match, test.expect);
    return {
      name: test.name ?? 'clang-query',
      passed,
      durationMs: nowMs() - start,
      diagnostic: passed ? undefined : describeClangQueryFailure(match, test.expect),
    };
  },
  doctest: async (em, test, plan, timeoutMs) => {
    // Compile the doctest sources together with whatever
    // the workspace's resolved build already specifies (student code
    // + `doctest_main.cpp` typically), then run the produced binary
    // and parse its console reporter output.
    const start = nowMs();
    const planBuild = plan.build ?? {};
    const planSources = (planBuild.sources ?? []) as string[];
    const sources = [...planSources, ...test.sourceFiles];
    if (sources.length === 0) {
      return {
        name: test.name ?? 'doctest',
        passed: false,
        durationMs: nowMs() - start,
        diagnostic: 'doctest: no source files (set plan.build.sources or test.sourceFiles).',
      };
    }
    const result = await em.compileAndRun(undefined, {
      build: { ...planBuild, sources },
      stdin: 'none',
      stdout: 'capture',
      stderr: 'capture',
      timeoutMs,
    });
    if (result.timedOut) {
      return {
        name: test.name ?? 'doctest',
        passed: false,
        durationMs: nowMs() - start,
        diagnostic: `doctest: timed out after ${timeoutMs}ms.`,
      };
    }

    const report = parseDoctestConsole(result.stdout);
    const passed = report.status === 'success' && report.cases.failed === 0 && report.assertions.failed === 0;
    return {
      name: test.name ?? 'doctest',
      passed,
      durationMs: nowMs() - start,
      diagnostic: passed ? undefined : describeDoctestFailure(report, result),
    };
  },
};

/**
 * Run every case in `plan`, emitting a `test-case` event after each, and
 * return an aggregated report. Cases run sequentially because they share a
 * single workspace + build state; parallelism belongs at the plan level
 * (run multiple plans concurrently against separate cores).
 */
export async function runTests(em: EmceptionAPI, plan: TestPlan, opts: { signal?: AbortSignal } = {}): Promise<TestReport> {
  const totalStart = nowMs();
  const cases: TestCaseResult[] = [];
  let passed = 0;
  let failed = 0;

  // Collect hidden/solution paths up-front so per-case
  // diagnostics can be scrubbed before they leave the engine. We resolve
  // this once per plan rather than once per case to keep the cost flat.
  const redactor = plan.redactHidden ? await buildRedactor(em) : identity;

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

    const result = redactCase(await runOne(em, test, plan), redactor);
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

type Redactor = (input: string) => string;

const identity: Redactor = (s) => s;

function redactCase(result: TestCaseResult, redact: Redactor): TestCaseResult {
  if (redact === identity || result.diagnostic === undefined) return result;
  return { ...result, diagnostic: redact(result.diagnostic) };
}

/**
 * Build a redactor that masks any path marked `hidden` or `solution`. The
 * returned function replaces every occurrence of such a path (and its
 * basename) with `<hidden>` to keep grader hints out of student-visible
 * reports. Falls back to identity if the workspace can't list files.
 */
async function buildRedactor(em: EmceptionAPI): Promise<Redactor> {
  let entries: Array<{ path: string; visibility?: string }>;
  try {
    entries = await em.workspace.listFiles({
      includeHidden: true,
      includeSolution: true,
    });
  } catch {
    return identity;
  }

  const sensitive = entries
    .filter((e) => e.visibility === 'hidden' || e.visibility === 'solution')
    .flatMap((e) => {
      const base = e.path.split('/').pop();
      return base && base !== e.path ? [e.path, base] : [e.path];
    })
    // Longest-first prevents a basename from being redacted before its
    // full path, which would leave dangling directory fragments.
    .sort((a, b) => b.length - a.length)
    .map(escapeRegex);

  if (sensitive.length === 0) return identity;
  const re = new RegExp(sensitive.join('|'), 'g');
  return (s) => s.replace(re, '<hidden>');
}

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

async function runOne(em: EmceptionAPI, test: TestCase, plan: TestPlan): Promise<TestCaseResult> {
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
  return typeof expected === 'string' ? actual === expected : expected.test(actual);
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

function evalClangQueryExpect(match: MatchResult, expect: Extract<TestCase, { kind: 'clang-query' }>['expect']): boolean {
  if (expect === 'found') return match.count > 0;
  if (expect === 'not-found') return match.count === 0;
  return match.count >= expect.minCount;
}

function describeClangQueryFailure(match: MatchResult, expect: Extract<TestCase, { kind: 'clang-query' }>['expect']): string {
  const sample = match.samples.map((s) => `${s.kind ?? '?'}${s.name ? ` "${s.name}"` : ''}`).join(', ');
  if (expect === 'found') {
    return `clang-query: expected at least one match, found 0.`;
  }
  if (expect === 'not-found') {
    return `clang-query: expected no matches, found ${match.count}${sample ? ` (e.g. ${sample})` : ''}.`;
  }
  return `clang-query: expected at least ${expect.minCount} match(es), found ${match.count}${sample ? ` (e.g. ${sample})` : ''}.`;
}

function describeDoctestFailure(report: ReturnType<typeof parseDoctestConsole>, result: { stderr: string; exitCode: number }): string {
  if (report.status === 'crash') {
    return `doctest: binary crashed before printing summary (exit ${result.exitCode}).${result.stderr ? `\n${result.stderr}` : ''}`;
  }
  const head =
    `doctest: ${report.cases.failed}/${report.cases.total} test cases failed, ` + `${report.assertions.failed}/${report.assertions.total} assertions failed.`;
  const detail = report.failures
    .slice(0, 5)
    .map((f) => {
      const where = f.file ? `${f.file}:${f.line ?? '?'}` : '?';
      const exp = f.expanded ? `\n    values: ${f.expanded}` : '';
      return `  - [${f.testCase}] ${where}: ${f.expression}${exp}`;
    })
    .join('\n');
  const more = report.failures.length > 5 ? `\n  (+${report.failures.length - 5} more failures)` : '';
  return detail ? `${head}\n${detail}${more}` : head;
}
