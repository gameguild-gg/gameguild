import type { OnMount } from '@monaco-editor/react';
import { Terminal } from '@xterm/xterm';
import { bootInWorker, type WorkerBootResult } from 'emception';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels';
import DockGroupPanel from './DockGroup';
import FileExplorer from './FileExplorer';
import type { DockGroup, OpenTab, TabType, TerminalTab, WorkspaceConfig, WorkspaceFile } from './ide-types';
import { DEFAULT_IMAGE, WORKSPACE_STORAGE_KEY, parseWorkspaceBundle, resolveArgs, workspaceConfigToState } from './ide-types';
import { buildFileTree, inferLanguage, isSourceFile, isTextFile, makeWasiStubs, toWorkspaceFsPath } from './ide-utils';
import TerminalPanel from './TerminalPanel';
import { DEFAULT_PRESET, PRESETS, PRESET_IDS } from './workspace-presets';

/** Creates a line-buffered stdin reader from the tty. Shared by WASI, CMake, and Python paths. */
function makeLineBufferedStdin(tty: { readByteExclusive: () => number | Promise<number> | null }): () => Promise<number> {
  const lineQueue: number[] = [];
  let lineBuf = '';
  let lineCursor = 0;
  return async (): Promise<number> => {
    if (lineQueue.length > 0) return lineQueue.shift()!;
    while (true) {
      const raw = tty.readByteExclusive();
      if (raw === null) {
        await new Promise<void>((resolve) => setTimeout(resolve, 8));
        continue;
      }
      const byte: number = typeof (raw as Promise<number>).then === 'function' ? await (raw as Promise<number>) : (raw as number);
      if (byte === -1 || byte === 0) {
        await new Promise<void>((resolve) => setTimeout(resolve, 8));
        continue;
      }
      if (byte === 127 || byte === 8) {
        if (lineCursor > 0) {
          lineBuf = lineBuf.slice(0, lineCursor - 1) + lineBuf.slice(lineCursor);
          lineCursor--;
        }
        continue;
      }
      if (byte === 13 || byte === 10) {
        // Ignore empty/whitespace-only CR/LF lines to avoid delivering stale
        // terminal newlines as immediate blank stdin records.
        if (lineBuf.trim().length === 0) {
          lineBuf = '';
          lineCursor = 0;
          continue;
        }
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
}

export interface IdeProps {
  title?: string;
  manifestUrl?: string;
  workspaceConfig?: WorkspaceConfig;
  workspaceUrl?: string;
}

export default function Ide({ title = 'Emception', manifestUrl = '/cdn/manifest.json', workspaceConfig, workspaceUrl }: IdeProps) {
  const editorRef = useRef<Parameters<OnMount>[0] | null>(null);
  const monacoRef = useRef<Parameters<OnMount>[1] | null>(null);
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

  // Resolve the active workspace config: prop > fetched bundle > default preset
  const [activePresetId, setActivePresetId] = useState<string>(workspaceConfig?.id ?? DEFAULT_PRESET.id);
  const [fetchedConfig, setFetchedConfig] = useState<WorkspaceConfig | null>(null);
  const resolvedConfig = workspaceConfig ?? fetchedConfig ?? PRESETS[activePresetId] ?? DEFAULT_PRESET;
  const initialState = workspaceConfigToState(resolvedConfig);

  const [files, setFiles] = useState<Record<string, WorkspaceFile>>(initialState.files);
  const [selectedPath, setSelectedPath] = useState(resolvedConfig.layout.activeFile);
  const [expandedDirs, setExpandedDirs] = useState<Set<string>>(initialState.expandedDirs);
  const [openTabs, setOpenTabs] = useState<OpenTab[]>(initialState.openTabs);
  const [activeTabId, setActiveTabId] = useState(initialState.activeTabId);

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

  // ── Fetch workspace from URL ──────────────────────────────────
  useEffect(() => {
    if (!workspaceUrl) return;
    let cancelled = false;
    (async () => {
      try {
        const resp = await fetch(workspaceUrl);
        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
        const text = await resp.text();
        const config = parseWorkspaceBundle(text);
        if (cancelled) return;
        setFetchedConfig(config);
        const state = workspaceConfigToState(config);
        setFiles(state.files);
        setOpenTabs(state.openTabs);
        setActiveTabId(state.activeTabId);
        setExpandedDirs(state.expandedDirs);
        setSelectedPath(config.layout.activeFile);
      } catch (e) {
        console.error('[Emception:IDE] Failed to fetch workspace bundle:', e);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [workspaceUrl]);

  // ── Switch workspace preset ───────────────────────────────────
  const switchWorkspace = useCallback((presetId: string) => {
    const preset = PRESETS[presetId];
    if (!preset) return;
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
    sdlScriptRef.current?.remove();
    sdlScriptRef.current = null;
    sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
    sdlBlobUrlsRef.current = [];
    setExecutionPhase('idle');

    setActivePresetId(presetId);
    const state = workspaceConfigToState(preset);
    setFiles(state.files);
    setOpenTabs(state.openTabs);
    setActiveTabId(state.activeTabId);
    setExpandedDirs(state.expandedDirs);
    setSelectedPath(preset.layout.activeFile);

    // Dispose stale Monaco models and pre-create models for all new preset files
    // so that getModels() reflects the new workspace immediately.
    const mc = monacoRef.current;
    if (mc) {
      mc.editor.getModels().forEach((m: { dispose: () => void }) => m.dispose());
      for (const [path, file] of Object.entries(state.files)) {
        if (file.type !== 'text') continue;
        const uri = mc.Uri.parse(`file://${path}`);
        if (!mc.editor.getModel(uri)) {
          mc.editor.createModel(file.content, inferLanguage(path), uri);
        }
      }
    }

    if (orchestratorRef.current) {
      orchestratorRef.current.tty.clear();
      orchestratorRef.current.tty.writeLine(`\x1b[32mSwitched to workspace: ${preset.label}\x1b[0m`);
    }
  }, []);

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
        // Mirror ALL xterm output to a hidden DOM element for Playwright E2E tests.
        // Must be patched BEFORE bootInWorker so the MiniShell banner (sent by the
        // Worker before the 'booted' reply) is also captured in the log.
        // eslint-disable-next-line no-control-regex
        const stripAnsi = (s: string) => s.replace(/\u001b\[[\d;]*m/g, '');
        const log = terminalLogRef;
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const xtermAny = xterm as any;
        if (!xtermAny.__emceptionLogPatched) {
          const origXtermWriteln = xtermAny.writeln.bind(xterm);
          xtermAny.writeln = (data: string | Uint8Array, callback?: () => void) => {
            origXtermWriteln(data, callback);
            if (log.current && typeof data === 'string') log.current.textContent += stripAnsi(data) + '\n';
          };
          const origXtermWrite = xtermAny.write.bind(xterm);
          xtermAny.write = (data: string | Uint8Array, callback?: () => void) => {
            origXtermWrite(data, callback);
            if (log.current && typeof data === 'string') log.current.textContent += stripAnsi(data);
          };
          xtermAny.__emceptionLogPatched = true;
        }

        const result = await bootInWorker(manifestUrl, xterm);
        if (!isMounted()) {
          result.client.terminate();
          return;
        }
        // Patch tty.clear to also clear the mirror log so each compile run starts fresh.
        const origClear = result.tty.clear.bind(result.tty);
        result.tty.clear = () => {
          origClear();
          if (log.current) log.current.textContent = '';
        };
        orchestratorRef.current = result;
        // Expose worker client on window for E2E / debug access
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (window as any).__emception_client__ = result.client;
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
      orchestratorRef.current?.client.terminate();
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
    const state = workspaceConfigToState(resolvedConfig);
    setFiles(state.files);
    setSelectedPath(resolvedConfig.layout.activeFile);
    setExpandedDirs(state.expandedDirs);
    setOpenTabs(state.openTabs);
    setActiveTabId(state.activeTabId);
    setTerminalTabs([{ id: 'terminal-1', title: 'bash' }]);
    setActiveTerminalId('terminal-1');
    if (orchestratorRef.current) {
      orchestratorRef.current.tty.clear();
      orchestratorRef.current.tty.writeLine('\x1b[32mWorkspace reset.\x1b[0m');
    } else {
      xtermRef.current?.clear();
      xtermRef.current?.writeln('\x1b[32mWorkspace reset.\x1b[0m');
    }
  }, [resolvedConfig]);

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
    monacoRef.current = monaco;
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

    // Determine which source file to compile/run
    const entryPoint = resolvedConfig.compile.sourceDetect?.entryPoint;
    const compileTarget = isSourceFile(activeFile.path)
      ? activeFile.path
      : entryPoint && files[entryPoint]
        ? entryPoint
        : textFiles.find((f) => isSourceFile(f.path))?.path;

    try {
      // ── Sync all text files to VFS ──────────────────────────────
      tty.clear();
      console.log(`${P} COMPILE & RUN START`);
      const enc = new TextEncoder();
      for (const file of textFiles) {
        const fsPath = toWorkspaceFsPath(file.path);
        await client.writeFile(fsPath, enc.encode(file.content));
        console.log(`${P} Synced ${file.path} -> ${fsPath}`);
      }

      const t0 = performance.now();
      const runType = resolvedConfig.run.type;

      // ── Python script path ──────────────────────────────────────
      if (runType === 'python-script') {
        const pyFile = compileTarget ?? entryPoint ?? '/src/main.py';
        const fsPath = toWorkspaceFsPath(pyFile);
        const args = resolvedConfig.run.args ? resolveArgs(resolvedConfig.run.args, fsPath) : ['python3', fsPath];
        setStatus('Running Python...');
        tty.writeLine(`\x1b[36mRunning ${pyFile}...\x1b[0m`);
        setExecutionPhase('running');
        const lineBufferedStdin = makeLineBufferedStdin(tty);
        await client.run(args[0], args, {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
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
        return;
      }

      // ── CMake build path ────────────────────────────────────────
      if (runType === 'cmake-build') {
        if (!compileTarget && !entryPoint) {
          setExecutionPhase('idle');
          setStatus('No source file found');
          tty.writeError('No source file found in workspace.');
          return;
        }
        setStatus('CMake configure...');
        tty.writeLine('\x1b[36mCMake configure...\x1b[0m');
        const configArgs = resolvedConfig.compile.args;
        const configResult = await client.run(configArgs[0], configArgs, {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
          onStdout: (t: string) => {
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            tty.writeError(t);
          },
        });
        if (configResult.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus('CMake configure failed');
          tty.writeLine(`\x1b[31mCMake configure failed (exit ${configResult.exitCode})\x1b[0m`);
          return;
        }
        setStatus('Ninja build...');
        tty.writeLine('\x1b[36mNinja build...\x1b[0m');
        const buildDir = configArgs.includes('-B') ? configArgs[configArgs.indexOf('-B') + 1] : '/home/user/build';
        const ninjaResult = await client.run('ninja', ['ninja', '-C', buildDir], {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
          onStdout: (t: string) => {
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            tty.writeError(t);
          },
        });
        const duration = ((performance.now() - t0) / 1000).toFixed(2);
        if (ninjaResult.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus(`Build failed (${duration}s)`);
          tty.writeLine(`\x1b[31mNinja build failed (exit ${ninjaResult.exitCode})\x1b[0m`);
          return;
        }
        tty.writeLine(`\x1b[32mBuild successful in ${duration}s\x1b[0m`);
        tty.writeLine('Running...');
        const runArgs = resolvedConfig.run.args ?? ['wasi-run', resolvedConfig.compile.output];
        setExecutionPhase('running');
        const lineBufferedStdin = makeLineBufferedStdin(tty);
        await client.run(runArgs[0], runArgs, {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
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
        return;
      }

      // ── SDL3 canvas path ────────────────────────────────────────
      if (runType === 'sdl3-canvas') {
        if (!compileTarget) {
          setExecutionPhase('idle');
          setStatus('No compilable source file found');
          tty.writeError('No .c/.cpp source file found in workspace.');
          return;
        }
        setStatus('Compiling...');
        tty.writeLine(`Compiling ${compileTarget}...`);
        tty.writeLine('\x1b[36mSDL3 detected \u2014 compiling object...\x1b[0m');

        const sourceFsPath = toWorkspaceFsPath(compileTarget);
        const sdlObjPath = '/tmp/emception-sdl-main.o';
        const wasmPath = resolvedConfig.compile.output || '/home/user/main.wasm';

        // Compile with clang directly (no emcc phase) to avoid emcc subprocess
        // execution issues and standalone archive auto-selection.
        const sdlCompile = await client.run(
          'clang',
          [
            'clang',
            '--target=wasm32-unknown-emscripten',
            '--sysroot=/usr/lib/emscripten/cache/sysroot',
            '-Xclang',
            '-iwithsysroot/include/fakesdl',
            '-Xclang',
            '-iwithsysroot/include/compat',
            '-I/usr/include',
            '-O1',
            '-c',
            sourceFsPath,
            '-o',
            sdlObjPath,
          ],
          {
            cwd: resolvedConfig.compile.cwd ?? '/home/user',
            onStdout: (t: string) => {
              console.log(t);
              tty.writeLine(t);
            },
            onStderr: (t: string) => {
              console.error(t);
              tty.writeError(t);
            },
          },
        );

        const sdlDuration = ((performance.now() - t0) / 1000).toFixed(2);
        if (sdlCompile.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus(`SDL3 compilation failed (${sdlDuration}s)`);
          tty.writeLine(`\x1b[31mSDL3 compile step failed (exit ${sdlCompile.exitCode})\x1b[0m`);
          return;
        }

        tty.writeLine('\x1b[36mSDL3 linking (wasm-ld)...\x1b[0m');

        const sdlLink = await client.run(
          'wasm-ld',
          [
            'wasm-ld',
            sdlObjPath,
            '-o',
            wasmPath,
            '-L/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten',
            '-L/usr/lib/emscripten/src/lib',
            '/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten/crt1.o',
            '/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten/libSDL3.a',
            '--no-entry',
            '--import-undefined',
            '--allow-undefined',
            '--export-if-defined=SDL_AppInit',
            '--export-if-defined=SDL_AppIterate',
            '--export-if-defined=SDL_AppEvent',
            '--export-if-defined=SDL_AppQuit',
            '--export-table',
            '--table-base=1',
            '-z',
            'stack-size=65536',
            '-lGL-getprocaddr',
            '-lal',
            '-lhtml5',
            '-lstubs',
            '-lc',
            '-ldlmalloc',
            '-lcompiler_rt',
            '-lc++-noexcept',
            '-lc++abi-noexcept',
            '-lsockets',
          ],
          {
            cwd: resolvedConfig.compile.cwd ?? '/home/user',
            onStdout: (t: string) => {
              console.log(t);
              tty.writeLine(t);
            },
            onStderr: (t: string) => {
              console.error(t);
              tty.writeError(t);
            },
          },
        );

        if (sdlLink.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus(`SDL3 compilation failed (${sdlDuration}s)`);
          tty.writeLine(`\x1b[31mSDL3 link step failed (exit ${sdlLink.exitCode})\x1b[0m`);
          return;
        }

        tty.writeLine(`\x1b[32mSDL3 compiled in ${sdlDuration}s — loading...\x1b[0m`);

        // Read the compiled WASM binary from the VFS.
        const wasmBytes = await client.getFile(wasmPath);
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
        setActiveTabId(`tab:${compileTarget}`);

        // Wait for React to flush + browser to paint so canvasRef.current is ready
        await new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));

        const canvas = canvasRef.current;
        if (!canvas) {
          setExecutionPhase('idle');
          tty.writeError('SDL canvas element not found — open the SDL Canvas tab first');
          return;
        }

        // Initialize to the SDL demo's expected render size so the runtime
        // attaches to a correctly sized target immediately.
        canvas.width = 800;
        canvas.height = 600;

        // Revoke previous SDL blob URLs
        sdlScriptRef.current?.remove();
        sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
        sdlBlobUrlsRef.current = [];

        // Patch sdl3-runtime.mjs before creating the blob URL.
        let runtimeText = new TextDecoder().decode(runtimeBytes instanceof Uint8Array ? runtimeBytes : new Uint8Array(runtimeBytes as ArrayBuffer));
        const emAsmFallback = (varName: string) =>
          `if(!ASM_CONSTS[${varName}]){var _s=UTF8ToString(${varName});` + `ASM_CONSTS[${varName}]=eval("(function($0,$1,$2,$3,$4,$5,$6,$7,$8,$9){"+_s+"})");}`;
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

        const wasmMemory: WebAssembly.Memory | null = null;
        const wasiStubs = makeWasiStubs(
          () => wasmMemory,
          (s: string) => tty.writeLine(s),
        );

        let sdlLoadOk = true;
        let sdlCallbackFns: { init?: (appstate: number, argc: number, argv: number) => number; iterate?: (appstate: number) => number } | null = null;
        const moduleTimeout = new Promise<never>((_, reject) => setTimeout(() => reject(new Error('SDL3 module load timeout (30s)')), 30_000));
        const sdlMod = await Promise.race([
          createSDL3Module({
            canvas: canvas,
            keyboardListeningElement: canvas,
            wasmBinary: wasmBytes,
            locateFile: (filename: string) => filename,
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            instantiateWasm(info: any, receiveInstance: (inst: WebAssembly.Instance) => void) {
              const env = {
                ...info.env,
                emscripten_notify_memory_growth: () => { },
              };
              const imports = { ...info, env, wasi_snapshot_preview1: wasiStubs };
              tty.writeLine('\x1b[90mSDL3: instantiating WASM…\x1b[0m');
              WebAssembly.instantiate(new Uint8Array(wasmBytes as unknown as ArrayBuffer), imports)
                .then((result) => {
                  tty.writeLine('\x1b[90mSDL3: WASM ok, patching exports…\x1b[0m');
                  const origExports = result.instance.exports;
                  // Capture callback exports directly from raw WASM exports so
                  // we can drive callback-only SDL apps even when glue doesn't
                  // surface these as Module methods.
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  const raw = origExports as any;
                  sdlCallbackFns = {
                    init: typeof raw.SDL_AppInit === 'function' ? raw.SDL_AppInit.bind(raw) : undefined,
                    iterate: typeof raw.SDL_AppIterate === 'function' ? raw.SDL_AppIterate.bind(raw) : undefined,
                  };
                  const patchedExports =
                    typeof origExports['__wasm_call_ctors'] === 'function' && typeof (origExports as Record<string, unknown>)['main'] === 'function'
                      ? origExports
                      : new Proxy(origExports, {
                        get(target, prop) {
                          if (prop === '__wasm_call_ctors' && !(prop in target)) return () => { };
                          if ((prop === 'main' || prop === '_main') && !(prop in target)) {
                            const noOpMain = () => 0;
                            // eslint-disable-next-line @typescript-eslint/no-explicit-any
                            (noOpMain as any).__emceptionNoop = true;
                            return noOpMain;
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

        // Some SDL callback builds linked with --no-entry need an explicit kick
        // from JS to start their lifecycle callbacks/main loop.
        try {
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          const sdlAny = sdlMod as any;
          let started = false;
          const callbackFns = sdlCallbackFns as {
            init?: (appstate: number, argc: number, argv: number) => number;
            iterate?: (appstate: number) => number;
          } | null;
          const wasmFns = sdlAny?.asm ?? sdlAny?.wasmExports ?? {};
          const appInit = callbackFns?.init ?? sdlAny?._SDL_AppInit ?? sdlAny?.SDL_AppInit ?? wasmFns?._SDL_AppInit ?? wasmFns?.SDL_AppInit;
          const appIterate = callbackFns?.iterate ?? sdlAny?._SDL_AppIterate ?? sdlAny?.SDL_AppIterate ?? wasmFns?._SDL_AppIterate ?? wasmFns?.SDL_AppIterate;

          // Prefer explicit callback lifecycle startup when exported.
          if (typeof appInit === 'function' && typeof appIterate === 'function') {
            tty.writeLine('\x1b[90mSDL3: starting callback lifecycle loop…\x1b[0m');
            const initResult = appInit(0, 0, 0);
            if (initResult !== 0) {
              tty.writeLine(`\x1b[33mSDL3: AppInit returned non-continue (${initResult})\x1b[0m`);
            }
            const step = () => {
              if (!sdlModuleRef.current) return;
              try {
                const iterateResult = appIterate(0);
                // SDL_APP_CONTINUE == 0
                if (iterateResult !== 0) {
                  tty.writeLine(`\x1b[90mSDL3: iterate requested stop (${iterateResult})\x1b[0m`);
                  return;
                }
              } catch (err: unknown) {
                const msg = err instanceof Error ? err.message : String(err);
                if (msg !== 'unwind') tty.writeError(`SDL3 iterate error: ${msg}`);
                return;
              }
              requestAnimationFrame(step);
            };
            requestAnimationFrame(step);
            started = true;
          } else if (typeof sdlAny?.callMain === 'function') {
            tty.writeLine('\x1b[90mSDL3: calling main entry…\x1b[0m');
            sdlAny.callMain([]);
            started = true;
          } else if (typeof sdlAny?._main === 'function' && !sdlAny._main.__emceptionNoop) {
            tty.writeLine('\x1b[90mSDL3: invoking _main…\x1b[0m');
            sdlAny._main(0, 0);
            started = true;
          } else if (typeof sdlAny?._SDL_main === 'function') {
            tty.writeLine('\x1b[90mSDL3: invoking _SDL_main…\x1b[0m');
            sdlAny._SDL_main(0, 0);
            started = true;
          }

          if (!started) tty.writeLine('\x1b[33mSDL3 warning: no runnable entry/callback exports found.\x1b[0m');
        } catch (e: unknown) {
          const msg = e instanceof Error ? e.message : String(e);
          // Emscripten may throw "unwind" to enter async main loop; ignore that.
          if (msg !== 'unwind') {
            tty.writeError(`SDL3 entry invocation error: ${msg}`);
          }
        }

        sdlModuleRef.current = sdlMod as { pauseMainLoop?: () => void } | null;
        setExecutionPhase('running');
        setStatus(`SDL3 done (${((performance.now() - tTotal) / 1000).toFixed(1)}s) — running`);
        tty.writeLine('\x1b[32mSDL3 rendering in canvas tab →\x1b[0m');
        return;
      }

      // ── Standard WASI terminal path ─────────────────────────────
      if (!compileTarget) {
        setExecutionPhase('idle');
        setStatus('No compilable source file found');
        tty.writeError('No .c/.cpp source file found in workspace.');
        return;
      }
      setStatus('Compiling...');
      tty.writeLine(`Compiling ${compileTarget}...`);
      const compileArgs =
        resolvedConfig.compile.args.length > 0
          ? resolveArgs(resolvedConfig.compile.args, toWorkspaceFsPath(compileTarget))
          : ['emcc', toWorkspaceFsPath(compileTarget), '-o', '/home/user/main.wasm', '-O2'];
      const result = await client.run(compileArgs[0], compileArgs, {
        cwd: resolvedConfig.compile.cwd ?? '/home/user',
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
      setStatus('Compilation successful');
      tty.writeLine(`\x1b[32mCompilation successful in ${duration}s\x1b[0m`);
      const wasmPath = resolvedConfig.compile.output || '/home/user/main.wasm';
      let wasmBytes = await client.getFile(wasmPath);

      // emcc may return before all subprocess-linked outputs are flushed to VFS.
      // Give the canonical artifact a short grace window before triggering fallback.
      if ((!wasmBytes || wasmBytes.length === 0) && resolvedConfig.run.type === 'wasi-terminal') {
        const waitUntil = performance.now() + 12_000;
        while ((!wasmBytes || wasmBytes.length === 0) && performance.now() < waitUntil) {
          await new Promise<void>((resolve) => setTimeout(resolve, 120));
          wasmBytes = await client.getFile(wasmPath);
        }
      }

      if ((!wasmBytes || wasmBytes.length === 0) && resolvedConfig.run.type === 'wasi-terminal') {
        tty.writeLine('\x1b[33mOutput artifact missing after emcc; attempting fallback link pipeline...\x1b[0m');
        const fallbackObj = '/tmp/emception-fallback-main.o';
        const sourceFsPath = toWorkspaceFsPath(compileTarget);

        const clangFallback = await client.run(
          'clang',
          [
            'clang',
            '--target=wasm32-unknown-emscripten',
            '--sysroot=/usr/lib/emscripten/cache/sysroot',
            '-Xclang',
            '-iwithsysroot/include/fakesdl',
            '-Xclang',
            '-iwithsysroot/include/compat',
            '-O2',
            '-c',
            sourceFsPath,
            '-o',
            fallbackObj,
          ],
          {
            cwd: resolvedConfig.compile.cwd ?? '/home/user',
            onStdout: (t: string) => tty.writeLine(t),
            onStderr: (t: string) => tty.writeError(t),
          },
        );

        if (clangFallback.exitCode === 0) {
          const crtPath = '/usr/lib/emscripten/cache-lib/wasm32-emscripten/crt1.o';
          const crtBytes = await client.getFile(crtPath);
          const magic = crtBytes
            ? Array.from((crtBytes as Uint8Array).slice(0, 8))
              .map((b: number) => b.toString(16).padStart(2, '0'))
              .join('')
            : 'none';
          tty.writeLine(`crt1.o probe: bytes=${crtBytes?.length ?? 0}, head=${magic}`);

          const lldFallback = await client.run(
            'wasm-ld',
            [
              'wasm-ld',
              fallbackObj,
              '-o',
              wasmPath,
              '-L/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten',
              '-L/usr/lib/emscripten/src/lib',
              '/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten/crt1.o',
              '--entry=main',
              '--import-undefined',
              '--allow-undefined',
              '--export-table',
              '--table-base=1',
              '--global-base=1024',
              '-z',
              'stack-size=65536',
              '-lGL-getprocaddr',
              '-lal',
              '-lhtml5',
              '-lstubs',
              '-lc',
              '-ldlmalloc',
              '-lcompiler_rt',
              '-lc++-noexcept',
              '-lc++abi-noexcept',
              '-lsockets',
            ],
            {
              cwd: resolvedConfig.compile.cwd ?? '/home/user',
              onStdout: (t: string) => tty.writeLine(t),
              onStderr: (t: string) => tty.writeError(t),
            },
          );

          if (lldFallback.exitCode !== 0) {
            tty.writeLine('\x1b[31mFallback linker step failed.\x1b[0m');
          }
        } else {
          tty.writeLine('\x1b[31mFallback compile step failed.\x1b[0m');
        }

        wasmBytes = await client.getFile(wasmPath);
      }
      console.log(`${P} Compilation output: main.wasm=${wasmBytes?.length ?? 0}`);
      tty.writeLine('Running...');
      const lineBufferedStdin = makeLineBufferedStdin(tty);
      const runArgs = resolvedConfig.run.args ?? ['wasi-run', resolvedConfig.compile.output || '/home/user/main.wasm'];
      setExecutionPhase('running');
      await client.run(runArgs[0], runArgs, {
        cwd: resolvedConfig.compile.cwd ?? '/home/user',
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
      console.log(`${P} COMPILE & RUN COMPLETE`);
      restoreEditorFocus();
    }
  };

  const handleTest = async () => {
    const testConfig = resolvedConfig.test;
    if (!orchestratorRef.current || !testConfig) return;
    stoppedRef.current = false;
    setExecutionPhase('compiling');
    setActiveTerminalId('terminal-1');
    const restoreEditorFocus = () => {
      editorRef.current?.focus();
    };
    const { client, tty } = orchestratorRef.current;
    const tTotal = performance.now();
    try {
      tty.clear();
      tty.writeLine('\x1b[36mRunning tests...\x1b[0m');

      // Sync files to VFS
      const textFiles = Object.values(files).filter((f) => f.type === 'text' && isTextFile(f.path));
      const enc = new TextEncoder();
      for (const file of textFiles) {
        const fsPath = toWorkspaceFsPath(file.path);
        await client.writeFile(fsPath, enc.encode(file.content));
      }

      // Compile test if needed
      if (testConfig.compileArgs && testConfig.compileArgs.length > 0) {
        setStatus('Compiling tests...');
        const compileResult = await client.run(testConfig.tool, testConfig.compileArgs, {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
          onStdout: (t: string) => {
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            tty.writeError(t);
          },
        });
        if (compileResult.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus('Test compilation failed');
          tty.writeLine(`\x1b[31mTest compilation failed (exit ${compileResult.exitCode})\x1b[0m`);
          return;
        }
      }

      // Run tests
      setStatus('Running tests...');
      setExecutionPhase('running');
      const lineBufferedStdin = makeLineBufferedStdin(tty);
      const runResult = await client.run(testConfig.runArgs[0], testConfig.runArgs, {
        cwd: resolvedConfig.compile.cwd ?? '/home/user',
        onStdout: (t: string) => {
          tty.write(t.replace(/\n/g, '\r\n'));
        },
        onStderr: (t: string) => {
          tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`);
        },
        stdin: lineBufferedStdin,
      });

      setExecutionPhase('idle');
      const duration = ((performance.now() - tTotal) / 1000).toFixed(1);
      if (runResult.exitCode === 0) {
        setStatus(`Tests passed (${duration}s)`);
        tty.writeLine(`\x1b[32m✓ All tests passed (${duration}s)\x1b[0m`);
      } else {
        setStatus(`Tests failed (${duration}s)`);
        tty.writeLine(`\x1b[31m✗ Tests failed (exit ${runResult.exitCode}, ${duration}s)\x1b[0m`);
      }
    } catch (e) {
      setExecutionPhase('idle');
      if (!stoppedRef.current) {
        setStatus('Test error');
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
          {/* Workspace preset picker */}
          {!workspaceConfig && !workspaceUrl && (
            <select
              data-testid="workspace-picker"
              value={activePresetId}
              onChange={(e) => switchWorkspace(e.target.value)}
              style={{
                height: 24,
                fontSize: '0.72rem',
                borderRadius: 4,
                border: '1px solid #45475a',
                background: '#1e1e2e',
                color: '#cdd6f4',
                cursor: 'pointer',
                padding: '0 0.4rem',
              }}
            >
              {PRESET_IDS.map((id) => (
                <option key={id} value={id}>
                  {PRESETS[id].label}
                </option>
              ))}
            </select>
          )}
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
              {resolvedConfig.run.type === 'python-script' ? '▶ Run' : '▶'}
            </button>
          )}
          {resolvedConfig.features.showTestButton && resolvedConfig.test && (
            <button
              data-testid="test-button"
              onClick={handleTest}
              disabled={executionPhase !== 'idle' || !isReady}
              style={{
                height: 24,
                padding: '0 0.75rem',
                fontSize: '0.8rem',
                fontWeight: 500,
                borderRadius: 4,
                border: 'none',
                cursor: executionPhase === 'idle' && isReady ? 'pointer' : 'not-allowed',
                background: executionPhase === 'idle' && isReady ? '#cba6f7' : '#313244',
                color: executionPhase === 'idle' && isReady ? '#11111b' : '#585b70',
              }}
            >
              ✓ Test
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
