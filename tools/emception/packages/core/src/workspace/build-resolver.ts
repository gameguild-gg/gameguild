/**
 * Build config resolver (Phase 3.5 / Phase 4 prep).
 *
 * Resolves a final, flat `WorkspaceBuildConfig` from three optional layers,
 * lowest precedence first:
 *
 *   1. Preset defaults (e.g. `cpp` -> `clang++ -std=c++20`).
 *   2. Workspace persisted config (`.emception/build.json`).
 *   3. Call-site overrides (`compileAndRun({ build, flags, sources })`).
 *
 * Merge rules:
 *   - Arrays concat + dedup (preserve insertion order).
 *   - Scalars overwrite (later wins).
 *   - Records (`defines`, `env`, `cmake`) merge by key, later wins.
 *
 * Throws `BuildConfigError` (re-exported from @emception/core/errors) on
 * impossible combos (e.g. cmake + sources both set).
 */

import { BuildConfigError } from '../errors';
import { PRESETS, type PresetName } from '../presets';
import type { WorkspaceBuildConfig } from '../types';

export interface ResolveBuildInput {
  preset?: PresetName;
  workspace?: WorkspaceBuildConfig;
  callsite?: Partial<WorkspaceBuildConfig> & { flags?: string[] };
}

export type ResolvedBuild = WorkspaceBuildConfig;

function dedup<T>(arr: T[]): T[] {
  const seen = new Set<T>();
  const out: T[] = [];
  for (const v of arr) {
    if (!seen.has(v)) {
      seen.add(v);
      out.push(v);
    }
  }
  return out;
}

function mergeArrays(a?: string[], b?: string[]): string[] | undefined {
  if (!a && !b) return undefined;
  return dedup([...(a ?? []), ...(b ?? [])]);
}

function mergeRecord<V>(a?: Record<string, V>, b?: Record<string, V>): Record<string, V> | undefined {
  if (!a && !b) return undefined;
  return { ...(a ?? {}), ...(b ?? {}) };
}

function mergePair(base: WorkspaceBuildConfig, layer: WorkspaceBuildConfig): WorkspaceBuildConfig {
  return {
    compiler: layer.compiler ?? base.compiler,
    std: layer.std ?? base.std,
    cflags: mergeArrays(base.cflags, layer.cflags),
    cxxflags: mergeArrays(base.cxxflags, layer.cxxflags),
    ldflags: mergeArrays(base.ldflags, layer.ldflags),
    defines: mergeRecord(base.defines, layer.defines),
    includePaths: mergeArrays(base.includePaths, layer.includePaths),
    libPaths: mergeArrays(base.libPaths, layer.libPaths),
    libs: mergeArrays(base.libs, layer.libs),
    sources: mergeArrays(base.sources, layer.sources),
    output: layer.output ?? base.output,
    env: mergeRecord(base.env, layer.env),
    cmake:
      base.cmake || layer.cmake
        ? {
            sourceDir: layer.cmake?.sourceDir ?? base.cmake?.sourceDir,
            buildDir: layer.cmake?.buildDir ?? base.cmake?.buildDir,
            configureArgs: mergeArrays(base.cmake?.configureArgs, layer.cmake?.configureArgs),
            buildArgs: mergeArrays(base.cmake?.buildArgs, layer.cmake?.buildArgs),
          }
        : undefined,
  };
}

/** Resolve the final build config given the three optional layers. */
export function resolveBuild(input: ResolveBuildInput): ResolvedBuild {
  const presetBuild = input.preset ? PRESETS[input.preset].build : {};
  let merged = mergePair(presetBuild, input.workspace ?? {});

  if (input.callsite) {
    const { flags, ...callsite } = input.callsite;
    merged = mergePair(merged, callsite as WorkspaceBuildConfig);
    if (flags && flags.length > 0) {
      // Legacy `flags` is appended to cflags (per WorkspaceBuildConfig docs).
      merged.cflags = dedup([...(merged.cflags ?? []), ...flags]);
    }
  }

  validate(merged);
  return merged;
}

function validate(b: WorkspaceBuildConfig): void {
  if (b.cmake && b.sources && b.sources.length > 0) {
    throw new BuildConfigError(
      'resolveBuild: cannot combine `cmake` with `sources` — pick one (CMake workspace OR direct source list).',
    );
  }
  if (b.compiler && !['clang', 'clang++', 'emcc', 'em++'].includes(b.compiler)) {
    throw new BuildConfigError(`resolveBuild: unknown compiler '${b.compiler}'.`);
  }
}
