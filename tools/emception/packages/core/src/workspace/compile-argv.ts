/**
 * Compile argv builder.
 *
 * Pure helper: turns a `ResolvedBuild` into the (compiler, argv) pair that a
 * `RuntimeAdapter` would hand to its tool runner. Lives in `@emception/core`
 * so the same logic is reachable from the browser, Node, CLI, and test
 * harnesses without any DOM or Node-specific imports.
 *
 * Convention: this helper does NOT validate (that's `resolveBuild`'s job) and
 * it does NOT touch the filesystem. It only formats flags. Caller is
 * responsible for ensuring the source list is non-empty when invoking the
 * compiler.
 */

import { BuildConfigError } from '../errors';
import type { NativeBuildConfig } from '../types';

export interface CompileInvocation {
  /** The compiler binary name (`clang`, `clang++`, `emcc`, `em++`). */
  compiler: 'clang' | 'clang++' | 'emcc' | 'em++';
  /** Full argv to pass to the compiler, excluding the binary itself. */
  argv: string[];
  /** The output path (echoes `build.output`, defaulting to `a.out`). */
  output: string;
}

export interface BuildArgvOptions {
  /** Override the source list (e.g. when the caller already materialized
   * a single inline source to a temp path). Falls back to `build.sources`. */
  sources?: string[];
  /** Override the compiler choice (last word — beats `build.compiler`). */
  compiler?: CompileInvocation['compiler'];
}

/**
 * Build a `(compiler, argv, output)` triple from a resolved native build config.
 *
 * Order of flags (stable so snapshots stay diffable):
 *   1. `-D<key>[=value]` (sorted by key)
 *   2. `-I<path>` (preserve insertion order)
 *   3. `flags`
 *   4. sources (preserve insertion order)
 *   5. `-L<path>`, `-l<name>`
 *   6. `ldflags`
 *   7. `-o <output>`
 */
export function buildArgv(build: NativeBuildConfig, opts: BuildArgvOptions = {}): CompileInvocation {
  const compiler = opts.compiler ?? build.compiler;
  if (!compiler) {
    throw new BuildConfigError('buildArgv: no compiler resolved — set `build.compiler` or pass a preset.');
  }

  const sources = opts.sources ?? build.sources ?? [];
  const output = build.output ?? 'a.out';

  const argv: string[] = [];

  if (build.defines) {
    for (const key of Object.keys(build.defines).sort()) {
      const v = build.defines[key];
      argv.push(v === true ? `-D${key}` : `-D${key}=${v}`);
    }
  }

  for (const inc of build.includePaths ?? []) argv.push(`-I${inc}`);

  if (build.flags) argv.push(...build.flags);

  argv.push(...sources);

  for (const lp of build.libPaths ?? []) argv.push(`-L${lp}`);
  for (const lib of build.libs ?? []) argv.push(`-l${lib}`);

  if (build.ldflags) argv.push(...build.ldflags);

  argv.push('-o', output);

  return { compiler, argv, output };
}
