/**
 * Pluggable I/O interface for stdin / stdout / stderr.
 *
 * Any component that needs to read user input or display output can depend
 * on this interface instead of a concrete terminal implementation.
 * TTYBridge is the default xterm.js-backed implementation.
 */
export interface IOProvider {
    /** Read a single byte from stdin. Returns immediately if buffered, otherwise blocks. */
    readByte(): number | null | Promise<number>;

    /** Write a line to stdout (appends newline). */
    writeLine(text: string): void;

    /** Write raw text to stdout (no trailing newline). */
    write(text: string): void;

    /** Write a line to stderr (typically styled red). */
    writeError(text: string): void;

    /** Clear all output. */
    clear(): void;

    /** Enable/disable local echo of stdin input (for interactive programs). */
    setStdinEcho?(enabled: boolean): void;
}
