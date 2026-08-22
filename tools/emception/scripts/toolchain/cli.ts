import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

import { cleanToolchain, type CleanScope } from './clean.ts';
import { loadToolchainState, writeToolchainLockAtomic } from './lock.ts';
import type { ToolName } from './lock.ts';
import { createToolSourceProvider } from './provider.ts';
import { findOutdatedTools, hashDirectory, planToolchainUpdate, type ToolSourceProvider } from './sources.ts';

const TOOL_NAMES: readonly ToolName[] = [
  'emsdk', 'llvm', 'binaryen', 'python', 'sdl3', 'cmake', 'brotli', 'imgui',
  'raylib', 'raygui', 'physac', 'allegro', 'curlLite', 'zstdWindows', 'msys2Make',
];

const BUILD_SCRIPTS: Record<string, string> = {
  emsdk: 'build:emsdk',
  binaryen: 'build:binaryen',
  python: 'build:cpython',
  llvm: 'build:llvm',
  cmake: 'build:cmake',
  brotli: 'build:brotli',
  curlLite: 'build:libcurl-lite',
  sdl3: 'build:sdl3',
  imgui: 'build:imgui',
  raylib: 'build:raylib',
  raygui: 'build:raylib',
  physac: 'build:raylib',
  allegro: 'build:allegro',
  sysroot: 'build:sysroot',
  light: 'build:toolchain:light',
  heavy: 'build:toolchain:heavy',
  graphics: 'build:graphics',
  all: 'build:all',
};

export interface ToolchainCliDependencies {
  root?: string;
  provider?: ToolSourceProvider;
  output?: (line: string) => void;
  runScript?: (script: string, environment?: NodeJS.ProcessEnv) => void;
}

function isToolName(value: string): value is ToolName {
  return TOOL_NAMES.includes(value as ToolName);
}

function defaultRunner(root: string) {
  return (script: string, environment: NodeJS.ProcessEnv = process.env) => {
    const executable = process.platform === 'win32' ? 'pnpm.cmd' : 'pnpm';
    const result = spawnSync(executable, ['run', script], { cwd: root, env: environment, stdio: 'inherit' });
    if (result.status !== 0) throw new Error(`pnpm run ${script} failed with exit ${result.status}`);
  };
}

function usage(): string {
  return [
    'Usage: pnpm toolchain <command>',
    '  doctor',
    '  outdated [tool|all] [--json]',
    '  update <tool|all> <version|latest> [--dry-run] [--verify]',
    '  build <tool|group|all> [--force]',
    '  release',
    '  verify [--light|--heavy]',
    '  clean <artifacts|cache|all>',
  ].join('\n');
}

export async function runToolchainCli(args: readonly string[], dependencies: ToolchainCliDependencies = {}): Promise<void> {
  const root = dependencies.root ?? process.cwd();
  const output = dependencies.output ?? console.log;
  const provider = dependencies.provider ?? createToolSourceProvider();
  const runScript = dependencies.runScript ?? defaultRunner(root);
  const [command, ...rest] = args;

  if (!command || command === 'help' || command === '--help') {
    output(usage());
    return;
  }

  if (command === 'doctor') {
    const { config, lock } = await loadToolchainState(root);
    for (const [name, tool] of Object.entries(lock.tools)) {
      if (tool.source.kind === 'workspace') {
        const actual = hashDirectory(path.resolve(root, tool.source.path));
        if (actual !== tool.source.contentHash) throw new Error(`Workspace hash mismatch for ${name}`);
      }
    }
    output(`Toolchain OK: ${Object.keys(lock.tools).length} tools, ABI ${config.runtimeAbi}`);
    return;
  }

  if (command === 'outdated') {
    const target = rest.find((value) => !value.startsWith('--')) ?? 'all';
    if (target !== 'all' && !isToolName(target)) throw new Error(`Unknown tool: ${target}`);
    const { config, lock } = await loadToolchainState(root);
    const outdated = await findOutdatedTools(config, lock, provider, target);
    if (rest.includes('--json')) output(JSON.stringify(outdated, null, 2));
    else if (outdated.length === 0) output('All checked tools are current.');
    else outdated.forEach((tool) => output(`${tool.name}: ${tool.current} -> ${tool.latest}`));
    return;
  }

  if (command === 'update') {
    const [target = '', requested = ''] = rest.filter((value) => !value.startsWith('--'));
    if ((target !== 'all' && !isToolName(target)) || !requested) throw new Error(usage());
    const state = await loadToolchainState(root);
    const updated = await planToolchainUpdate(state.config, state.lock, target, requested, provider);
    const changes = Object.entries(updated.tools)
      .filter(([name, tool]) => JSON.stringify(tool) !== JSON.stringify(state.lock.tools[name]))
      .map(([name, tool]) => `${name}: ${state.lock.tools[name]?.version ?? '(absent)'} -> ${tool.version}`);
    if (rest.includes('--verify')) {
      output('Verifying affected source identities before accepting the update...');
      for (const [name, tool] of Object.entries(updated.tools)) {
        if (tool.source.kind === 'workspace') {
          const actual = hashDirectory(path.resolve(root, tool.source.path));
          if (actual !== tool.source.contentHash) throw new Error(`Workspace hash mismatch for ${name}`);
        }
      }
    }
    if (rest.includes('--dry-run')) {
      if (changes.length === 0) output(`[dry-run] ${target} is already resolved to ${requested}; lockfile unchanged.`);
      else changes.forEach((change) => output(`[dry-run] ${change}`));
      return;
    }
    await writeToolchainLockAtomic(root, state.config, updated);
    if (changes.length === 0) output(`${target} already matches ${requested}.`);
    else changes.forEach((change) => output(`Updated ${change}`));
    return;
  }

  if (command === 'clean') {
    const scope = rest[0] as CleanScope | undefined;
    if (!scope || !['artifacts', 'cache', 'all'].includes(scope)) throw new Error(usage());
    await cleanToolchain(root, scope);
    output(`Removed Toolchain ${scope}.`);
    return;
  }

  if (command === 'build') {
    const target = rest.find((value) => !value.startsWith('--')) ?? 'all';
    const script = BUILD_SCRIPTS[target];
    if (!script) throw new Error(`Unknown build target: ${target}`);
    runScript(script, { ...process.env, ...(rest.includes('--force') ? { EMCEPTION_FORCE_BUILD: '1' } : {}) });
    return;
  }

  if (command === 'release') {
    runScript('build:cdn:serial');
    return;
  }

  if (command === 'verify') {
    runScript('test:scripts');
    if (rest.includes('--heavy')) runScript('build:all');
    else runScript('build:toolchain:light');
    return;
  }

  throw new Error(`Unknown Toolchain command: ${command}\n${usage()}`);
}

const entrypoint = process.argv[1] ? path.resolve(process.argv[1]) : '';
if (entrypoint === fileURLToPath(import.meta.url)) {
  runToolchainCli(process.argv.slice(2)).catch((error: unknown) => {
    console.error(error instanceof Error ? error.message : String(error));
    process.exitCode = 1;
  });
}
