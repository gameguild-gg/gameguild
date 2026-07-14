// Typed tools surface.
//
// TOOL_REGISTRY enumerates every tool callable through em.run(). The ToolName
// union is derived from the registry so consumers get autocomplete and bad
// names become compile-time errors. The Tools interface gives a typed
// `em.tools.<tool>(args, opts)` shorthand that just delegates to em.run().
//
// The registry is a pure data table; no behavior, no DOM, no runtime deps —
// safe to import from any package and any environment.

import type { RunOptions, ToolResult } from './types.js';

/**
 * Metadata for a single tool that can be invoked through em.run / em.tools.
 * `bundles` lists which sysroot bundles must be preloaded before the tool is
 * usable — the runtime can use this to lazy-load on first invocation.
 */
export interface ToolDescriptor {
  /** The argv[0] passed to the worker. */
  name: string;
  /** Sysroot bundles required before this tool can run. */
  bundles: readonly string[];
  /** Short human description, surfaced by `emception doctor` and IDE pickers. */
  description: string;
}

export const TOOL_REGISTRY = {
  clang: {
    name: 'clang',
    bundles: ['llvm'],
    description: 'C compiler driver (LLVM/clang).',
  },
  'clang++': {
    name: 'clang++',
    bundles: ['llvm'],
    description: 'C++ compiler driver (LLVM/clang).',
  },
  'wasm-ld': {
    name: 'wasm-ld',
    bundles: ['llvm'],
    description: 'WebAssembly linker (LLVM lld).',
  },
  emcc: {
    name: 'emcc',
    bundles: ['llvm'],
    description: 'Emscripten C compiler driver.',
  },
  'em++': {
    name: 'em++',
    bundles: ['llvm'],
    description: 'Emscripten C++ compiler driver.',
  },
  cmake: {
    name: 'cmake',
    bundles: ['cmake'],
    description: 'Cross-platform build-system generator.',
  },
  ninja: {
    name: 'ninja',
    bundles: ['ninja'],
    description: 'Small, fast build system.',
  },
  python3: {
    name: 'python3',
    bundles: ['cpython'],
    description: 'CPython 3 interpreter.',
  },
} as const satisfies Record<string, ToolDescriptor>;

/** Union of every registered tool name. Drives autocomplete on em.tools.* */
export type ToolName = keyof typeof TOOL_REGISTRY;

/**
 * Typed shorthand surface. `em.tools.clang(['-c', 'main.c'])` is exactly
 * equivalent to `em.run('clang', ['-c', 'main.c'])` but with autocomplete on
 * the tool name. Implementations live in @emception/browser.
 */
export type Tools = {
  [K in ToolName]: (argv?: string[], opts?: RunOptions) => Promise<ToolResult>;
};

/**
 * Build a `Tools` object that delegates each property to a single
 * `run(name, argv, opts)` function. Runtime adapters use this to expose the
 * typed surface without hand-writing one wrapper per tool.
 */
export function createTools(run: (cmd: string, argv?: string[], opts?: RunOptions) => Promise<ToolResult>): Tools {
  const out = {} as Tools;
  for (const key of Object.keys(TOOL_REGISTRY) as ToolName[]) {
    out[key] = (argv?: string[], opts?: RunOptions) => run(key, argv, opts);
  }
  return out;
}
