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
import { mountVFSFS } from './vfs/emscripten-vfsfs';
import type { VFSManager } from './vfs/index';

const LOG_PREFIX = '[Emception:Kernel]';
function elapsed(t0: number): string { return `${(performance.now() - t0).toFixed(1)}ms`; }
function fmtSize(n: number): string {
  if (n < 1024) return `${n}B`;
  if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)}KB`;
  return `${(n / (1024 * 1024)).toFixed(1)}MB`;
}

/** Sentinel error thrown by WASI proc_exit to unwind the call stack. */
class WasiExit extends Error {
  code: number;
  constructor(code: number) {
    super(`WASI proc_exit(${code})`);
    this.code = code;
  }
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
  /**
   * When true, the tool invocation is an info/version query (e.g. --version).
   * setupProcessFS will skip mounting VFSFS since the tool doesn't need
   * filesystem access.
   */
  isInfoQuery?: boolean;
}

/* ------------------------------------------------------------------ */
/*  Tool descriptors — standalone .wasm modules                        */
/* ------------------------------------------------------------------ */

interface ToolDescriptor {
  /** Path to the standalone .wasm module (also used to derive the .mjs glue URL) */
  modulePath: string;
  /** Directories whose contents should be harvested back to kernel VFS after run */
  harvestDirs?: string[];
}

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

    // Special case: 'wasi-run' executes a compiled standalone WASM binary
    // using a minimal in-browser WASI runtime.
    if (toolBasename === 'wasi-run') {
      console.log(`${LOG_PREFIX}   Dispatching to runWasi (WASI runtime)`);
      return this.runWasi(argv, options);
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

    let descriptor: ReturnType<typeof this.resolveToolDescriptor>;
    try {
      descriptor = this.resolveToolDescriptor(tool);
    } catch {
      const msg = `${toolBasename}: command not found`;
      console.warn(`${LOG_PREFIX}   ${msg}`);
      options.onStderr?.(msg);
      return { exitCode: 127, stdout: '', stderr: msg };
    }
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

    // Capture a reference to the VFS for locateFile
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
        if (isPythonTool) console.log(`${LOG_PREFIX}   [print] ${text}`);
        options.onStdout?.(text);
      },
      printErr: (text: string) => {
        stderrChunks.push(text);
        console.error(`${LOG_PREFIX}   [printErr] ${text}`);
        options.onStderr?.(text);
      },
      stdin: options.stdin ?? (() => null),
      // Resolve .wasm URL from the CDN manifest
      locateFile: (path: string) => {
        if (path.endsWith('.wasm')) {
          const url = vfs.getUrl(descriptor.modulePath);
          if (url) {
            // Blob URLs are ready to use; CDN URLs need compression suffix stripped
            return url.startsWith('blob:') ? url : url.replace(/\.(br|gz)$/, '');
          }
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

            const subBasename = parts[0].split('/').pop() ?? parts[0];
            const isVersionCheck = parts.includes('--version') || parts.includes('-v');
            if (OPTIONAL_TOOLS.has(subBasename) && !isVersionCheck) {
              console.log(`${LOG_PREFIX}   [subprocess] Skipping optional tool "${subBasename}" — returning no-op (exit 0)`);
              instanceRef.FS.writeFile('/tmp/.subprocess_stdout', '');
              instanceRef.FS.writeFile('/tmp/.subprocess_stderr', '');
              return (0 << 8) | 0;
            }

            // With VFSFS write-through, writes from the parent process
            // go to VFS immediately.  The child process's VFSFS mount
            // will lazily read those files via JSPI.

            const subStdout: string[] = [];
            const subStderr: string[] = [];
            const subResult = await runner.run(parts[0], parts, {
              cwd: request.cwd || options.cwd,
              onStdout: (t) => subStdout.push(t),
              onStderr: (t) => subStderr.push(t),
              isInfoQuery: isVersionCheck,
            });

            // With VFSFS write-through, the child's output files are
            // already in VFS.  The parent's VFSFS will lazily load them
            // on next access via JSPI.  No re-population needed.

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

    // Guard: if the factory returned an object without FS (stub .mjs that
    // has no real WASM module behind it), treat the tool as a no-op.
    // Tools like llvm-objcopy, llvm-ar, llvm-nm, llc may not have compiled
    // WASM modules — they are optional post-processing tools whose absence
    // does not prevent a working output binary.
    if (!instance.FS) {
      console.warn(`${LOG_PREFIX}   ⚠️ No FS on instance — stub module, returning no-op (exit 0)`);
      console.log(`${LOG_PREFIX}   Process complete (no-op stub, total spawn: ${elapsed(tSpawn)})`);
      return {
        exitCode: 0,
        stdout: stdoutChunks.join('\n'),
        stderr: stderrChunks.join('\n'),
      };
    }

    // Step 3: Mount VFSFS + install JSPI hooks for on-demand file loading
    const tFS = performance.now();
    console.log(`${LOG_PREFIX}   Step 3/4: Mounting VFSFS + JSPI hooks...`);
    moduleConfig['__modulePath'] = descriptor.modulePath;
    const fileData = this.setupProcessFS(instance, moduleConfig, options);

    // For Python-based tools, inject the subprocess shim to replace stdlib subprocess
    if (isPythonTool) {
      // Inject shim into the VFSFS fileData map (not MEMFS writeFile)
      try {
        const shimBytes = typeof SUBPROCESS_SHIM === 'string'
          ? new TextEncoder().encode(SUBPROCESS_SHIM)
          : SUBPROCESS_SHIM;
        fileData.set('/usr/lib/python3.14/subprocess.py', shimBytes as Uint8Array);
        console.log(`${LOG_PREFIX}   Injected subprocess shim`);
      } catch {
        console.warn(`${LOG_PREFIX}   ⚠️ Failed to inject subprocess shim`);
      }

      // Inject a sitecustomize.py that:
      // 1. Replaces sys.stderr with a safe file-backed writer (fd 2 is
      //    broken in WASM — WASI errno EBADF=8 on write).
      // 2. Installs a custom excepthook that writes unhandled exceptions
      //    to /tmp/python_error.txt so the tool-runner can read them after
      //    the process exits (Python's normal stderr is broken in WASM).
      try {
        const SITE_CUSTOMIZE = `
import sys, io, traceback as _tb

class _SafeStderr(io.TextIOBase):
    """File-backed stderr replacement since WASM fd 2 yields EBADF."""
    def write(self, s):
        try:
            with open('/tmp/stderr.log', 'a') as f:
                f.write(str(s))
        except: pass
        return len(str(s))
    def flush(self): pass
    def writable(self): return True
    @property
    def encoding(self): return 'utf-8'
    @property
    def errors(self): return 'backslashreplace'

sys.stderr = _SafeStderr()
sys.__stderr__ = sys.stderr

_orig = sys.excepthook
def _hook(t, v, tb):
    try:
        with open('/tmp/python_error.txt', 'w') as f:
            _tb.print_exception(t, v, tb, file=f)
    except: pass
    try: _orig(t, v, tb)
    except: pass
sys.excepthook = _hook
`;
        fileData.set('/usr/lib/python3.14/sitecustomize.py', new TextEncoder().encode(SITE_CUSTOMIZE));
        console.log(`${LOG_PREFIX}   Injected sitecustomize.py (safe stderr + exception capture)`);
      } catch {
        console.warn(`${LOG_PREFIX}   ⚠️ Failed to inject sitecustomize.py`);
      }

      // Create sentinel stub files for all LLVM/Binaryen tools in /usr/bin/.
      // Emscripten's Python code (shared.py check_llvm_version, building.py
      // get_binaryen_version) calls os.path.exists() on these paths.
      // The actual execution goes through the subprocess shim → ToolRunner,
      // but the existence check happens directly on the Emscripten FS.
      // With VFSFS, these stubs go into the fileData map.
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
      for (const stubPath of TOOL_STUBS) {
        fileData.set(stubPath, STUB);
      }
      console.log(`${LOG_PREFIX}   Created ${TOOL_STUBS.length} tool stubs in fileData`);
    }
    console.log(`${LOG_PREFIX}   Step 3/4 done: FS set up in ${elapsed(tFS)}`);

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

    // Debug: dump process FS state after Python tool exits to diagnose linking failures
    if (isPythonTool && instance.FS) {
      try {
        // List files in home directory to see what emcc produced
        const homeFiles = instance.FS.readdir('/home/user').filter((f: string) => f !== '.' && f !== '..');
        console.log(`${LOG_PREFIX}   [DEBUG] /home/user/ files: ${JSON.stringify(homeFiles)}`);

        // List files in /tmp/ for any emcc temp artifacts
        const tmpFiles = instance.FS.readdir('/tmp').filter((f: string) => f !== '.' && f !== '..');
        console.log(`${LOG_PREFIX}   [DEBUG] /tmp/ files: ${JSON.stringify(tmpFiles)}`);

        // Check if the emscripten cache was populated correctly
        try {
          const cacheFiles = instance.FS.readdir('/home/user/.emscripten_cache/sysroot/lib/wasm32-emscripten')
            .filter((f: string) => f !== '.' && f !== '..');
          console.log(`${LOG_PREFIX}   [DEBUG] Cache lib files (first 10): ${JSON.stringify(cacheFiles.slice(0, 10))} (total: ${cacheFiles.length})`);
        } catch {
          console.error(`${LOG_PREFIX}   [DEBUG] Cache lib dir NOT FOUND at /home/user/.emscripten_cache/sysroot/lib/wasm32-emscripten`);
        }

        // Check for emcc-generated temp files in /tmp
        for (const f of tmpFiles) {
          if (f.startsWith('emscripten_temp') || f.endsWith('.json') || f.endsWith('.txt')) {
            try {
              const content = new TextDecoder().decode(instance.FS.readFile(`/tmp/${f}`));
              console.log(`${LOG_PREFIX}   [DEBUG] /tmp/${f}: ${content.slice(0, 500)}`);
            } catch { /* skip binary files */ }
          }
        }

        // Read Python exception capture file if it exists
        try {
          const errContent = new TextDecoder().decode(instance.FS.readFile('/tmp/python_error.txt'));
          console.error(`${LOG_PREFIX}   [DEBUG] PYTHON EXCEPTION:\n${errContent}`);
          // Forward to terminal so the user sees it
          for (const line of errContent.split('\n')) {
            if (line.length > 0) {
              stderrChunks.push(line);
              options.onStderr?.(line);
            }
          }
        } catch { /* file doesn't exist — no unhandled exception */ }

        // Read Python stderr log (redirected from broken fd 2)
        try {
          const stderrLog = new TextDecoder().decode(instance.FS.readFile('/tmp/stderr.log'));
          if (stderrLog.length > 0) {
            console.log(`${LOG_PREFIX}   [DEBUG] Python stderr.log:\n${stderrLog.slice(0, 2000)}`);
            // Forward each line to the onStderr callback so it appears in the terminal
            for (const line of stderrLog.split('\n')) {
              if (line.length > 0) {
                stderrChunks.push(line);
                options.onStderr?.(line);
              }
            }
          }
        } catch { /* file doesn't exist — no stderr output */ }
      } catch (e) {
        console.warn(`${LOG_PREFIX}   [DEBUG] FS dump failed:`, e);
      }
    }

    // With VFSFS write-through, output files are already in VFS.
    // No explicit harvest step needed.

    // Process is done — let GC reclaim the WASM linear memory
    console.log(`${LOG_PREFIX}   Process complete, releasing WASM instance (total spawn: ${elapsed(tSpawn)})`);

    return {
      exitCode,
      stdout: stdoutChunks.join('\n'),
      stderr: stderrChunks.join('\n'),
    };
  }

  /* ---------------------------------------------------------------- */
  /*  Process FS bridging (VFSFS mount + JSPI hooks)                   */
  /* ---------------------------------------------------------------- */

  /**
   * Set up the process FS for a tool invocation using VFSFS mounts.
   *
   * Instead of copying files or patching lookupPath, this method:
   *   1. Mounts VFSFS at /usr, /etc (backed by kernel VFS + JSPI on-demand fetch)
   *   2. Registers path aliases for sysroot cache mapping
   *   3. Creates essential synthetic files (shim, config, stubs)
   *   4. Sets CWD
   *
   * File loading is entirely on-demand via JSPI: the patched glue code
   * (patch 6) calls Module["onPreOpen"]/["onPreStat"] before each syscall,
   * which fetches from CDN → IDB → memCache and suspends the WASM stack.
   *
   * When `options.isInfoQuery` is true, only basic dir creation + CWD is done.
   */
  private setupProcessFS(
    instance: EmscriptenInstance,
    moduleConfig: Record<string, unknown>,
    options: RunOptions,
  ): Map<string, Uint8Array> {
    const FS = instance.FS;
    const isInfoQuery = options.isInfoQuery === true;
    const isPythonDescriptor = (moduleConfig['__modulePath'] as string || '').includes('python');

    // For info queries (--version), just set CWD
    if (isInfoQuery) {
      console.log(`${LOG_PREFIX}     [FAST] Info query — skipping FS setup`);
      const cwd = options.cwd || '/home/user';
      try { FS.mkdirTree(cwd); } catch { /* exists */ }
      try { FS.chdir(cwd); } catch { /* ignore */ }
      return new Map();
    }

    // Path aliases for sysroot cache mapping
    const pathAliases = new Map<string, string>();
    if (isPythonDescriptor) {
      pathAliases.set(
        '/home/user/.emscripten_cache/sysroot/lib',
        '/usr/lib/emscripten/cache-lib',
      );
      pathAliases.set(
        '/home/user/.emscripten_cache/sysroot/include',
        '/usr/include',
      );
    }

    // Mount VFSFS at system paths — all file access goes through VFS + JSPI
    const fileData = mountVFSFS(FS, moduleConfig, this.vfs, {
      mountPoints: ['/usr', '/etc'],
      pathAliases,
    });

    // Sysroot scaffold (Python tools only)
    if (isPythonDescriptor) {
      try { FS.mkdirTree('/home/user/.emscripten_cache/sysroot'); } catch { /* exists */ }
      FS.writeFile('/home/user/.emscripten_cache/sysroot_install.stamp', 'prebuilt');
      try { FS.mkdirTree('/home/user/.emscripten_cache/sysroot/lib'); } catch { /* exists */ }
      try { FS.mkdirTree('/home/user/.emscripten_cache/sysroot/include'); } catch { /* exists */ }
      console.log(`${LOG_PREFIX}     Sysroot dirs created with ${pathAliases.size} path aliases`);
    }

    // Set CWD
    const cwd = options.cwd || '/home/user';
    try { FS.mkdirTree(cwd); } catch { /* exists */ }
    try { FS.chdir(cwd); } catch { /* ignore */ }

    return fileData;
  }

  /* ---------------------------------------------------------------- */
  /*  WASI runtime (standalone WASM execution)                         */
  /* ---------------------------------------------------------------- */

  /**
   * Run a standalone WASM binary compiled by emcc with a minimal WASI runtime.
   *
   * When emcc compiles to a .wasm output, it produces a standalone binary that
   * uses WASI (WebAssembly System Interface) imports for I/O.  This method
   * provides fd_write (stdout/stderr), proc_exit, and stubs for the remaining
   * WASI snapshot_preview1 imports so that simple programs (printf, return 0)
   * can execute directly in the browser without Node.js.
   */
  private async runWasi(argv: string[], options: RunOptions = {}): Promise<ToolResult> {
    const tTotal = performance.now();
    // argv: ['wasi-run', '/home/user/main.wasm', ...extra args]
    const wasmPath = argv.length > 1 ? argv[1] : '/home/user/main.wasm';
    console.log(`${LOG_PREFIX} ===== WASI RUN: ${wasmPath} =====`);

    // 1. Read the WASM binary from the VFS
    const wasmBytes = await this.vfs.fetchFile(wasmPath);
    if (!wasmBytes) {
      const msg = `WASI run: compiled WASM not found: ${wasmPath}`;
      console.error(`${LOG_PREFIX} ❌ ${msg}`);
      options.onStderr?.(msg);
      return { exitCode: 1, stdout: '', stderr: msg };
    }
    console.log(`${LOG_PREFIX}   WASM binary: ${fmtSize(wasmBytes.length)}`);

    const stdoutChunks: string[] = [];
    const stderrChunks: string[] = [];
    let exitCode = 0;

    // 2. Build minimal WASI imports
    const decoder = new TextDecoder();

    // WASI fd_write: write iovs to a file descriptor and return bytes written.
    // fd 1 = stdout, fd 2 = stderr
    const fd_write = (
      fd: number,
      iovsPtr: number,
      iovsLen: number,
      nwrittenPtr: number,
    ): number => {
      if (!memory) {
        console.error(`${LOG_PREFIX}   [WASI] fd_write called but memory is not set!`);
        return 8; // EBADF
      }
      const mem = new DataView(memory.buffer);
      let totalWritten = 0;
      for (let i = 0; i < iovsLen; i++) {
        const base = iovsPtr + i * 8;
        const ptr = mem.getUint32(base, true);
        const len = mem.getUint32(base + 4, true);
        const bytes = new Uint8Array(memory.buffer, ptr, len);
        const text = decoder.decode(bytes, { stream: true });
        totalWritten += len;
        if (fd === 1) {
          // Split on newlines and emit each line
          const lines = text.split('\n');
          for (let j = 0; j < lines.length; j++) {
            const line = lines[j];
            if (j < lines.length - 1) {
              // Complete line
              stdoutChunks.push(line);
              options.onStdout?.(line);
            } else if (line.length > 0) {
              // Partial line (no trailing newline)
              stdoutChunks.push(line);
              options.onStdout?.(line);
            }
          }
        } else if (fd === 2) {
          const lines = text.split('\n');
          for (let j = 0; j < lines.length; j++) {
            const line = lines[j];
            if (j < lines.length - 1) {
              stderrChunks.push(line);
              options.onStderr?.(line);
            } else if (line.length > 0) {
              stderrChunks.push(line);
              options.onStderr?.(line);
            }
          }
        }
      }
      mem.setUint32(nwrittenPtr, totalWritten, true);
      return 0; // ESUCCESS
    };

    // environ_sizes_get: return 0 env vars
    const environ_sizes_get = (countPtr: number, sizePtr: number): number => {
      const mem = new DataView(memory.buffer);
      mem.setUint32(countPtr, 0, true);
      mem.setUint32(sizePtr, 0, true);
      return 0;
    };

    // environ_get: no-op (zero env vars)
    const environ_get = (): number => 0;

    // args: return program name + any extra args
    const programArgs = argv.slice(1); // ['main.wasm', ...extra]
    const encodedArgs = programArgs.map((a) => new TextEncoder().encode(a + '\0'));
    const totalArgSize = encodedArgs.reduce((sum, a) => sum + a.length, 0);

    const args_sizes_get = (countPtr: number, sizePtr: number): number => {
      const mem = new DataView(memory.buffer);
      mem.setUint32(countPtr, programArgs.length, true);
      mem.setUint32(sizePtr, totalArgSize, true);
      return 0;
    };

    const args_get = (argvPtr: number, argvBufPtr: number): number => {
      const mem = new DataView(memory.buffer);
      const buf = new Uint8Array(memory.buffer);
      let offset = argvBufPtr;
      for (let i = 0; i < encodedArgs.length; i++) {
        mem.setUint32(argvPtr + i * 4, offset, true);
        buf.set(encodedArgs[i], offset);
        offset += encodedArgs[i].length;
      }
      return 0;
    };

    const proc_exit = (code: number): void => {
      exitCode = code;
      console.log(`${LOG_PREFIX}   WASI proc_exit(${code})`);
      throw new WasiExit(code);
    };

    const fd_close = (): number => 0;
    const fd_seek = (): number => 8; // EBADF — not seekable
    const fd_read = (
      _fd: number, _iovsPtr: number, _iovsLen: number, nreadPtr: number,
    ): number => {
      const mem = new DataView(memory.buffer);
      mem.setUint32(nreadPtr, 0, true); // EOF
      return 0;
    };
    const fd_fdstat_get = (fd: number, statPtr: number): number => {
      const mem = new DataView(memory.buffer);
      // fs_filetype: REGULAR_FILE=4, CHARACTER_DEVICE=2
      mem.setUint8(statPtr, fd <= 2 ? 2 : 4); // filetype
      mem.setUint16(statPtr + 2, 0, true); // fs_flags
      // rights_base and rights_inheriting (8 bytes each, zero them)
      mem.setBigUint64(statPtr + 8, BigInt(0), true);
      mem.setBigUint64(statPtr + 16, BigInt(0), true);
      return 0;
    };
    const fd_prestat_get = (): number => 8; // EBADF — no preopened dirs
    const fd_prestat_dir_name = (): number => 8;
    const clock_time_get = (
      _id: number, _precision: bigint, timePtr: number,
    ): number => {
      const mem = new DataView(memory.buffer);
      const now = BigInt(Math.round(performance.now() * 1_000_000)); // nanoseconds
      mem.setBigUint64(timePtr, now, true);
      return 0;
    };
    const random_get = (bufPtr: number, bufLen: number): number => {
      const buf = new Uint8Array(memory.buffer, bufPtr, bufLen);
      crypto.getRandomValues(buf);
      return 0;
    };

    const wasiImports: Record<string, Function> = {
      fd_write,
      fd_read,
      fd_close,
      fd_seek,
      fd_fdstat_get,
      fd_prestat_get,
      fd_prestat_dir_name,
      proc_exit,
      environ_sizes_get,
      environ_get,
      args_sizes_get,
      args_get,
      clock_time_get,
      random_get,
      // Stubs for less common WASI calls
      path_open: () => 44, // ENOENT
      path_filestat_get: () => 44,
      path_create_directory: () => 63, // ENOSYS
      path_remove_directory: () => 63,
      path_unlink_file: () => 63,
      path_rename: () => 63,
      path_readlink: () => 63,
      path_symlink: () => 63,
      fd_advise: () => 0,
      fd_allocate: () => 63,
      fd_datasync: () => 0,
      fd_sync: () => 0,
      fd_tell: () => 63,
      fd_readdir: () => 63,
      fd_renumber: () => 63,
      fd_pwrite: () => 63,
      fd_pread: () => 63,
      poll_oneoff: () => 63,
      sched_yield: () => 0,
      sock_accept: () => 63,
      sock_recv: () => 63,
      sock_send: () => 63,
      sock_shutdown: () => 63,
    };

    // 3. Compile and instantiate

    let memory: WebAssembly.Memory = undefined as unknown as WebAssembly.Memory;

    try {
      console.log(`${LOG_PREFIX}   Compiling WASM module...`);
      const tCompile = performance.now();
      const wasmBuffer = new ArrayBuffer(wasmBytes.byteLength);
      new Uint8Array(wasmBuffer).set(wasmBytes);
      const wasmModule = await WebAssembly.compile(wasmBuffer);
      console.log(`${LOG_PREFIX}   WASM compiled in ${elapsed(tCompile)}`);

      // Inspect required imports to build the import object dynamically
      const importDescs = WebAssembly.Module.imports(wasmModule);
      const importObject: Record<string, Record<string, WebAssembly.ImportValue>> = {};

      for (const imp of importDescs) {
        if (!importObject[imp.module]) {
          importObject[imp.module] = {};
        }
        if (imp.module === 'wasi_snapshot_preview1') {
          importObject[imp.module][imp.name] =
            wasiImports[imp.name] ??
            ((..._args: unknown[]) => {
              console.warn(`${LOG_PREFIX}   WASI stub called: ${imp.name}`);
              return 0;
            });
        } else if (imp.module === 'wasi_unstable') {
          // Older WASI — map the same implementations
          importObject[imp.module][imp.name] =
            wasiImports[imp.name] ??
            ((..._args: unknown[]) => {
              console.warn(`${LOG_PREFIX}   WASI unstable stub: ${imp.name}`);
              return 0;
            });
        } else if (imp.kind === 'memory') {
          const mem = new WebAssembly.Memory({ initial: 256, maximum: 16384 });
          importObject[imp.module][imp.name] = mem;
          memory = mem;
        } else if (imp.kind === 'table') {
          importObject[imp.module][imp.name] = new WebAssembly.Table({
            initial: 0,
            element: 'anyfunc',
          });
        } else {
          // Stub unknown import (Emscripten env functions, etc.)
          importObject[imp.module][imp.name] = (..._args: unknown[]) => {
            // Only warn once per import to avoid log spam
            console.warn(`${LOG_PREFIX}   Unknown import stub: ${imp.module}.${imp.name}`);
            return 0;
          };
        }
      }

      console.log(`${LOG_PREFIX}   Instantiating WASM...`);
      const tInst = performance.now();
      const instance = await WebAssembly.instantiate(wasmModule, importObject);
      console.log(`${LOG_PREFIX}   WASM instantiated in ${elapsed(tInst)}`);

      // Get the memory export (most WASM modules export their memory)
      if (!memory && instance.exports.memory) {
        memory = instance.exports.memory as WebAssembly.Memory;
      }

      // Log exports and imports for debugging
      const exportNames = Object.keys(instance.exports);
      console.log(`${LOG_PREFIX}   WASM exports: [${exportNames.join(', ')}]`);

      // 4. Call _start (WASI entry point) or main
      const startFn = instance.exports._start as (() => void) | undefined;
      const mainFn = instance.exports.main as ((argc: number, argv: number) => number) | undefined;
      const initFn = instance.exports.__wasm_call_ctors as (() => void) | undefined;

      // Call global constructors if present
      if (initFn) {
        try { initFn(); } catch { /* ok */ }
      }

      if (startFn) {
        console.log(`${LOG_PREFIX}   Calling _start()...`);
        try {
          startFn();
        } catch (e) {
          if (e instanceof WasiExit) {
            exitCode = e.code;
          } else {
            throw e;
          }
        }
      } else if (mainFn) {
        console.log(`${LOG_PREFIX}   Calling main()...`);
        exitCode = mainFn(0, 0);
      } else {
        console.warn(`${LOG_PREFIX}   No _start or main export found`);
        exitCode = 0;
      }

      console.log(`${LOG_PREFIX} ===== WASI COMPLETE: exitCode=${exitCode}, total=${elapsed(tTotal)} =====`);
    } catch (e) {
      if (e instanceof WasiExit) {
        exitCode = e.code;
        console.log(`${LOG_PREFIX} ===== WASI COMPLETE (proc_exit): exitCode=${exitCode}, total=${elapsed(tTotal)} =====`);
      } else {
        const msg = e instanceof Error ? e.message : String(e);
        console.error(`${LOG_PREFIX} ❌ WASI run error after ${elapsed(tTotal)}:`, msg);
        if (e instanceof Error && e.stack) {
          console.error(`${LOG_PREFIX}   Stack: ${e.stack}`);
        }
        stderrChunks.push(msg);
        options.onStderr?.(msg);
        return {
          exitCode: 1,
          stdout: stdoutChunks.join('\n'),
          stderr: stderrChunks.join('\n'),
        };
      }
    }

    return {
      exitCode,
      stdout: stdoutChunks.join('\n'),
      stderr: stderrChunks.join('\n'),
    };
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
        return { modulePath: '/usr/lib/python.wasm' };
      }
      if (basename === 'emcc' || basename === 'em++') {
        // Fall through to the emcc/em++ handler below
      }
    }

    if (name === 'python3' || name === 'python') {
      return { modulePath: '/usr/lib/python.wasm' };
    }

    // Normalize: use basename for emcc/em++ matching (handle /usr/lib/emscripten/emcc paths)
    const toolBasename = name.includes('/') ? (name.split('/').pop() || name) : name;
    if (toolBasename === 'emcc' || toolBasename === 'em++') {
      return { modulePath: '/usr/lib/python.wasm' };
    }

    throw new Error(`Unknown tool: ${name}`);
  }

  private async loadModuleFactory(wasmPath: string): Promise<ModuleFactory> {
    console.log(`${LOG_PREFIX}   loadModuleFactory: ${wasmPath}`);

    // Preload the bundle containing this WASM so getUrl returns blob URLs
    const bundleName = this.vfs.getBundleForFile(wasmPath);
    if (bundleName) {
      console.log(`${LOG_PREFIX}   loadModuleFactory: preloading bundle "${bundleName}" for ${wasmPath}`);
      await this.vfs.preloadBundle(bundleName);
    }

    return loadModuleFactory(wasmPath, {
      getGlueUrl: (path) => {
        // For bundled files: get the .mjs blob URL directly
        const mjsPath = path.replace(/\.wasm$/, '.mjs');
        const mjsUrl = this.vfs.getUrl(mjsPath);
        if (mjsUrl && mjsUrl.startsWith('blob:')) return mjsUrl;
        // For non-bundled files: derive .mjs URL from .wasm URL
        const baseUrl = this.vfs.getUrl(path);
        if (baseUrl) {
          return baseUrl.replace(/\.(br|gz)$/, '').replace(/\.wasm$/, '.mjs');
        }
        // Fallback: add /cdn prefix for files not in manifest
        return `/cdn${path.replace('.wasm', '.mjs')}`;
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
