/** Apply the versioned Emception glue patch set to the frozen release sysroot. */

import fs from 'node:fs';
import path from 'node:path';
import { toolchainPaths } from './toolchain/paths.ts';
import { fileURLToPath } from 'node:url';
import { PATCH_SET_VERSION, patchCanvasRuntimeDirectory, patchGlueDirectory } from './lib/glue-patches.mjs';
import { enableBuildKeepalive } from './lib/keepalive.ts';

enableBuildKeepalive('patch-glue');

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const P = toolchainPaths(ROOT);
const STAGED_SYSROOT = process.env.STAGED_SYSPATH ?? P.stagedSysroot;
const STAGED_LIB = path.join(STAGED_SYSROOT, 'usr', 'lib');
const EMSCRIPTEN_TOOLS = path.join(STAGED_LIB, 'emscripten', 'tools');

const ALL_TOOLS = [
  'clang', 'lld', 'python',
  'wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce',
  'llvm-nm', 'llvm-ar', 'llvm-objcopy', 'llc',
  'cmake', 'curl',
] as const;

function patchColoredLogger(filePath: string): boolean {
  if (!fs.existsSync(filePath)) {
    throw new Error(`required Emscripten source is missing: ${filePath}`);
  }
  const source = fs.readFileSync(filePath, 'utf8');
  const importNeedle = 'import ctypes\nimport logging';
  const importReplacement = 'try:\n    import ctypes\nexcept ImportError:\n    ctypes = None\nimport logging';
  if (source.includes(importReplacement)) return false;
  if (!source.includes(importNeedle)) {
    throw new Error(`${filePath}: unsupported colored_logger.py shape`);
  }

  let patched = source.replace(importNeedle, importReplacement);
  const windllNeedle = '  kernel32 = ctypes.windll.kernel32';
  if (patched.includes(windllNeedle)) {
    patched = patched.replace(
      windllNeedle,
      '  if ctypes is None:\n    return False\n  kernel32 = ctypes.windll.kernel32',
    );
  }
  fs.writeFileSync(filePath, patched, 'utf8');
  return true;
}

async function main(): Promise<void> {
  console.log(`Applying ${PATCH_SET_VERSION} to ${STAGED_SYSROOT}`);
  const glue = await patchGlueDirectory({ libDirectory: STAGED_LIB, tools: ALL_TOOLS });
  const canvas = await patchCanvasRuntimeDirectory({ runtimeDirectory: path.join(STAGED_LIB, 'emscripten') });
  const coloredLoggerChanged = patchColoredLogger(path.join(EMSCRIPTEN_TOOLS, 'colored_logger.py'));

  console.log(
    `Patch set verified: ${glue.foundFiles} glue pairs + ${canvas.foundFiles} canvas runtimes, ` +
      `${glue.patchCount + canvas.patchCount} patches, ` +
      `${glue.changedFiles + canvas.changedFiles + Number(coloredLoggerChanged)} changed files.`,
  );
}

main().catch((error: unknown) => {
  console.error('[patch-glue] Failed:', error);
  process.exitCode = 1;
});
