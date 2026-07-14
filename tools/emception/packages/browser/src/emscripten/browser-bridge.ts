/**
 * Browser bridge: the JS-side of _browser_bridge that CPython calls into.
 *
 * When CPython executes `_browser_bridge.system(cmd, cwd)`, the Emscripten
 * JS glue calls this function, which delegates to ToolRunner.system().
 *
 * This is registered with CPython via Emscripten's `-sEXTRA_EXPORTED_RUNTIME_METHODS`
 * or injected as a built-in module in CPython's frozen modules.
 */

import type { ToolRunner } from '../tool-runner.js';

/**
 * Create the _browser_bridge function table for injection into CPython.
 * Returns an object that maps Python function names to JS implementations.
 */
export function createBrowserBridge(runner: ToolRunner): BrowserBridge {
  return {
    system: async (command: string, cwd: string): Promise<[number, string, string]> => {
      const stdoutChunks: string[] = [];
      const stderrChunks: string[] = [];

      const parts = parseCommand(command);
      if (parts.length === 0) return [0, '', ''];

      const result = await runner.run(parts[0], parts, {
        cwd: cwd || undefined,
        onStdout: (t) => stdoutChunks.push(t),
        onStderr: (t) => stderrChunks.push(t),
      });

      return [result.exitCode, stdoutChunks.join('\n'), stderrChunks.join('\n')];
    },

    getenv: (name: string, defaultValue: string): string => {
      const env: Record<string, string> = {
        PATH: '/usr/bin',
        HOME: '/home/user',
        TMPDIR: '/tmp',
        EM_CONFIG: '/etc/emscripten.config',
        EMSCRIPTEN_ROOT: '/usr/lib/emscripten',
        LLVM_ROOT: '/usr/bin',
        BINARYEN_ROOT: '/usr',
        CMAKE_ROOT: '/usr/share/cmake-4.3',
      };
      return env[name] ?? defaultValue;
    },

    which: (name: string): string | null => {
      const knownTools = [
        'clang', 'clang++', 'lld', 'wasm-ld', 'llvm-nm', 'llvm-ar',
        'llvm-objcopy', 'llc', 'wasm-opt', 'wasm-as', 'wasm-emscripten-finalize',
        'python3', 'emcc', 'em++',
        'ninja', 'cmake', 'curl',
      ];
      if (knownTools.includes(name)) return `/usr/bin/${name}`;
      return null;
    },
  };
}

export interface BrowserBridge {
  system(command: string, cwd: string): Promise<[number, string, string]>;
  getenv(name: string, defaultValue: string): string;
  which(name: string): string | null;
}

function parseCommand(cmd: string): string[] {
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
      if (current) { tokens.push(current); current = ''; }
    } else {
      current += ch;
    }
  }
  if (current) tokens.push(current);
  return tokens;
}
