/**
 * Line buffer for stdin: accumulates bytes into lines and supports simple
 * backspace. Can be used by the TTY bridge or shell for line editing.
 */

export class LineBuffer {
  private line = '';
  private cursor = 0;

  reset(): void {
    this.line = '';
    this.cursor = 0;
  }

  getLine(): string {
    return this.line;
  }

  getCursor(): number {
    return this.cursor;
  }

  /** Process one byte; returns the line when CR/LF is received, otherwise null. */
  feed(byte: number): string | null {
    const ch = String.fromCharCode(byte);
    if (byte === 13 || byte === 10) {
      const result = this.line;
      this.reset();
      return result;
    }
    if (byte === 127 || byte === 8) {
      if (this.cursor > 0) {
        this.line = this.line.slice(0, this.cursor - 1) + this.line.slice(this.cursor);
        this.cursor--;
      }
      return null;
    }
    if (byte >= 32 || byte === 9) {
      this.line = this.line.slice(0, this.cursor) + ch + this.line.slice(this.cursor);
      this.cursor++;
    }
    return null;
  }

  /** Insert a string at the current cursor (e.g. paste). */
  insert(s: string): void {
    this.line = this.line.slice(0, this.cursor) + s + this.line.slice(this.cursor);
    this.cursor += s.length;
  }
}
