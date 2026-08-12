/**
 * Minimal shell (Option A): TypeScript REPL that dispatches to the tool runner.
 * Supports history, tab completion, and filesystem builtins.
 */

import type { IOProvider } from 'emception';
import type { ToolRunner } from './tool-runner.js';
import type { VFSManager } from './vfs/index.js';

export class MiniShell {
  private runner: ToolRunner;
  private tty: IOProvider;
  private vfs: VFSManager | null;
  private cwd = '/home/user';
  private env: Record<string, string> = {
    PATH: '/usr/bin',
    HOME: '/home/user',
    TERM: 'xterm-256color',
  };
  private history: string[] = [];
  private historyIndex = -1;
  private readonly MAX_HISTORY = 200;

  constructor(runner: ToolRunner, tty: IOProvider, vfs?: VFSManager) {
    this.runner = runner;
    this.tty = tty;
    this.vfs = vfs ?? null;
  }

  async start(): Promise<void> {
    this.tty.writeLine('Browser Toolchain Shell');
    this.tty.writeLine('Type "help" for available commands.\n');

    while (true) {
      this.tty.write(`\x1b[32m${this.cwd}\x1b[0m $ `);
      const line = await this.readLine();
      if (!line.trim()) continue;
      this.addHistory(line.trim());
      const exitCode = await this.execute(line.trim());
      if (exitCode === -1) return;
    }
  }

  private async execute(input: string): Promise<number> {
    const [cmd, ...args] = this.parseCommand(input);
    const P = '[Emception:Shell]';

    switch (cmd) {
      case 'cd':
        return this.builtinCd(args);
      case 'pwd':
        this.tty.writeLine(this.cwd);
        return 0;
      case 'env':
        for (const [k, v] of Object.entries(this.env)) {
          this.tty.writeLine(`${k}=${v}`);
        }
        return 0;
      case 'echo':
        this.tty.writeLine(args.join(' '));
        return 0;
      case 'export': {
        const [key, ...val] = (args[0] ?? '').split('=');
        if (key) this.env[key] = val.join('=').trim();
        return 0;
      }
      case 'ls':
        return this.builtinLs(args);
      case 'cat':
        return this.builtinCat(args);
      case 'mkdir':
        return this.builtinMkdir(args);
      case 'rm':
        return this.builtinRm(args);
      case 'touch':
        return this.builtinTouch(args);
      case 'write':
        return this.builtinWrite(args);
      case 'history':
        for (let i = 0; i < this.history.length; i++) {
          this.tty.writeLine(`  ${i + 1}  ${this.history[i]}`);
        }
        return 0;
      case 'clear':
        this.tty.clear();
        return 0;
      case 'curl':
      case 'wget':
        return this.builtinCurl(args);
      case 'help':
        this.tty.writeLine('Built-in: cd, pwd, export, env, echo, ls, cat, mkdir, rm, touch, write, curl, wget, history, clear, help, exit');
        this.tty.writeLine('Tools: emcc, em++, clang, clang++, python3, wasm-opt, ninja, cmake');
        return 0;
      case 'exit':
        return -1;
      default: {
        // If the command is a .wasm file (e.g. ./main.wasm or main.wasm),
        // run it with the built-in WASI runtime.
        const cmdName = cmd.replace(/^\.\//, '');
        if (cmdName.endsWith('.wasm')) {
          const interactiveTty = this.tty as IOProvider & {
            enterExclusiveStdin?: () => void;
            exitExclusiveStdin?: () => void;
            readByteExclusive?: () => number | null | Promise<number>;
            supportsSynchronousExclusiveStdin?: boolean;
          };
          const wasmPath = this.resolvePath(cmdName);
          console.log(`${P} Running WASM binary: ${wasmPath}`);
          const t0 = performance.now();
          interactiveTty.enterExclusiveStdin?.();
          interactiveTty.setStdinEcho?.(true);
          let result;
          try {
            result = await this.runner.run('wasi-run', ['wasi-run', wasmPath, ...args], {
              env: this.env,
              cwd: this.cwd,
              onStdout: (t) => this.tty.write(t.replace(/\n/g, '\r\n')),
              onStderr: (t) => this.tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`),
              stdin: interactiveTty.supportsSynchronousExclusiveStdin && interactiveTty.readByteExclusive
                ? () => interactiveTty.readByteExclusive!()
                : () => -1,
            });
          } finally {
            interactiveTty.setStdinEcho?.(false);
            interactiveTty.exitExclusiveStdin?.();
          }
          console.log(`${P} WASM "${wasmPath}" finished: exitCode=${result.exitCode} in ${(performance.now() - t0).toFixed(1)}ms`);
          if (result.exitCode !== 0) {
            this.tty.writeError(`Exit code: ${result.exitCode}`);
          }
          return result.exitCode;
        }

        console.log(`${P} Executing external tool: ${cmd} [${[cmd, ...args].map(a => `"${a}"`).join(', ')}]`);
        const t0 = performance.now();
        const result = await this.runner.run(cmd, [cmd, ...args], {
          env: this.env,
          cwd: this.cwd,
          onStdout: (t) => this.tty.writeLine(t),
          onStderr: (t) => this.tty.writeError(t),
        });
        console.log(`${P} Tool "${cmd}" finished: exitCode=${result.exitCode} in ${(performance.now() - t0).toFixed(1)}ms`);
        if (result.exitCode !== 0) {
          this.tty.writeError(`Exit code: ${result.exitCode}`);
        }
        return result.exitCode;
      }
    }
  }

  private resolvePath(p: string): string {
    if (p.startsWith('/')) return this.normalizePath(p);
    return this.normalizePath(this.cwd + '/' + p);
  }

  private normalizePath(path: string): string {
    const parts = path.split('/').filter((s) => s && s !== '.');
    const result: string[] = [];
    for (const part of parts) {
      if (part === '..') result.pop();
      else result.push(part);
    }
    return '/' + result.join('/');
  }

  private builtinCd(args: string[]): number {
    const target = args[0] || (this.env.HOME ?? '/home/user');
    this.cwd = this.resolvePath(target);
    return 0;
  }

  private async builtinLs(args: string[]): Promise<number> {
    if (!this.vfs) {
      this.tty.writeError('ls: VFS not available');
      return 1;
    }
    const dir = args[0] ? this.resolvePath(args[0]) : this.cwd;
    try {
      const entries = await this.vfs.overlay.readdir(dir);
      if (entries.length === 0) {
        this.tty.writeLine('(empty)');
      } else {
        const sorted = [...entries].sort();
        this.tty.writeLine(sorted.join('  '));
      }
      return 0;
    } catch (e) {
      this.tty.writeError(`ls: ${e instanceof Error ? e.message : String(e)}`);
      return 1;
    }
  }

  private async builtinCat(args: string[]): Promise<number> {
    if (!this.vfs) {
      this.tty.writeError('cat: VFS not available');
      return 1;
    }
    if (args.length === 0) {
      this.tty.writeError('cat: missing file operand');
      return 1;
    }
    for (const arg of args) {
      const path = this.resolvePath(arg);
      const data = await this.vfs.overlay.readFile(path);
      if (data === null) {
        this.tty.writeError(`cat: ${arg}: No such file`);
        return 1;
      }
      this.tty.writeLine(new TextDecoder().decode(data));
    }
    return 0;
  }

  private async builtinMkdir(args: string[]): Promise<number> {
    if (!this.vfs) {
      this.tty.writeError('mkdir: VFS not available');
      return 1;
    }
    if (args.length === 0) {
      this.tty.writeError('mkdir: missing operand');
      return 1;
    }
    for (const arg of args) {
      const path = this.resolvePath(arg);
      const ok = await this.vfs.overlay.mkdir(path);
      if (!ok) {
        this.tty.writeError(`mkdir: cannot create directory '${arg}'`);
      }
    }
    return 0;
  }

  private async builtinRm(args: string[]): Promise<number> {
    if (!this.vfs) {
      this.tty.writeError('rm: VFS not available');
      return 1;
    }
    if (args.length === 0) {
      this.tty.writeError('rm: missing operand');
      return 1;
    }
    for (const arg of args) {
      const path = this.resolvePath(arg);
      const ok = await this.vfs.overlay.deleteFile(path);
      if (!ok) {
        this.tty.writeError(`rm: cannot remove '${arg}'`);
      }
    }
    return 0;
  }

  private async builtinTouch(args: string[]): Promise<number> {
    if (!this.vfs) {
      this.tty.writeError('touch: VFS not available');
      return 1;
    }
    if (args.length === 0) {
      this.tty.writeError('touch: missing operand');
      return 1;
    }
    for (const arg of args) {
      const path = this.resolvePath(arg);
      const existing = await this.vfs.overlay.readFile(path);
      if (existing === null) {
        await this.vfs.overlay.writeFile(path, new Uint8Array(0));
      }
    }
    return 0;
  }

  /**
   * write <file> <content> — write text content to a file.
   */
  private async builtinWrite(args: string[]): Promise<number> {
    if (!this.vfs) {
      this.tty.writeError('write: VFS not available');
      return 1;
    }
    if (args.length < 2) {
      this.tty.writeError('Usage: write <file> <content>');
      return 1;
    }
    const path = this.resolvePath(args[0]);
    const content = args.slice(1).join(' ');
    await this.vfs.overlay.writeFile(path, new TextEncoder().encode(content + '\n'));
    return 0;
  }

  /**
   * curl/wget builtin — fetch a URL using the browser fetch API.
   * Supports: curl [-o file] [-s] URL
   */
  private async builtinCurl(args: string[]): Promise<number> {
    let outputFile: string | null = null;
    let silent = false;
    let url: string | null = null;

    for (let i = 0; i < args.length; i++) {
      if (args[i] === '-o' && i + 1 < args.length) {
        outputFile = args[++i];
      } else if (args[i] === '-O') {
        // Save with remote filename
        outputFile = '__auto__';
      } else if (args[i] === '-s' || args[i] === '--silent' || args[i] === '-q') {
        silent = true;
      } else if (args[i] === '-L' || args[i] === '--location') {
        // Follow redirects — fetch does this by default, ignore
      } else if (!args[i].startsWith('-')) {
        url = args[i];
      }
    }

    if (!url) {
      this.tty.writeError('Usage: curl [-o file] [-s] URL');
      return 1;
    }

    // Derive filename from URL for -O
    if (outputFile === '__auto__') {
      const urlPath = new URL(url).pathname;
      outputFile = urlPath.split('/').pop() || 'download';
    }

    if (!silent) {
      this.tty.writeLine(`Fetching ${url}...`);
    }

    try {
      const response = await fetch(url, { mode: 'cors', redirect: 'follow' });
      if (!response.ok) {
        this.tty.writeError(`curl: (${response.status}) ${response.statusText}`);
        return 1;
      }
      const data = new Uint8Array(await response.arrayBuffer());

      if (outputFile && this.vfs) {
        const path = this.resolvePath(outputFile);
        await this.vfs.overlay.writeFile(path, data);
        if (!silent) {
          this.tty.writeLine(`Saved ${data.length} bytes to ${outputFile}`);
        }
      } else {
        // Print text to terminal
        const text = new TextDecoder().decode(data);
        this.tty.writeLine(text);
      }
      return 0;
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      if (msg.includes('Failed to fetch') || msg.includes('NetworkError') || msg.includes('CORS')) {
        this.tty.writeError(`curl: CORS blocked by ${new URL(url).hostname}`);
        this.tty.writeError('  The remote server does not allow cross-origin requests from the browser.');
        this.tty.writeError('  Try a CORS-friendly API (e.g. httpbin.org, jsonplaceholder.typicode.com).');
      } else {
        this.tty.writeError(`curl: ${msg}`);
      }
      return 1;
    }
  }

  private addHistory(line: string): void {
    if (this.history.length > 0 && this.history[this.history.length - 1] === line) {
      return;
    }
    this.history.push(line);
    if (this.history.length > this.MAX_HISTORY) {
      this.history.shift();
    }
    this.historyIndex = -1;
  }

  private async readLine(): Promise<string> {
    let line = '';
    let cursor = 0;
    this.historyIndex = -1;
    let savedLine = '';

    while (true) {
      const b = this.tty.readByte();
      const byte =
        b === null
          ? null
          : typeof (b as Promise<number>).then === 'function'
            ? await (b as Promise<number>)
            : (b as number);
      if (byte === null) continue;

      // CR / LF => submit
      if (byte === 13 || byte === 10) {
        this.tty.writeLine('');
        return line;
      }

      // Backspace
      if (byte === 127 || byte === 8) {
        if (cursor > 0) {
          line = line.slice(0, cursor - 1) + line.slice(cursor);
          cursor--;
          this.tty.write('\b \b');
        }
        continue;
      }

      // Escape sequences (arrow keys)
      if (byte === 27) {
        const b2 = this.tty.readByte();
        const next = typeof (b2 as Promise<number>).then === 'function' ? await (b2 as Promise<number>) : (b2 as number);
        if (next === 91) {
          const b3 = this.tty.readByte();
          const code = typeof (b3 as Promise<number>).then === 'function' ? await (b3 as Promise<number>) : (b3 as number);
          if (code === 65) {
            // Up arrow — history previous
            if (this.history.length > 0) {
              if (this.historyIndex === -1) {
                savedLine = line;
                this.historyIndex = this.history.length - 1;
              } else if (this.historyIndex > 0) {
                this.historyIndex--;
              }
              this.clearLine(line);
              line = this.history[this.historyIndex];
              cursor = line.length;
              this.tty.write(line);
            }
          } else if (code === 66) {
            // Down arrow — history next
            if (this.historyIndex !== -1) {
              if (this.historyIndex < this.history.length - 1) {
                this.historyIndex++;
                this.clearLine(line);
                line = this.history[this.historyIndex];
              } else {
                this.historyIndex = -1;
                this.clearLine(line);
                line = savedLine;
              }
              cursor = line.length;
              this.tty.write(line);
            }
          }
          // Left/Right arrows ignored for simplicity
        }
        continue;
      }

      // Tab — attempt completion
      if (byte === 9) {
        const completed = this.tabComplete(line);
        if (completed && completed !== line) {
          this.clearLine(line);
          line = completed;
          cursor = line.length;
          this.tty.write(line);
        }
        continue;
      }

      // Printable character
      if (byte >= 32) {
        const ch = String.fromCharCode(byte);
        line = line.slice(0, cursor) + ch + line.slice(cursor);
        cursor++;
        // Re-render visible part (simple: just echo the char)
        this.tty.write(ch);
      }
    }
  }

  private clearLine(line: string): void {
    for (let i = 0; i < line.length; i++) {
      this.tty.write('\b \b');
    }
  }

  private tabComplete(line: string): string | null {
    const parts = line.split(/\s+/);
    const prefix = parts[parts.length - 1] ?? '';
    if (!prefix) return null;

    if (parts.length === 1) {
      // Complete command names
      const commands = [
        'cd', 'pwd', 'env', 'echo', 'export', 'ls', 'cat', 'mkdir', 'rm',
        'touch', 'write', 'curl', 'wget', 'history', 'clear', 'help', 'exit',
        'clang', 'clang++', 'emcc', 'em++', 'python3', 'wasm-opt',
        'lld', 'wasm-ld', 'llvm-nm', 'llvm-ar', 'llvm-objcopy', 'llc',
        'ninja', 'cmake',
      ];
      const matches = commands.filter((c) => c.startsWith(prefix));
      if (matches.length === 1) {
        return matches[0] + ' ';
      }
      if (matches.length > 1) {
        this.tty.writeLine('');
        this.tty.writeLine(matches.join('  '));
        return line;
      }
    }

    return null;
  }

  private parseCommand(cmd: string): string[] {
    const tokens: string[] = [];
    let current = '';
    let inQuote: string | null = null;
    for (const ch of cmd) {
      if (inQuote) {
        if (ch === inQuote) inQuote = null;
        else current += ch;
      } else if (ch === '"' || ch === "'") {
        inQuote = ch;
      } else if (ch === ' ' || ch === '\t') {
        if (current) {
          tokens.push(current);
          current = '';
        }
      } else {
        current += ch;
      }
    }
    if (current) tokens.push(current);
    return tokens;
  }
}
