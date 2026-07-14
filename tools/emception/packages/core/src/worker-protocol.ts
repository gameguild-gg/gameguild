/**
 * Worker ↔ Main Thread message protocol.
 *
 * Defines the shape of every message that crosses the postMessage boundary.
 * Both worker-entry.ts and worker-client.ts import these types.
 */

/* ------------------------------------------------------------------ */
/*  Main → Worker messages                                             */
/* ------------------------------------------------------------------ */

/** Boot the toolchain inside the Worker. */
export interface BootMessage {
    type: 'boot';
    manifestUrl: string;
    /** Page origin (e.g. "http://localhost:3099") so the Worker can resolve relative URLs. */
    origin: string;
    /** Optional tool version overrides from the manifest. */
    toolVersions?: { pythonMajorMinor?: string; pythonMajorMinorCompact?: string };
}

/** Run a tool (emcc, clang, wasi-run, etc.). */
export interface RunMessage {
    type: 'run';
    id: number;
    tool: string;
    argv: string[];
    options: {
        env?: Record<string, string>;
        cwd?: string;
        wantStdin?: boolean;
        /** Opaque hints forwarded to ToolRunner (e.g. which CDN bundles are needed). */
        hints?: { bundlesNeeded?: string[] };
    };
}

/** A single stdin byte for an in-progress run that requested stdin. */
export interface StdinMessage {
    type: 'stdin';
    id: number;
    byte: number;
}

/** Read a file from the kernel VFS. */
export interface GetFileMessage {
    type: 'getFile';
    id: number;
    path: string;
}

/** Write a file to the kernel VFS overlay. */
export interface WriteFileMessage {
    type: 'writeFile';
    id: number;
    path: string;
    /** Transferred (not copied) for performance. */
    data: Uint8Array;
}

/** List directory contents. */
export interface ListDirMessage {
    type: 'listDir';
    id: number;
    path: string;
}

/** Reset VFS writable layers (clear /tmp, /home/user, overlay writes). */
export interface ResetVfsMessage {
    type: 'resetVfs';
    id: number;
}

export type MainToWorkerMessage =
    | BootMessage
    | RunMessage
    | StdinMessage
    | GetFileMessage
    | WriteFileMessage
    | ListDirMessage
    | ResetVfsMessage;

/* ------------------------------------------------------------------ */
/*  Worker → Main messages                                             */
/* ------------------------------------------------------------------ */

/** Boot completed successfully. */
export interface BootedMessage {
    type: 'booted';
}

/** Boot failed. */
export interface BootErrorMessage {
    type: 'bootError';
    error: string;
}

/** Incremental stdout from an in-progress run. */
export interface StdoutMessage {
    type: 'stdout';
    id: number;
    text: string;
}

/** Incremental stderr from an in-progress run. */
export interface StderrMessage {
    type: 'stderr';
    id: number;
    text: string;
}

/** Worker needs stdin bytes for a run (enables exclusive stdin on main). */
export interface StdinRequestMessage {
    type: 'stdinRequest';
    id: number;
    /** Shared ring-buffer control block: [readIndex, writeIndex, closed]. */
    controlBuffer: SharedArrayBuffer;
    /** Shared ring-buffer payload bytes. */
    dataBuffer: SharedArrayBuffer;
}

/** Shell needs a shared stdin channel for a foreground interactive WASI run. */
export interface ShellStdinRequestMessage {
    type: 'shellStdinRequest';
    /** Shared ring-buffer control block: [readIndex, writeIndex, closed]. */
    controlBuffer: SharedArrayBuffer;
    /** Shared ring-buffer payload bytes. */
    dataBuffer: SharedArrayBuffer;
}

/** A run completed. */
export interface RunResultMessage {
    type: 'runResult';
    id: number;
    exitCode: number;
    stdout: string;
    stderr: string;
}

/** getFile result. data is null when file not found. */
export interface GetFileResultMessage {
    type: 'getFileResult';
    id: number;
    /** Transferred (not copied). Null if file not found. */
    data: Uint8Array | null;
}

/** writeFile result. */
export interface WriteFileResultMessage {
    type: 'writeFileResult';
    id: number;
    ok: boolean;
    error?: string;
}

/** listDir result. */
export interface ListDirResultMessage {
    type: 'listDirResult';
    id: number;
    entries: string[];
}

/** resetVfs result. */
export interface ResetVfsResultMessage {
    type: 'resetVfsResult';
    id: number;
    ok: boolean;
    error?: string;
}

/** Shell output line (for when MiniShell runs in the Worker). */
export interface ShellOutputMessage {
    type: 'shellOutput';
    text: string;
}

/** Shell wants raw write (no newline). */
export interface ShellWriteMessage {
    type: 'shellWrite';
    text: string;
}

/** Shell wants to read a line from the terminal. */
export interface ShellReadRequest {
    type: 'shellReadByte';
}

/** Shell wants to clear terminal. */
export interface ShellClearMessage {
    type: 'shellClear';
}

/** Shell wants to set stdin echo mode. */
export interface ShellSetEchoMessage {
    type: 'shellSetEcho';
    enabled: boolean;
}

/** Shell wants exclusive stdin mode. */
export interface ShellExclusiveStdinMessage {
    type: 'shellExclusiveStdin';
    enter: boolean;
}

/** Forwarded console message from the Worker. */
export interface LogMessage {
    type: 'log';
    level: 'log' | 'warn' | 'error' | 'info' | 'debug';
    args: unknown[];
}

export type WorkerToMainMessage =
    | BootedMessage
    | BootErrorMessage
    | StdoutMessage
    | StderrMessage
    | StdinRequestMessage
    | ShellStdinRequestMessage
    | RunResultMessage
    | GetFileResultMessage
    | WriteFileResultMessage
    | ListDirResultMessage
    | ResetVfsResultMessage
    | ShellOutputMessage
    | ShellWriteMessage
    | ShellReadRequest
    | ShellClearMessage
    | ShellSetEchoMessage
    | ShellExclusiveStdinMessage
    | LogMessage;
