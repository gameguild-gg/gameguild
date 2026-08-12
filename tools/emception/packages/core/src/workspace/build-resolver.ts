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

import { BUILD_PRESETS } from '../build-presets.js';
import { BuildConfigError } from '../errors.js';
import type { CMakeBuildConfig, NativeBuildConfig, PythonBuildConfig, WorkspaceBuildConfig } from '../types.js';
import { ToolchainPreset } from '../types.js';

export interface ResolveBuildInput {
  preset?: ToolchainPreset;
  workspace?: WorkspaceBuildConfig;
  callsite?: Partial<WorkspaceBuildConfig>;
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

function isNativeKind(toolchain: ToolchainPreset): boolean {
  return toolchain !== ToolchainPreset.CMake && toolchain !== ToolchainPreset.Python;
}

function mergeLayer(base: WorkspaceBuildConfig, layer: Partial<WorkspaceBuildConfig>): WorkspaceBuildConfig {
  if (layer.toolchain !== undefined && layer.toolchain !== base.toolchain) {
    throw new BuildConfigError(
      `resolveBuild: cannot merge build configs of different toolchains ('${base.toolchain}' and '${layer.toolchain}'). Use the same toolchain for workspace and callsite overrides.`,
    );
  }
  if (isNativeKind(base.toolchain)) {
    const l = layer as Partial<NativeBuildConfig>;
    return {
      toolchain: base.toolchain,
      compiler: l.compiler ?? (base as NativeBuildConfig).compiler,
      flags: mergeArrays((base as NativeBuildConfig).flags, l.flags),
      ldflags: mergeArrays((base as NativeBuildConfig).ldflags, l.ldflags),
      defines: mergeRecord((base as NativeBuildConfig).defines, l.defines),
      includePaths: mergeArrays((base as NativeBuildConfig).includePaths, l.includePaths),
      libPaths: mergeArrays((base as NativeBuildConfig).libPaths, l.libPaths),
      libs: mergeArrays((base as NativeBuildConfig).libs, l.libs),
      sources: mergeArrays((base as NativeBuildConfig).sources, l.sources),
      output: l.output ?? (base as NativeBuildConfig).output,
      env: mergeRecord((base as NativeBuildConfig).env, l.env),
    } as NativeBuildConfig;
  } else if (base.toolchain === ToolchainPreset.CMake) {
    const l = layer as Partial<CMakeBuildConfig>;
    return {
      toolchain: ToolchainPreset.CMake,
      sourceDir: l.sourceDir ?? base.sourceDir,
      buildDir: l.buildDir ?? base.buildDir,
      configureArgs: mergeArrays(base.configureArgs, l.configureArgs),
      buildArgs: mergeArrays(base.buildArgs, l.buildArgs),
      targets: mergeArrays(base.targets, l.targets),
      env: mergeRecord(base.env, l.env),
    };
  } else {
    const l = layer as Partial<PythonBuildConfig>;
    return { toolchain: ToolchainPreset.Python, env: mergeRecord((base as PythonBuildConfig).env, l.env) };
  }
}

/** Resolve the final build config given the three optional layers. */
export function resolveBuild(input: ResolveBuildInput): ResolvedBuild {
  const presetBuild: WorkspaceBuildConfig = input.preset ? BUILD_PRESETS[input.preset] : { toolchain: ToolchainPreset.CPP };
  const merged = [input.workspace ?? {}, input.callsite ?? {}].reduce<WorkspaceBuildConfig>(mergeLayer, presetBuild);

  validate(merged);
  return merged;
}

function validate(b: WorkspaceBuildConfig): void {
  if (b.toolchain === ToolchainPreset.CMake && b.targets && b.targets.length > 0) {
    const empty = b.targets.find((t) => typeof t !== 'string' || t.trim() === '');
    if (empty !== undefined) {
      throw new BuildConfigError('resolveBuild: `targets` entries must be non-empty strings (CMake target names).');
    }
  }
  if (isNativeKind(b.toolchain) && (b as NativeBuildConfig).compiler && !['clang', 'clang++', 'emcc', 'em++'].includes((b as NativeBuildConfig).compiler!)) {
    throw new BuildConfigError(`resolveBuild: unknown compiler '${(b as NativeBuildConfig).compiler}'.`);
  }
}
