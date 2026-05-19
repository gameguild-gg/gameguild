/**
 * Build config resolver.
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

import { BUILD_PRESETS, type BuildPresetName } from '../build-presets';
import { BuildConfigError } from '../errors';
import type { CMakeBuildConfig, NativeBuildConfig, PythonBuildConfig, WorkspaceBuildConfig } from '../types';

export interface ResolveBuildInput {
  preset?: BuildPresetName;
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

function mergeLayer(base: WorkspaceBuildConfig, layer: Partial<WorkspaceBuildConfig>): WorkspaceBuildConfig {
  if (layer.kind !== undefined && layer.kind !== base.kind) {
    throw new BuildConfigError(
      `resolveBuild: cannot merge build configs of different kinds ('${base.kind}' and '${layer.kind}'). Use the same kind for workspace and callsite overrides.`,
    );
  }
  switch (base.kind) {
    case 'native': {
      const l = layer as Partial<NativeBuildConfig>;
      return {
        kind: 'native',
        compiler: l.compiler ?? base.compiler,
        std: l.std ?? base.std,
        cflags: mergeArrays(base.cflags, l.cflags),
        cxxflags: mergeArrays(base.cxxflags, l.cxxflags),
        ldflags: mergeArrays(base.ldflags, l.ldflags),
        defines: mergeRecord(base.defines, l.defines),
        includePaths: mergeArrays(base.includePaths, l.includePaths),
        libPaths: mergeArrays(base.libPaths, l.libPaths),
        libs: mergeArrays(base.libs, l.libs),
        sources: mergeArrays(base.sources, l.sources),
        output: l.output ?? base.output,
        env: mergeRecord(base.env, l.env),
      };
    }
    case 'cmake': {
      const l = layer as Partial<CMakeBuildConfig>;
      return {
        kind: 'cmake',
        cmake:
          base.cmake || l.cmake
            ? {
                sourceDir: l.cmake?.sourceDir ?? base.cmake?.sourceDir,
                buildDir: l.cmake?.buildDir ?? base.cmake?.buildDir,
                configureArgs: mergeArrays(base.cmake?.configureArgs, l.cmake?.configureArgs),
                buildArgs: mergeArrays(base.cmake?.buildArgs, l.cmake?.buildArgs),
                targets: mergeArrays(base.cmake?.targets, l.cmake?.targets),
              }
            : undefined,
        env: mergeRecord(base.env, l.env),
      };
    }
    case 'python': {
      const l = layer as Partial<PythonBuildConfig>;
      return { kind: 'python', env: mergeRecord(base.env, l.env) };
    }
  }
}

/** Resolve the final build config given the three optional layers. */
export function resolveBuild(input: ResolveBuildInput): ResolvedBuild {
  const presetBuild: WorkspaceBuildConfig = input.preset ? BUILD_PRESETS[input.preset].build : { kind: 'native' };
  const { flags, ...callsite } = input.callsite ?? {};
  const merged = [input.workspace ?? {}, callsite as Partial<WorkspaceBuildConfig>].reduce<WorkspaceBuildConfig>(
    mergeLayer,
    presetBuild,
  );

  if (flags && flags.length > 0) {
    if (merged.kind !== 'native') {
      throw new BuildConfigError(`resolveBuild: legacy \`flags\` are only valid for native builds (current kind: '${merged.kind}').`);
    }
    merged.cflags = dedup([...(merged.cflags ?? []), ...flags]);
  }

  validate(merged);
  return merged;
}

function validate(b: WorkspaceBuildConfig): void {
  if (b.kind === 'cmake' && b.cmake?.targets && b.cmake.targets.length > 0) {
    const empty = b.cmake.targets.find((t) => typeof t !== 'string' || t.trim() === '');
    if (empty !== undefined) {
      throw new BuildConfigError('resolveBuild: `cmake.targets` entries must be non-empty strings (CMake target names).');
    }
  }
  if (b.kind === 'native' && b.compiler && !['clang', 'clang++', 'emcc', 'em++'].includes(b.compiler)) {
    throw new BuildConfigError(`resolveBuild: unknown compiler '${b.compiler}'.`);
  }
}
