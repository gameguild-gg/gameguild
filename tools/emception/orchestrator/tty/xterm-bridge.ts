/**
 * TTY bridge: xterm.js <-> stdin/stdout for WASM modules.
 */

import { Terminal } from '@xterm/xterm';

export class TTYBridge {
  private terminal: Terminal;
  private inputBuffer: number[] = [];
  private inputResolvers: Array<(byte: number) => void> = [];

  constructor(container: HTMLElement) {
    this.terminal = new Terminal({
      cols: 120,
      rows: 40,
      fontFamily: '"Fira Code", monospace',
      fontSize: 14,
      theme: { background: '#1e1e2e' },
    });
    this.terminal.open(container);
    this.terminal.onData((data: string) => {
      for (let i = 0; i < data.length; i++) {
        const byte = data.charCodeAt(i);
        if (this.inputResolvers.length > 0) {
          this.inputResolvers.shift()!(byte);
        } else {
          this.inputBuffer.push(byte);
        }
      }
    });
  }

  /**
   * Called by Emscripten's stdin hook. Returns one byte or a Promise that resolves with a byte.
   */
  readByte(): number | null | Promise<number> {
    if (this.inputBuffer.length > 0) {
      return this.inputBuffer.shift()!;
    }
    return new Promise<number>((resolve) => {
      this.inputResolvers.push(resolve);
    });
  }

  writeLine(text: string): void {
    this.terminal.writeln(text);
  }

  write(text: string): void {
    this.terminal.write(text);
  }

  writeError(text: string): void {
    this.terminal.writeln(`\x1b[31m${text}\x1b[0m`);
  }

  clear(): void {
    this.terminal.clear();
  }
}
