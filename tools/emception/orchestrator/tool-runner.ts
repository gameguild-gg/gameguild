/**
 * Micro-kernel Tool Runner
 *
 * Each tool invocation spawns an isolated WASM process with its own linear
 * memory, libc, heap, and Emscripten filesystem. The kernel (this module)
 * manages:
 *
 *   - Process lifecycle: create → configure → run → teardown
 *   - VFS ↔ process FS bridging: pre-populate input files, harvest outputs
 *   - TTY: route stdout/stderr from the process to the caller
 *   - ENV: inject environment variables into each process
 *
 * There is NO shared MAIN_MODULE, NO dlopen/dlsym, NO shared linear memory.
 * Each tool is compiled as a standalone Emscripten module (MODULARIZE + EXPORT_ES6).
 */

import { SUBPROCESS_SHIM } from './emscripten/subprocess-shim';
import { loadModuleFactory } from './loader/wasm-module';
import type { VFSManager } from './vfs/index';

const LOG_PREFIX = '[Emception:Kernel]';
function elapsed(t0: number): string { return `${(performance.now() - t0).toFixed(1)}ms`; }
function fmtSize(n: number): string {
  if (n < 1024) return `${n}B`;
  if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)}KB`;
  return `${(n / (1024 * 1024)).toFixed(1)}MB`;
}

/* ------------------------------------------------------------------ */
/*  Public interfaces                                                  */
/* ------------------------------------------------------------------ */

export interface ToolResult {
  exitCode: number;
  stdout: string;
  stderr: string;
}

export interface RunOptions {
  env?: Record<string, string>;
  cwd?: string;
  onStdout?: (text: string) => void;
  onStderr?: (text: string) => void;
  stdin?: () => number | null;
  /** Additional dirs to populate recursively in the child process FS from VFS */
  extraPreloadDirs?: string[];
}

/* ------------------------------------------------------------------ */
/*  Tool descriptors — standalone .wasm modules                        */
/* ------------------------------------------------------------------ */

interface ToolDescriptor {
  /** Path to the standalone .wasm module (also used to derive the .mjs glue URL) */
  modulePath: string;
  /** Files to pre-populate in the process FS before callMain() */
  preloadFiles?: string[];
  /** Directories to pre-populate (immediate children only from kernel VFS) */
  preloadDirs?: string[];
  /** Directories to pre-populate recursively (entire subtrees from kernel VFS) */
  preloadDirsRecursive?: string[];
  /** Directories whose contents should be harvested back to kernel VFS after run */
  harvestDirs?: string[];
}

const PYTHON_PRELOAD_FILES = ['/usr/lib/python314.zip'];

// Minimal Python stdlib files needed for early initialization
const PYTHON_INIT_FILES = [
  '/usr/lib/python3.14/os.py',
  '/usr/lib/python3.14/stat.py',
  '/usr/lib/python3.14/posixpath.py',
  '/usr/lib/python3.14/genericpath.py',
  '/usr/lib/python3.14/abc.py',
  '/usr/lib/python3.14/_collections_abc.py',
  '/usr/lib/python3.14/_sitebuiltins.py',
  '/usr/lib/python3.14/codecs.py',
  '/usr/lib/python3.14/io.py',
  '/usr/lib/python3.14/site.py',
  '/usr/lib/python3.14/encodings/__init__.py',
  '/usr/lib/python3.14/encodings/aliases.py',
  '/usr/lib/python3.14/encodings/utf_8.py',
  '/usr/lib/python3.14/encodings/ascii.py',
  '/usr/lib/python3.14/encodings/latin_1.py',
];

/**
 * Tool registry — maps tool names to standalone WASM module paths.
 * No shared libraries, no entry symbols needed (each module has standard main()).
 */
const TOOL_REGISTRY: Record<string, ToolDescriptor> = {
  'clang': {
    modulePath: '/usr/lib/clang.wasm',
  },
  'clang++': {
    modulePath: '/usr/lib/clang.wasm',
  },
  'lld': {
    modulePath: '/usr/lib/lld.wasm',
  },
  'wasm-ld': {
    modulePath: '/usr/lib/lld.wasm',
  },
  'llvm-nm': {
    modulePath: '/usr/lib/llvm-nm.wasm',
  },
  'llvm-ar': {
    modulePath: '/usr/lib/llvm-ar.wasm',
  },
  'llvm-objcopy': {
    modulePath: '/usr/lib/llvm-objcopy.wasm',
  },
  'llc': {
    modulePath: '/usr/lib/llc.wasm',
  },
  'wasm-opt': {
    modulePath: '/usr/lib/wasm-opt.wasm',
  },
  'wasm-as': {
    modulePath: '/usr/lib/wasm-as.wasm',
  },
  'wasm-ctor-eval': {
    modulePath: '/usr/lib/wasm-ctor-eval.wasm',
  },
  'wasm-emscripten-finalize': {
    modulePath: '/usr/lib/wasm-emscripten-finalize.wasm',
  },
  'wasm-metadce': {
    modulePath: '/usr/lib/wasm-metadce.wasm',
  },
};

/**
 * Optional post-processing tools: these strip debug info, optimize, etc.
 * If their WASM modules don't exist or they crash at runtime, they can be
 * safely skipped since the output from wasm-ld is already a valid WASM binary.
 */
const OPTIONAL_TOOLS = new Set([
  'llvm-objcopy', 'llvm-strip',
  'wasm-opt', 'wasm-metadce', 'wasm-ctor-eval', 'wasm-emscripten-finalize',
]);

type ModuleFactory = (config: Record<string, unknown>) => Promise<EmscriptenInstance>;

/** Minimal shape of an Emscripten-generated module instance */
interface EmscriptenInstance {
  FS: {
    writeFile(path: string, data: string | Uint8Array): void;
    readFile(path: string, opts?: { encoding?: string }): Uint8Array;
    readdir(path: string): string[];
    stat(path: string): { size: number; mode: number };
    mkdirTree(path: string): void;
    mkdir(path: string): void;
    chdir(path: string): void;
    unlink(path: string): void;
    isDir(mode: number): boolean;
    symlink(target: string, path: string): void;
  };
  callMain?(argv: string[]): number;
  EXITSTATUS?: number;
  [key: string]: unknown;
}

/* ------------------------------------------------------------------ */
/*  Kernel (ToolRunner)                                                */
/* ------------------------------------------------------------------ */

export class ToolRunner {
  private vfs: VFSManager;

  constructor(vfs: VFSManager) {
    this.vfs = vfs;
  }

  /* ---------------------------------------------------------------- */
  /*  Public API                                                       */
  /* ---------------------------------------------------------------- */

  /**
   * Run a tool by name with the given argv. Each call spawns an isolated
   * WASM process and tears it down after completion.
   */
  async run(tool: string, argv: string[], options: RunOptions = {}): Promise<ToolResult> {
    const tTotal = performance.now();
    console.log(`${LOG_PREFIX} ===== RUN: ${tool} =====`);
    console.log(`${LOG_PREFIX}   argv: [${argv.map(a => `"${a}"`).join(', ')}]`);

    // Extract basename from tool path (handle both 'node' and '/usr/bin/node')
    const toolBasename = tool.includes('/') ? (tool.split('/').pop() || tool) : tool;

    // Special case: 'node' runs compiled JavaScript output in the browser.
    if (toolBasename === 'node') {
      console.log(`${LOG_PREFIX}   Dispatching to runJavaScript (node emulation)`);
      return this.runJavaScript(argv, options);
    }

    // Fix for emcc/em++: inject the python script path
    if (toolBasename === 'emcc' || toolBasename === 'em++') {
      const scriptPath = '/usr/lib/emscripten/emcc.py';
      if (argv.length > 0) {
        argv = [argv[0], scriptPath, ...argv.slice(1)];
      } else {
        argv = [tool, scriptPath];
      }
      console.log(`${LOG_PREFIX}   Injected Python script: ${scriptPath}`);
    }

    // Use module-level OPTIONAL_TOOLS set (defined near TOOL_REGISTRY)

    const descriptor = this.resolveToolDescriptor(tool);
    console.log(`${LOG_PREFIX}   Descriptor: module=${descriptor.modulePath}`);

    // Check if the tool's WASM module exists in the VFS. Optional post-processing
    // tools (llvm-objcopy, wasm-opt, etc.) may not be compiled. Their absence
    // doesn't prevent a working WASM binary — wasm-ld's output is already valid.
    if (OPTIONAL_TOOLS.has(toolBasename)) {
      const wasmExists = await this.vfs.fetchFile(descriptor.modulePath);
      if (!wasmExists) {
        console.log(`${LOG_PREFIX}   [SKIP] Optional tool "${toolBasename}" — WASM module not found, returning no-op (exit 0)`);
        console.log(`${LOG_PREFIX} ===== RUN COMPLETE: ${tool} — exitCode=0 (skipped), total=${elapsed(tTotal)} =====`);
        return { exitCode: 0, stdout: '', stderr: '' };
      }
    }

    // Spawn an isolated WASM process
    const result = await this.spawnProcess(descriptor, argv, options);

    console.log(`${LOG_PREFIX} ===== RUN COMPLETE: ${tool} — exitCode=${result.exitCode}, total=${elapsed(tTotal)} =====`);
    return result;
  }

  /**
   * Run a shell command string (for system() / popen() emulation).
   */
  async system(command: string, cwd?: string): Promise<number> {
    const parts = this.parseCommand(command);
    if (parts.length === 0) return 0;
    const result = await this.run(parts[0], parts, { cwd });
    return result.exitCode;
  }

  /**
   * Read a file from the kernel VFS.
   */
  async getFile(path: string): Promise<Uint8Array | null> {
    return this.vfs.fetchFile(path);
  }

  /**
   * List directory contents from the kernel VFS overlay.
   */
  async listDir(path: string): Promise<string[]> {
    try {
      return await this.vfs.overlay.readdir(path);
    } catch {
      return [];
    }
  }

  /* ---------------------------------------------------------------- */
  /*  Process spawning (micro-kernel core)                             */
  /* ---------------------------------------------------------------- */

  /**
   * Spawn an isolated WASM process for a tool invocation:
   *
   *   1. Load the module factory (cached Emscripten JS glue)
   *   2. Configure process: ENV, argv, stdout/stderr hooks, FS callbacks
   *   3. Instantiate the WASM module (creates fresh linear memory)
   *   4. Pre-populate the process FS with files from the kernel VFS
   *   5. Call main(argc, argv)
   *   6. Harvest output files back to the kernel VFS
   *   7. Discard the process (GC reclaims the linear memory)
   */
  private async spawnProcess(
    descriptor: ToolDescriptor,
    argv: string[],
    options: RunOptions,
  ): Promise<ToolResult> {
    const tSpawn = performance.now();
    const stdoutChunks: string[] = [];
    const stderrChunks: string[] = [];

    // Step 1: Load module factory (may be cached from a previous invocation)
    const tFactory = performance.now();
    console.log(`${LOG_PREFIX}   Step 1/4: Loading module factory for ${descriptor.modulePath}...`);
    const factory = await this.loadModuleFactory(descriptor.modulePath);
    console.log(`${LOG_PREFIX}   Step 1/4 done: factory loaded in ${elapsed(tFactory)}`);

    // Is this a Python-based tool (emcc/em++)?
    const isPythonTool = descriptor.modulePath.includes('python');

    // Build environment
    const envVars: Record<string, string> = {
      PYTHONHOME: '/usr',
      PYTHONPATH: '/usr/lib/python3.14:/usr/lib/python314.zip:/usr/lib/emscripten',
      PATH: '/usr/bin',
      HOME: '/home/user',
      TMPDIR: '/tmp',
      EM_CONFIG: '/etc/emscripten.config',
      PYTHONDONTWRITEBYTECODE: '1',
      // Force unbuffered stdout/stderr so Python tracebacks are visible
      // before the WASM module exits (CPython's Py_FinalizeEx can fail
      // before flushing C stdio buffers).
      PYTHONUNBUFFERED: '1',
      ...(isPythonTool ? { EMCC_SKIP_SANITY_CHECK: '1' } : {}),
      ...(options.env || {}),
    };

    // Capture a reference to the VFS for the onMissingFile callback
    const vfs = this.vfs;

    // Step 2: Configure and instantiate the WASM module
    const tInst = performance.now();
    console.log(`${LOG_PREFIX}   Step 2/4: Instantiating isolated WASM process...`);

    const moduleConfig: Record<string, unknown> = {
      // Skip callMain during init — we call it manually after FS population
      noInitialRun: true,
      // Allow the process to exit normally
      noExitRuntime: false,
      // Pass argv[0] as thisProgram
      thisProgram: argv[0] || 'tool',
      // ENV injection strategy:
      // python.mjs (Emscripten glue at /usr/lib/python.mjs) is patched to
      // merge moduleArg.ENV into local ENV variable:
      //   var ENV={};if(moduleArg&&moduleArg["ENV"]){for(var _k in moduleArg["ENV"]){ENV[_k]=moduleArg["ENV"][_k]}}
      // This ensures PYTHONHOME, PYTHONPATH, etc. are available before
      // getEnvStrings() is called by Python's environ_get syscall.
      ENV: envVars,
      // Capture abort info
      onAbort: (what: unknown) => {
        console.error(`${LOG_PREFIX} ⚠️ onAbort called with:`, what);
      },
      // TTY hooks
      print: (text: string) => {
        stdoutChunks.push(text);
        options.onStdout?.(text);
      },
      printErr: (text: string) => {
        stderrChunks.push(text);
        options.onStderr?.(text);
      },
      stdin: options.stdin ?? (() => null),
      // Lazy file fetch: when the process FS encounters a missing file,
      // ask the kernel VFS for it
      onMissingFile: async (path: string) => vfs.fetchFile(path),
      // Resolve .wasm URL from the CDN manifest
      locateFile: (path: string) => {
        if (path.endsWith('.wasm')) {
          const url = vfs.getUrl(descriptor.modulePath);
          return url ? url.replace(/\.(br|gz)$/, '') : path;
        }
        return path;
      },
      // Override arguments for WASM main (used by Emscripten's callMain)
      arguments: argv.slice(1),
    };

    // For Python-based tools (emcc/em++), provide systemCallback for subprocess
    // interception via JSPI. When Python calls os.system('__dispatch_subprocess'),
    // this callback intercepts it, runs the tool via this.run(), and communicates
    // results back through files in the process FS.
    let instanceRef: EmscriptenInstance | null = null;
    if (isPythonTool) {
      const runner = this;
      moduleConfig['systemCallback'] = async (cmdStr: string): Promise<number> => {
        if (cmdStr === '__dispatch_subprocess' && instanceRef) {
          try {
            // Read the subprocess request from the process FS
            const requestData = String(instanceRef.FS.readFile('/tmp/.subprocess_request', { encoding: 'utf8' }));
            const request = JSON.parse(requestData) as { cmd: string; cwd: string };
            console.log(`${LOG_PREFIX}   [subprocess] Dispatching: ${request.cmd.slice(0, 120)}...`);

            // Parse the command and run through the tool runner
            const parts = runner.parseCommand(request.cmd);
            if (parts.length === 0) {
              instanceRef.FS.writeFile('/tmp/.subprocess_stdout', '');
              instanceRef.FS.writeFile('/tmp/.subprocess_stderr', '');
              return 0;
            }

            // Skip optional tools entirely — don't even spawn them —
            // UNLESS the call is a version check (emcc probes the tool version).
            // wasm-opt, llvm-objcopy etc. may crash at runtime due to LLVM
            // version mismatches, and the wasm-ld output is already valid.
            const subBasename = parts[0].split('/').pop() ?? parts[0];
            const isVersionCheck = parts.includes('--version') || parts.includes('-v');
            if (OPTIONAL_TOOLS.has(subBasename) && !isVersionCheck) {
              console.log(`${LOG_PREFIX}   [subprocess] Skipping optional tool "${subBasename}" — returning no-op (exit 0)`);
              instanceRef.FS.writeFile('/tmp/.subprocess_stdout', '');
              instanceRef.FS.writeFile('/tmp/.subprocess_stderr', '');
              return (0 << 8) | 0;
            }

            // Harvest files from the parent process FS into the kernel VFS.
            // This ensures child processes (e.g., clang) can access files that
            // the parent (emcc) created (e.g., sysroot cache, temp files).
            // Also harvest /usr/lib/emscripten/system/lib so that clang can
            // find system library sources when compiling them.
            const harvestPaths = ['/home/user', '/tmp'];
            for (const hp of harvestPaths) {
              try {
                await runner.harvestDir(instanceRef.FS, hp);
              } catch { /* ok if dir doesn't exist */ }
            }

            const subStdout: string[] = [];
            const subStderr: string[] = [];
            const subResult = await runner.run(parts[0], parts, {
              cwd: request.cwd || options.cwd,
              onStdout: (t) => subStdout.push(t),
              onStderr: (t) => subStderr.push(t),
            });

            // Sync files from the kernel VFS back into the parent process FS.
            // The child process (e.g. clang) already harvested its output files
            // to the kernel VFS. Now we re-populate the parent's FS so emcc can
            // access them (e.g. reading /tmp/emscripten_temp_*/main.o).
            for (const hp of harvestPaths) {
              try {
                await runner.populateDirFromVFS(instanceRef.FS, hp);
              } catch { /* ok if dir doesn't exist */ }
            }

            // Write results back to the process FS
            instanceRef.FS.writeFile('/tmp/.subprocess_stdout', subStdout.join('\n'));
            instanceRef.FS.writeFile('/tmp/.subprocess_stderr', subStderr.join('\n'));

            console.log(`${LOG_PREFIX}   [subprocess] Done: exitCode=${subResult.exitCode}`);
            if (subStderr.length > 0) {
              for (const line of subStderr) {
                console.error(`${LOG_PREFIX}   [subprocess] stderr: ${line}`);
              }
            }

            // Optional tools may crash on actual processing passes even though
            // --version works fine. Treat their failures as non-fatal.
            let effectiveExitCode = subResult.exitCode;
            if (effectiveExitCode !== 0 && OPTIONAL_TOOLS.has(subBasename)) {
              console.log(`${LOG_PREFIX}   [subprocess] Optional tool "${subBasename}" failed (exit ${effectiveExitCode}) — treating as non-fatal`);
              effectiveExitCode = 0;
              instanceRef.FS.writeFile('/tmp/.subprocess_stderr', '');
            }
            // Return in _W_EXITCODE format: (exitCode << 8) | signal
            return (effectiveExitCode << 8) | 0;
          } catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            console.error(`${LOG_PREFIX}   [subprocess] Error: ${msg}`);
            if (instanceRef) {
              instanceRef.FS.writeFile('/tmp/.subprocess_stdout', '');
              instanceRef.FS.writeFile('/tmp/.subprocess_stderr', msg);
            }
            return (1 << 8) | 0;
          }
        }
        // Unknown system() command — return ENOSYS
        return -52;
      };
    }

    let instance: EmscriptenInstance;
    try {
      instance = await (factory as ModuleFactory)(moduleConfig);
      instanceRef = instance;
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      console.error(`${LOG_PREFIX} ❌ Failed to instantiate WASM module ${descriptor.modulePath}:`, msg);
      return {
        exitCode: 1,
        stdout: stdoutChunks.join('\n'),
        stderr: `Failed to instantiate ${descriptor.modulePath}: ${msg}`,
      };
    }

    console.log(`${LOG_PREFIX}   Step 2/4 done: process instantiated in ${elapsed(tInst)}`);

    // Step 3: Pre-populate the process FS with files from the kernel VFS
    const tFS = performance.now();
    console.log(`${LOG_PREFIX}   Step 3/4: Populating process filesystem...`);
    await this.populateProcessFS(instance, descriptor, options);

    // For Python-based tools, inject the subprocess shim to replace stdlib subprocess
    if (isPythonTool) {
      try {
        instance.FS.mkdirTree('/usr/lib/python3.14');
        instance.FS.writeFile('/usr/lib/python3.14/subprocess.py', SUBPROCESS_SHIM);
        console.log(`${LOG_PREFIX}   Injected subprocess shim`);
      } catch {
        console.warn(`${LOG_PREFIX}   ⚠️ Failed to inject subprocess shim`);
      }

      // Create sentinel stub files for all LLVM/Binaryen tools in /usr/bin/.
      // Emscripten's Python code (shared.py check_llvm_version, building.py
      // get_binaryen_version) calls os.path.exists() on these paths.
      // The actual execution goes through the subprocess shim → ToolRunner,
      // but the existence check happens directly on the Emscripten FS.
      // populateDir may fail silently for CDN-fetched stubs, so we create
      // them explicitly here to guarantee os.path.exists() returns True.
      const STUB = new TextEncoder().encode('stub\n');
      const TOOL_STUBS = [
        '/usr/bin/clang', '/usr/bin/clang++',
        '/usr/bin/wasm-ld', '/usr/bin/lld',
        '/usr/bin/llvm-ar', '/usr/bin/llvm-nm', '/usr/bin/llvm-objcopy',
        '/usr/bin/llc',
        '/usr/bin/wasm-opt', '/usr/bin/wasm-as',
        '/usr/bin/wasm-ctor-eval', '/usr/bin/wasm-emscripten-finalize',
        '/usr/bin/wasm-metadce',
        '/usr/bin/node', '/usr/bin/python3',
      ];
      try {
        instance.FS.mkdirTree('/usr/bin');
        for (const stubPath of TOOL_STUBS) {
          try { instance.FS.writeFile(stubPath, STUB); } catch { /* ok */ }
        }
        console.log(`${LOG_PREFIX}   Created ${TOOL_STUBS.length} tool stubs in /usr/bin/`);
      } catch {
        console.warn(`${LOG_PREFIX}   ⚠️ Failed to create tool stubs`);
      }
    }
    console.log(`${LOG_PREFIX}   Step 3/4 done: FS populated in ${elapsed(tFS)}`);

    // Step 4: Call main(argc, argv) — the tool runs to completion
    const tRun = performance.now();
    const mainArgv = argv.slice(1); // Emscripten's callMain expects argv without argv[0]
    console.log(`${LOG_PREFIX}   Step 4/4: callMain([${mainArgv.map(a => `"${a}"`).join(', ')}])...`);

    let exitCode: number;
    try {
      if (typeof instance.callMain === 'function') {
        // callMain may return a Promise if main is JSPI-wrapped (WebAssembly.promising)
        const result: unknown = instance.callMain(mainArgv);
        exitCode = (result && typeof (result as Promise<number>).then === 'function'
          ? await (result as Promise<number>)
          : (result as number)) ?? 0;
      } else {
        // Fallback: Some modules may not export callMain
        console.warn(`${LOG_PREFIX}   ⚠️ callMain not found — module may have run during init`);
        exitCode = instance.EXITSTATUS ?? 0;
      }
      console.log(`${LOG_PREFIX}   Step 4/4 done: exitCode=${exitCode} in ${elapsed(tRun)}`);
    } catch (e: unknown) {
      // Emscripten throws to unwind the stack on process exit.
      // If the error has a 'status' field, that's the exit code.
      if (e && typeof e === 'object' && 'status' in e) {
        exitCode = (e as { status: number }).status;
        console.log(`${LOG_PREFIX}   Step 4/4 done: process exited with status=${exitCode} in ${elapsed(tRun)}`);
      } else {
        exitCode = instance.EXITSTATUS ?? 1;
        const msg = e instanceof Error ? e.message : String(e);
        // Don't treat "Program terminated with exit(0)" as an error
        if (msg.includes('exit(0)') || exitCode === 0) {
          exitCode = 0;
          console.log(`${LOG_PREFIX}   Step 4/4 done: clean exit via throw in ${elapsed(tRun)}`);
        } else {
          console.error(`${LOG_PREFIX} ❌ callMain threw: ${msg}`);
          if (e instanceof Error && e.stack) {
            console.error(`${LOG_PREFIX}   Stack trace: ${e.stack}`);
          }
          // Log WASM memory info for debugging
          try {
            const mem = (instance as Record<string, unknown>)['wasmMemory'] as WebAssembly.Memory | undefined;
            if (mem) {
              console.error(`${LOG_PREFIX}   WASM memory: ${(mem.buffer.byteLength / 1024 / 1024).toFixed(1)}MB`);
            }
          } catch { /* ignore */ }
          // Log any stderr that was captured before the abort
          if (stderrChunks.length > 0) {
            console.error(`${LOG_PREFIX}   stderr before abort: ${stderrChunks.join('; ').slice(0, 500)}`);
          }
          stderrChunks.push(msg);
          options.onStderr?.(msg);
        }
      }
    }

    // CPython-WASM finalization fix: Py_FinalizeEx() often fails in WASM
    // environments, causing Py_RunMain() to return exitcode=120 even though
    // the Python script (emcc.py) completed successfully. Exit code 120 is
    // specifically set in CPython's Modules/main.c when Py_FinalizeEx() < 0.
    // This is a known WASM limitation — finalization cleanup (flushing stdio,
    // running atexit handlers, freeing modules) doesn't fully work in MEMFS.
    if (isPythonTool && exitCode === 120) {
      console.log(`${LOG_PREFIX}   Treating exit code 120 as success (CPython Py_FinalizeEx failure in WASM)`);
      exitCode = 0;
    }

    // Harvest output files from the process FS back to the kernel VFS
    await this.harvestProcessFS(instance, descriptor);

    // Process is done — let GC reclaim the WASM linear memory
    console.log(`${LOG_PREFIX}   Process complete, releasing WASM instance (total spawn: ${elapsed(tSpawn)})`);

    return {
      exitCode,
      stdout: stdoutChunks.join('\n'),
      stderr: stderrChunks.join('\n'),
    };
  }

  /* ---------------------------------------------------------------- */
  /*  Process FS bridging                                              */
  /* ---------------------------------------------------------------- */

  /**
   * Pre-populate the isolated process's Emscripten FS with files needed
   * for the tool to run. This includes:
   *   - Explicit preloadFiles from the descriptor (e.g. Python stdlib)
   *   - Working directory contents
   *   - Any CWD setup
   */
  private async populateProcessFS(
    instance: EmscriptenInstance,
    descriptor: ToolDescriptor,
    options: RunOptions,
  ): Promise<void> {
    const FS = instance.FS;

    // Ensure standard directories exist
    for (const dir of ['/tmp', '/home', '/home/user', '/usr', '/usr/lib', '/usr/bin', '/etc']) {
      try { FS.mkdirTree(dir); } catch { /* exists */ }
    }

    // Pre-load explicitly listed files
    const preloadFiles = descriptor.preloadFiles || [];
    if (preloadFiles.length > 0) {
      console.log(`${LOG_PREFIX}     Preloading ${preloadFiles.length} file(s)...`);
      for (const filePath of preloadFiles) {
        await this.writeFileToProcessFS(FS, filePath);
      }
    }

    // Pre-load explicit directories (shallow — only immediate children)
    const preloadDirs = descriptor.preloadDirs || [];
    for (const dirPath of preloadDirs) {
      await this.populateDir(FS, dirPath, false);
    }

    // Pre-load directories recursively (full subtrees)
    const preloadDirsRecursive = descriptor.preloadDirsRecursive || [];
    for (const dirPath of preloadDirsRecursive) {
      console.log(`${LOG_PREFIX}     preloadDirRecursive: ${dirPath}`);
      await this.populateDir(FS, dirPath, true);
    }

    // Set CWD
    const cwd = options.cwd || '/home/user';
    try { FS.mkdirTree(cwd); } catch { /* exists */ }

    // Copy files from the kernel VFS cwd into the process FS (recursively,
    // so child processes like clang can access files created by the parent)
    await this.populateDir(FS, cwd, true);

    // Also populate /tmp from VFS — subprocess parents write temp files here
    // (e.g. emcc creates /tmp/emscripten_temp_xxx/ with source/object files)
    if (cwd !== '/tmp') {
      await this.populateDir(FS, '/tmp', true);
    }

    // Populate additional directories from VFS (e.g. system lib sources
    // needed by clang when compiling system libraries for emcc)
    if (options.extraPreloadDirs) {
      for (const dir of options.extraPreloadDirs) {
        console.log(`${LOG_PREFIX}     extraPreloadDir: ${dir}`);
        await this.populateDir(FS, dir, true);
      }
    }

    // Pre-populate the Emscripten cache from VFS cache-lib so emcc doesn't
    // try to compile system libraries at runtime. The pre-built cache lives
    // at /usr/lib/emscripten/cache-lib/ in the VFS but emcc expects it at
    // ~/.emscripten_cache/sysroot/lib/ (CACHE config + /sysroot/lib/).
    const isPythonDescriptor = descriptor.modulePath.includes('python');
    if (isPythonDescriptor) {
      // Create the sysroot_install.stamp FIRST so emcc's ensure_sysroot() /
      // cache.get() skips the install_system_headers step entirely.  Combined
      // with FROZEN_CACHE=True in emscripten.config, this prevents emcc from
      // ever trying to walk the source tree with os.scandir (which would fail
      // in the WASM MEMFS that is populated on-demand).
      console.log(`${LOG_PREFIX}     Creating sysroot_install.stamp...`);
      try { FS.mkdirTree('/home/user/.emscripten_cache'); } catch { /* exists */ }
      FS.writeFile('/home/user/.emscripten_cache/sysroot_install.stamp', 'prebuilt');

      console.log(`${LOG_PREFIX}     Populating emscripten cache from VFS cache-lib...`);
      await this.populateDirMapped(
        FS,
        '/usr/lib/emscripten/cache-lib',           // VFS source
        '/home/user/.emscripten_cache/sysroot/lib', // process FS destination
        true,
      );

      // Map system include headers into the sysroot cache.
      // Clang uses --sysroot=~/.emscripten_cache/sysroot, so it looks for
      // headers at sysroot/include/.  The headers live at /usr/include/ in
      // the VFS (placed there by the Emscripten build).
      console.log(`${LOG_PREFIX}     Populating sysroot include headers...`);
      await this.populateDirMapped(
        FS,
        '/usr/include',                                  // VFS source
        '/home/user/.emscripten_cache/sysroot/include',  // destination
        true,
      );
    }

    try {
      FS.chdir(cwd);
    } catch (e) {
      console.warn(`${LOG_PREFIX}     Failed to chdir to ${cwd}`, e);
    }
  }

  /**
   * Write a single file from the kernel VFS into the process's Emscripten FS.
   */
  private async writeFileToProcessFS(
    FS: EmscriptenInstance['FS'],
    filePath: string,
  ): Promise<boolean> {
    try {
      const data = await this.vfs.fetchFile(filePath);
      if (!data) {
        console.warn(`${LOG_PREFIX}     VFS miss: ${filePath}`);
        return false;
      }

      // Ensure parent directory exists
      const dir = filePath.substring(0, filePath.lastIndexOf('/'));
      if (dir) {
        try { FS.mkdirTree(dir); } catch { /* exists */ }
      }

      FS.writeFile(filePath, data);
      console.log(`${LOG_PREFIX}     preloaded: ${filePath} (${data.length} bytes)`);
      return true;
    } catch (e) {
      console.warn(`${LOG_PREFIX}     Failed to write ${filePath} to process FS:`, e);
      return false;
    }
  }

  /**
   * Recursively populate a directory in the process FS from the kernel VFS.
   */
  private async populateDir(
    FS: EmscriptenInstance['FS'],
    dirPath: string,
    recursive: boolean = false,
  ): Promise<void> {
    try {
      const entries = await this.vfs.overlay.readdir(dirPath);
      console.log(`${LOG_PREFIX}     populateDir: ${dirPath} → ${entries.length} entries (recursive=${recursive})`);

      // Separate entries into dirs and files for parallel fetching
      const dirs: string[] = [];
      const files: { path: string; symlink?: string }[] = [];

      for (const entry of entries) {
        if (entry === '.' || entry === '..') continue;
        const fullPath = dirPath === '/' ? `/${entry}` : `${dirPath}/${entry}`;
        try {
          const stat = await this.vfs.overlay.stat(fullPath);
          if (stat && stat.type === 'dir') {
            try { FS.mkdirTree(fullPath); } catch { /* exists */ }
            if (recursive) dirs.push(fullPath);
          } else if (stat && stat.type === 'symlink' && stat.symlinkTarget) {
            files.push({ path: fullPath, symlink: stat.symlinkTarget });
          } else if (stat) {
            files.push({ path: fullPath });
          }
        } catch {
          // Stat failed — skip
        }
      }

      // Create symlinks synchronously, fetch regular files in parallel batches
      const BATCH_SIZE = 32;
      const regularFiles: string[] = [];
      for (const f of files) {
        if (f.symlink) {
          const dir = f.path.substring(0, f.path.lastIndexOf('/'));
          if (dir) { try { FS.mkdirTree(dir); } catch { /* exists */ } }
          try { FS.symlink(f.symlink, f.path); } catch { /* exists */ }
        } else {
          regularFiles.push(f.path);
        }
      }

      // Fetch files in parallel batches for much better throughput
      for (let i = 0; i < regularFiles.length; i += BATCH_SIZE) {
        const batch = regularFiles.slice(i, i + BATCH_SIZE);
        await Promise.all(batch.map(p => this.writeFileToProcessFS(FS, p)));
      }

      // Recurse into subdirectories
      for (const dir of dirs) {
        await this.populateDir(FS, dir, true);
      }
    } catch (e) {
      // Directory doesn't exist in kernel VFS — that's OK
      console.warn(`${LOG_PREFIX}     populateDir FAILED: ${dirPath}`, e);
    }
  }

  /**
   * Populate a directory in the process FS from a DIFFERENT VFS path.
   * Reads from vfsSrcPath in the kernel VFS, writes to fsDstPath in the process FS.
   * This enables mapping pre-built cache files to the expected cache location.
   */
  private async populateDirMapped(
    FS: EmscriptenInstance['FS'],
    vfsSrcPath: string,
    fsDstPath: string,
    recursive: boolean = true,
  ): Promise<void> {
    try {
      const entries = await this.vfs.overlay.readdir(vfsSrcPath);
      for (const entry of entries) {
        if (entry === '.' || entry === '..') continue;
        const srcFull = vfsSrcPath === '/' ? `/${entry}` : `${vfsSrcPath}/${entry}`;
        const dstFull = fsDstPath === '/' ? `/${entry}` : `${fsDstPath}/${entry}`;
        try {
          const stat = await this.vfs.overlay.stat(srcFull);
          if (stat && stat.type === 'dir') {
            try { FS.mkdirTree(dstFull); } catch { /* exists */ }
            if (recursive) {
              await this.populateDirMapped(FS, srcFull, dstFull, true);
            }
          } else if (stat) {
            // Fetch file data from VFS source path, write to process FS dest path
            const data = await this.vfs.fetchFile(srcFull);
            if (data) {
              const dir = dstFull.substring(0, dstFull.lastIndexOf('/'));
              if (dir) {
                try { FS.mkdirTree(dir); } catch { /* exists */ }
              }
              FS.writeFile(dstFull, data);
            }
          }
        } catch {
          // Stat failed — skip
        }
      }
    } catch (e) {
      console.warn(`${LOG_PREFIX}     populateDirMapped FAILED: ${vfsSrcPath} → ${fsDstPath}`, e);
    }
  }

  /**
   * Harvest output files from the process FS back to the kernel VFS.
   * Scans directories listed in descriptor.harvestDirs (default: CWD, /tmp).
   */
  private async harvestProcessFS(
    instance: EmscriptenInstance,
    descriptor: ToolDescriptor,
  ): Promise<void> {
    const FS = instance.FS;
    const harvestDirs = descriptor.harvestDirs || ['/home/user', '/tmp'];

    for (const dir of harvestDirs) {
      try {
        await this.harvestDir(FS, dir);
      } catch {
        // Directory may not exist in process FS
      }
    }
  }

  /**
   * Recursively harvest files from a process FS directory into the kernel VFS.
   * Only harvests files that are new or modified compared to what the kernel VFS has.
   */
  private async harvestDir(
    FS: EmscriptenInstance['FS'],
    dirPath: string,
  ): Promise<void> {
    let entries: string[];
    try {
      entries = FS.readdir(dirPath);
    } catch {
      return;
    }

    for (const entry of entries) {
      if (entry === '.' || entry === '..') continue;
      const fullPath = dirPath === '/' ? `/${entry}` : `${dirPath}/${entry}`;

      try {
        const stat = FS.stat(fullPath);
        if (FS.isDir(stat.mode)) {
          // Ensure directory exists in the kernel VFS (even if empty)
          try { await this.vfs.overlay.mkdir(fullPath); } catch { /* exists */ }
          // Recurse into subdirectories (with depth limit)
          await this.harvestDir(FS, fullPath);
        } else {
          // Read the file from the process FS
          const data = FS.readFile(fullPath, { encoding: undefined });
          if (data && data.length > 0) {
            // Write back to kernel VFS
            await this.vfs.overlay.writeFile(fullPath, data);
          }
        }
      } catch {
        // Skip files that can't be read
      }
    }
  }

  /**
   * Recursively populate a process FS directory from the kernel VFS.
   * Used to sync child-process output files back into the parent process FS
   * after a subprocess call. For example, if clang writes /tmp/emscripten_temp_xxx/main.o,
   * this method copies that file from VFS into emcc's process FS so emcc can read it.
   */
  async populateDirFromVFS(
    FS: EmscriptenInstance['FS'],
    dirPath: string,
  ): Promise<void> {
    let entries: string[];
    try {
      entries = await this.vfs.overlay.readdir(dirPath);
    } catch {
      return; // Directory doesn't exist in VFS
    }

    if (!entries || entries.length === 0) return;

    try { FS.mkdirTree(dirPath); } catch { /* exists */ }

    for (const name of entries) {
      if (name === '.' || name === '..') continue;
      const fullPath = dirPath === '/' ? `/${name}` : `${dirPath}/${name}`;

      try {
        const stat = await this.vfs.overlay.stat(fullPath);
        if (!stat) continue;

        if (stat.type === 'dir') {
          await this.populateDirFromVFS(FS, fullPath);
        } else if (stat.type === 'symlink' && stat.symlinkTarget) {
          try { FS.symlink(stat.symlinkTarget, fullPath); } catch { /* exists */ }
        } else {
          // Only write if the file doesn't already exist in the process FS,
          // or if the size differs (a crude "modified" check)
          let needsWrite = true;
          try {
            const existing = FS.stat(fullPath);
            if (existing && existing.size === stat.size) {
              needsWrite = false;
            }
          } catch {
            // File doesn't exist in process FS; will write
          }

          if (needsWrite) {
            const data = await this.vfs.fetchFile(fullPath);
            if (data) {
              const dir = fullPath.substring(0, fullPath.lastIndexOf('/'));
              if (dir) { try { FS.mkdirTree(dir); } catch { /* exists */ } }
              FS.writeFile(fullPath, data);
            }
          }
        }
      } catch {
        // Skip entries that can't be stat'd
      }
    }
  }

  /* ---------------------------------------------------------------- */
  /*  JavaScript evaluation (node emulation)                           */
  /* ---------------------------------------------------------------- */

  /**
   * Run an Emscripten-compiled JavaScript file in the browser.
   *
   * After emcc produces main.js + main.wasm, this evaluator:
   *   1. Reads main.js from the kernel VFS.
   *   2. Reads the companion .wasm file.
   *   3. Sets up a Module object with wasmBinary + print/printErr hooks.
   *   4. Evaluates the JS via `new Function()`.
   */
  private async runJavaScript(argv: string[], options: RunOptions = {}): Promise<ToolResult> {
    const tTotal = performance.now();
    const scriptPath = argv.length > 1 ? argv[1] : argv[0];
    console.log(`${LOG_PREFIX} ===== NODE (JS eval): ${scriptPath} =====`);

    // Special case: compiler.mjs is a heavy Node.js script that's used for
    // post-processing after clang compilation. The actual compilation is done
    // by clang, so we can safely skip the node execution and return minimal output.
    if (scriptPath.includes('compiler.mjs')) {
      console.log(`${LOG_PREFIX}   [SKIPPED] compiler.mjs (post-processing, already done by clang)`);
      // Return minimal valid JSON matching the format that compiler.mjs --symbols-only
      // produces (see src/jsifier.mjs runJSify symbolsOnly branch).
      // Python's get_js_sym_info() -> json.loads(output) expects:
      //   deps: dict[str, list[str]]  — JS symbol → native dependencies
      //   asyncFuncs: list[str]       — async JS library functions
      //   extraLibraryFuncs: list[str] — extra library function names
      const output = JSON.stringify({
        deps: {},
        asyncFuncs: [],
        extraLibraryFuncs: [],
      });
      options.onStdout?.(output);
      return { exitCode: 0, stdout: output, stderr: '' };
    }

    // Read the JS file from the kernel VFS
    console.log(`${LOG_PREFIX}   Reading JS file from VFS: ${scriptPath}`);
    const jsBytes = await this.vfs.fetchFile(scriptPath);
    if (!jsBytes) {
      const msg = `File not found: ${scriptPath}`;
      console.error(`${LOG_PREFIX} ❌ JS file not found: ${scriptPath}`);
      options.onStderr?.(msg);
      return { exitCode: 1, stdout: '', stderr: msg };
    }
    console.log(`${LOG_PREFIX}   JS file read: ${fmtSize(jsBytes.length)}`);

    const jsCode = new TextDecoder().decode(jsBytes);

    // Read companion .wasm (e.g., main.wasm next to main.js)
    const wasmPath = scriptPath.replace(/\.js$/, '.wasm');
    const wasmBytes = await this.vfs.fetchFile(wasmPath);
    console.log(`${LOG_PREFIX}   Companion WASM: ${wasmPath} — ${wasmBytes ? fmtSize(wasmBytes.length) : 'NOT FOUND'}`);

    const stdoutChunks: string[] = [];
    const stderrChunks: string[] = [];

    const printFn = (text: string) => {
      stdoutChunks.push(text);
      options.onStdout?.(text);
    };
    const printErrFn = (text: string) => {
      stderrChunks.push(text);
      options.onStderr?.(text);
    };

    try {
      const moduleConfig: Record<string, unknown> = {
        print: printFn,
        printErr: printErrFn,
        noExitRuntime: false,
        arguments: argv.slice(2),
      };

      if (wasmBytes) {
        moduleConfig['wasmBinary'] = wasmBytes.buffer.slice(
          wasmBytes.byteOffset,
          wasmBytes.byteOffset + wasmBytes.byteLength,
        );
      }

      // Create a simple loader for node: modules
      // This allows Node.js scripts to run in a browser-like environment
      const createNodeModuleLoader = () => {
        // Return stub objects for common Node APIs
        return {
          assert: { ok: () => { } },
          util: {
            parseArgs: () => ({ values: {}, positionals: [] }),
            debuglog: () => () => { },
          },
          fs: {
            readFileSync: () => '',
            writeFileSync: () => { },
            promises: { readFile: () => '' },
          },
          path: { resolve: (...args: any[]) => args[args.length - 1] },
          process: { argv: [], exit: () => { }, cwd: () => '/' },
          module: { exports: {} },
          require: (id: string) => createNodeModuleLoader()[id as keyof ReturnType<typeof createNodeModuleLoader>] || {},
        };
      };

      // Wrap the code to handle both import and require
      // Convert import statements to require-like calls
      let processedCode = jsCode;

      // This regex matches import statements - handle single and multiline
      processedCode = processedCode.replace(
        /import\s+(?:(?:\{[^}]*\})|(?:[a-zA-Z_$][a-zA-Z0-9_$]*))\s+from\s+['"]node:([^'"]+)['"]/g,
        (match) => {
          // Just comment out the import - we'll provide globals
          return '/* ' + match + ' */';
        }
      );

      // Handle multiline imports by converting them to no-ops
      processedCode = processedCode.replace(
        /import\s+\{[\s\S]*?\}\s+from\s+['"]node:([^'"]+)['"]/g,
        '/* multiline import removed */'
      );

      const nodeModules = createNodeModuleLoader();
      const globalStubs = Object.entries(nodeModules).map(
        ([name, obj]) => `const ${name} = ${JSON.stringify(obj, (_, v) => typeof v === 'function' ? '[Function]' : v)};`
      ).join('\n');

      const wrappedCode = `
        return (async function(Module) {
          ${globalStubs}
          ${processedCode}
          return Module;
        })(Module);
      `;
      console.log(`${LOG_PREFIX}   Evaluating Emscripten JS module...`);
      const tEval = performance.now();
      const fn = new Function('Module', wrappedCode);
      await fn(moduleConfig);
      console.log(`${LOG_PREFIX}   JS module evaluation completed in ${elapsed(tEval)}`);

      const exitCode =
        typeof moduleConfig['EXITSTATUS'] === 'number'
          ? (moduleConfig['EXITSTATUS'] as number)
          : 0;

      console.log(`${LOG_PREFIX} ===== NODE COMPLETE: exitCode=${exitCode}, total=${elapsed(tTotal)} =====`);

      return {
        exitCode,
        stdout: stdoutChunks.join('\n'),
        stderr: stderrChunks.join('\n'),
      };
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      console.error(`${LOG_PREFIX} ❌ JS eval error after ${elapsed(tTotal)}:`, msg);
      stderrChunks.push(msg);
      options.onStderr?.(msg);
      return {
        exitCode: 1,
        stdout: stdoutChunks.join('\n'),
        stderr: stderrChunks.join('\n'),
      };
    }
  }

  /* ---------------------------------------------------------------- */
  /*  Internal helpers                                                 */
  /* ---------------------------------------------------------------- */

  private resolveToolDescriptor(name: string): ToolDescriptor {
    // First, try direct registry lookup
    const descriptor = TOOL_REGISTRY[name];
    if (descriptor) {
      console.log(`${LOG_PREFIX}   resolveToolDescriptor: "${name}" → registry match`);
      return descriptor;
    }

    // If name is a path (e.g., /usr/bin/clang), extract the basename
    if (name.includes('/')) {
      const basename = name.split('/').pop() || name;
      const baseDescriptor = TOOL_REGISTRY[basename];
      if (baseDescriptor) {
        console.log(`${LOG_PREFIX}   resolveToolDescriptor: "${name}" → basename "${basename}" registry match`);
        return baseDescriptor;
      }
      // Check for full path patterns
      if (basename === 'python3' || basename === 'python' || basename === 'python3.14') {
        return {
          modulePath: '/usr/lib/python.wasm',
          preloadFiles: [...PYTHON_PRELOAD_FILES, ...PYTHON_INIT_FILES],
        };
      }
      if (basename === 'emcc' || basename === 'em++') {
        // Fall through to the emcc/em++ handler below
      }
    }

    if (name === 'python3' || name === 'python') {
      return {
        modulePath: '/usr/lib/python.wasm',
        preloadFiles: [...PYTHON_PRELOAD_FILES, ...PYTHON_INIT_FILES],
      };
    }

    // Normalize: use basename for emcc/em++ matching (handle /usr/lib/emscripten/emcc paths)
    const toolBasename = name.includes('/') ? (name.split('/').pop() || name) : name;
    if (toolBasename === 'emcc' || toolBasename === 'em++') {
      return {
        modulePath: '/usr/lib/python.wasm',
        preloadFiles: [
          ...PYTHON_PRELOAD_FILES,
          ...PYTHON_INIT_FILES,
          '/etc/emscripten.config',
        ],
        // Top-level .py files (non-recursive)
        preloadDirs: ['/usr/lib/emscripten', '/usr/bin'],
        // tools/ (Python package), src/ (JS libraries), third_party, and
        // system headers (include + lib header subdirs needed by ensure_sysroot).
        // system/lib/ (5565 source files) is NOT preloaded eagerly — the
        // pre-built cache in cache-lib/ provides compiled .a files, so emcc
        // doesn't need to recompile system libraries.  If a child process
        // (clang) needs individual source files, populateDir is called on
        // demand via extraPreloadDirs during subprocess dispatch.
        // 
        // However, ensure_sysroot() needs to copy certain subdirs like
        // compiler-rt/include, libcxx/include, etc., so include those.
        preloadDirsRecursive: [
          '/usr/lib/emscripten/tools',
          '/usr/lib/emscripten/src',
          '/usr/lib/emscripten/third_party',
          '/usr/lib/emscripten/system/include',
          '/usr/lib/emscripten/system/lib/compiler-rt',
          '/usr/lib/emscripten/system/lib/libcxx',
          '/usr/lib/emscripten/system/lib/libcxxabi',
          '/usr/lib/emscripten/system/lib/libunwind',
          '/usr/lib/emscripten/system/lib/llvm-libc',
          '/usr/lib/emscripten/system/lib/mimalloc',
          '/usr/lib/emscripten/system/bin',
        ],
      };
    }

    throw new Error(`Unknown tool: ${name}`);
  }

  private async loadModuleFactory(wasmPath: string): Promise<ModuleFactory> {
    console.log(`${LOG_PREFIX}   loadModuleFactory: ${wasmPath}`);
    return loadModuleFactory(wasmPath, {
      getGlueUrl: (path) => {
        const baseUrl = this.vfs.getUrl(path);
        if (baseUrl) {
          return baseUrl.replace(/\.(br|gz)$/, '').replace(/\.wasm$/, '.mjs');
        }
        return path.replace('.wasm', '.mjs');
      },
    }) as Promise<ModuleFactory>;
  }

  private parseCommand(cmd: string): string[] {
    const tokens: string[] = [];
    let current = '';
    let inQuote: string | null = null;
    for (const ch of cmd) {
      if (inQuote) {
        if (ch === inQuote) {
          inQuote = null;
        } else {
          current += ch;
        }
      } else if (ch === '"' || ch === "'") {
        inQuote = ch;
      } else if (ch === ' ' || ch === '\t') {
        if (current) {
          tokens.push(current);
          current = '';
        }
      } else {
        current += ch;
      }
    }
    if (current) tokens.push(current);
    return tokens;
  }
}
