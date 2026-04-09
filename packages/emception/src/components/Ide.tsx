import type { OnMount } from '@monaco-editor/react';
import { Terminal } from '@xterm/xterm';
import { bootInWorker, type WorkerBootResult } from 'emception';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels';
import DockGroupPanel from './DockGroup';
import FileExplorer from './FileExplorer';
import type { DockGroup, OpenTab, TabType, TerminalTab, WorkspaceFile } from './ide-types';
import { DEFAULT_IMAGE, INITIAL_FILES, WORKSPACE_STORAGE_KEY } from './ide-types';
import { buildFileTree, buildSDL3ArgsPort, detectsSDL, inferLanguage, isSourceFile, isTextFile, makeWasiStubs, toWorkspaceFsPath } from './ide-utils';
import TerminalPanel from './TerminalPanel';

export interface IdeProps {
  title?: string;
  manifestUrl?: string;
}

export default function Ide({ title = 'WebAssembly C++ Toolchain', manifestUrl = '/cdn/manifest.json' }: IdeProps) {
  const editorRef = useRef<Parameters<OnMount>[0] | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const orchestratorRef = useRef<WorkerBootResult | null>(null);
  const xtermRef = useRef<Terminal | null>(null);
  const terminalLogRef = useRef<HTMLPreElement | null>(null);
  /** Tracks blob URLs created for SDL output so they can be revoked on reset/unmount */
  const sdlBlobUrlsRef = useRef<string[]>([]);
  /** Tracks the injected SDL script element for cleanup on recompile/reset/unmount */
  const sdlScriptRef = useRef<HTMLScriptElement | null>(null);
  /** Tracks the live SDL3 Emscripten module so its RAF loop can be stopped */

  const sdlModuleRef = useRef<{ pauseMainLoop?: () => void } | null>(null);

  const [files, setFiles] = useState<Record<string, WorkspaceFile>>(INITIAL_FILES);
  const [selectedPath, setSelectedPath] = useState('/src/main.cpp');
  const [expandedDirs, setExpandedDirs] = useState<Set<string>>(new Set(['/src', '/assets', '/runtime']));
  const [openTabs, setOpenTabs] = useState<OpenTab[]>([
    { id: 'tab:/src/sdl-main.cpp', path: '/src/sdl-main.cpp', type: 'text', group: 'main' },
    { id: 'tab:/src/main.cpp', path: '/src/main.cpp', type: 'text', group: 'main' },
    { id: 'tab:/runtime/sdl-canvas', path: '/runtime/sdl-canvas', type: 'canvas', group: 'right' },
  ]);
  const [activeTabId, setActiveTabId] = useState('tab:/src/sdl-main.cpp');

  const [terminalTabs, setTerminalTabs] = useState<TerminalTab[]>([{ id: 'terminal-1', title: 'bash' }]);
  const [activeTerminalId, setActiveTerminalId] = useState('terminal-1');

  const [status, setStatus] = useState('Initializing...');
  const [isReady, setIsReady] = useState(false);
  const [terminalReady, setTerminalReady] = useState(false);
  const [executionPhase, setExecutionPhase] = useState<'idle' | 'compiling' | 'running'>('idle');
  /** Set to true by handleStop so the catch block in handleCompile knows it was intentional */
  const stoppedRef = useRef(false);

  const fileTree = buildFileTree(Object.keys(files));
  const activeTab = openTabs.find((t) => t.id === activeTabId) ?? openTabs[0] ?? null;
  const activeFile = activeTab ? files[activeTab.path] : null;
  const activeFileName = activeFile ? (activeFile.path.split('/').filter(Boolean).pop() ?? '') : '';
  const groupTabs = (group: DockGroup) => openTabs.filter((t) => t.group === group);
  const hasRightGroup = groupTabs('right').length > 0;
  const hasBottomGroup = groupTabs('bottom').length > 0;

  useEffect(() => {
    try {
      const raw = window.localStorage.getItem(WORKSPACE_STORAGE_KEY);
      if (!raw) return;
      const parsed = JSON.parse(raw) as {
        files?: Record<string, WorkspaceFile>;
        selectedPath?: string;
        expandedDirs?: string[];
        openTabs?: OpenTab[];
        activeTabId?: string;
      };
      if (parsed.files && Object.keys(parsed.files).length > 0) setFiles(parsed.files);
      if (parsed.selectedPath) setSelectedPath(parsed.selectedPath);
      if (parsed.expandedDirs) setExpandedDirs(new Set(parsed.expandedDirs));
      if (parsed.openTabs && parsed.openTabs.length > 0) setOpenTabs(parsed.openTabs);
      if (parsed.activeTabId) setActiveTabId(parsed.activeTabId);
    } catch {
      /* ignore storage read errors */
    }
  }, []);

  useEffect(() => {
    try {
      // Clear canvas blob URLs (not valid across reloads)
      const filesToSave = Object.fromEntries(Object.entries(files).map(([k, v]) => [k, v.type === 'canvas' ? { ...v, content: '' } : v]));
      window.localStorage.setItem(
        WORKSPACE_STORAGE_KEY,
        JSON.stringify({
          files: filesToSave,
          selectedPath,
          expandedDirs: [...expandedDirs],
          openTabs,
          activeTabId,
        }),
      );
    } catch {
      /* ignore storage write errors */
    }
  }, [files, selectedPath, expandedDirs, openTabs, activeTabId]);

  const handleBootTerminalReady = useCallback((term: Terminal) => {
    xtermRef.current = term;
    (window as Window & { __xterm__?: Terminal }).__xterm__ = term;
    term.writeln('\x1b[32mWelcome to the Browser C/C++ Toolchain!\x1b[0m');
    term.writeln('Booting system...');
    setTerminalReady(true);
  }, []);

  const doBootstrap = useCallback(
    async (isMounted: () => boolean = () => true) => {
      const xterm = xtermRef.current;
      if (!xterm) return;
      setIsReady(false);
      setStatus('Booting toolchain...');
      try {
        const result = await bootInWorker(manifestUrl, xterm);
        if (!isMounted()) return;
        // Mirror tty output to a hidden DOM element for Playwright E2E tests.
        // eslint-disable-next-line no-control-regex
        const stripAnsi = (s: string) => s.replace(/\u001b\[[\d;]*m/g, '');
        const log = terminalLogRef;
        const origWriteLine = result.tty.writeLine.bind(result.tty);
        const origWriteError = result.tty.writeError.bind(result.tty);
        const origClear = result.tty.clear.bind(result.tty);
        result.tty.writeLine = (text: string) => {
          origWriteLine(text);
          if (log.current) log.current.textContent += stripAnsi(text) + '\n';
        };
        result.tty.writeError = (text: string) => {
          origWriteError(text);
          if (log.current) log.current.textContent += stripAnsi(text) + '\n';
        };
        result.tty.clear = () => {
          origClear();
          if (log.current) log.current.textContent = '';
        };
        orchestratorRef.current = result;
        setStatus('Ready');
        setIsReady(true);
        xterm.writeln('\x1b[32mSystem Ready.\x1b[0m');
        xterm.writeln('Click \x1b[1m▶\x1b[0m to compile & run.');
      } catch (err) {
        if (!isMounted()) return;
        setStatus(`Error: ${err instanceof Error ? err.message : String(err)}`);
        xterm.writeln(`\x1b[31mBoot failed: ${err}\x1b[0m`);
      }
    },
    [manifestUrl],
  );

  useEffect(() => {
    if (!terminalReady || !xtermRef.current) return;
    let mounted = true;
    doBootstrap(() => mounted);
    return () => {
      mounted = false;
      orchestratorRef.current = null;
    };
  }, [terminalReady, manifestUrl, doBootstrap]);

  // Revoke SDL blob URLs and remove injected script on unmount
  useEffect(() => {
    return () => {
      sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
      sdlScriptRef.current?.remove();
    };
  }, []);

  const ensureOpenTab = useCallback(
    (path: string, group: DockGroup = 'main') => {
      const file = files[path];
      if (!file) return;
      const id = `tab:${path}`;
      setOpenTabs((prev) => {
        const existing = prev.find((t) => t.id === id);
        if (existing) return prev.map((t) => (t.id === id ? { ...t, group } : t));
        return [...prev, { id, path, type: file.type, group }];
      });
      setActiveTabId(id);
    },
    [files],
  );

  const closeTab = useCallback((tabId: string) => {
    setOpenTabs((prev) => {
      const idx = prev.findIndex((t) => t.id === tabId);
      if (idx === -1) return prev;
      const next = prev.filter((t) => t.id !== tabId);
      setActiveTabId((cur) => {
        if (cur !== tabId) return cur;
        return next[idx]?.id ?? next[idx - 1]?.id ?? next[0]?.id ?? '';
      });
      return next;
    });
  }, []);

  const moveTabToGroup = useCallback((tabId: string, group: DockGroup) => {
    setOpenTabs((prev) => prev.map((t) => (t.id === tabId ? { ...t, group } : t)));
    setActiveTabId(tabId);
  }, []);

  const createFile = useCallback(
    (kind: TabType) => {
      const baseDir = kind === 'canvas' ? '/runtime' : kind === 'image' ? '/assets' : '/src';
      const defaultName = kind === 'canvas' ? 'new-canvas' : kind === 'image' ? 'new-image.svg' : 'new-file.cpp';
      const input = window.prompt(`Create new ${kind} file`, `${baseDir}/${defaultName}`);
      if (!input) return;
      const path = input.startsWith('/') ? input : `${baseDir}/${input}`;
      if (files[path]) {
        window.alert(`File already exists: ${path}`);
        return;
      }
      const content =
        kind === 'image' ? DEFAULT_IMAGE : kind === 'canvas' ? '' : kind === 'text' && path.endsWith('.h') ? '#pragma once\n\n' : '// New source file\n';
      setFiles((prev) => ({ ...prev, [path]: { path, type: kind, content } }));
      const parts = path.split('/').filter(Boolean);
      if (parts.length > 1) {
        setExpandedDirs((prev) => {
          const next = new Set(prev);
          let cur = '';
          for (let i = 0; i < parts.length - 1; i++) {
            cur += `/${parts[i]}`;
            next.add(cur);
          }
          return next;
        });
      }
      setSelectedPath(path);
      ensureOpenTab(path, 'main');
    },
    [files, ensureOpenTab],
  );

  const renameSelectedFile = useCallback(() => {
    if (!selectedPath || !files[selectedPath]) return;
    const nextPath = window.prompt('Rename file', selectedPath);
    if (!nextPath || nextPath === selectedPath) return;
    if (files[nextPath]) {
      window.alert(`File already exists: ${nextPath}`);
      return;
    }
    const norm = nextPath.startsWith('/') ? nextPath : `/${nextPath}`;
    const file = files[selectedPath];
    setFiles((prev) => {
      const clone = { ...prev };
      delete clone[selectedPath];
      clone[norm] = { ...file, path: norm };
      return clone;
    });
    setOpenTabs((prev) => prev.map((tab) => (tab.path === selectedPath ? { ...tab, path: norm, id: `tab:${norm}` } : tab)));
    setSelectedPath(norm);
    setActiveTabId((cur) => (cur === `tab:${selectedPath}` ? `tab:${norm}` : cur));
  }, [selectedPath, files]);

  const deleteSelectedFile = useCallback(() => {
    if (!selectedPath || !files[selectedPath]) return;
    if (!window.confirm(`Delete ${selectedPath}?`)) return;
    setFiles((prev) => {
      const c = { ...prev };
      delete c[selectedPath];
      return c;
    });
    closeTab(`tab:${selectedPath}`);
    setSelectedPath(Object.keys(files).find((p) => p !== selectedPath) ?? '');
  }, [selectedPath, files, closeTab]);

  const resetWorkspace = useCallback(() => {
    if (!window.confirm('Reset the workspace to the default demo files and layout?')) return;
    // Stop SDL3 loop if running
    const sdlMod = sdlModuleRef.current;
    if (sdlMod) {
      try {
        sdlMod.pauseMainLoop?.();
      } catch {
        /* ignore */
      }
      sdlModuleRef.current = null;
    }
    // Revoke any outstanding SDL blob URLs and remove injected script
    sdlScriptRef.current?.remove();
    sdlScriptRef.current = null;
    sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
    sdlBlobUrlsRef.current = [];
    setExecutionPhase('idle');
    setFiles(INITIAL_FILES);
    setSelectedPath('/src/sdl-main.cpp');
    setExpandedDirs(new Set(['/src', '/assets', '/runtime']));
    setOpenTabs([
      { id: 'tab:/src/sdl-main.cpp', path: '/src/sdl-main.cpp', type: 'text', group: 'main' },
      { id: 'tab:/src/main.cpp', path: '/src/main.cpp', type: 'text', group: 'main' },
      { id: 'tab:/runtime/sdl-canvas', path: '/runtime/sdl-canvas', type: 'canvas', group: 'right' },
    ]);
    setActiveTabId('tab:/src/sdl-main.cpp');
    setTerminalTabs([{ id: 'terminal-1', title: 'bash' }]);
    setActiveTerminalId('terminal-1');
    if (orchestratorRef.current) {
      orchestratorRef.current.tty.clear();
      orchestratorRef.current.tty.writeLine('\x1b[32mWorkspace reset.\x1b[0m');
    } else {
      xtermRef.current?.clear();
      xtermRef.current?.writeln('\x1b[32mWorkspace reset.\x1b[0m');
    }
  }, []);

  const createTerminalTab = useCallback(() => {
    setTerminalTabs((prev) => {
      const nextId = `terminal-${prev.length + 1}`;
      const next = [...prev, { id: nextId, title: `bash ${prev.length + 1}` }];
      setActiveTerminalId(nextId);
      return next;
    });
  }, []);

  const closeTerminalTab = useCallback((tabId: string) => {
    setTerminalTabs((prev) => {
      if (prev.length === 1) return prev;
      const idx = prev.findIndex((t) => t.id === tabId);
      if (idx === -1) return prev;
      const next = prev.filter((t) => t.id !== tabId);
      setActiveTerminalId((cur) => {
        if (cur !== tabId) return cur;
        return (next[idx] ?? next[idx - 1] ?? next[0])?.id ?? 'terminal-1';
      });
      return next;
    });
  }, []);

  const handleEditorDidMount: OnMount = (editor, monaco) => {
    editorRef.current = editor;
    // Expose for e2e tests (e.g. Playwright can read file content via window.monaco)
    (window as unknown as Record<string, unknown>).monaco = monaco;
  };

  const handleEditorChange = useCallback((path: string, value: string) => {
    setFiles((prev) => ({ ...prev, [path]: { ...prev[path], content: value } }));
  }, []);

  const handleCompile = async () => {
    if (!orchestratorRef.current || !activeFile || activeFile.type !== 'text') return;
    stoppedRef.current = false;
    setExecutionPhase('compiling');
    setActiveTerminalId('terminal-1');
    const P = '[Emception:IDE]';
    // Always return keyboard focus to the editor when done, regardless of exit path.
    // xterm captures document-level keydown when it has focus, blocking Monaco input.
    const restoreEditorFocus = () => {
      editorRef.current?.focus();
    };
    const tTotal = performance.now();
    const { client, tty } = orchestratorRef.current;
    const textFiles = Object.values(files).filter((f) => f.type === 'text' && isTextFile(f.path));
    const compileTarget = isSourceFile(activeFile.path)
      ? activeFile.path
      : Object.keys(files).includes('/src/main.cpp')
        ? '/src/main.cpp'
        : textFiles.find((f) => isSourceFile(f.path))?.path;
    try {
      if (!compileTarget) {
        setExecutionPhase('idle');
        setStatus('No compilable source file found');
        tty.writeError('No .c/.cpp source file found in workspace.');
        return;
      }
      setStatus('Compiling...');
      tty.clear();
      tty.writeLine(`Compiling ${compileTarget}...`);
      const enc = new TextEncoder();
      for (const file of textFiles) {
        const fsPath = toWorkspaceFsPath(file.path);
        await client.writeFile(fsPath, enc.encode(file.content));
        console.log(`${P} Synced ${file.path} -> ${fsPath}`);
      }

      // ── SDL3 path ─────────────────────────────────────────────────
      // Detect SDL3 includes — compiles to main.wasm (no JS glue generation).
      // The pre-built sdl3-runtime.mjs shell loaded dynamically from the VFS
      // provides all WebGL + emscripten bindings and runs the WASM binary.
      const t0 = performance.now();
      if (detectsSDL(files)) {
        tty.writeLine('\x1b[36mSDL3 detected \u2014 compiling...\x1b[0m');

        const sdlResult = await client.run('emcc', buildSDL3ArgsPort(toWorkspaceFsPath(compileTarget)), {
          cwd: '/home/user',
          onStdout: (t: string) => {
            console.log(t);
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            console.error(t);
            tty.writeError(t);
          },
        });
        const sdlDuration = ((performance.now() - t0) / 1000).toFixed(2);
        if (sdlResult.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus(`SDL3 compilation failed (${sdlDuration}s)`);
          tty.writeLine(`\x1b[31mSDL3 compilation failed (exit ${sdlResult.exitCode})\x1b[0m`);
          return;
        }
        tty.writeLine(`\x1b[32mSDL3 compiled in ${sdlDuration}s — loading...\x1b[0m`);

        // Read the compiled WASM binary from the VFS.
        // emcc outputs both main.js and main.wasm when targeting .js;
        // only main.wasm is used — the generated JS glue is discarded.
        const wasmBytes = await client.getFile('/home/user/main.wasm');
        if (!wasmBytes) {
          setExecutionPhase('idle');
          tty.writeError('main.wasm not found — emcc may have failed to produce it alongside main.js');
          return;
        }

        // Read the pre-built SDL3 JS runtime shell from the VFS
        const runtimeBytes = await client.getFile('/usr/lib/emscripten/sdl3-runtime.mjs');
        if (!runtimeBytes) {
          setExecutionPhase('idle');
          tty.writeError('sdl3-runtime.mjs not found in VFS — rebuild the CDN bundle');
          return;
        }

        // Mark canvas tab as SDL-active (keeps the canvas element visible)
        setFiles((prev) => ({
          ...prev,
          '/runtime/sdl-canvas': { ...prev['/runtime/sdl-canvas'], content: 'sdl' },
        }));
        ensureOpenTab('/runtime/sdl-canvas', 'right');
        // Restore active tab to the source file so the compile button stays enabled
        // for re-compile.  ensureOpenTab always sets the new tab as active, which
        // would flip activeFile to type 'canvas', disabling the compile button.
        setActiveTabId(`tab:${compileTarget}`);

        // Wait for React to flush + browser to paint so canvasRef.current is ready
        await new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));

        const canvas = canvasRef.current;
        if (!canvas) {
          setExecutionPhase('idle');
          tty.writeError('SDL canvas element not found — open the SDL Canvas tab first');
          return;
        }

        // Revoke previous SDL blob URLs
        sdlScriptRef.current?.remove();
        sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
        sdlBlobUrlsRef.current = [];

        // Patch sdl3-runtime.mjs before creating the blob URL.
        // The pre-built runtime's ASM_CONSTS table keys are memory offsets from
        // the stub WASM it was compiled with.  User WASM has different offsets.
        // When a lookup misses, read the JS body from WASM linear memory via
        // UTF8ToString (which is in-scope inside the module closure) and eval it —
        // the resulting function captures the module's closure so UTF8ToString,
        // Module, HEAPU8, etc. are all accessible inside the EM_ASM body.
        let runtimeText = new TextDecoder().decode(runtimeBytes instanceof Uint8Array ? runtimeBytes : new Uint8Array(runtimeBytes as ArrayBuffer));
        const emAsmFallback = (varName: string) =>
          `if(!ASM_CONSTS[${varName}]){var _s=UTF8ToString(${varName});` + `ASM_CONSTS[${varName}]=eval("(function($0,$1,$2,$3,$4,$5,$6,$7,$8,$9){"+_s+"})");}`;
        // Also patch assignWasmExports to fall back to SDL_malloc/SDL_free when
        // the user's standalone WASM doesn't export plain malloc/free, and patch
        // stringToNewUTF8 to use the best available allocator.
        // _free is used in sdl3-runtime.mjs ASM_CONSTS but never declared in its var list.
        // ES modules run in strict mode, so an undeclared assignment throws ReferenceError.
        // Declare it at module scope and assign it from SDL_free.
        runtimeText = runtimeText
          .replace('var _main,_SDL_free,', 'var _free,_main,_SDL_free,')
          .replace('_malloc=wasmExports["malloc"]', '_malloc=wasmExports["malloc"]||wasmExports["SDL_malloc"]')
          .replace(
            '_SDL_free=Module["_SDL_free"]=wasmExports["SDL_free"]',
            '_SDL_free=Module["_SDL_free"]=wasmExports["SDL_free"];_free=wasmExports["free"]||_SDL_free',
          )

          .replace(
            'var stringToNewUTF8=str=>{var size=lengthBytesUTF8(str)+1;var ret=_malloc(size)',
            'var stringToNewUTF8=str=>{var size=lengthBytesUTF8(str)+1;var allocFn=_malloc||_SDL_malloc;var ret=allocFn(size)',
          );
        const ORIG_RUNEMASM = 'var runEmAsmFunction=(code,sigPtr,argbuf)=>{var args=readEmAsmArgs(sigPtr,argbuf);return ASM_CONSTS[code](...args)}';
        const ORIG_RUNMTEMASM =
          'var runMainThreadEmAsm=(emAsmAddr,sigPtr,argbuf,sync)=>{var args=readEmAsmArgs(sigPtr,argbuf);return ASM_CONSTS[emAsmAddr](...args)}';
        if (runtimeText.includes(ORIG_RUNEMASM)) {
          runtimeText = runtimeText.replace(
            ORIG_RUNEMASM,
            `var runEmAsmFunction=(code,sigPtr,argbuf)=>{var args=readEmAsmArgs(sigPtr,argbuf);${emAsmFallback('code')}return ASM_CONSTS[code](...args)}`,
          );
        } else {
          tty.writeError('sdl3-runtime patch: runEmAsmFunction not found — EM_ASM may fail');
        }
        if (runtimeText.includes(ORIG_RUNMTEMASM)) {
          runtimeText = runtimeText.replace(
            ORIG_RUNMTEMASM,
            `var runMainThreadEmAsm=(emAsmAddr,sigPtr,argbuf,sync)=>{var args=readEmAsmArgs(sigPtr,argbuf);${emAsmFallback('emAsmAddr')}return ASM_CONSTS[emAsmAddr](...args)}`,
          );
        }

        // Patch: Scope SDL3's keyboard event handlers to the canvas element.
        // SDL3's emscripten backend registers keydown/keyup/keypress handlers
        // on window/document (globally), which captures ALL keyboard input and
        // calls e.preventDefault() — blocking Monaco editor, xterm, and any
        // other UI element from receiving keystroke events.
        // This patch inserts an early-return guard at the top of the keyboard
        // event handler: if the event target is not the canvas, let it pass
        // through to the rest of the page unblocked.
        const ORIG_KEY_HANDLER = 'var keyEventHandlerFunc=e=>{var keyEventData=JSEvents.keyEvent';
        const PATCHED_KEY_HANDLER = 'var keyEventHandlerFunc=e=>{if(Module["canvas"]&&e.target!==Module["canvas"])return;var keyEventData=JSEvents.keyEvent';
        if (runtimeText.includes(ORIG_KEY_HANDLER)) {
          runtimeText = runtimeText.replace(ORIG_KEY_HANDLER, PATCHED_KEY_HANDLER);
        } else {
          tty.writeError('sdl3-runtime patch: keyEventHandlerFunc not found — keyboard may be captured globally');
        }

        // Create a blob URL for the ES6 runtime module so we can dynamically import it
        const runtimeBlob = new Blob([new TextEncoder().encode(runtimeText)], { type: 'application/javascript' });
        const runtimeUrl = URL.createObjectURL(runtimeBlob);
        sdlBlobUrlsRef.current = [runtimeUrl];

        // Dynamically import the MODULARIZE ES6 factory and instantiate with WASM + canvas
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const { default: createSDL3Module } = await import(/* webpackIgnore: true */ /* @vite-ignore */ runtimeUrl as any);

        // The user's WASM is compiled as standalone/WASI (-o main.wasm) because
        // emcc's full JS-generation step (compiler.mjs) is not available in this
        // environment.  Standalone WASM imports from wasi_snapshot_preview1 which
        // sdl3-runtime.mjs doesn't provide.  We inject stubs via instantiateWasm
        // so the user binary can be instantiated through the pre-built SDL3 runtime.
        const wasmMemory: WebAssembly.Memory | null = null;
        const wasiStubs = makeWasiStubs(
          () => wasmMemory,
          (s: string) => tty.writeLine(s),
        );

        let sdlLoadOk = true;
        const moduleTimeout = new Promise<never>((_, reject) => setTimeout(() => reject(new Error('SDL3 module load timeout (30s)')), 30_000));
        const sdlMod = await Promise.race([
          createSDL3Module({
            canvas: canvas,
            keyboardListeningElement: canvas,
            wasmBinary: wasmBytes,
            // When loaded from a blob URL, import.meta.url is a blob URL and
            // new URL('sdl3-runtime.wasm', blobUrl) throws "Invalid URL".
            // Providing locateFile makes findWasmBinary() return the plain
            // filename string; getBinarySync then uses the wasmBinary buffer
            // directly because file == wasmBinaryFile && wasmBinary is truthy.
            locateFile: (filename: string) => filename,
            // Intercept WebAssembly instantiation to inject WASI stubs alongside
            // the emscripten env imports provided by sdl3-runtime.mjs.
            // emscripten_notify_memory_growth may be absent from the pre-built
            // runtime's wasmImports (DCE removed it since the stub never grows
            // memory), but user WASM compiled with ALLOW_MEMORY_GROWTH=1 imports
            // it.  WebAssembly.Memory.buffer is always current after a grow, so
            // a no-op handler is safe for canvas-only SDL3 apps.
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            instantiateWasm(info: any, receiveInstance: (inst: WebAssembly.Instance) => void) {
              const env = {
                ...info.env,
                emscripten_notify_memory_growth: () => {
                  // wasmMemory.buffer is updated automatically after memory.grow();
                  // our WASI stubs re-read .buffer on every call so no action needed.
                },
              };
              const imports = { ...info, env, wasi_snapshot_preview1: wasiStubs };
              tty.writeLine('\x1b[90mSDL3: instantiating WASM…\x1b[0m');
              WebAssembly.instantiate(new Uint8Array(wasmBytes as unknown as ArrayBuffer), imports)
                .then((result) => {
                  tty.writeLine('\x1b[90mSDL3: WASM ok, patching exports…\x1b[0m');
                  // Standalone WASM (-o main.wasm) calls __wasm_call_ctors internally
                  // from _start, so it is not re-exported as a standalone symbol.
                  // The sdl3-runtime.mjs JS glue calls wasmExports.__wasm_call_ctors()
                  // directly after instantiation.  WebAssembly.Instance.exports is
                  // non-extensible, so we wrap it in a Proxy that fills in the gap.
                  const origExports = result.instance.exports;
                  const patchedExports =
                    typeof origExports['__wasm_call_ctors'] === 'function' && typeof (origExports as Record<string, unknown>)['main'] === 'function'
                      ? origExports
                      : new Proxy(origExports, {
                        get(target, prop) {
                          // __wasm_call_ctors: called internally from _start in
                          // standalone mode, so it is not re-exported.
                          if (prop === '__wasm_call_ctors' && !(prop in target)) return () => { };
                          // main: Emscripten 3.x exports look for wasmExports["main"]
                          // (no underscore prefix). Standalone WASM uses _start (WASI
                          // entry) instead. emscripten_set_main_loop with
                          // simulate_infinite_loop unwinds via a thrown 'unwind' string;
                          // proc_exit(0) also exits — absorb both so the animation-frame
                          // loop keeps running.
                          if ((prop === 'main' || prop === '_main') && !(prop in target)) {
                            return () => {
                              try {
                                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                                (target as any)['_start']?.();
                              } catch (e: unknown) {
                                // 'unwind' string = emscripten_set_main_loop unwind (normal, loop is live).
                                // Error with proc_exit message = SDL init failure — log it.
                                const msg = e instanceof Error ? e.message : String(e);
                                if (msg !== 'unwind' && !msg.startsWith('SDL3 proc_exit(0)')) {
                                  tty.writeError(`SDL3 init error: ${msg}`);
                                }
                              }
                              return 0;
                            };
                          }
                          // eslint-disable-next-line @typescript-eslint/no-explicit-any
                          return (target as any)[prop];
                        },
                      });
                  const patchedInstance = new Proxy(result.instance, {
                    get(target, prop) {
                      if (prop === 'exports') return patchedExports;
                      // eslint-disable-next-line @typescript-eslint/no-explicit-any
                      return (target as any)[prop];
                    },
                  });
                  tty.writeLine('\x1b[90mSDL3: calling receiveInstance…\x1b[0m');
                  receiveInstance(patchedInstance);
                })
                .catch((err: unknown) => {
                  tty.writeError(`SDL3 WASM instantiation failed: ${err}`);
                  setStatus('SDL3 load failed');
                });
              return {};
            },
            print: (line: string) => tty.writeLine(line),
            printErr: (line: string) => tty.writeError(line),
          }),
          moduleTimeout,
        ]).catch((e: unknown) => {
          sdlLoadOk = false;
          tty.writeError(`SDL3 module error: ${e}`);
          setStatus('SDL3 load failed');
          return null;
        });

        if (!sdlLoadOk) {
          setExecutionPhase('idle');
          return;
        }
        sdlModuleRef.current = sdlMod as { pauseMainLoop?: () => void } | null;
        setExecutionPhase('running');
        setStatus(`SDL3 done (${((performance.now() - tTotal) / 1000).toFixed(1)}s)`);
        tty.writeLine('\x1b[32mSDL3 rendering in canvas tab →\x1b[0m');
        return;
      }

      // ── Standard WASI path ────────────────────────────────────────
      const result = await client.run('emcc', ['emcc', toWorkspaceFsPath(compileTarget), '-o', '/home/user/main.wasm', '-O2'], {
        cwd: '/home/user',
        onStdout: (t: string) => {
          console.log(t);
          tty.writeLine(t);
        },
        onStderr: (t: string) => {
          console.error(t);
          tty.writeError(t);
        },
      });
      const duration = ((performance.now() - t0) / 1000).toFixed(2);
      if (result.exitCode !== 0) {
        setExecutionPhase('idle');
        setStatus('Compilation failed');
        tty.writeLine(`\x1b[31mCompilation failed (exit ${result.exitCode})\x1b[0m`);
        return;
      }
      setStatus(`Compiled (${duration}s)`);
      tty.writeLine(`\x1b[32mCompilation successful in ${duration}s\x1b[0m`);
      tty.writeLine('Running...');
      const lineQueue: number[] = [];
      let lineBuf = '';
      let lineCursor = 0;
      const lineBufferedStdin = async (): Promise<number> => {
        if (lineQueue.length > 0) return lineQueue.shift()!;
        while (true) {
          const raw = tty.readByteExclusive();
          const byte: number = typeof (raw as Promise<number>).then === 'function' ? await (raw as Promise<number>) : (raw as number);
          if (byte === -1) return -1;
          if (byte === 127 || byte === 8) {
            if (lineCursor > 0) {
              lineBuf = lineBuf.slice(0, lineCursor - 1) + lineBuf.slice(lineCursor);
              lineCursor--;
            }
            continue;
          }
          if (byte === 13 || byte === 10) {
            for (let i = 0; i < lineBuf.length; i++) lineQueue.push(lineBuf.charCodeAt(i));
            lineQueue.push(10);
            lineBuf = '';
            lineCursor = 0;
            return lineQueue.shift()!;
          }
          if (byte >= 32) {
            const ch = String.fromCharCode(byte);
            lineBuf = lineBuf.slice(0, lineCursor) + ch + lineBuf.slice(lineCursor);
            lineCursor++;
          }
        }
      };
      setExecutionPhase('running');
      await client.run('wasi-run', ['wasi-run', '/home/user/main.wasm'], {
        cwd: '/home/user',
        onStdout: (t: string) => {
          tty.write(t.replace(/\n/g, '\r\n'));
        },
        onStderr: (t: string) => {
          tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`);
        },
        stdin: lineBufferedStdin,
      });
      setExecutionPhase('idle');
      setStatus(`Done (${((performance.now() - tTotal) / 1000).toFixed(1)}s)`);
    } catch (e) {
      setExecutionPhase('idle');
      if (!stoppedRef.current) {
        console.error(`${P} Exception:`, e);
        setStatus('Error during execution');
        tty.writeError(String(e));
      }
    } finally {
      restoreEditorFocus();
    }
  };

  const handleStop = async () => {
    if (executionPhase !== 'running') return;

    // SDL3 path: the RAF loop runs on the main thread; pause it and clean up.
    // The worker is idle (it only compiled), so we can keep it for re-compile.
    const sdlMod = sdlModuleRef.current;
    if (sdlMod) {
      try {
        sdlMod.pauseMainLoop?.();
      } catch {
        /* ignore */
      }
      sdlModuleRef.current = null;
      sdlScriptRef.current?.remove();
      sdlScriptRef.current = null;
      sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
      sdlBlobUrlsRef.current = [];
      // Clear the canvas pixels so the placeholder overlay reappears
      const canvas = canvasRef.current;
      if (canvas) canvas.getContext('2d')?.clearRect(0, 0, canvas.width, canvas.height);
      // Reset the file content sentinel so the placeholder is shown again
      setFiles((prev) => ({
        ...prev,
        '/runtime/sdl-canvas': { ...prev['/runtime/sdl-canvas'], content: '' },
      }));
      setExecutionPhase('idle');
      setStatus('Stopped');
      xtermRef.current?.writeln('\x1b[33mExecution stopped.\x1b[0m');
      editorRef.current?.focus();
      return;
    }

    // WASI path: the worker is actively running wasi-run — terminate and reboot.
    if (!orchestratorRef.current) return;
    stoppedRef.current = true;
    const { client } = orchestratorRef.current;
    client.terminate();
    orchestratorRef.current = null;
    setExecutionPhase('idle');
    setIsReady(false);
    setStatus('Stopped — rebooting...');
    xtermRef.current?.writeln('\x1b[33mExecution stopped.\x1b[0m');
    await doBootstrap();
    editorRef.current?.focus();
  };

  /** Shared resize-handle style for all PanelResizeHandle instances */
  const resizerStyle: React.CSSProperties = {
    width: 4,
    background: '#313244',
    cursor: 'col-resize',
    transition: 'background 0.15s',
  };
  const resizerVStyle: React.CSSProperties = { ...resizerStyle, width: '100%', height: 4, cursor: 'row-resize' };

  return (
    <div className="emception-ide" style={{ display: 'flex', flexDirection: 'column', height: '100%', width: '100%', fontFamily: 'system-ui, sans-serif' }}>
      {/* Hidden log for Playwright E2E assertions — not visible to users */}
      <pre data-testid="terminal" ref={terminalLogRef} hidden aria-hidden="true" style={{ display: 'none' }} />
      {/* ── Title bar ── */}
      <header
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '0 1rem',
          height: 36,
          background: '#181825',
          borderBottom: '1px solid #313244',
          flexShrink: 0,
        }}
      >
        <h1 style={{ fontSize: '0.8rem', fontWeight: 600, color: '#cdd6f4', margin: 0, letterSpacing: '0.03em' }}>{title}</h1>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <span data-testid="status" style={{ fontSize: '0.72rem', color: '#a6adc8' }}>
            {status}
          </span>
          {executionPhase === 'running' ? (
            <button
              data-testid="stop-button"
              onClick={handleStop}
              style={{
                height: 24,
                padding: '0 0.75rem',
                fontSize: '0.8rem',
                fontWeight: 500,
                borderRadius: 4,
                border: 'none',
                cursor: 'pointer',
                background: '#f38ba8',
                color: '#11111b',
              }}
            >
              &#9632; Stop
            </button>
          ) : (
            <button
              data-testid="compile-button"
              onClick={handleCompile}
              disabled={executionPhase !== 'idle' || !isReady || activeFile?.type !== 'text'}
              style={{
                height: 24,
                padding: '0 0.75rem',
                fontSize: '0.8rem',
                fontWeight: 500,
                borderRadius: 4,
                border: 'none',
                cursor: executionPhase === 'idle' && isReady && activeFile?.type === 'text' ? 'pointer' : 'not-allowed',
                background: executionPhase === 'idle' && isReady && activeFile?.type === 'text' ? '#a6e3a1' : '#313244',
                color: executionPhase === 'idle' && isReady && activeFile?.type === 'text' ? '#11111b' : '#585b70',
              }}
            >
              &#9654;
            </button>
          )}
          <button
            onClick={resetWorkspace}
            style={{
              height: 24,
              padding: '0 0.6rem',
              fontSize: '0.8rem',
              borderRadius: 4,
              border: '1px solid #45475a',
              cursor: 'pointer',
              background: 'transparent',
              color: '#a6adc8',
            }}
          >
            Reset
          </button>
        </div>
      </header>

      {/* ── Main body: sidebar | editor + terminal ── */}
      <PanelGroup direction="horizontal" style={{ flex: 1, overflow: 'hidden' }}>
        {/* Sidebar */}
        <Panel defaultSize={18} minSize={10} maxSize={40} style={{ overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
          <FileExplorer
            files={files}
            selectedPath={selectedPath}
            expandedDirs={expandedDirs}
            fileTree={fileTree}
            onSelectPath={setSelectedPath}
            onToggleDir={(path) =>
              setExpandedDirs((prev) => {
                const next = new Set(prev);
                if (next.has(path)) next.delete(path);
                else next.add(path);
                return next;
              })
            }
            onOpenTab={(path) => ensureOpenTab(path, 'main')}
            onCreateFile={createFile}
            onRename={renameSelectedFile}
            onDelete={deleteSelectedFile}
          />
        </Panel>

        <PanelResizeHandle style={resizerStyle} />

        {/* Editor + terminal column */}
        <Panel style={{ display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
          <PanelGroup direction="vertical" style={{ flex: 1, overflow: 'hidden' }}>
            {/* Editor row: main (+ optional right panel) */}
            <Panel style={{ display: 'flex', overflow: 'hidden' }}>
              <PanelGroup direction="horizontal" style={{ flex: 1, overflow: 'hidden' }}>
                <Panel minSize={20} style={{ overflow: 'hidden' }}>
                  <DockGroupPanel
                    group="main"
                    tabs={groupTabs('main')}
                    activeTabId={activeTabId}
                    files={files}
                    canvasRef={canvasRef}
                    onSetActiveTab={setActiveTabId}
                    onCloseTab={closeTab}
                    onMoveTab={moveTabToGroup}
                    onEditorMount={handleEditorDidMount}
                    onEditorChange={handleEditorChange}
                  />
                </Panel>

                {hasRightGroup && (
                  <>
                    <PanelResizeHandle style={resizerStyle} />
                    <Panel defaultSize={35} minSize={15} style={{ overflow: 'hidden' }}>
                      <DockGroupPanel
                        group="right"
                        tabs={groupTabs('right')}
                        activeTabId={activeTabId}
                        files={files}
                        canvasRef={canvasRef}
                        onSetActiveTab={setActiveTabId}
                        onCloseTab={closeTab}
                        onMoveTab={moveTabToGroup}
                        onEditorMount={handleEditorDidMount}
                        onEditorChange={handleEditorChange}
                      />
                    </Panel>
                  </>
                )}
              </PanelGroup>
            </Panel>

            {hasBottomGroup && (
              <>
                <PanelResizeHandle style={resizerVStyle} />
                <Panel defaultSize={25} minSize={10} style={{ overflow: 'hidden' }}>
                  <DockGroupPanel
                    group="bottom"
                    tabs={groupTabs('bottom')}
                    activeTabId={activeTabId}
                    files={files}
                    canvasRef={canvasRef}
                    onSetActiveTab={setActiveTabId}
                    onCloseTab={closeTab}
                    onMoveTab={moveTabToGroup}
                    onEditorMount={handleEditorDidMount}
                    onEditorChange={handleEditorChange}
                  />
                </Panel>
              </>
            )}

            {/* Terminal */}
            <PanelResizeHandle style={resizerVStyle} />
            <Panel defaultSize={28} minSize={8} style={{ overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
              <TerminalPanel
                terminalTabs={terminalTabs}
                activeTerminalId={activeTerminalId}
                onSetActiveTerminal={setActiveTerminalId}
                onNewTerminal={createTerminalTab}
                onCloseTerminal={closeTerminalTab}
                onBootTerminalReady={handleBootTerminalReady}
              />
            </Panel>
          </PanelGroup>
        </Panel>
      </PanelGroup>

      {/* ── Status bar ── */}
      <div
        style={{
          height: 22,
          background: '#89b4fa',
          display: 'flex',
          alignItems: 'center',
          padding: '0 0.75rem',
          gap: '1rem',
          flexShrink: 0,
          fontSize: '0.7rem',
          color: '#11111b',
        }}
      >
        <span>{isReady ? '✓ Ready' : '⟳ Booting...'}</span>
        {activeFile && <span>{activeFile.path}</span>}
        {activeFile?.type === 'text' && <span style={{ marginLeft: 'auto' }}>{inferLanguage(activeFileName).toUpperCase()}</span>}
      </div>
    </div>
  );
}
