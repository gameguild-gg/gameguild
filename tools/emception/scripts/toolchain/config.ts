import { readFileSync } from 'node:fs';

import {
  validateToolchainState,
  type LockedTool,
  type ToolName,
  type ToolchainConfig,
  type ToolchainLock,
} from './lock.ts';
import { toolchainPaths } from './paths.ts';

export function loadToolchainStateSync(root: string = process.cwd()) {
  const paths = toolchainPaths(root);
  const lockFile = process.env.EMCEPTION_TOOLCHAIN_LOCK ?? paths.lockFile;
  const config = JSON.parse(readFileSync(paths.configFile, 'utf8')) as ToolchainConfig;
  const lock = JSON.parse(readFileSync(lockFile, 'utf8')) as ToolchainLock;
  validateToolchainState(config, lock);
  return { config, lock, paths } as const;
}

export function lockedTool(lock: ToolchainLock, name: ToolName): LockedTool {
  const tool = lock.tools[name];
  if (!tool) throw new Error(`${name} is missing from the toolchain lock`);
  return tool;
}

export function lockedVersion(lock: ToolchainLock, name: ToolName): string {
  return lockedTool(lock, name).version;
}

export function pythonMajorMinor(version: string): string {
  const match = version.match(/^(\d+\.\d+)/);
  if (!match) throw new Error(`Invalid Python version: ${version}`);
  return match[1];
}

export function pythonMajorMinorCompact(version: string): string {
  return pythonMajorMinor(version).replace('.', '');
}
