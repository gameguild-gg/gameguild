// Public type surface for emception.

import type { EmceptionEventListener, EmceptionEventName, Unsubscribe } from './events';

/**
 * A single file in a workspace seed or VFS listing.
 *
 * - `content` — file bytes or UTF-8 text. Strings are normalised to
 *   UTF-8 by the workspace manager.
 * - `visibility` — governs which files the UI shows to the student:
 *   - `'public'` (default) — shown in the file explorer and editable.
 *   - `'hidden'` — compiled but never shown to the student.
 *   - `'solution'` — like hidden but skipped by graders when
 *     `redactHidden` is `true`; used for reference solutions.
 * - `readonly` — prevents the student from editing the file in the IDE.
 * - `executable` — sets the executable bit in the virtual FS (rarely needed).
 */
export interface FileEntry {
  content: string | Uint8Array;
  visibility?: 'public' | 'hidden' | 'solution';
  readonly?: boolean;
  executable?: boolean;
}

/** Map of virtual-path → file descriptor used to seed a workspace. String values are shorthand for `{ content: value }`. */
export type WorkspaceSeed = Record<string, FileEntry | string>;

/**
 * Compiler flags and linker settings for a workspace build.
 *
 * The resolver merges these with preset defaults and then forwards the
 * result to `buildArgv()` which produces the final `clang` / `em++` argv.
 *
 * Omit `compiler` to let the preset choose (e.g. `'emcc'` for web targets).
 * Set `std` to a C/C++ standard string: `'c17'`, `'c++20'`, etc.
 */
export interface WorkspaceBuildConfig {
  compiler?: 'clang' | 'clang++' | 'emcc' | 'em++';
  std?: string;
  cflags?: string[];
  cxxflags?: string[];
  ldflags?: string[];
  defines?: Record<string, string | true>;
  includePaths?: string[];
  libPaths?: string[];
  libs?: string[];
  sources?: string[];
  output?: string;
  env?: Record<string, string>;
  cmake?: {
    sourceDir?: string;
    buildDir?: string;
    configureArgs?: string[];
    buildArgs?: string[];
    /**
     * Multi-binary CMake projects: list of target names to build. The resolver
     * invokes `cmake --build <buildDir> --target <name>` per entry with shared
     * flags. Per-target customization belongs in `CMakeLists.txt`, not here.
     *
     * Only valid when `cmake` is set; merged via array concat + dedup.
     */
    targets?: string[];
  };
}

/**
 * Options passed to `workspace.switch()` or the `workspace` prop of
 * `<EmceptionRun>` / `<Ide>`. Controls the VFS namespace used for a session.
 *
 * - `name` — unique workspace identifier. Used as the localStorage key.
 * - `seed` — initial file map written on first open (or always when
 *   `seedPolicy` is `'overwrite'`).
 * - `seedPolicy` — controls how `seed` is applied:
 *   - `'once'` (default) — only writes if the workspace is new.
 *   - `'overwrite'` — replaces seed files every time. Existing non-seed files
 *     are kept. Useful for live-reloading starter code from the host.
 *   - `'merge'` — adds missing seed files without overwriting existing ones.
 * - `mountPath` — virtual mount point inside the Emscripten FS (default `/home/user`).
 * - `build` — default build config. The student can override it at runtime.
 */
export interface WorkspaceOptions {
  name: string;
  seed?: WorkspaceSeed;
  seedPolicy?: 'once' | 'overwrite' | 'merge';
  mountPath?: string;
  build?: WorkspaceBuildConfig;
}

// I/O sinks/sources are runtime-agnostic; xterm shape lives in @emception/xterm.
export type StdinInput =
  | string
  | Uint8Array
  | AsyncIterable<string | Uint8Array>
  | ReadableStream<Uint8Array>
  | (() => string | Uint8Array | null | Promise<string | Uint8Array | null>)
  | 'none';

export type StdoutSink = 'capture' | WritableStream<Uint8Array> | ((chunk: Uint8Array) => void | Promise<void>) | 'none';

/**
 * Per-invocation options for {@link EmceptionAPI.run} and
 * {@link EmceptionAPI.compileAndRun}.
 *
 * - `stdin` — feed data to the child process stdin. Use `'none'` (default)
 *   for interactive programs that need a terminal, or pass a string / async
 *   iterable for batch runs.
 * - `stdout` / `stderr` — capture output (`'capture'`) or pipe to a writable
 *   stream / callback. The default is determined by the active TTY mode.
 * - `timeoutMs` — abort the run after this many milliseconds. Causes
 *   `ToolResult.timedOut` to be `true` and `exitCode` to be non-zero.
 * - `signal` — cancel the run early by aborting this signal.
 * - `workspace` — override the workspace for this single invocation.
 */
export interface RunOptions {
  cwd?: string;
  env?: Record<string, string>;
  stdin?: StdinInput;
  stdout?: StdoutSink;
  stderr?: StdoutSink;
  timeoutMs?: number;
  signal?: AbortSignal;
  workspace?: string;
}

export interface CompileOptions extends RunOptions {
  sources?: string[];
  build?: Partial<WorkspaceBuildConfig>;
  /** Legacy: appended to cflags. */
  flags?: string[];
}

/**
 * Result returned by every {@link EmceptionAPI.run} / {@link EmceptionAPI.compileAndRun}
 * invocation. The shape is the **runtime contract** for tool execution and is
 * relied on by the testing engine, doctor checks, and UI surfaces.
 *
 * Invariants enforced by adapters and asserted by {@link assertToolResult}:
 *
 * - `exitCode` is a finite integer. `0` means success; non-zero means the tool
 *   reported an error. Implementations that have no concept of an exit code
 *   (rare) MUST still synthesize one (`0` on success, `1` on error).
 * - `stdout` and `stderr` are always strings (never `undefined`). Use the
 *   empty string when the tool produced no output on a stream.
 * - `durationMs` is wall-clock execution time measured by the adapter, not
 *   by the caller. It MUST be `>= 0`.
 * - `timedOut` is `true` iff the tool was aborted because it exceeded
 *   `RunOptions.timeoutMs`. When `timedOut` is `true` the `signal` field
 *   SHOULD be set (e.g. `'SIGTERM'` or the abort reason name) and `exitCode`
 *   SHOULD reflect the abort (commonly non-zero).
 * - `signal` is set when execution was terminated by a signal (timeout,
 *   abort, or external kill). It is omitted on normal exit.
 *
 * Adapters MUST NOT throw to indicate a tool failure: a non-zero exit, a
 * crash, or a timeout is still a successful adapter call that returns a
 * `ToolResult` describing what happened. Adapters throw only for *adapter*
 * failures (missing tool, invalid argv, transport error).
 */
export interface ToolResult {
  exitCode: number;
  stdout: string;
  stderr: string;
  durationMs: number;
  timedOut: boolean;
  signal?: string;
}

/**
 * One test case in a {@link TestPlan}. Use the `kind` discriminant:
 *
 * - `'stdio'` — compile + run with supplied stdin, assert stdout/stderr/exit.
 * - `'stdio-file'` — like `'stdio'` but reads input/expected from VFS files.
 * - `'clang-query'` — run a Clang AST matcher without executing the binary.
 * - `'doctest'` — compile with doctest enabled and run the test binary.
 * - `'custom'` — arbitrary async function; return a {@link TestCaseResult}.
 */
export type TestCase =
  | { kind: 'stdio'; stdin?: string; expectedStdout: string | RegExp; expectedStderr?: string | RegExp; expectedExit?: number; name?: string }
  | { kind: 'stdio-file'; inFile: string; expectedOutFile: string; name?: string }
  | { kind: 'clang-query'; matcher: string; expect: 'found' | 'not-found' | { minCount: number }; name?: string }
  | { kind: 'doctest'; sourceFiles: string[]; name?: string }
  | { kind: 'custom'; run: (em: EmceptionAPI) => Promise<TestCaseResult>; name?: string };

/**
 * Full test plan executed by {@link EmceptionAPI.runTests}.
 *
 * `build` overrides the workspace's default build config for this plan (e.g.
 * to add GTest link flags, set a specific output binary name, etc.).
 * `cases` is the ordered list of test cases to run.
 * `timeoutMsPerCase` caps individual case runtime (default: 10 000 ms).
 * `redactHidden` strips hidden-file content from diagnostics returned to the
 * student when `true` (default: `false`).
 */
export interface TestPlan {
  build?: Partial<WorkspaceBuildConfig> & { sources?: string[]; output?: string };
  cases: TestCase[];
  timeoutMsPerCase?: number;
  redactHidden?: boolean;
}

/**
 * Aggregated result of a single test case run by the testing engine.
 *
 * `passed` is `true` iff exit code was 0 and all assertions held.
 * `diagnostic` is a human-readable failure explanation shown in the UI
 * (redacted when the case involves hidden files and `redactHidden` is set).
 */
export interface TestCaseResult {
  name: string;
  passed: boolean;
  durationMs: number;
  diagnostic?: string;
}

/**
 * Summary returned by {@link EmceptionAPI.runTests}.
 * `passed` + `failed` = `cases.length`.
 * `totalDurationMs` is the sum of all case durations.
 */
export interface TestReport {
  passed: number;
  failed: number;
  totalDurationMs: number;
  cases: TestCaseResult[];
}

/**
 * Live workspace API exposed via {@link EmceptionAPI.workspace}.
 *
 * Provides file I/O, build config management, workspace switching, and
 * zip import/export. All operations are async and operate on the virtual
 * FS mounted at the workspace's `mountPath`.
 */
export interface WorkspaceAPI {
  list(): Promise<string[]>;
  switch(name: string): Promise<void>;
  reset(name?: string): Promise<void>;
  readFile(path: string): Promise<Uint8Array | null>;
  writeFile(path: string, data: Uint8Array | string, meta?: Partial<FileEntry>): Promise<void>;
  listFiles(opts?: { includeHidden?: boolean; includeSolution?: boolean }): Promise<Array<{ path: string } & FileEntry>>;
  setVisibility(path: string, v: FileEntry['visibility']): Promise<void>;
  getBuild(): Promise<WorkspaceBuildConfig>;
  setBuild(build: WorkspaceBuildConfig): Promise<void>;
  exportZip(): Promise<Blob>;
  importZip(blob: Blob): Promise<void>;
}

/**
 * The primary API object returned by `createEmception()`. Provides:
 *
 * - `workspace` — live VFS + build config management.
 * - `run(cmd, argv, opts)` — invoke a tool (clang, ninja, python, …) and
 *   get a {@link ToolResult} with exit code, stdout, stderr, and timing.
 * - `compileAndRun(sources, opts)` — convenience: build then run `a.out`.
 * - `runTests(plan, opts)` — compile + execute a full test suite and get a
 *   structured {@link TestReport}.
 * - `on(event, listener)` — subscribe to typed runtime events.
 * - `dispose()` — terminate the worker and release all resources.
 *
 * @example
 * ```ts
 * const em = await createEmception({ manifestUrl: '/cdn/manifest.json', tty: 'none' });
 * await em.workspace.writeFile('main.cpp', 'int main() { return 0; }');
 * const { exitCode } = await em.compileAndRun();
 * console.log(exitCode); // 0
 * em.dispose();
 * ```
 */
export interface EmceptionAPI {
  workspace: WorkspaceAPI;
  run(cmd: string, argv?: string[], opts?: RunOptions): Promise<ToolResult>;
  compileAndRun(sourceOrFiles?: string | string[], opts?: CompileOptions): Promise<ToolResult>;
  runTests(plan: TestPlan, opts?: { signal?: AbortSignal }): Promise<TestReport>;
  /** Subscribe to a typed runtime event. Returns an unsubscribe function. */
  on<E extends EmceptionEventName>(event: E, listener: EmceptionEventListener<E>): Unsubscribe;
  dispose(): void;
}
