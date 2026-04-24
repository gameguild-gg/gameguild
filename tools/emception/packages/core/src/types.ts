// Public type surface for emception. Phase 0 placeholder shapes; refined in later phases.

import type { EmceptionEventListener, EmceptionEventName, Unsubscribe } from './events';

export interface FileEntry {
    content: string | Uint8Array;
    visibility?: 'public' | 'hidden' | 'solution';
    readonly?: boolean;
    executable?: boolean;
}

export type WorkspaceSeed = Record<string, FileEntry | string>;

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
    };
}

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

export type StdoutSink =
    | 'capture'
    | WritableStream<Uint8Array>
    | ((chunk: Uint8Array) => void | Promise<void>)
    | 'none';

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

export interface ToolResult {
    exitCode: number;
    stdout: string;
    stderr: string;
    durationMs: number;
    timedOut: boolean;
    signal?: string;
}

export type TestCase =
    | { kind: 'stdio'; stdin?: string; expectedStdout: string | RegExp; expectedStderr?: string | RegExp; expectedExit?: number; name?: string }
    | { kind: 'stdio-file'; inFile: string; expectedOutFile: string; name?: string }
    | { kind: 'clang-query'; matcher: string; expect: 'found' | 'not-found' | { minCount: number }; name?: string }
    | { kind: 'doctest'; sourceFiles: string[]; name?: string }
    | { kind: 'custom'; run: (em: EmceptionAPI) => Promise<TestCaseResult>; name?: string };

export interface TestPlan {
    build?: Partial<WorkspaceBuildConfig> & { sources?: string[]; output?: string };
    cases: TestCase[];
    timeoutMsPerCase?: number;
    redactHidden?: boolean;
}

export interface TestCaseResult {
    name: string;
    passed: boolean;
    durationMs: number;
    diagnostic?: string;
}

export interface TestReport {
    passed: number;
    failed: number;
    totalDurationMs: number;
    cases: TestCaseResult[];
}

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

export interface EmceptionAPI {
    workspace: WorkspaceAPI;
    run(cmd: string, argv?: string[], opts?: RunOptions): Promise<ToolResult>;
    compileAndRun(sourceOrFiles?: string | string[], opts?: CompileOptions): Promise<ToolResult>;
    runTests(plan: TestPlan, opts?: { signal?: AbortSignal }): Promise<TestReport>;
    /** Subscribe to a typed runtime event. Returns an unsubscribe function. */
    on<E extends EmceptionEventName>(event: E, listener: EmceptionEventListener<E>): Unsubscribe;
    dispose(): void;
}
