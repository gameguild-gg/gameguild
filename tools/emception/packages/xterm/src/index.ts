// @gameguild/emception-xterm — adapter between xterm.js Terminal and emception streams.

import type { Terminal } from '@xterm/xterm';
import type { StdinInput, StdoutSink } from 'emception';

export { TTYBridge } from './bridge';

export interface XtermStdin {
    xterm: Terminal;
    /** Raw mode disables line buffering + local echo. Default false. */
    raw?: boolean;
}

export interface XtermStdout {
    xterm: Terminal;
}

/** Adapter shorthand normalized to a real WHATWG ReadableStream by emception. */
export function fromXterm(xterm: Terminal, opts?: { raw?: boolean }): XtermStdin {
    return { xterm, raw: opts?.raw ?? false };
}

export function toXterm(xterm: Terminal): XtermStdout {
    return { xterm };
}

// Allow mixing xterm shorthand into core StdinInput / StdoutSink unions.
export type XtermStdinInput = StdinInput | XtermStdin;
export type XtermStdoutSink = StdoutSink | XtermStdout;
