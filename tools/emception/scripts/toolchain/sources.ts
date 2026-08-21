import type { LockedTool, ToolName, ToolchainConfig, ToolchainLock } from './lock.ts';

export interface ToolSourceProvider {
  resolve(name: ToolName, requested: string, current?: LockedTool): Promise<LockedTool>;
  inspectEmsdk(emsdk: LockedTool): Promise<Partial<Record<ToolName, LockedTool>>>;
  latestVersion(name: ToolName, current: LockedTool): Promise<string>;
}

export interface OutdatedTool {
  name: ToolName;
  current: string;
  latest: string;
}

function cloneLock(lock: ToolchainLock): ToolchainLock {
  return structuredClone(lock);
}

function isToolName(value: string): value is ToolName {
  return [
    'emsdk', 'llvm', 'binaryen', 'python', 'sdl3', 'cmake', 'brotli', 'imgui',
    'raylib', 'raygui', 'physac', 'allegro', 'curlLite', 'zstdWindows', 'msys2Make',
  ].includes(value);
}

async function updateOne(
  config: ToolchainConfig,
  lock: ToolchainLock,
  name: ToolName,
  requested: string,
  provider: ToolSourceProvider,
): Promise<void> {
  const current = lock.tools[name];
  if (!current) throw new Error(`unknown tool: ${name}`);
  if (current.derivedFrom === 'emsdk' || config.emsdkGroup.includes(name)) {
    throw new Error(`${name} is controlled by emsdk; update emsdk instead`);
  }

  const resolved = await provider.resolve(name, requested, current);
  lock.tools[name] = resolved;

  if (name !== 'emsdk') return;
  const components = await provider.inspectEmsdk(resolved);
  for (const componentName of config.emsdkGroup) {
    const component = components[componentName];
    if (!component) throw new Error(`EMSDK inspection did not resolve ${componentName}`);
    if (component.derivedFrom !== 'emsdk') {
      throw new Error(`EMSDK component ${componentName} must declare derivedFrom=emsdk`);
    }
    lock.tools[componentName] = component;
  }
}

export async function planToolchainUpdate(
  config: ToolchainConfig,
  input: ToolchainLock,
  target: ToolName | 'all',
  requested: string,
  provider: ToolSourceProvider,
): Promise<ToolchainLock> {
  const lock = cloneLock(input);
  if (target !== 'all') {
    await updateOne(config, lock, target, requested, provider);
    return lock;
  }

  const independent = Object.entries(lock.tools)
    .filter(([, tool]) => !tool.derivedFrom && tool.source.kind !== 'workspace')
    .map(([name]) => name)
    .filter(isToolName)
    .sort((left, right) => {
      if (left === 'emsdk') return -1;
      if (right === 'emsdk') return 1;
      return left.localeCompare(right);
    });
  for (const name of independent) {
    await updateOne(config, lock, name, requested, provider);
  }
  return lock;
}

export async function findOutdatedTools(
  _config: ToolchainConfig,
  lock: ToolchainLock,
  provider: ToolSourceProvider,
  target: ToolName | 'all' = 'all',
): Promise<OutdatedTool[]> {
  const candidates = Object.entries(lock.tools)
    .filter(([name, tool]) => (target === 'all' || name === target) && !tool.derivedFrom && tool.source.kind !== 'workspace')
    .map(([name, tool]) => [name as ToolName, tool] as const)
    .sort(([left], [right]) => left.localeCompare(right));
  const result: OutdatedTool[] = [];
  for (const [name, tool] of candidates) {
    const latest = await provider.latestVersion(name, tool);
    if (latest !== tool.version) result.push({ name, current: tool.version, latest });
  }
  return result;
}
