/**
 * Boot sequence: load manifest, init VFS, start tool runner and shell.
 */

import { detectAsyncStrategy } from './async-bridge';
import { FetchBridge } from './net/fetch-bridge';
import { MiniShell } from './shell';
import { ToolRunner } from './tool-runner';
import { TTYBridge } from './tty/xterm-bridge';
import { IDBFS } from './vfs/idb';
import { createVFSManager } from './vfs/index';
import type { FSManifest } from './vfs/lazy';
import { LazyFS } from './vfs/lazy';
import { MemFS } from './vfs/mem';
import { OverlayFS } from './vfs/overlay';

export interface BootResult {
  runner: ToolRunner;
  vfs: ReturnType<typeof createVFSManager>;
  shell: MiniShell;
  tty: TTYBridge;
}

export async function boot(manifestUrl: string, terminalContainer: HTMLElement): Promise<BootResult> {
  const tBoot = performance.now();
  const P = '[Emception:Boot]';
  const ms = (t0: number) => `${(performance.now() - t0).toFixed(1)}ms`;

  console.log(`${P} ===== BOOT START =====`);
  console.log(`${P} Async strategy: ${detectAsyncStrategy()}`);

  // Step 1: Fetch manifest
  const t1 = performance.now();
  console.log(`${P} Step 1/6: Fetching manifest from ${manifestUrl}...`);
  const response = await fetch(manifestUrl);
  const manifest = (await response.json()) as FSManifest & { corsProxy?: string };
  const fileCount = Object.keys(manifest.files).length;
  const bundleCount = Object.keys(manifest.bundles || {}).length;
  console.log(`${P} Step 1/6 done: manifest loaded (${fileCount} files, ${bundleCount} bundles, baseUrl=${manifest.baseUrl}) in ${ms(t1)}`);

  // Step 2: Initialize LazyFS (CDN cache)
  const t2 = performance.now();
  console.log(`${P} Step 2/6: Initializing LazyFS (CDN + IDB cache)...`);
  const lazyFs = new LazyFS(manifest);
  await lazyFs.init();
  console.log(`${P} Step 2/6 done: LazyFS ready in ${ms(t2)}`);

  // Step 3: Initialize writable layers (MemFS, IDBFS)
  const t3 = performance.now();
  console.log(`${P} Step 3/6: Initializing MemFS + IDBFS...`);
  const memFs = new MemFS();    // write-layer fallback (for unmounted paths)
  const tmpFs = new MemFS();    // isolated /tmp mount — must NOT be the same as writeLayer
  const idbFs = new IDBFS('user-files');
  await idbFs.init();
  console.log(`${P} Step 3/6 done: writable FS layers ready in ${ms(t3)}`);

  // Step 4: Assemble OverlayFS + VFSManager
  const t4 = performance.now();
  console.log(`${P} Step 4/6: Assembling OverlayFS + VFSManager...`);
  const overlay = new OverlayFS(lazyFs, memFs);
  overlay.mount('/tmp', tmpFs);
  overlay.mount('/home', idbFs);
  await overlay.mkdir('/tmp');
  await overlay.mkdir('/home');
  await overlay.mkdir('/home/user');
  const vfs = createVFSManager(overlay, lazyFs);
  console.log(`${P} Step 4/6 done: VFS assembled in ${ms(t4)}`);

  // Step 5: Create runner, TTY, shell
  const t5 = performance.now();
  console.log(`${P} Step 5/6: Creating ToolRunner, TTYBridge, MiniShell...`);
  new FetchBridge({
    corsProxy: manifest.corsProxy ?? null,
  });

  const runner = new ToolRunner(vfs);
  const tty = new TTYBridge(terminalContainer);
  const shell = new MiniShell(runner, tty, vfs);
  console.log(`${P} Step 5/6 done: all components created in ${ms(t5)}`);

  // Step 6: Start shell
  const t6 = performance.now();
  console.log(`${P} Step 6/6: Starting shell...`);
  shell.start();
  console.log(`${P} Step 6/6 done: shell started in ${ms(t6)}`);

  console.log(`${P} ===== BOOT COMPLETE in ${ms(tBoot)} =====`);

  return { runner, vfs, shell, tty };
}

export { createBrowserBridge, SUBPROCESS_SHIM, type BrowserBridge } from './emscripten/index';
export { decompressBrotli, isBrotliSupported } from './loader/brotli';
export { clearModuleCache, loadModuleFactory } from './loader/wasm-module';
export { resolveGitTarball, type TarballInfo } from './net/git-tarball';
export type { RunOptions, ToolResult } from './tool-runner';
export { LineBuffer } from './tty/line-buffer';
export type { VFSManager } from './vfs/index';
export { createVFSManager, detectAsyncStrategy, MiniShell, ToolRunner, TTYBridge };

