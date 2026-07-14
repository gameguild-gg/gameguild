/**
 * Typed event surface for `EmceptionAPI.on()`.
 *
 * Each event has a precise payload shape. Hosts get IntelliSense for the
 * event name AND the listener parameter. Implementations should emit a
 * matching payload — see `EmceptionAPI.on()` overload in types.ts.
 */

/** Bundle / manifest loading progress. */
export interface ProgressEvent {
    /** Human-readable phase, e.g. 'manifest', 'bundle', 'sysroot'. */
    phase: string;
    /** Bytes / units loaded so far (when known). */
    loaded?: number;
    /** Total bytes / units expected (when known). */
    total?: number;
    /** Optional logical name of the asset being loaded. */
    asset?: string;
}

/** Fired exactly once when the runtime is ready to accept commands. */
export interface ReadyEvent {
    /** Wall-clock ms from instantiation to ready. */
    bootMs: number;
    /** Active manifest version (if any). */
    manifestVersion?: string;
}

/** Fired each time a sysroot bundle finishes loading. */
export interface BundleLoadedEvent {
    bundle: string;
    /** Decompressed bytes copied into the in-worker FS. */
    bytes: number;
    /** Wall-clock ms spent fetching + extracting. */
    durationMs: number;
}

/** Stdout chunk from a running tool. */
export interface StdoutEvent {
    /** Owning tool / pipeline id, when available. */
    source?: string;
    chunk: Uint8Array;
}

/** Stderr chunk from a running tool. */
export interface StderrEvent {
    source?: string;
    chunk: Uint8Array;
}

/** A tool invocation finished. */
export interface ExitEvent {
    source?: string;
    exitCode: number;
    durationMs: number;
    signal?: string;
}

/** A single test case completed (during runTests). */
export interface TestCaseEvent {
    name: string;
    passed: boolean;
    durationMs: number;
    diagnostic?: string;
}

/** Aggregated test report emitted at the end of `runTests`. */
export interface TestReportEvent {
    passed: number;
    failed: number;
    totalDurationMs: number;
    /** Number of cases reported. Equivalent to `passed + failed`. */
    totalCases: number;
}

/** Map of event names to payload types. */
export interface EmceptionEventMap {
    progress: ProgressEvent;
    ready: ReadyEvent;
    'bundle-loaded': BundleLoadedEvent;
    stdout: StdoutEvent;
    stderr: StderrEvent;
    exit: ExitEvent;
    'test-case': TestCaseEvent;
    'test-report': TestReportEvent;
}

export type EmceptionEventName = keyof EmceptionEventMap;

export type EmceptionEventListener<E extends EmceptionEventName> = (event: EmceptionEventMap[E]) => void;

/** Returned by `on()` to unsubscribe. */
export type Unsubscribe = () => void;
