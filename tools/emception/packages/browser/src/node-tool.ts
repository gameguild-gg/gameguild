/**
 * In-browser execution of Emscripten's Node.js build tools
 * (`tools/compiler.mjs`, `tools/preprocessor.mjs`,
 * `tools/acorn-optimizer.mjs`, ...).
 *
 * Python's emcc driver invokes these tools via `/usr/bin/node <tool> args`
 * with the tool's stdin (e.g. the settings JSON) piped in. This module runs
 * the real tool sources inside the Worker:
 *
 *   1. Tool sources (and their `/usr/lib/emscripten` dependency tree) are
 *      read from the kernel VFS.
 *   2. Imports are rewritten so the ESM graph is preserved via blob URLs:
 *      relative/bare specifiers become VFS paths, then VFS paths and `node:*`
 *      specifiers are mapped to blob URLs (builtins → shim modules).
 *   3. Node builtins (`node:fs`, `node:path`, `node:vm`, ...) are shimmed,
 *      with fs backed by the VFS (write cache for sync reads).
 *   4. `process.stdout` / `console` writes are captured and forwarded to the
 *      caller so the waiting Python process receives the generated JS glue +
 *      `//FORWARDED_DATA:` payload.
 */

/** Minimal VFS surface needed by the loader (subset of VFSManager). */
export interface NodeToolVfs {
  fetchFile(path: string): Promise<Uint8Array | null>;
  readFileSync(path: string): Uint8Array | null;
  writeFileSync(path: string, data: Uint8Array): void;
  existsSync(path: string): boolean;
  readdirSync(path: string): string[];
  statSync(path: string): { type: 'file' | 'dir'; size: number } | null;
  deleteFileSync(path: string): boolean;
  mkdirSync(path: string): void;
}

export interface NodeToolRunOptions {
  /** Working directory of the emulated process (affects path resolution). */
  cwd: string;
  /** argv, including argv[0] (the node binary path) and the script path. */
  argv: string[];
  /** Environment variables for the emulated process. */
  env?: Record<string, string>;
  /** Stdin content (settings JSON piped by Python's run_js_tool). */
  stdin?: string;
  /** Stdout listener (called with each write chunk, not lines). */
  onStdout?: (text: string) => void;
  /** Stderr listener. */
  onStderr?: (text: string) => void;
}

export interface NodeToolResult {
  exitCode: number;
  stdout: string;
  stderr: string;
}

const LOG_PREFIX = '[Emception:NodeTool]';
/** Worker console captured before runNodeTool swaps the global for the tool. */
const rawConsole = console;
const NODE_BUILTIN_PREFIX = 'node:';
const STDIN_FD = 0;
/** Global registry the shim modules read their implementation from. */
const SHIM_REGISTRY_KEY = '__emceptionNodeShims';

/* ------------------------------------------------------------------ */
/*  POSIX path helpers (node:path compatible for our usage)            */
/* ------------------------------------------------------------------ */

/** Normalize a VFS path: resolve `.`, `..`, collapse duplicate slashes. */
export function normalizeVfsPath(p: string): string {
  const absolute = p.startsWith('/');
  const parts: string[] = [];
  for (const part of p.split('/')) {
    if (!part || part === '.') continue;
    if (part === '..') {
      if (parts.length && parts[parts.length - 1] !== '..') parts.pop();
      else if (!absolute) parts.push('..');
      continue;
    }
    parts.push(part);
  }
  const joined = parts.join('/');
  return absolute ? `/${joined}` : joined || '.';
}

export const posixPath = {
  isAbsolute(p: string): boolean {
    return p.startsWith('/');
  },
  join(...parts: string[]): string {
    return normalizeVfsPath(parts.filter((part) => part !== '').join('/')) || '.';
  },
  resolve(...parts: string[]): string {
    return normalizeVfsPath(parts.join('/'));
  },
  dirname(p: string): string {
    const normalized = normalizeVfsPath(p);
    const idx = normalized.lastIndexOf('/');
    if (idx <= 0) return idx === 0 ? '/' : '.';
    return normalized.slice(0, idx);
  },
  basename(p: string): string {
    const normalized = normalizeVfsPath(p);
    const idx = normalized.lastIndexOf('/');
    return idx >= 0 ? normalized.slice(idx + 1) : normalized;
  },
  extname(p: string): string {
    const base = posixPath.basename(p);
    const idx = base.lastIndexOf('.');
    return idx > 0 ? base.slice(idx) : '';
  },
  relative(from: string, to: string): string {
    const a = normalizeVfsPath(from).split('/');
    const b = normalizeVfsPath(to).split('/');
    let i = 0;
    while (i < a.length && i < b.length && a[i] === b[i]) i++;
    const up = a.slice(i).filter((x) => x && x !== '.').map(() => '..');
    const down = b.slice(i).filter((x) => x && x !== '.');
    return [...up, ...down].join('/') || '.';
  },
};

/* ------------------------------------------------------------------ */
/*  Import rewriting                                                   */
/* ------------------------------------------------------------------ */

/**
 * Rewrite the static and dynamic import specifiers of a module source so that
 *   - relative imports (`./utility.mjs`) become absolute VFS paths
 *   - bare package imports (`acorn`) become absolute VFS paths under
 *     `<rootDir>/node_modules/...`
 *   - absolute and `node:*` specifiers are left in place
 *
 * `rootDir` is the tool root used for bare-specifier resolution.
 */
/**
 * Static import/export-from statements, anchored at line starts so the word
 * "import" inside a comment or string can never be matched. Named imports
 * (`import {\n ...\n} from`) may span lines, hence [^}]* inside braces.
 */
const STATIC_IMPORT_RE = /(?:^|\n)[ \t]*(?:import|export)\s+(?:\{[^}]*\}|[^;\n]*?)[ \t]*from[ \t]*'([^'\n]+)'/g;
const BARE_IMPORT_RE = /(?:^|\n)[ \t]*import[ \t]+'([^'\n]+)'/g;

export function rewriteModuleImports(source: string, modulePath: string, rootDir: string): string {
  let out = source.replace(STATIC_IMPORT_RE, (match, spec: string) => {
    return match.replace(`'${spec}'`, `'${resolveSpecifier(spec, modulePath, rootDir)}'`);
  });
  out = out.replace(BARE_IMPORT_RE, (match, spec: string) => {
    return match.replace(`'${spec}'`, `'${resolveSpecifier(spec, modulePath, rootDir)}'`);
  });
  const dynamicRe = /(\bimport\s*\(\s*)'([^'\n]+)'/g;
  out = out.replace(dynamicRe, (_match, head: string, spec: string) => {
    return `${head}'${resolveSpecifier(spec, modulePath, rootDir)}'`;
  });
  return out;
}

/** Node builtin modules the loader can shim. */
export const SHIMMED_NODE_BUILTINS: ReadonlySet<string> = new Set([
  'fs', 'fs/promises', 'path', 'os', 'url', 'util', 'assert', 'events',
]);

/**
 * Resolve an import specifier against the importing module.
 * Relative and bare (npm) specifiers become absolute VFS paths; `node:*` and
 * already-absolute specifiers pass through.
 */
export function resolveSpecifier(spec: string, modulePath: string, rootDir: string): string {
  if (spec.startsWith(NODE_BUILTIN_PREFIX) || spec.startsWith('/') || spec.startsWith('blob:')) {
    return spec;
  }
  if (spec.startsWith('./') || spec.startsWith('../')) {
    return normalizeVfsPath(posixPath.join(posixPath.dirname(modulePath), spec));
  }
  // Bare specifier (npm package) → <rootDir>/node_modules/<spec>
  const candidate = posixPath.join(rootDir, 'node_modules', spec);
  if (!posixPath.extname(candidate)) {
    const pkgJson = posixPath.join(candidate, 'package.json');
    if (vfsRef?.existsSync(pkgJson)) {
      try {
        const raw = textOf(vfsRef.readFileSync(pkgJson) ?? new Uint8Array(0));
        const pkg = JSON.parse(raw) as { main?: string; module?: string };
        const entryPath = posixPath.join(candidate, pkg.module ?? pkg.main ?? 'index.js');
        if (vfsRef.existsSync(entryPath)) return entryPath;
      } catch {
        /* fall through to index probing */
      }
    }
    for (const index of ['index.mjs', 'index.js']) {
      const indexPath = posixPath.join(candidate, index);
      if (vfsRef?.existsSync(indexPath)) return indexPath;
    }
  }
  return candidate;
}

/** VFS reference used during specifier resolution and module loading (per-run). */
let vfsRef: NodeToolVfs | null = null;

function textOf(bytes: Uint8Array): string {
  return new TextDecoder('utf-8', { fatal: false }).decode(bytes);
}

/* ------------------------------------------------------------------ */
/*  Node builtin shims                                                 */
/* ------------------------------------------------------------------ */

interface StdioState {
  stdout: string[];
  stderr: string[];
  onStdout?: (text: string) => void;
  onStderr?: (text: string) => void;
}

function makePathShim(): unknown {
  return posixPath;
}

function makeFsShim(vfs: NodeToolVfs, readStdin: () => Uint8Array, cwd: string): unknown {
  const asBytes = (data: string | Uint8Array): Uint8Array =>
    typeof data === 'string' ? new TextEncoder().encode(data) : data;
  const enoent = (op: string, path: string): Error =>
    Object.assign(new Error(`ENOENT: no such file or directory, ${op} '${path}'`), { code: 'ENOENT' });
  // Node resolves relative paths against process.cwd(); tools like
  // preprocessor.mjs rely on it (run with cwd = emscripten root).
  const resolvePath = (p: string): string =>
    p.startsWith('/') ? normalizeVfsPath(p) : posixPath.join(cwd, p);

  const readSync = (path: string | number, encoding?: string): string | Uint8Array => {
    // compiler.mjs / preprocessor.mjs pass fd 0 for stdin-piped settings.
    if (path === STDIN_FD || path === String(STDIN_FD)) {
      return encoding ? textOf(readStdin()) : readStdin();
    }
    const normalized = resolvePath(String(path));
    const data = vfs.readFileSync(normalized);
    if (!data) throw enoent('open', normalized);
    return encoding === 'utf8' || encoding === 'utf-8' ? textOf(data) : data;
  };

  return {
    readFileSync: readSync,
    writeFileSync(path: string, data: string | Uint8Array): void {
      vfs.writeFileSync(resolvePath(path), asBytes(data));
    },
    existsSync(path: string): boolean {
      return vfs.existsSync(resolvePath(path));
    },
    statSync(path: string): { isFile(): boolean; isDirectory(): boolean; size: number } {
      const normalized = resolvePath(path);
      const stats = vfs.statSync(normalized);
      if (!stats) throw enoent('stat', normalized);
      return {
        isFile: () => stats.type === 'file',
        isDirectory: () => stats.type === 'dir',
        size: stats.size,
      };
    },
    readdirSync(path: string): string[] {
      const normalized = resolvePath(path);
      if (!vfs.existsSync(normalized)) throw enoent('scandir', normalized);
      return vfs.readdirSync(normalized);
    },
    mkdirSync(path: string): void {
      const normalized = resolvePath(path);
      if (!vfs.existsSync(normalized)) vfs.mkdirSync(normalized);
    },
    rmSync(path: string): void {
      vfs.deleteFileSync(resolvePath(path));
    },
    mkdtempSync(prefix: string): string {
      // Verbose-mode scratch dir for the JS compiler. /tmp is writable.
      const dir = normalizeVfsPath(`${prefix}${Math.random().toString(16).slice(2, 8)}`);
      vfs.mkdirSync(dir);
      return dir;
    },
    promises: {
      async open(path: string) {
        const normalized = normalizeVfsPath(path);
        const chunks: string[] = [];
        return {
          async write(str: string) {
            chunks.push(str);
            return { bytesWritten: str.length };
          },
          async close() {
            vfs.writeFileSync(normalized, new TextEncoder().encode(chunks.join('')));
          },
        };
      },
      async readFile(path: string): Promise<string> {
        const data = await vfs.fetchFile(normalizeVfsPath(path));
        if (!data) throw enoent('open', path);
        return textOf(data);
      },
    },
  };
}

function makeOsShim(): unknown {
  return {
    tmpdir: () => '/tmp',
    EOL: '\n',
    platform: () => 'linux',
    cpus: () => [],
    totalmem: () => 0,
    homedir: () => '/home/user',
  };
}

function makeUrlShim(): unknown {
  return {
    /**
     * Tool sources use `new URL('.', import.meta.url)` + fileURLToPath to
     * derive __dirname; the loader rewrites those expressions to file:// URLs
     * of the module's VFS directory (see patchImportMetaUrls).
     */
    fileURLToPath(url: string): string {
      return normalizeVfsPath(String(url).replace(/^file:\/\//, ''));
    },
    pathToFileURL(path: string): string {
      return `file://${normalizeVfsPath(path)}`;
    },
  };
}

/** Minimal util.parseArgs: boolean/string options + positionals. */
export function parseArgsShim(
  argv: string[],
  config: { options?: Record<string, { type?: string; short?: string; default?: unknown }>; allowPositionals?: boolean },
): { values: Record<string, unknown>; positionals: string[] } {
  const values: Record<string, unknown> = {};
  const positionals: string[] = [];
  const options = config.options ?? {};
  const shortToLong = new Map<string, string>();
  for (const [name, opt] of Object.entries(options)) {
    if (opt.default !== undefined) values[name] = opt.default;
    if (opt.short) shortToLong.set(opt.short, name);
  }
  const args = argv.slice(2); // skip node binary + script path
  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    if (arg === '--') {
      positionals.push(...args.slice(i + 1));
      break;
    }
    if (arg.startsWith('--')) {
      let name = arg.slice(2);
      let inlineValue: string | undefined;
      const eq = name.indexOf('=');
      if (eq >= 0) {
        inlineValue = name.slice(eq + 1);
        name = name.slice(0, eq);
      }
      const opt = options[name];
      if (!opt) throw new Error(`util.parseArgs: unknown option --${name}`);
      if (opt.type === 'string') {
        values[name] = inlineValue ?? args[++i];
      } else {
        values[name] = inlineValue !== undefined ? inlineValue !== 'false' : true;
      }
    } else if (arg.startsWith('-') && arg.length > 1 && !/^-\d/.test(arg)) {
      const short = arg.slice(1, 2);
      const name = shortToLong.get(short);
      if (!name) throw new Error(`util.parseArgs: unknown option -${short}`);
      const opt = options[name];
      if (opt.type === 'string') {
        const rest = arg.slice(2);
        values[name] = rest || args[++i];
      } else {
        values[name] = true;
      }
    } else if (config.allowPositionals) {
      positionals.push(arg);
    }
  }
  return { values, positionals };
}

function makeUtilShim(argv: string[]): unknown {
  return {
    parseArgs: (config: Parameters<typeof parseArgsShim>[1]) => parseArgsShim(argv, config),
    inspect: (value: unknown) => String(value),
    promisify: (fn: unknown) => fn,
    debuglog: () => () => undefined,
    types: { isPromise: (v: unknown) => Object.prototype.toString.call(v) === '[object Promise]' },
  };
}

function makeAssertShim(): unknown {
  function fail(message?: string): never {
    throw new Error(message ? `Assertion failed: ${message}` : 'Assertion failed');
  }
  const assertFn = (condition: unknown, message?: string | Error): void => {
    if (!condition) fail(typeof message === 'string' ? message : message?.message);
  };
  const assertObj = assertFn as unknown as Record<string, unknown>;
  assertObj['ok'] = assertFn;
  assertObj['equal'] = (a: unknown, b: unknown, m?: string) => { if (a !== b) fail(m ?? `${a} != ${b}`); };
  assertObj['strictEqual'] = assertObj['equal'];
  return assertObj;
}

function makeEventsShim(): unknown {
  class EventEmitter {
    private listeners = new Map<string, Set<() => void>>();
    on(event: string, fn: () => void): this {
      let set = this.listeners.get(event);
      if (!set) this.listeners.set(event, (set = new Set()));
      set.add(fn);
      return this;
    }
    once(event: string, fn: () => void): this {
      const wrapped = () => {
        fn();
        this.listeners.get(event)?.delete(wrapped);
      };
      return this.on(event, wrapped);
    }
    emit(event: string): boolean {
      const set = this.listeners.get(event);
      if (!set) return false;
      for (const fn of [...set]) fn();
      return true;
    }
    removeListener(event: string, fn: () => void): this {
      this.listeners.get(event)?.delete(fn);
      return this;
    }
  }
  return { EventEmitter };
}

/**
 * Collect the names of the script's own top-level `var` declarations. These
 * must land on the vm context object (node vm semantics) after evaluation;
 * function-local declarations are nested and therefore not harvestable from
 * the wrapper scope, which makes the name scan safe to over-match.
 */
export function collectTopLevelVarNames(code: string): string[] {
  const names = new Set<string>();
  const re = /(?:^|[\n;])[ \t]*var\s+([A-Za-z_$][\w$]*)\s*=/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(code))) names.add(m[1]);
  return [...names];
}

/**
 * Top-level declaration names of a module (const/let/var/function/class).
 * Used to seed the cycle-merge alias dedupe so a member's own declarations
 * win over another member's import alias of the same name.
 */
function collectTopLevelDeclarationNames(code: string): string[] {
  const names = new Set<string>();
  const re = /(?:^|\n)[ \t]*(?:const|let|var|function|class)\s+([A-Za-z_$][\w$]*)/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(code))) names.add(m[1]);
  return [...names];
}

function makeVmShim(): unknown {
  const run = (code: string, context: Record<string, unknown>, filename?: string): unknown => {
    // const/let/class/function top-level names are collected too: they are
    // readable within this evaluation, so the harvest persists them onto the
    // sandbox where later macro evaluations on the same context find them
    // (real Node keeps lexical declarations on the vm context).
    const varNames = collectTopLevelDeclarationNames(code);
    const harvest = varNames
      .map((name) => `try { __out[${JSON.stringify(name)}] = ${name}; } catch { /* nested/not defined */ }`)
      .join('\n');
    // The whole script is inlined at wrapper top level: its own top-level
    // declarations hoist into this scope, nested ones stay nested. Reads
    // resolve through `with (sandbox)` so macro expressions see settings.
    // Macro evaluation needs the COMPLETION VALUE (processMacros uses the
    // return of `{{{ expr }}}`). Whole-library evaluation passes statements
    // (for loops, var decls) that are not valid expressions — fall back to
    // statement mode. The harvest runs AFTER the code so freshly declared
    // names are captured onto the sandbox for later evaluations.
    // eslint-disable-next-line no-new-func
    const prologue = filename ? `//# sourceURL=${filename}\n` : '';
    const harvestBlock = varNames.length > 0 ? `;var __out = {};\n${harvest}\nObject.assign(sandbox, __out);\n` : '';
    const makeFn = (body: string): ((sandbox: Record<string, unknown>) => unknown) =>
      new Function('sandbox', `with (sandbox) { ${prologue}${body} }`) as (
        sandbox: Record<string, unknown>,
      ) => unknown;
    try {
      return makeFn(`var __completion = (${code});${harvestBlock}return __completion;`)(context ?? {});
    } catch (e) {
      if (e instanceof SyntaxError) {
        return makeFn(`${code}\n${harvestBlock}`)(context ?? {});
      }
      throw e;
    }
  };
  return {
    createContext(sandbox: Record<string, unknown>): Record<string, unknown> {
      return sandbox ?? {};
    },
    isContext(): boolean {
      return true;
    },
    runInNewContext(code: string, sandbox: Record<string, unknown>, options?: { filename?: string }): unknown {
      return run(code, sandbox ?? {}, options?.filename);
    },
    runInContext(code: string, context: Record<string, unknown>, options?: { filename?: string }): unknown {
      return run(code, context, options?.filename);
    },
  };
}

/* ------------------------------------------------------------------ */
/*  process / console shims                                            */
/* ------------------------------------------------------------------ */

interface ProcessExitError extends Error {
  __processExit?: number;
}

function makeProcessShim(argv: string[], env: Record<string, string>, cwd: string, stdio: StdioState): unknown {
  return {
    argv,
    cwd: () => cwd,
    env,
    stdout: {
      write: (chunk: string) => {
        stdio.stdout.push(chunk);
        stdio.onStdout?.(chunk);
        return true;
      },
      // compiler.mjs's error path waits for a 'drain' that never blocks here
      once: (_event: string, fn: () => void) => { fn(); },
      on: (_event: string, fn: () => void) => { fn(); },
    },
    stderr: {
      write: (chunk: string) => {
        stdio.stderr.push(chunk);
        stdio.onStderr?.(chunk);
        return true;
      },
    },
    exitCode: 0,
    exit(code = 0): never {
      const err = new Error(`process.exit(${code})`) as ProcessExitError;
      err.__processExit = code;
      throw err;
    },
    on(): void {
      /* process events (e.g. 'exit' profiling hooks) are no-ops here */
    },
    platform: 'linux',
    version: 'v22.0.0',
  };
}

function makeConsoleShim(stdio: StdioState): unknown {
  const fmt = (args: unknown[]): string =>
    args.map((a) => (typeof a === 'string' ? a : safeInspect(a))).join(' ');
  const write = (target: 'stdout' | 'stderr', args: unknown[]): void => {
    const text = fmt(args) + '\n';
    if (target === 'stdout') {
      stdio.stdout.push(text);
      stdio.onStdout?.(text);
    } else {
      stdio.stderr.push(text);
      stdio.onStderr?.(text);
    }
  };
  return {
    log: (...args: unknown[]) => write('stdout', args),
    info: (...args: unknown[]) => write('stdout', args),
    warn: (...args: unknown[]) => write('stderr', args),
    error: (...args: unknown[]) => write('stderr', args),
    debug: (...args: unknown[]) => write('stderr', args),
  };
}

function safeInspect(value: unknown): string {
  try {
    if (typeof value === 'object' && value !== null) return JSON.stringify(value);
    return String(value);
  } catch {
    return String(value);
  }
}

/* ------------------------------------------------------------------ */
/*  Module graph building                                              */
/* ------------------------------------------------------------------ */

/** Extract the import specifiers present in (already rewritten) source. */
export function extractSpecifiers(source: string): string[] {
  const specs = new Set<string>();
  const staticRe = /(?:^|\n)[ \t]*(?:import|export)\s+(?:\{[^}]*\}|[^;\n]*?)[ \t]*from[ \t]*'([^'\n]+)'/g;
  const bareRe = /(?:^|\n)[ \t]*import[ \t]+'([^'\n]+)'/g;
  const dynamicRe = /(\bimport\s*\(\s*)'([^'\n]+)'/g;
  let m: RegExpExecArray | null;
  while ((m = staticRe.exec(source))) specs.add(m[1]);
  while ((m = bareRe.exec(source))) specs.add(m[1]);
  while ((m = dynamicRe.exec(source))) specs.add(m[2]);
  return [...specs];
}

/** Replace all exact occurrences of a quoted import specifier. */
export function replaceImportSpecifier(source: string, from: string, to: string): string {
  return source.split(`'${from}'`).join(`'${to}'`);
}

/**
 * Patch `new URL('<relative>', import.meta.url)` expressions to file:// URLs
 * of the module's VFS directory. Blob URLs are opaque, so relative resolution
 * against import.meta.url would not work; the url shim understands file://.
 */
export function patchImportMetaUrls(source: string, modulePath: string): string {
  const dir = posixPath.dirname(modulePath);
  return source
    .replace(/new URL\((['"])([^'"]*)\1,\s*import\.meta\.url\)/g, (_m, _q: string, rel: string) => {
      const resolved = rel === '.' ? `${dir}/` : posixPath.join(dir, rel);
      return `new URL('file://${resolved}')`;
    })
    .replace(/\bimport\.meta\.dirname\b/g, `'${dir}'`);
}

/** Detect a UMD/CommonJS script with no ESM syntax (e.g. terser.js, acorn UMD). */
export function isClassicScriptModule(source: string): boolean {
  const stripped = source.replace(/(^|\n)\s*\/\/[^\n]*/g, '').replace(/\/\*[\s\S]*?\*\//g, '');
  return !/(^|[\n;])\s*export\s+(\{|default\b|async\b|function\b|const\b|let\b|var\b|\*|class\b)/.test(stripped)
    && !/(^|[\n;])\s*import\s+[\s{*]|(^|[\n;])\s*import\s*\(/.test(stripped);
}

/** Wrap a UMD/CommonJS script so it becomes an ES module with a default export. */
export function wrapUmdModule(source: string): string {
  return `var exports = {}, module = { exports: {} };\n${source}\nexport default (module.exports && Object.keys(module.exports).length ? module.exports : exports);\n`;
}

function escapeRegExp(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/** Tracks which built modules are UMD-wrapped (per graph build). */
let umdFlagCache = new Map<string, boolean>();

/**
 * ESM import cycles among the tool sources (src/modules.mjs ↔
 * src/parseTools.mjs). Blob module contents are immutable, so a cycle can
 * never be built recursively; instead the whole group is concatenated into
 * ONE blob where the intra-group imports are simply stripped (their names
 * are already in scope) and every member path maps to that same blob, so
 * importers on either side resolve all exports.
 */
const CYCLE_GROUPS: readonly (readonly string[])[] = [
  ['/usr/lib/emscripten/src/modules.mjs', '/usr/lib/emscripten/src/parseTools.mjs'],
];

function cycleGroupOf(vfsPath: string): readonly string[] | undefined {
  return CYCLE_GROUPS.find((group) => group.includes(vfsPath));
}

/** Remove `import ... from '<spec>';` statements for spec (already absolute). */
export function stripImportOf(source: string, spec: string): string {
  const escaped = escapeRegExp(spec);
  const named = new RegExp(`(?:^|\\n)[ \\t]*import\\s*\\{[^}]*\\}\\s*from\\s*'${escaped}'\\s*;?`, 'g');
  const nsOrDefault = new RegExp(`(?:^|\\n)[ \\t]*import\\s+(?:\\*\\s+as\\s+)?[\\w$]+\\s*from\\s*'${escaped}'\\s*;?`, 'g');
  const bare = new RegExp(`(?:^|\\n)[ \\t]*import\\s+'${escaped}'\\s*;?`, 'g');
  return source.replace(named, '\n').replace(nsOrDefault, '\n').replace(bare, '\n');
}

interface BuiltinBinding {
  spec: string;
  /** named imports: [{imported local name, source export name}] */
  named: { local: string; source: string }[];
  /** namespace local name (`import * as X`) if present */
  namespaceLocal?: string;
  /** bare default import local name (`import X from`) if present */
  defaultLocal?: string;
}

/** Parse a member's `import ... from 'node:x';` into its bindings. */
function parseBuiltinImport(source: string, spec: string): BuiltinBinding {
  const escaped = escapeRegExp(spec);
  const re = new RegExp(`(?:^|\\n)[ \\t]*import\\s+(\\{[^}]*\\}|\\*\\s+as\\s+[\\w$]+|[\\w$]+)\\s*from\\s*'${escaped}'`, 'g');
  const binding: BuiltinBinding = { spec, named: [] };
  let m: RegExpExecArray | null;
  while ((m = re.exec(source))) {
    const clause = m[1];
    if (clause.startsWith('{')) {
      for (const part of clause.slice(1, -1).split(',')) {
        const names = part.trim().split(/\s+as\s+/);
        if (!names[0]) continue;
        binding.named.push({ source: names[0], local: names[1] ?? names[0] });
      }
    } else if (clause.startsWith('*')) {
      binding.namespaceLocal = clause.replace(/\*\s+as\s+/, '');
    } else {
      binding.defaultLocal = clause;
    }
  }
  return binding;
}

function builtinBindingAlias(binding: BuiltinBinding, namespaceConst: string, skip: (name: string) => boolean): string {
  const lines: string[] = [];
  if (binding.namespaceLocal && !skip(binding.namespaceLocal)) lines.push(`const ${binding.namespaceLocal} = ${namespaceConst};`);
  if (binding.defaultLocal && !skip(binding.defaultLocal)) lines.push(`const ${binding.defaultLocal} = ${namespaceConst}.default ?? ${namespaceConst};`);
  for (const { local, source } of binding.named) {
    if (skip(local)) continue;
    lines.push(`const ${local} = ${namespaceConst}.${source};`);
  }
  return lines.join('\n');
}

async function buildCycleGroup(
  members: readonly string[],
  rootDir: string,
  resolveShimUrl: (name: string) => string,
  cache: Map<string, string>,
): Promise<string> {
  // node: builtins may be imported by several members under the same local
  // name; import each once in a prologue and re-expose every member's exact
  // bindings (namespace / default / named) via consts, so the merged blob has
  // a single import per builtin and no duplicate declarations.
  const prologueImports: string[] = [];
  const namespaceConsts = new Map<string, string>();
  const emittedLocals = new Set<string>();
  const memberBodies: string[] = [];
  const memberSources = new Map<string, string>();
  for (const member of members) {
    const bytes = await vfsRef!.fetchFile(member);
    if (!bytes) throw new Error(`[NodeTool] module not found in VFS: ${member}`);
    let source = textOf(bytes);
    source = rewriteModuleImports(source, member, rootDir);
    source = patchImportMetaUrls(source, member);
    for (const other of members) {
      if (other !== member) source = stripImportOf(source, other);
    }
    memberSources.set(member, source);
    // A member's own top-level declarations own their names: aliases for the
    // same name from another member's import (e.g. srcDir declared in
    // modules.mjs AND imported from utility.mjs by parseTools.mjs) must yield.
    for (const name of collectTopLevelDeclarationNames(source)) emittedLocals.add(name);
  }
  for (const member of members) {
    let source = memberSources.get(member)!;
    const memberAliases: string[] = [];
    for (const spec of extractSpecifiers(source)) {
      if (spec.startsWith(NODE_BUILTIN_PREFIX)) {
        const binding = parseBuiltinImport(source, spec);
        let namespaceConst = namespaceConsts.get(spec);
        if (!namespaceConst) {
          const name = spec.slice(NODE_BUILTIN_PREFIX.length).replaceAll('/', '_');
          namespaceConst = `__builtin_${name}__`;
          namespaceConsts.set(spec, namespaceConst);
          prologueImports.push(`import * as ${namespaceConst} from '${resolveShimUrl(spec.slice(NODE_BUILTIN_PREFIX.length))}';`);
        }
        source = stripImportOf(source, spec);
        memberAliases.push(builtinBindingAlias(binding, namespaceConst, (name) => {
          if (emittedLocals.has(name)) return true;
          emittedLocals.add(name);
          return false;
        }));
      } else if (spec.startsWith('/') && !spec.startsWith('blob:')) {
        const depUrl = await buildModuleGraph(spec, rootDir, resolveShimUrl, cache);
        // Two members may import the same dep (utility.mjs) with overlapping
        // local names — hoist one namespace import and alias per-member
        // bindings, same dedupe strategy as the builtins.
        const binding = parseBuiltinImport(source, spec);
        let depConst = namespaceConsts.get(`dep:${spec}`);
        if (!depConst) {
          depConst = `__dep_${namespaceConsts.size}__`;
          namespaceConsts.set(`dep:${spec}`, depConst);
          prologueImports.push(`import * as ${depConst} from '${depUrl}';`);
        }
        source = stripImportOf(source, spec);
        memberAliases.push(builtinBindingAlias(binding, depConst, (name) => {
          if (emittedLocals.has(name)) return true;
          emittedLocals.add(name);
          return false;
        }));
      }
    }
    memberBodies.push([`// ===== merged cycle group member: ${member} =====`, ...memberAliases, source].join('\n'));
  }
  const blobSource = [...prologueImports, ...memberBodies].join('\n');
  const blobUrl = URL.createObjectURL(new Blob([blobSource], { type: 'text/javascript' }));
  for (const member of members) {
    cache.set(member, blobUrl);
    umdFlagCache.set(member, false);
  }
  return blobUrl;
}

/**
 * Build the blob module graph for a VFS entry: fetch source, rewrite
 * relative/bare imports to VFS paths, map `node:*` imports to shim blob URLs
 * and absolute VFS-path imports to dependency blob URLs (recursively), then
 * return the entry blob URL. Identical VFS paths map to identical blob URLs,
 * so shared state across modules is preserved.
 */
export async function buildModuleGraph(
  vfsPath: string,
  rootDir: string,
  resolveShimUrl: (name: string) => string,
  cache: Map<string, string>,
): Promise<string> {
  const cached = cache.get(vfsPath);
  if (cached) return cached;

  const group = cycleGroupOf(vfsPath);
  if (group) {
    const groupKey = group[0];
    const groupUrl = cache.get(groupKey);
    if (groupUrl) return groupUrl;
    return buildCycleGroup(group, rootDir, resolveShimUrl, cache);
  }

  const bytes = await vfsRef!.fetchFile(vfsPath);
  if (!bytes) throw new Error(`[NodeTool] module not found in VFS: ${vfsPath}`);
  let source = textOf(bytes);
  const classic = isClassicScriptModule(source);
  if (classic) {
    source = wrapUmdModule(source);
  }
  source = rewriteModuleImports(source, vfsPath, rootDir);
  source = patchImportMetaUrls(source, vfsPath);

  for (const spec of extractSpecifiers(source)) {
    if (spec.startsWith(NODE_BUILTIN_PREFIX)) {
      source = replaceImportSpecifier(source, spec, resolveShimUrl(spec.slice(NODE_BUILTIN_PREFIX.length)));
    } else if (spec.startsWith('/') && !spec.startsWith('blob:')) {
      const depUrl = await buildModuleGraph(spec, rootDir, resolveShimUrl, cache);
      if (classic) continue; // UMD wrapper has no real imports
      // A UMD dep only exposes a default export; namespace imports
      // (`import * as x from ...`) must be flattened to the default.
      if (umdFlagCache.get(spec)) {
        const nsImportRe = new RegExp(`import\\s+\\*\\s+as\\s+([A-Za-z_$][\\w$]*)\\s+from\\s+'${escapeRegExp(spec)}'`, 'g');
        source = source.replace(nsImportRe, (_m, name: string) => `import ${name}__umd from '${spec}'; const ${name} = ${name}__umd;`);
      }
      source = replaceImportSpecifier(source, spec, depUrl);
    }
  }

  const blobUrl = URL.createObjectURL(new Blob([source], { type: 'text/javascript' }));
  cache.set(vfsPath, blobUrl);
  umdFlagCache.set(vfsPath, classic);
  return blobUrl;
}

/* ------------------------------------------------------------------ */
/*  Runner                                                             */
/* ------------------------------------------------------------------ */

/**
 * Run a Node.js tool module from the VFS in the current Worker. The entry
 * module executes with top-level await; imports resolve through blob URLs
 * with node builtins shimmed.
 */
export async function runNodeTool(vfs: NodeToolVfs, options: NodeToolRunOptions): Promise<NodeToolResult> {
  const tTotal = performance.now();
  vfsRef = vfs;
  umdFlagCache = new Map<string, boolean>();

  const scriptPath = normalizeVfsPath(options.argv[1] ?? options.argv[0]);
  const rootDir = scriptPath.startsWith('/usr/lib/emscripten/')
    ? '/usr/lib/emscripten'
    : posixPath.dirname(scriptPath);

  const stdio: StdioState = {
    stdout: [],
    stderr: [],
    onStdout: options.onStdout,
    onStderr: options.onStderr,
  };

  // Stdin (settings JSON piped by Python's run_js_tool). compiler.mjs reads
  // fd 0 lazily via readFile(0), so hand out the remainder on first read.
  const stdinBytes = options.stdin ? new TextEncoder().encode(options.stdin) : new Uint8Array(0);
  let stdinCursor = 0;
  const readStdin = (): Uint8Array => {
    const rest = stdinBytes.slice(stdinCursor);
    stdinCursor = stdinBytes.length;
    return rest;
  };

  const env: Record<string, string> = { ...(options.env ?? {}) };
  const fsShim = makeFsShim(vfs, readStdin, options.cwd);
  const builtins: Record<string, unknown> = {
    fs: fsShim,
    'fs/promises': (fsShim as { promises: unknown }).promises,
    path: makePathShim(),
    os: makeOsShim(),
    url: makeUrlShim(),
    util: makeUtilShim(options.argv),
    assert: makeAssertShim(),
    events: makeEventsShim(),
    vm: makeVmShim(),
  };

  // Shim modules fetch their implementation from a global registry (blob
  // module source must be static text; functions cannot be serialized).
  const globalScope = globalThis as unknown as Record<string, unknown>;
  const prevRegistry = globalScope[SHIM_REGISTRY_KEY] as Record<string, unknown> | undefined;
  const registry: Record<string, unknown> = {};
  globalScope[SHIM_REGISTRY_KEY] = registry;

  const builtinUrls = new Map<string, string>();
  const resolveShimUrl = (name: string): string => {
    let url = builtinUrls.get(name);
    if (!url) {
      registry[name] = builtins[name] ?? {};
      const impl = (builtins[name] ?? {}) as Record<string, unknown>;
      const namedExports = Object.keys(impl)
        .filter((key) => /^[A-Za-z_$][\w$]*$/.test(key))
        .map((key) => `export const ${key} = shim[${JSON.stringify(key)}];`)
        .join('\n');
      const source = `const shim = globalThis[${JSON.stringify(SHIM_REGISTRY_KEY)}][${JSON.stringify(name)}];\nexport default shim;\n${namedExports}\n`;
      url = URL.createObjectURL(new Blob([source], { type: 'text/javascript' }));
      builtinUrls.set(name, url);
    }
    return url;
  };

  // Blob modules inherit the Worker global scope, so temporarily install
  // process/console shims as globals while the tool runs.
  const savedGlobals = new Map<string, unknown>();
  const globalOverrides: Record<string, unknown> = {
    process: makeProcessShim(options.argv, env, options.cwd, stdio),
    console: makeConsoleShim(stdio),
  };
  for (const [key, value] of Object.entries(globalOverrides)) {
    if (key in globalScope) savedGlobals.set(key, globalScope[key]);
    globalScope[key] = value;
  }

  // The global console is already swapped to the tool shim here — log to the
  // real worker console so diagnostics never contaminate the tool's stdout
  // (Python json.loads()es symbols-only output verbatim).
  rawConsole.log(`${LOG_PREFIX} Running ${scriptPath} (cwd=${options.cwd}, stdin=${stdinBytes.length}B)`);

  let exitCode = 0;
  try {
    const entryUrl = await buildModuleGraph(scriptPath, rootDir, resolveShimUrl, new Map());
    await import(entryUrl);
  } catch (e) {
    const err = e as ProcessExitError;
    if (typeof err.__processExit === 'number') {
      exitCode = err.__processExit;
    } else {
      exitCode = 1;
      const msg = err instanceof Error ? `${err.message}\n${err.stack ?? ''}` : String(err);
      rawConsole.error(`${LOG_PREFIX} ${posixPath.basename(scriptPath)} failed:`, msg);
      stdio.stderr.push(msg + '\n');
      stdio.onStderr?.(msg + '\n');
    }
  } finally {
    for (const [key, value] of savedGlobals) globalScope[key] = value;
    for (const key of Object.keys(globalOverrides)) {
      if (!savedGlobals.has(key)) delete globalScope[key];
    }
    if (prevRegistry === undefined) delete globalScope[SHIM_REGISTRY_KEY];
    else globalScope[SHIM_REGISTRY_KEY] = prevRegistry;
  }

  console.log(`${LOG_PREFIX} ${posixPath.basename(scriptPath)} exitCode=${exitCode} total=${(performance.now() - tTotal).toFixed(0)}ms`);
  return {
    exitCode,
    stdout: stdio.stdout.join(''),
    stderr: stdio.stderr.join(''),
  };
}
