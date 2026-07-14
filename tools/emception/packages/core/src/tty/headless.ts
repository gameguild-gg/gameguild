/**
 * Headless IOProvider — no terminal, no DOM.
 *
 * Used by `createEmception({ tty: 'none' })` and by the Node runtime
 * (headless graders) when no interactive console is wanted. Stdout / stderr
 * fan out to optional sink callbacks; input always returns EOF.
 *
 * Lives in @emception/core because both browser and node need it and it has
 * zero platform dependencies.
 */

import type { IOProvider } from './io-provider';

export interface HeadlessIOProviderOptions {
  /** Called for every `write()` / `writeLine()` chunk. Default: no-op. */
  onStdout?: (text: string) => void;
  /** Called for every `writeError()` chunk. Default: no-op. */
  onStderr?: (text: string) => void;
}

export class HeadlessIOProvider implements IOProvider {
  readonly supportsSynchronousExclusiveStdin = false;

  private readonly onStdout: (text: string) => void;
  private readonly onStderr: (text: string) => void;

  constructor(opts: HeadlessIOProviderOptions = {}) {
    this.onStdout = opts.onStdout ?? (() => {});
    this.onStderr = opts.onStderr ?? (() => {});
  }

  /** Always returns EOF — no interactive input on a headless TTY. */
  readByte(): number | null {
    return null;
  }

  write(text: string): void {
    this.onStdout(text);
  }

  writeLine(text: string): void {
    this.onStdout(text + '\n');
  }

  writeError(text: string): void {
    this.onStderr(text + '\n');
  }

  clear(): void {
    // no-op: nothing to clear in a headless sink
  }

  setStdinEcho(_enabled: boolean): void {
    // no-op: no terminal to echo to
  }

  enterExclusiveStdin(): void {
    // no-op
  }

  exitExclusiveStdin(): void {
    // no-op
  }

  readByteExclusive(): number | null {
    return null;
  }
}
